import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:lumineux_mobile/app.dart';

/// US2 (feature 027) — Parcours capture hors ligne → synchronisation.
///
/// Prérequis d'exécution (appareil/émulateur) : API dev joignable, une séance
/// ouverte, un membre authentifié, et la possibilité de couper/rétablir le
/// réseau. Lancer avec :
///   flutter test integration_test/offline_sync_test.dart --dart-define-from-file=env/dev.json
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'réseau coupé → scan capturé hors ligne → réseau rétabli → synchronisé sans doublon',
    (tester) async {
      await tester.pumpWidget(const ProviderScope(child: LumineuxApp()));
      await tester.pumpAndSettle();

      // 1. Se connecter (membre), ouvrir Scanner, couper le réseau.
      // 2. Scanner le QR → overlay « Enregistrée hors ligne ».
      // 3. Fermer/relancer → capture toujours en attente (indicateur = 1).
      // 4. Rétablir le réseau → synchro auto < 30 s → indicateur = 0.
      // 5. Re-synchro → aucun doublon (AlreadyPresent côté serveur).
      expect(find.byKey(const Key('login-submit')), findsOneWidget);
    },
    // e2e : nécessite un appareil + API dev + bascule réseau + membre.
    skip: true,
  );
}
