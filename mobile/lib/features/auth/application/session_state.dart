import '../data/auth_dtos.dart';

/// Statuts de la machine à états de session (voir data-model.md).
enum SessionStatus {
  /// Au démarrage, avant lecture du coffre.
  unknown,

  /// Lecture du coffre en cours.
  restoring,

  /// Jeton valide + identité chargée.
  authenticated,

  /// Login a renvoyé `403 password_change_required`.
  passwordChangeRequired,

  /// Aucun jeton / expiré / déconnecté.
  anonymous,
}

/// État applicatif de session (non persisté), porté par le `SessionController`.
class SessionState {
  const SessionState._(this.status, {this.user, this.reference, this.message});

  final SessionStatus status;

  /// Identité du membre lorsque [status] == authenticated.
  final CurrentUser? user;

  /// Référence pré-remplie pour l'écran d'activation (passwordChangeRequired).
  final String? reference;

  /// Message à présenter (ex. « Session expirée »).
  final String? message;

  const SessionState.unknown() : this._(SessionStatus.unknown);

  const SessionState.restoring() : this._(SessionStatus.restoring);

  const SessionState.authenticated(CurrentUser user)
      : this._(SessionStatus.authenticated, user: user);

  const SessionState.passwordChangeRequired(String reference)
      : this._(SessionStatus.passwordChangeRequired, reference: reference);

  const SessionState.anonymous({String? message})
      : this._(SessionStatus.anonymous, message: message);

  bool get isAuthenticated => status == SessionStatus.authenticated;
}
