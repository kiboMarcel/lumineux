/// Configuration d'environnement, injectée au build via `--dart-define` /
/// `--dart-define-from-file` (profils `env/dev.json`, `env/prod.json`).
///
/// Aucun secret n'est codé en dur : l'URL de base est fournie au lancement.
class Env {
  const Env._();

  /// URL de base de l'API Lumineux (HTTPS). Défaut : émulateur Android en dev.
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://10.0.2.2:4311',
  );

  /// Indicateur de profil de développement. En dev uniquement, une exception
  /// TLS ciblée (certificat auto-signé) est tolérée. En prod : HTTPS strict.
  static const bool isDev = bool.fromEnvironment('IS_DEV', defaultValue: true);

  /// Racine versionnée de l'API consommée par le client.
  static String get apiRoot => '$apiBaseUrl/api/v1';
}
