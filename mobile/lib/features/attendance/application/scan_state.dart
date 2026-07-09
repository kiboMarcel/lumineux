import '../data/scan_dtos.dart';

/// Statuts de l'écran Scanner (voir data-model.md).
enum ScanStatus { permissionUnknown, permissionDenied, scanning, submitting, result }

/// Nature d'un résultat présenté dans l'overlay modal.
/// [offlineQueued] : capture mise en file hors ligne (feature 027, ton neutre).
enum ScanResultKind { success, alreadyPresent, offlineQueued, error }

/// Contenu de l'overlay de résultat (aller-retour API uniquement).
class ScanResultView {
  const ScanResultView(this.kind, this.title, this.subtitle);

  final ScanResultKind kind;
  final String title;
  final String? subtitle;

  bool get isError => kind == ScanResultKind.error;

  factory ScanResultView.fromOutcome(ScanOutcome outcome) {
    final time = _formatTime(outcome.attendance.arrivalTime);
    final name = outcome.attendance.memberFullName;
    // Repli heure seule si le nom est absent (memberFullName nullable).
    final subtitle = (name == null || name.isEmpty) ? time : '$name · $time';
    return outcome.created
        ? ScanResultView(ScanResultKind.success, 'Présence enregistrée', subtitle)
        : ScanResultView(
            ScanResultKind.alreadyPresent, 'Déjà enregistrée', subtitle);
  }

  factory ScanResultView.error(String message) =>
      ScanResultView(ScanResultKind.error, 'Échec du scan', message);

  /// Capture hors ligne réussie (feature 027, FR-001). [alreadyQueued] signale
  /// un re-scan d'une séance déjà en file (dédup FR-014) — ton neutre, jamais
  /// une erreur.
  factory ScanResultView.offlineQueued({bool alreadyQueued = false}) =>
      ScanResultView(
        ScanResultKind.offlineQueued,
        alreadyQueued ? 'Déjà capturée hors ligne' : 'Enregistrée hors ligne',
        'À synchroniser dès le retour du réseau',
      );
}

/// État applicatif de l'écran Scanner (non persisté).
class ScanState {
  const ScanState._(this.status, {this.result, this.hint});

  final ScanStatus status;

  /// Présent lorsque [status] == result (overlay modal).
  final ScanResultView? result;

  /// Indice transitoire non bloquant (ex. « Code non reconnu ») ; la détection
  /// reste active. Utilisé uniquement en [ScanStatus.scanning].
  final String? hint;

  const ScanState.permissionUnknown() : this._(ScanStatus.permissionUnknown);
  const ScanState.permissionDenied() : this._(ScanStatus.permissionDenied);
  const ScanState.scanning({String? hint})
      : this._(ScanStatus.scanning, hint: hint);
  const ScanState.submitting() : this._(ScanStatus.submitting);
  const ScanState.result(ScanResultView result)
      : this._(ScanStatus.result, result: result);
}

String _formatTime(DateTime dt) {
  final local = dt.toLocal();
  final hh = local.hour.toString().padLeft(2, '0');
  final mm = local.minute.toString().padLeft(2, '0');
  return '$hh:$mm';
}
