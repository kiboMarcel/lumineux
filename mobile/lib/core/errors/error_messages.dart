import '../network/api_exception.dart';

/// Contexte d'appel, pour nuancer le message d'un 401 selon le parcours.
enum ErrorContext { login, activate, reset, general }

/// Code métier signalant l'obligation de changer de mot de passe (login).
const String kPasswordChangeRequired = 'password_change_required';

/// Traduit une [ApiException] en message **français** compréhensible.
/// L'API reste l'autorité ; on affiche `detail`/`title` du ProblemDetails
/// quand ils sont présents (jamais de secret dans ces champs).
String messageForApiException(
  ApiException e, {
  ErrorContext context = ErrorContext.general,
}) {
  switch (e.type) {
    case ApiErrorType.network:
      return 'Réseau indisponible, réessayez.';
    case ApiErrorType.server:
      return 'Une erreur est survenue. Réessayez plus tard.';
    case ApiErrorType.unauthorized:
      switch (context) {
        case ErrorContext.login:
          return 'Identifiants invalides.';
        case ErrorContext.activate:
          return 'Référence ou mot de passe temporaire invalide.';
        case ErrorContext.reset:
          return 'Jeton invalide ou expiré.';
        case ErrorContext.general:
          return 'Session expirée. Veuillez vous reconnecter.';
      }
    case ApiErrorType.forbidden:
      if (e.code == kPasswordChangeRequired) {
        return 'Un changement de mot de passe est requis.';
      }
      return e.detail ?? e.title ?? 'Accès refusé.';
    case ApiErrorType.validation:
      return e.detail ?? e.title ?? 'Données invalides.';
    case ApiErrorType.unknown:
      return e.detail ?? e.title ?? 'Une erreur est survenue.';
  }
}
