import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/errors/error_messages.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';

void main() {
  group('messageForApiException', () {
    test('401 au login → identifiants invalides', () {
      final msg = messageForApiException(
        const ApiException(ApiErrorType.unauthorized),
        context: ErrorContext.login,
      );
      expect(msg, 'Identifiants invalides.');
    });

    test('401 général → session expirée', () {
      final msg = messageForApiException(
          const ApiException(ApiErrorType.unauthorized));
      expect(msg, contains('Session expirée'));
    });

    test('401 en réinitialisation → jeton invalide', () {
      final msg = messageForApiException(
        const ApiException(ApiErrorType.unauthorized),
        context: ErrorContext.reset,
      );
      expect(msg, 'Jeton invalide ou expiré.');
    });

    test('403 password_change_required → message dédié', () {
      final msg = messageForApiException(
        const ApiException(ApiErrorType.forbidden,
            code: kPasswordChangeRequired),
      );
      expect(msg, contains('changement de mot de passe'));
    });

    test('network → réseau indisponible', () {
      final msg =
          messageForApiException(const ApiException(ApiErrorType.network));
      expect(msg, contains('Réseau indisponible'));
    });

    test('validation → detail du ProblemDetails', () {
      final msg = messageForApiException(
        const ApiException(ApiErrorType.validation, detail: 'Champ manquant'),
      );
      expect(msg, 'Champ manquant');
    });
  });
}
