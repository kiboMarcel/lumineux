/// Catégories d'erreurs applicatives produites à partir des réponses de l'API.
///
/// Le mapping HTTP → [ApiErrorType] est centralisé dans `dio_client.dart`
/// (`mapDioException`) et les messages FR dans `errors/error_messages.dart`.
enum ApiErrorType {
  /// 401 — non authentifié (identifiants invalides au login, sinon session expirée).
  unauthorized,

  /// 403 — refusé ; peut porter un code métier (`password_change_required`).
  forbidden,

  /// 400 — validation (ProblemDetails RFC 7807).
  validation,

  /// 404 — ressource introuvable (séance introuvable au scan).
  notFound,

  /// 409 — conflit (séance close au scan).
  conflict,

  /// 410 — ressource expirée (jeton QR périmé au scan).
  gone,

  /// Timeout / hors ligne / erreur de connexion.
  network,

  /// 5xx — erreur serveur.
  server,

  /// Cas non catégorisé.
  unknown,
}

/// Exception typée normalisant toute erreur d'appel API pour la présentation.
///
/// Ne contient **aucun secret** : uniquement statut, code métier et libellés
/// non sensibles issus du `ProblemDetails`.
class ApiException implements Exception {
  const ApiException(
    this.type, {
    this.statusCode,
    this.code,
    this.title,
    this.detail,
  });

  final ApiErrorType type;
  final int? statusCode;

  /// Code métier lu dans le `ProblemDetails` (ex. `password_change_required`).
  final String? code;
  final String? title;
  final String? detail;

  @override
  String toString() =>
      'ApiException(type: $type, status: $statusCode, code: $code)';
}
