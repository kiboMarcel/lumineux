import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/scan_state.dart';
import 'package:lumineux_mobile/features/attendance/data/scan_dtos.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

AttendanceResponse _resp({String? name}) => AttendanceResponse(
      id: 1,
      sessionId: 123,
      memberId: 42,
      memberFullName: name,
      arrivalTime: DateTime.utc(2026, 7, 9, 14, 32),
      endTime: null,
      source: 'Scan',
      status: 'Valid',
      originAntennaId: 3,
    );

const _validQr = '{"v":1,"s":123,"t":"tok"}';

void main() {
  late MockAttendanceApi api;
  late ProviderContainer container;

  setUp(() {
    api = MockAttendanceApi();
    container = ProviderContainer(
      overrides: [attendanceApiProvider.overrideWithValue(api)],
    );
    addTearDown(container.dispose);
  });

  ScanState state() => container.read(scanControllerProvider);

  test('QR valide → 201 → résultat succès (créée) avec nom + heure', () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);
    when(() => api.scan(123, 'tok')).thenAnswer(
        (_) async => ScanOutcome(attendance: _resp(name: 'Aline'), created: true));

    await controller.onDetect(_validQr);

    expect(state().status, ScanStatus.result);
    expect(state().result!.kind, ScanResultKind.success);
    expect(state().result!.subtitle, contains('Aline'));
  });

  test('QR valide → 200 → résultat « déjà présente »', () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);
    when(() => api.scan(123, 'tok')).thenAnswer(
        (_) async => ScanOutcome(attendance: _resp(name: 'Aline'), created: false));

    await controller.onDetect(_validQr);

    expect(state().result!.kind, ScanResultKind.alreadyPresent);
  });

  test('nom absent → sous-titre = heure seule (repli)', () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);
    when(() => api.scan(123, 'tok')).thenAnswer(
        (_) async => ScanOutcome(attendance: _resp(), created: true));

    await controller.onDetect(_validQr);

    expect(state().result!.subtitle, isNot(contains('·')));
  });

  test('anti double-soumission : détection ignorée hors de l\'état scanning',
      () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);
    when(() => api.scan(123, 'tok')).thenAnswer(
        (_) async => ScanOutcome(attendance: _resp(name: 'Aline'), created: true));

    await controller.onDetect(_validQr); // → result
    // Un nouveau code capté alors qu'un résultat est affiché est ignoré.
    await controller.onDetect('{"v":1,"s":999,"t":"autre"}');

    verify(() => api.scan(123, 'tok')).called(1);
    verifyNever(() => api.scan(999, 'autre'));
  });
}
