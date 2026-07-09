import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/scan_state.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:mocktail/mocktail.dart';

import '../../../support/harness.dart';

const _qr = '{"v":1,"s":123,"t":"tok"}';

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

  test('re-scan hors ligne de la même séance → dédupé, 1 seule entrée (FR-014)',
      () async {
    final controller = container.read(scanControllerProvider.notifier);
    controller.onPermissionResolved(true);
    when(() => api.scan(123, 'tok'))
        .thenThrow(const ApiException(ApiErrorType.network));

    // 1er scan hors ligne : capturé.
    await controller.onDetect(_qr);
    expect(state().result!.kind, ScanResultKind.offlineQueued);
    final opId1 =
        (await container.read(offlineQueueStoreProvider).readAll()).single.clientOperationId;

    // Reprise de la détection puis 2e scan de la même séance.
    controller.dismissResult();
    await controller.onDetect(_qr);

    // Toujours une seule entrée, l'originale conservée (même opId).
    final queue = await container.read(offlineQueueStoreProvider).readAll();
    expect(queue, hasLength(1));
    expect(queue.single.clientOperationId, opId1);

    // Retour explicite « déjà capturée hors ligne ».
    expect(state().result!.kind, ScanResultKind.offlineQueued);
    expect(state().result!.title, 'Déjà capturée hors ligne');
  });
}
