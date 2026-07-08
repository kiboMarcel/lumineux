import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:lumineux_mobile/app.dart';

/// US3 (T043) — Oublié → réinitialisation → connexion.
///
/// Prérequis : API dev joignable + accès à l'e-mail de réinitialisation pour
/// récupérer le jeton (saisie/collage en M0).
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'mot de passe oublié → message générique → réinitialisation par jeton',
    (tester) async {
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();

      // 1. Aller sur « Mot de passe oublié ».
      await tester.tap(find.byKey(const Key('login-forgot-link')));
      await tester.pumpAndSettle();

      // 2. Saisir une référence → message générique (anti-énumération).
      await tester.enterText(
          find.byKey(const Key('forgot-reference')), 'REMPLACER');
      await tester.tap(find.byKey(const Key('forgot-submit')));
      await tester.pumpAndSettle();
      expect(find.byKey(const Key('forgot-message')), findsOneWidget);

      // 3. Écran de réinitialisation : jeton (e-mail) + nouveau mot de passe.
      await tester.tap(find.byKey(const Key('forgot-to-reset')));
      await tester.pumpAndSettle();
      await tester.enterText(
          find.byKey(const Key('reset-token')), 'JETON-EMAIL');
      await tester.enterText(
          find.byKey(const Key('reset-new-password')), 'Nouveau123');
      await tester.tap(find.byKey(const Key('reset-submit')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('reset-success')), findsOneWidget);
    },
    // e2e : nécessite un émulateur/simulateur + API dev + jeton e-mail.
    skip: true,
  );
}
