import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_queue_store.dart';
import 'package:mocktail/mocktail.dart';

class MockSecureStorage extends Mock implements FlutterSecureStorage {}

/// Coffre en mémoire : simule la persistance clé→valeur pour les tests
/// lecture-modification-écriture de la file.
MockSecureStorage inMemoryStorage() {
  final storage = MockSecureStorage();
  final backing = <String, String>{};

  when(() => storage.read(
        key: any(named: 'key'),
        aOptions: any(named: 'aOptions'),
      )).thenAnswer((inv) async => backing[inv.namedArguments[#key] as String]);

  when(() => storage.write(
        key: any(named: 'key'),
        value: any(named: 'value'),
        aOptions: any(named: 'aOptions'),
      )).thenAnswer((inv) async {
    backing[inv.namedArguments[#key] as String] =
        inv.namedArguments[#value] as String;
  });

  when(() => storage.delete(
        key: any(named: 'key'),
        aOptions: any(named: 'aOptions'),
      )).thenAnswer((inv) async {
    backing.remove(inv.namedArguments[#key] as String);
  });

  return storage;
}

PendingCapture _capture(String opId, int sessionId) => PendingCapture(
      clientOperationId: opId,
      sessionId: sessionId,
      token: 'secret-$opId',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14),
    );

void main() {
  setUpAll(() => registerFallbackValue(const AndroidOptions()));

  late OfflineQueueStore store;

  setUp(() => store = OfflineQueueStore(inMemoryStorage()));

  test('file vide au départ', () async {
    expect(await store.readAll(), isEmpty);
  });

  test('add persiste la capture et readAll la relit (FR-003)', () async {
    await store.add(_capture('op-1', 10));
    final all = await store.readAll();
    expect(all, hasLength(1));
    expect(all.single.clientOperationId, 'op-1');
    expect(all.single.token, 'secret-op-1');
  });

  test('déduplication par séance : 2e capture même séance ignorée (FR-014)',
      () async {
    final added1 = await store.add(_capture('op-1', 10));
    final added2 = await store.add(_capture('op-2', 10));

    expect(added1, isTrue);
    expect(added2, isFalse); // dédupée
    final all = await store.readAll();
    expect(all, hasLength(1));
    expect(all.single.clientOperationId, 'op-1'); // l'existante est conservée
  });

  test('captures de séances différentes coexistent', () async {
    await store.add(_capture('op-1', 10));
    await store.add(_capture('op-2', 20));
    expect(await store.readAll(), hasLength(2));
    expect(await store.containsSession(10), isTrue);
    expect(await store.containsSession(99), isFalse);
  });

  test('update remplace la capture par clientOperationId', () async {
    await store.add(_capture('op-1', 10));
    await store.update(
        _capture('op-1', 10).copyWith(state: PendingState.transientFailed, attemptCount: 2));
    final all = await store.readAll();
    expect(all.single.state, PendingState.transientFailed);
    expect(all.single.attemptCount, 2);
  });

  test('remove retire la capture et purge son jeton (FR-009)', () async {
    await store.add(_capture('op-1', 10));
    await store.add(_capture('op-2', 20));
    await store.remove('op-1');
    final all = await store.readAll();
    expect(all, hasLength(1));
    expect(all.single.clientOperationId, 'op-2');
  });

  test('contenu corrompu → file vide (purge défensive)', () async {
    final storage = MockSecureStorage();
    when(() => storage.read(key: any(named: 'key'), aOptions: any(named: 'aOptions')))
        .thenAnswer((_) async => 'pas-du-json{');
    when(() => storage.delete(key: any(named: 'key'), aOptions: any(named: 'aOptions')))
        .thenAnswer((_) async {});
    final s = OfflineQueueStore(storage);
    expect(await s.readAll(), isEmpty);
  });
}
