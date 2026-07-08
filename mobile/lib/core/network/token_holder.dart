import '../storage/secure_token_store.dart';

/// Détenteur **en mémoire** du jeton courant, partagé entre l'intercepteur
/// Bearer (lecture synchrone) et le `SessionController` (écriture).
///
/// Évite un cycle de dépendances réseau ↔ session : le contrôleur enregistre
/// [onUnauthorized] pour être notifié d'un 401 en cours d'usage (→ purge).
class TokenHolder {
  AuthToken? current;

  /// Déclenché par l'intercepteur d'erreurs sur un 401 (session à purger).
  void Function()? onUnauthorized;
}
