import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/presentation/scanner_screen.dart';

import '../../support/harness.dart';

Widget _app(FakeScannerFacade facade, FakeCameraPermission perm) => ProviderScope(
      overrides: [
        scannerFacadeProvider.overrideWithValue(facade),
        cameraPermissionProvider.overrideWithValue(perm),
        attendanceApiProvider.overrideWithValue(MockAttendanceApi()),
      ],
      child: const MaterialApp(home: Scaffold(body: ScannerScreen())),
    );

void main() {
  testWidgets('permission refusée → message + « Ouvrir les réglages »',
      (tester) async {
    final perm = FakeCameraPermission(granted: false);
    await tester.pumpWidget(_app(FakeScannerFacade(), perm));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('scanner-permission-denied')), findsOneWidget);
    expect(find.byKey(const Key('scanner-open-settings')), findsOneWidget);
    expect(find.byKey(const Key('scanner-hint')), findsNothing);

    await tester.tap(find.byKey(const Key('scanner-open-settings')));
    await tester.pump();
    expect(perm.settingsOpened, isTrue);
  });

  testWidgets('permission accordée → aperçu (cadre de visée)', (tester) async {
    await tester.pumpWidget(
        _app(FakeScannerFacade(), FakeCameraPermission(granted: true)));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('scanner-permission-denied')), findsNothing);
    expect(find.byKey(const Key('scanner-hint')), findsOneWidget);
  });
}
