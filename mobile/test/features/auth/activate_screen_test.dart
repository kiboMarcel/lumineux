import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/presentation/activate_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  testWidgets(
      'référence pré-remplie + mot de passe non conforme rejeté sans réseau',
      (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    when(() => store.clear()).thenAnswer((_) async {});
    // Login déclenche l'obligation de changement → état passwordChangeRequired.
    when(() => api.login(any())).thenThrow(const ApiException(
      ApiErrorType.forbidden,
      code: 'password_change_required',
    ));

    final container = makeContainer(api: api, store: store);
    await container
        .read(sessionControllerProvider.notifier)
        .login('M-77', 'temp');

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const ActivateScreen()),
      ),
    );
    await tester.pump();

    // Référence pré-remplie affichée.
    expect(find.text('M-77'), findsOneWidget);

    // Saisie non conforme → validation immédiate, aucun appel activate.
    await tester.enterText(
        find.byKey(const Key('activate-temporary')), 'temp1234');
    await tester.enterText(
        find.byKey(const Key('activate-new-password')), 'abc');
    await tester.ensureVisible(find.byKey(const Key('activate-submit')));
    await tester.tap(find.byKey(const Key('activate-submit')));
    await tester.pump();

    expect(find.textContaining('au moins'), findsOneWidget);
    verifyNever(() => api.activate(any()));
  });
}
