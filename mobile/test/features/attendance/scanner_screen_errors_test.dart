import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/presentation/scanner_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

Widget _app(FakeScannerFacade facade, MockAttendanceApi api) => ProviderScope(
      overrides: [
        scannerFacadeProvider.overrideWithValue(facade),
        cameraPermissionProvider.overrideWithValue(FakeCameraPermission()),
        attendanceApiProvider.overrideWithValue(api),
      ],
      child: const MaterialApp(home: Scaffold(body: ScannerScreen())),
    );

void main() {
  testWidgets('erreur serveur (410) → overlay erreur + « Scanner à nouveau »',
      (tester) async {
    final facade = FakeScannerFacade();
    final api = MockAttendanceApi();
    when(() => api.scan(123, 'tok')).thenThrow(
        const ApiException(ApiErrorType.gone, detail: 'Code QR expiré'));

    await tester.pumpWidget(_app(facade, api));
    await tester.pumpAndSettle();

    facade.emit('{"v":1,"s":123,"t":"tok"}');
    await tester.pumpAndSettle();

    expect(find.text('Code QR expiré'), findsOneWidget);
    expect(find.text('Scanner à nouveau'), findsOneWidget);
  });

  testWidgets('code non reconnu → indice transitoire, pas d\'overlay',
      (tester) async {
    final facade = FakeScannerFacade();
    final api = MockAttendanceApi();

    await tester.pumpWidget(_app(facade, api));
    await tester.pumpAndSettle();

    facade.emit('QR-étranger');
    await tester.pump(); // affiche le SnackBar (indice transitoire)

    expect(find.text('Code non reconnu'), findsOneWidget);
    expect(find.byKey(const Key('scan-result-title')), findsNothing);
    verifyNever(() => api.scan(any(), any()));
  });
}
