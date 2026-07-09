import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:lumineux_mobile/app.dart';

/// US1 (T023) — Parcours de scan de bout en bout.
///
/// Prérequis d'exécution (appareil/émulateur avec caméra) : API dev joignable,
/// une séance ouverte, la console web (SPA) projetant le QR JSON `{v,s,t}`, et
/// un membre authentifié. Lancer avec :
///   flutter test integration_test/scan_test.dart --dart-define-from-file=env/dev.json
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'ouvrir Scanner → viser le QR → présence enregistrée → re-scan',
    (tester) async {
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();

      // 1. Se connecter (membre) puis ouvrir l'onglet Scanner.
      // 2. Accorder la caméra, viser le QR de séance valide.
      // 3. Overlay « Présence enregistrée » (nom + heure).
      // 4. Fermer → re-viser le même QR → « Déjà enregistrée ».
      expect(find.byKey(const Key('login-submit')), findsOneWidget);
    },
    // e2e : nécessite un appareil avec caméra + API dev + SPA à jour + membre.
    skip: true,
  );
}
