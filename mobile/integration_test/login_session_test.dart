import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:lumineux_mobile/app.dart';

/// US1 (T030) — Connexion → accueil → restauration → expiration.
///
/// Prérequis d'exécution (émulateur/simulateur) : API dev joignable en HTTPS
/// et un membre provisionné (référence + mot de passe défini). Lancer avec :
///   flutter test integration_test/login_session_test.dart \
///     --dart-define-from-file=env/dev.json
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'connexion valide → accueil identifié, puis restauration au relancement',
    (tester) async {
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();

      // 1. Écran de connexion.
      expect(find.byKey(const Key('login-submit')), findsOneWidget);

      // 2. Saisir des identifiants valides.
      await tester.enterText(
          find.byKey(const Key('login-reference')), 'REMPLACER');
      await tester.enterText(
          find.byKey(const Key('login-password')), 'REMPLACER');
      await tester.tap(find.byKey(const Key('login-submit')));
      await tester.pumpAndSettle();

      // 3. Accueil authentifié affichant l'identité.
      expect(find.byKey(const Key('home-display-name')), findsOneWidget);

      // 4. Relancer l'app → session restaurée (jeton au coffre, non expiré).
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('home-display-name')), findsOneWidget);
    },
    // e2e : nécessite un émulateur/simulateur + API dev + membre valide.
    skip: true,
  );
}
