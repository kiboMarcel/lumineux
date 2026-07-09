import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_scan_dtos.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

void main() {
  setUpAll(registerSyncFallbacks);

  late MockAttendanceApi api;
  late FakeConnectivity conn;
  late ProviderContainer container;

  setUp(() {
    api = MockAttendanceApi();
    conn = FakeConnectivity();
    container = ProviderContainer(overrides: [
      attendanceApiProvider.overrideWithValue(api),
      connectivityFacadeProvider.overrideWithValue(conn),
      secureStorageProvider.overrideWithValue(inMemorySecureStorage()),
      clockProvider.overrideWithValue(FixedClock(DateTime.utc(2026, 7, 9, 14))),
    ]);
    addTearDown(() {
      conn.dispose();
      container.dispose();
    });
  });

  test('ré-envoi d\'un élément déjà synchronisé → AlreadyPresent, aucun doublon',
      () async {
    final store = container.read(offlineQueueStoreProvider);
    await store.add(PendingCapture(
      clientOperationId: 'op-stable',
      sessionId: 10,
      token: 'tok',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14),
    ));

    // Le serveur, ayant déjà l'opération, répond AlreadyPresent (idempotence).
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(
              clientOperationId: 'op-stable',
              outcome: 'AlreadyPresent',
              attendanceId: 99),
        ]));

    await container.read(syncControllerProvider.notifier).sync();
    // Un 2e cycle ne doit rien renvoyer (file vide) et ne pas rappeler l'API.
    await container.read(syncControllerProvider.notifier).sync();

    expect(await store.readAll(), isEmpty);
    verify(() => api.syncBatch(10, any())).called(1);
  });

  test('le clientOperationId envoyé est immuable (clé d\'idempotence)', () async {
    final store = container.read(offlineQueueStoreProvider);
    await store.add(PendingCapture(
      clientOperationId: 'op-fixed',
      sessionId: 10,
      token: 'tok',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14),
    ));

    List<OfflineScanItem>? sent;
    when(() => api.syncBatch(10, any())).thenAnswer((inv) async {
      sent = inv.positionalArguments[1] as List<OfflineScanItem>;
      return const OfflineScanBatchResponse([
        OfflineScanResult(clientOperationId: 'op-fixed', outcome: 'Created'),
      ]);
    });

    await container.read(syncControllerProvider.notifier).sync();

    expect(sent!.single.clientOperationId, 'op-fixed');
  });
}
