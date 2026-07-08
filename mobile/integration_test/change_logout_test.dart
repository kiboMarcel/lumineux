import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:lumineux_mobile/app.dart';

/// US4 (T049) — Changement de mot de passe → déconnexion → relancement.
///
/// Prérequis : API dev joignable + un membre valide (déjà activé).
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'changer le mot de passe puis se déconnecter (aucune restauration)',
    (tester) async {
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();

      // 1. Connexion.
      await tester.enterText(
          find.byKey(const Key('login-reference')), 'REMPLACER');
      await tester.enterText(
          find.byKey(const Key('login-password')), 'REMPLACER');
      await tester.tap(find.byKey(const Key('login-submit')));
      await tester.pumpAndSettle();

      // 2. Changer le mot de passe.
      await tester.tap(find.byKey(const Key('home-change-password')));
      await tester.pumpAndSettle();
      await tester.enterText(
          find.byKey(const Key('change-current')), 'REMPLACER');
      await tester.enterText(
          find.byKey(const Key('change-new-password')), 'Nouveau123');
      await tester.tap(find.byKey(const Key('change-submit')));
      await tester.pumpAndSettle();

      // 3. Déconnexion → retour connexion.
      await tester.tap(find.byKey(const Key('home-logout-button')));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('login-submit')), findsOneWidget);

      // 4. Relancer → aucune session restaurée.
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('login-submit')), findsOneWidget);
    },
    // e2e : nécessite un émulateur/simulateur + API dev + membre activé.
    skip: true,
  );
}
