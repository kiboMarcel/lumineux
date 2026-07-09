import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../data/attendance_api.dart';
import 'providers.dart';
import 'qr_payload.dart';
import 'scan_state.dart';

/// Orchestre le scan : parsing du QR, soumission à l'API, résultat/erreur,
/// anti double-soumission. Aucune règle métier : l'API décide.
class ScanController extends Notifier<ScanState> {
  late final AttendanceApi _api;
  String? _lastUnrecognized;

  @override
  ScanState build() {
    _api = ref.read(attendanceApiProvider);
    return const ScanState.permissionUnknown();
  }

  /// Résultat de la résolution de permission caméra (US3).
  void onPermissionResolved(bool granted) {
    state = granted
        ? const ScanState.scanning()
        : const ScanState.permissionDenied();
  }

  /// Détection d'un code. **Suspendue** hors de l'état `scanning`
  /// (anti double-soumission). Un payload non reconnu ne bloque pas : indice
  /// transitoire et la détection continue.
  Future<void> onDetect(String raw) async {
    if (state.status != ScanStatus.scanning) return;

    final parsed = QrPayloadResult.parse(raw);
    if (!parsed.isValid) {
      if (_lastUnrecognized != raw) {
        _lastUnrecognized = raw;
        state = const ScanState.scanning(hint: 'Code non reconnu');
      }
      return;
    }
    _lastUnrecognized = null;
    await _submit(parsed.payload!);
  }

  Future<void> _submit(QrPayload payload) async {
    state = const ScanState.submitting();
    try {
      final outcome = await _api.scan(payload.sessionId, payload.token);
      state = ScanState.result(ScanResultView.fromOutcome(outcome));
    } on ApiException catch (e) {
      if (e.type == ApiErrorType.unauthorized) {
        // 401 : le socle purge la session et le routeur redirige vers /login.
        state = const ScanState.scanning();
        return;
      }
      state = ScanState.result(ScanResultView.error(messageForApiException(e)));
    } catch (_) {
      state = ScanState.result(ScanResultView.error('Une erreur est survenue.'));
    }
  }

  /// Ferme l'overlay de résultat et reprend la détection.
  void dismissResult() {
    state = const ScanState.scanning();
  }

  /// Efface l'indice transitoire une fois affiché (garde l'état `scanning`).
  void clearHint() {
    if (state.status == ScanStatus.scanning && state.hint != null) {
      state = const ScanState.scanning();
    }
  }
}
