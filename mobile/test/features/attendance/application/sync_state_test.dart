import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/sync_notice.dart';
import 'package:lumineux_mobile/features/attendance/application/sync_state.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';

import '../../../support/harness.dart';

PendingCapture _cap(int sessionId) => PendingCapture(
      clientOperationId: 'op-$sessionId',
      sessionId: sessionId,
      token: 't',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14),
    );

void main() {
  late FakeConnectivity conn;
  late ProviderContainer container;

  setUp(() {
    conn = FakeConnectivity();
    container = ProviderContainer(overrides: [
      connectivityFacadeProvider.overrideWithValue(conn),
      secureStorageProvider.overrideWithValue(inMemorySecureStorage()),
      clockProvider.overrideWithValue(FixedClock(DateTime.utc(2026, 7, 9, 14))),
    ]);
    addTearDown(() {
      conn.dispose();
      container.dispose();
    });
  });

  test('SyncStatus vide → isEmpty', () {
    const s = SyncStatus();
    expect(s.isEmpty, isTrue);
  });

  test('refreshStatus agrège le nombre en attente et les avis non acquittés',
      () async {
    await container.read(offlineQueueStoreProvider).add(_cap(10));
    await container.read(offlineQueueStoreProvider).add(_cap(20));
    await container.read(syncNoticeStoreProvider).add(SyncNotice(
          clientOperationId: 'op-x',
          sessionId: 30,
          kind: NoticeKind.rejected,
          reason: 'Refusée',
          occurredAt: DateTime.utc(2026, 7, 9, 14),
        ));

    await container.read(syncControllerProvider.notifier).refreshStatus();
    final status = container.read(syncControllerProvider);

    expect(status.pendingCount, 2);
    expect(status.notices, hasLength(1));
    expect(status.isEmpty, isFalse);
  });

  test('un avis acquitté n\'apparaît plus dans l\'agrégat', () async {
    await container.read(syncNoticeStoreProvider).add(SyncNotice(
          clientOperationId: 'op-x',
          sessionId: 30,
          kind: NoticeKind.rejected,
          reason: 'Refusée',
          occurredAt: DateTime.utc(2026, 7, 9, 14),
        ));
    await container.read(syncControllerProvider.notifier).refreshStatus();
    expect(container.read(syncControllerProvider).notices, hasLength(1));

    await container
        .read(syncControllerProvider.notifier)
        .acknowledgeNotice('op-x');

    expect(container.read(syncControllerProvider).notices, isEmpty);
  });
}
