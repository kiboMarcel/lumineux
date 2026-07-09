import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/scan_state.dart';
import 'package:lumineux_mobile/features/attendance/presentation/scan_result_overlay.dart';

Widget _host(ScanResultView result, {VoidCallback? onDismiss}) => MaterialApp(
      home: Scaffold(
        body: Stack(
          children: [
            ScanResultOverlay(result: result, onDismiss: onDismiss ?? () {}),
          ],
        ),
      ),
    );

void main() {
  testWidgets('capture hors ligne : titre/sous-titre neutres, bouton Fermer',
      (tester) async {
    await tester.pumpWidget(_host(ScanResultView.offlineQueued()));

    expect(find.text('Enregistrée hors ligne'), findsOneWidget);
    expect(find.text('À synchroniser dès le retour du réseau'), findsOneWidget);
    // Ce n'est pas une erreur → bouton « Fermer » (pas « Scanner à nouveau »).
    expect(find.text('Fermer'), findsOneWidget);
    expect(find.text('Scanner à nouveau'), findsNothing);
    // Icône neutre hors ligne, pas d'icône d'erreur.
    expect(find.byIcon(Icons.cloud_off_outlined), findsOneWidget);
    expect(find.byIcon(Icons.error_outline), findsNothing);
  });

  testWidgets('variante « déjà capturée hors ligne » (dédup)', (tester) async {
    await tester
        .pumpWidget(_host(ScanResultView.offlineQueued(alreadyQueued: true)));
    expect(find.text('Déjà capturée hors ligne'), findsOneWidget);
  });

  testWidgets('aucun jeton affiché dans l\'overlay (SC-005)', (tester) async {
    await tester.pumpWidget(_host(ScanResultView.offlineQueued()));
    expect(find.textContaining('tok'), findsNothing);
  });

  testWidgets('Fermer déclenche onDismiss', (tester) async {
    var dismissed = false;
    await tester.pumpWidget(
        _host(ScanResultView.offlineQueued(), onDismiss: () => dismissed = true));
    await tester.tap(find.text('Fermer'));
    expect(dismissed, isTrue);
  });
}
