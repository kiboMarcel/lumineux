import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/scan_state.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

void main() {
  late MockAttendanceApi api;
  late ProviderContainer container;

  setUp(() {
    api = MockAttendanceApi();
    container = ProviderContainer(overrides: [
      attendanceApiProvider.overrideWithValue(api),
      secureStorageProvider.overrideWithValue(inMemorySecureStorage()),
      clockProvider.overrideWithValue(FixedClock(DateTime.utc(2026, 7, 9, 14))),
    ]);
    addTearDown(container.dispose);
  });

  ScanState state() => container.read(scanControllerProvider);

  test('QR non reconnu hors ligne → jamais en file, indice affiché (FR-001a)',
      () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);
    // Même si le réseau échouait, le QR malformé ne doit jamais être soumis.
    when(() => api.scan(any(), any()))
        .thenThrow(const ApiException(ApiErrorType.network));

    await controller.onDetect('QR-étranger-non-json');

    expect(state().status, ScanStatus.scanning);
    expect(state().hint, 'Code non reconnu');
    verifyNever(() => api.scan(any(), any()));
    expect(await container.read(offlineQueueStoreProvider).readAll(), isEmpty);
  });

  test('QR valide en structure mais sans jeton → non reconnu, rien en file',
      () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);

    await controller.onDetect('{"v":1,"s":123}'); // pas de "t"

    expect(state().hint, 'Code non reconnu');
    expect(await container.read(offlineQueueStoreProvider).readAll(), isEmpty);
  });
}
