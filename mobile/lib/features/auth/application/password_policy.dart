/// Validation **de confort** de la politique de mot de passe, répliquant les
/// règles publiques de l'API pour un retour immédiat (FR-017). L'API reste
/// l'autorité (un rejet serveur prime).
class PasswordPolicy {
  const PasswordPolicy._();

  static const int minLength = 8;

  static final RegExp _hasLetter = RegExp('[A-Za-zÀ-ÿ]');
  static final RegExp _hasDigit = RegExp('[0-9]');

  /// Retourne un message d'erreur FR si non conforme, sinon `null`.
  static String? validate(String password) {
    if (password.length < minLength) {
      return 'Le mot de passe doit contenir au moins $minLength caractères.';
    }
    if (!_hasLetter.hasMatch(password)) {
      return 'Le mot de passe doit contenir au moins une lettre.';
    }
    if (!_hasDigit.hasMatch(password)) {
      return 'Le mot de passe doit contenir au moins un chiffre.';
    }
    return null;
  }

  static bool isValid(String password) => validate(password) == null;
}
