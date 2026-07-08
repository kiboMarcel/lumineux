import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/auth/application/password_policy.dart';

void main() {
  group('PasswordPolicy', () {
    test('accepte un mot de passe conforme (lettre + chiffre, longueur)', () {
      expect(PasswordPolicy.validate('abcd1234'), isNull);
      expect(PasswordPolicy.isValid('Motdepasse1'), isTrue);
    });

    test('refuse un mot de passe trop court', () {
      expect(PasswordPolicy.validate('ab12'), isNotNull);
      expect(PasswordPolicy.isValid('ab12'), isFalse);
    });

    test('refuse sans lettre', () {
      expect(PasswordPolicy.validate('12345678'), contains('lettre'));
    });

    test('refuse sans chiffre', () {
      expect(PasswordPolicy.validate('abcdefgh'), contains('chiffre'));
    });
  });
}
