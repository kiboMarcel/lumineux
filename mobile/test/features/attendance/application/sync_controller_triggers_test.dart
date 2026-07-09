import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_scan_dtos.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

Future<void> _pump() async {
  for (var i = 0; i < 5; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

PendingCapture _cap(int sessionId) => PendingCapture(
      clientOperationId: 'op-$sessionId',
      sessionId: sessionId,
      token: 'tok',
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

  test('retour de connectivité → déclenche une synchro (file non vide, FR-006)',
      () async {
    container.read(syncControllerProvider); // build → abonnement connectivité
    await container.read(offlineQueueStoreProvider).add(_cap(10));
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(clientOperationId: 'op-10', outcome: 'Created'),
        ]));

    conn.emit(true);
    await _pump();

    verify(() => api.syncBatch(10, any())).called(1);
  });

  test('file vide → aucun envoi même au retour du réseau', () async {
    container.read(syncControllerProvider);

    conn.emit(true);
    await _pump();

    verifyNever(() => api.syncBatch(any(), any()));
  });

  test('relance manuelle (syncNow) déclenche un envoi', () async {
    await container.read(offlineQueueStoreProvider).add(_cap(10));
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(clientOperationId: 'op-10', outcome: 'Created'),
        ]));

    await container.read(syncControllerProvider.notifier).syncNow();

    verify(() => api.syncBatch(10, any())).called(1);
  });
}
