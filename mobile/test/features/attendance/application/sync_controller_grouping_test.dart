import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
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

  test('captures multi-séances → un POST par séance (FR-005)', () async {
    final store = container.read(offlineQueueStoreProvider);
    await store.add(_cap('op-10', 10));
    await store.add(_cap('op-20', 20));

    when(() => api.syncBatch(any(), any())).thenAnswer((inv) async {
      final sessionId = inv.positionalArguments[0] as int;
      final items = inv.positionalArguments[1] as List<OfflineScanItem>;
      return OfflineScanBatchResponse([
        OfflineScanResult(
            clientOperationId: items.single.clientOperationId,
            outcome: 'Created',
            attendanceId: sessionId),
      ]);
    });

    await container.read(syncControllerProvider.notifier).sync();

    // Un envoi par séance, chacun avec son seul item (dédup FR-014).
    verify(() => api.syncBatch(10, any())).called(1);
    verify(() => api.syncBatch(20, any())).called(1);
    expect(await store.readAll(), isEmpty);
  });
}
