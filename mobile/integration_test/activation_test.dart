import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:lumineux_mobile/app.dart';

/// US2 (T037) — Mot de passe temporaire → activation → accueil.
///
/// Prérequis : API dev joignable + un membre avec mot de passe **temporaire**.
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'connexion avec mot de passe temporaire → activation → accueil',
    (tester) async {
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();

      // 1. Login avec un mot de passe temporaire → bascule activation.
      await tester.enterText(
          find.byKey(const Key('login-reference')), 'REMPLACER');
      await tester.enterText(
          find.byKey(const Key('login-password')), 'TEMPORAIRE');
      await tester.tap(find.byKey(const Key('login-submit')));
      await tester.pumpAndSettle();

      // 2. Écran d'activation avec référence pré-remplie.
      expect(find.byKey(const Key('activate-submit')), findsOneWidget);

      // 3. Définir un nouveau mot de passe conforme → accueil.
      await tester.enterText(
          find.byKey(const Key('activate-temporary')), 'TEMPORAIRE');
      await tester.enterText(
          find.byKey(const Key('activate-new-password')), 'Nouveau123');
      await tester.tap(find.byKey(const Key('activate-submit')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('home-display-name')), findsOneWidget);
    },
    // e2e : nécessite un émulateur/simulateur + API dev + membre temporaire.
    skip: true,
  );
}
