import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/scan_state.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

const _validQr = '{"v":1,"s":123,"t":"tok"}';

void main() {
  late MockAttendanceApi api;
  late ProviderContainer container;

  setUp(() {
    api = MockAttendanceApi();
    container = ProviderContainer(
      overrides: [
        attendanceApiProvider.overrideWithValue(api),
        // Feature 027 : le chemin réseau capture hors ligne → coffre en mémoire.
        secureStorageProvider.overrideWithValue(inMemorySecureStorage()),
        clockProvider
            .overrideWithValue(FixedClock(DateTime.utc(2026, 7, 9, 14))),
      ],
    );
    addTearDown(container.dispose);
  });

  ScanState state() => container.read(scanControllerProvider);

  Future<void> scanning() async {
    container.read(scanControllerProvider.notifier).onPermissionResolved(true);
  }

  test('410 gone → overlay erreur avec message serveur', () async {
    await scanning();
    when(() => api.scan(123, 'tok')).thenThrow(
        const ApiException(ApiErrorType.gone, detail: 'Code QR expiré'));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().status, ScanStatus.result);
    expect(state().result!.isError, isTrue);
    expect(state().result!.subtitle, 'Code QR expiré');
  });

  test('409 conflict → overlay erreur', () async {
    await scanning();
    when(() => api.scan(123, 'tok')).thenThrow(const ApiException(
        ApiErrorType.conflict,
        detail: 'La réunion est terminée : enregistrement impossible.'));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().result!.isError, isTrue);
    expect(state().result!.subtitle, contains('terminée'));
  });

  test('404 notFound → overlay erreur', () async {
    await scanning();
    when(() => api.scan(123, 'tok')).thenThrow(
        const ApiException(ApiErrorType.notFound, detail: 'Séance introuvable.'));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().result!.isError, isTrue);
  });

  test('401 → purge (socle) : retour à scanning, aucun overlay', () async {
    await scanning();
    when(() => api.scan(123, 'tok'))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().status, ScanStatus.scanning);
    expect(state().result, isNull);
  });

  test('réseau → capture hors ligne (feature 027, FR-004), pas une erreur',
      () async {
    await scanning();
    when(() => api.scan(123, 'tok'))
        .thenThrow(const ApiException(ApiErrorType.network));

    await container.read(scanControllerProvider.notifier).onDetect(_validQr);

    expect(state().result!.kind, ScanResultKind.offlineQueued);
    expect(state().result!.isError, isFalse);
    expect(await container.read(offlineQueueStoreProvider).readAll(),
        hasLength(1));
  });

  test('payload non reconnu → indice transitoire, aucun appel API', () async {
    await scanning();

    await container
        .read(scanControllerProvider.notifier)
        .onDetect('QR-étranger-non-json');

    expect(state().status, ScanStatus.scanning);
    expect(state().hint, 'Code non reconnu');
    verifyNever(() => api.scan(any(), any()));
  });
}
