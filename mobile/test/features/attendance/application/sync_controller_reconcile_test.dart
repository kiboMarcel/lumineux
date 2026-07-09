import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/sync_notice.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_scan_dtos.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

PendingCapture _cap(String opId, int sessionId) => PendingCapture(
      clientOperationId: opId,
      sessionId: sessionId,
      token: 'tok-$opId',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14),
    );

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

  Future<void> seed(PendingCapture c) =>
      container.read(offlineQueueStoreProvider).add(c);
  Future<List<PendingCapture>> queue() =>
      container.read(offlineQueueStoreProvider).readAll();
  Future<List<SyncNotice>> notices() =>
      container.read(syncNoticeStoreProvider).readAll();

  test('Created → retiré de la file, aucun avis', () async {
    await seed(_cap('op-1', 10));
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(clientOperationId: 'op-1', outcome: 'Created', attendanceId: 5),
        ]));

    await container.read(syncControllerProvider.notifier).sync();

    expect(await queue(), isEmpty);
    expect(await notices(), isEmpty);
  });

  test('AlreadyPresent → retiré (succès idempotent), aucun avis', () async {
    await seed(_cap('op-1', 10));
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(clientOperationId: 'op-1', outcome: 'AlreadyPresent', attendanceId: 5),
        ]));

    await container.read(syncControllerProvider.notifier).sync();

    expect(await queue(), isEmpty);
    expect(await notices(), isEmpty);
  });

  test('Rejected → retiré + avis avec raison serveur (SC-004)', () async {
    await seed(_cap('op-1', 10));
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(
              clientOperationId: 'op-1',
              outcome: 'Rejected',
              reason: 'Jeton QR invalide au moment du scan.'),
        ]));

    await container.read(syncControllerProvider.notifier).sync();

    expect(await queue(), isEmpty);
    final n = await notices();
    expect(n, hasLength(1));
    expect(n.single.kind, NoticeKind.rejected);
    expect(n.single.reason, 'Jeton QR invalide au moment du scan.');
  });

  test('erreur réseau → conservé, attemptCount incrémenté, aucun avis (FR-007)',
      () async {
    await seed(_cap('op-1', 10));
    when(() => api.syncBatch(10, any()))
        .thenThrow(const ApiException(ApiErrorType.network));

    await container.read(syncControllerProvider.notifier).sync();

    final q = await queue();
    expect(q, hasLength(1));
    expect(q.single.attemptCount, 1);
    expect(q.single.state, PendingState.transientFailed);
    expect(await notices(), isEmpty);
  });

  test('401 → conservé sans incrément (session purgée par le socle)', () async {
    await seed(_cap('op-1', 10));
    when(() => api.syncBatch(10, any()))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));

    await container.read(syncControllerProvider.notifier).sync();

    final q = await queue();
    expect(q, hasLength(1));
    expect(q.single.attemptCount, 0);
    expect(await notices(), isEmpty);
  });

  test('404 (non transitoire) → échec définitif signalé et retiré', () async {
    await seed(_cap('op-1', 10));
    when(() => api.syncBatch(10, any())).thenThrow(
        const ApiException(ApiErrorType.notFound, detail: 'Séance introuvable.'));

    await container.read(syncControllerProvider.notifier).sync();

    expect(await queue(), isEmpty);
    final n = await notices();
    expect(n.single.kind, NoticeKind.permanentlyFailed);
  });
}
