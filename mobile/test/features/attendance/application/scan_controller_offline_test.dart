import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/scan_state.dart';
import 'package:lumineux_mobile/features/attendance/data/scan_dtos.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

const _validQr = '{"v":1,"s":123,"t":"tok"}';

AttendanceResponse _resp() => AttendanceResponse(
      id: 1,
      sessionId: 123,
      memberId: 42,
      memberFullName: 'Aline',
      arrivalTime: DateTime.utc(2026, 7, 9, 14, 32),
      endTime: null,
      source: 'Scan',
      status: 'Valid',
      originAntennaId: 3,
    );

void main() {
  late MockAttendanceApi api;
  late ProviderContainer container;

  setUp(() {
    api = MockAttendanceApi();
    container = ProviderContainer(overrides: [
      attendanceApiProvider.overrideWithValue(api),
      secureStorageProvider.overrideWithValue(inMemorySecureStorage()),
      clockProvider.overrideWithValue(FixedClock(DateTime.utc(2026, 7, 9, 14, 3, 12))),
    ]);
    addTearDown(container.dispose);
  });

  ScanState state() => container.read(scanControllerProvider);

  test('échec réseau → capture mise en file + résultat « hors ligne » (FR-001)',
      () async {
    container.read(scanControllerProvider.notifier).onPermissionResolved(true);
    when(() => api.scan(123, 'tok'))
        .thenThrow(const ApiException(ApiErrorType.network));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    // Résultat neutre « hors ligne », pas une erreur.
    expect(state().status, ScanStatus.result);
    expect(state().result!.kind, ScanResultKind.offlineQueued);
    expect(state().result!.isError, isFalse);

    // La capture est réellement en file avec les bons champs (FR-002).
    final queue = await container.read(offlineQueueStoreProvider).readAll();
    expect(queue, hasLength(1));
    final c = queue.single;
    expect(c.sessionId, 123);
    expect(c.token, 'tok');
    expect(c.clientArrivalTime, DateTime.utc(2026, 7, 9, 14, 3, 12));
    expect(c.clientOperationId, isNotEmpty);
    expect(c.clientOperationId.length, lessThanOrEqualTo(64));
  });

  test('succès en ligne (201) → aucune entrée en file (FR-004)', () async {
    container.read(scanControllerProvider.notifier).onPermissionResolved(true);
    when(() => api.scan(123, 'tok')).thenAnswer(
        (_) async => ScanOutcome(attendance: _resp(), created: true));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().result!.kind, ScanResultKind.success);
    expect(await container.read(offlineQueueStoreProvider).readAll(), isEmpty);
  });

  test('erreur non-réseau (410) → overlay erreur, aucune capture (FR-004)',
      () async {
    container.read(scanControllerProvider.notifier).onPermissionResolved(true);
    when(() => api.scan(123, 'tok')).thenThrow(
        const ApiException(ApiErrorType.gone, detail: 'Code QR expiré'));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().result!.isError, isTrue);
    expect(await container.read(offlineQueueStoreProvider).readAll(), isEmpty);
  });

  test('401 → retour scanning, aucune capture (FR-004)', () async {
    container.read(scanControllerProvider.notifier).onPermissionResolved(true);
    when(() => api.scan(123, 'tok'))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().status, ScanStatus.scanning);
    expect(await container.read(offlineQueueStoreProvider).readAll(), isEmpty);
  });
}
