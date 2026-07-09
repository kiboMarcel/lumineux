import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/sync_notice.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_scan_dtos.dart';
import 'package:lumineux_mobile/features/attendance/presentation/sync_status_banner.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

PendingCapture _cap(int sessionId) => PendingCapture(
      clientOperationId: 'op-$sessionId',
      sessionId: sessionId,
      token: 't',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14),
    );

SyncNotice _notice(String opId) => SyncNotice(
      clientOperationId: opId,
      sessionId: 30,
      kind: NoticeKind.rejected,
      reason: 'Jeton QR invalide au moment du scan.',
      occurredAt: DateTime.utc(2026, 7, 9, 14),
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

  Future<void> pumpBanner(WidgetTester tester) async {
    await tester.pumpWidget(UncontrolledProviderScope(
      container: container,
      child: const MaterialApp(home: Scaffold(body: SyncStatusBanner())),
    ));
    await tester.pump();
  }

  testWidgets('rien en attente, aucun avis → bandeau masqué', (tester) async {
    await container.read(syncControllerProvider.notifier).refreshStatus();
    await pumpBanner(tester);
    expect(find.byKey(const Key('sync-status-banner')), findsNothing);
  });

  testWidgets('compteur en attente + bouton Réessayer (FR-011)', (tester) async {
    await container.read(offlineQueueStoreProvider).add(_cap(10));
    await container.read(offlineQueueStoreProvider).add(_cap(20));
    await container.read(syncControllerProvider.notifier).refreshStatus();

    await pumpBanner(tester);

    expect(find.byKey(const Key('sync-status-banner')), findsOneWidget);
    expect(find.text('2 présences à synchroniser'), findsOneWidget);
    expect(find.byKey(const Key('sync-retry')), findsOneWidget);
  });

  testWidgets('avis de rejet affiché avec sa raison (SC-004)', (tester) async {
    await container.read(syncNoticeStoreProvider).add(_notice('op-r'));
    await container.read(syncControllerProvider.notifier).refreshStatus();

    await pumpBanner(tester);

    expect(find.text('Présence refusée'), findsOneWidget);
    expect(find.text('Jeton QR invalide au moment du scan.'), findsOneWidget);
    expect(find.byKey(const Key('sync-notice-ack-op-r')), findsOneWidget);
  });

  testWidgets('« J\'ai compris » acquitte l\'avis (disparaît)', (tester) async {
    await container.read(syncNoticeStoreProvider).add(_notice('op-r'));
    await container.read(syncControllerProvider.notifier).refreshStatus();
    await pumpBanner(tester);

    await tester.tap(find.byKey(const Key('sync-notice-ack-op-r')));
    await tester.pumpAndSettle();

    expect(find.text('Présence refusée'), findsNothing);
  });

  testWidgets('Réessayer déclenche une synchro', (tester) async {
    await container.read(offlineQueueStoreProvider).add(_cap(10));
    await container.read(syncControllerProvider.notifier).refreshStatus();
    when(() => api.syncBatch(10, any())).thenAnswer((_) async =>
        const OfflineScanBatchResponse([
          OfflineScanResult(clientOperationId: 'op-10', outcome: 'Created'),
        ]));

    await pumpBanner(tester);
    await tester.tap(find.byKey(const Key('sync-retry')));
    await tester.pumpAndSettle();

    verify(() => api.syncBatch(10, any())).called(1);
  });

  testWidgets('aucun jeton affiché dans le bandeau (SC-005)', (tester) async {
    await container.read(offlineQueueStoreProvider).add(_cap(10));
    await container.read(syncNoticeStoreProvider).add(_notice('op-r'));
    await container.read(syncControllerProvider.notifier).refreshStatus();

    await pumpBanner(tester);

    expect(find.textContaining('op-'), findsNothing);
    expect(find.textContaining('tok'), findsNothing);
  });
}
