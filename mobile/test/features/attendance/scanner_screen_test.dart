import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/data/scan_dtos.dart';
import 'package:lumineux_mobile/features/attendance/presentation/scanner_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

AttendanceResponse _resp() => AttendanceResponse(
      id: 1,
      sessionId: 123,
      memberId: 42,
      memberFullName: 'Aline Kouadio',
      arrivalTime: DateTime.utc(2026, 7, 9, 14, 32),
      endTime: null,
      source: 'Scan',
      status: 'Valid',
      originAntennaId: 3,
    );

void main() {
  testWidgets('scan réussi → overlay succès (nom + heure) puis reprise',
      (tester) async {
    final facade = FakeScannerFacade();
    final api = MockAttendanceApi();
    when(() => api.scan(123, 'tok'))
        .thenAnswer((_) async => ScanOutcome(attendance: _resp(), created: true));

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          scannerFacadeProvider.overrideWithValue(facade),
          cameraPermissionProvider.overrideWithValue(FakeCameraPermission()),
          attendanceApiProvider.overrideWithValue(api),
        ],
        child: const MaterialApp(home: Scaffold(body: ScannerScreen())),
      ),
    );
    await tester.pumpAndSettle();

    // Caméra active : consigne de visée visible.
    expect(find.byKey(const Key('scanner-hint')), findsOneWidget);

    facade.emit('{"v":1,"s":123,"t":"tok"}');
    await tester.pumpAndSettle();

    expect(find.text('Présence enregistrée'), findsOneWidget);
    expect(find.textContaining('Aline Kouadio'), findsOneWidget);

    // Fermeture → reprise du scan (overlay disparu).
    await tester.tap(find.byKey(const Key('scan-result-dismiss')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('scan-result-title')), findsNothing);
  });
}
