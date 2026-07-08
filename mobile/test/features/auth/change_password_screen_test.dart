import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:lumineux_mobile/features/auth/presentation/change_password_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  testWidgets('mot de passe non conforme rejeté sans appel réseau',
      (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    when(() => store.clear()).thenAnswer((_) async {});
    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const ChangePasswordScreen()),
      ),
    );

    await tester.enterText(
        find.byKey(const Key('change-current')), 'old12345');
    await tester.enterText(
        find.byKey(const Key('change-new-password')), 'abc');
    await tester.tap(find.byKey(const Key('change-submit')));
    await tester.pump();

    expect(find.textContaining('au moins'), findsOneWidget);
    verifyNever(() => api.changePassword(any<ChangePasswordRequest>()));
  });

  testWidgets('changement réussi → confirmation puis retour accueil',
      (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    when(() => store.clear()).thenAnswer((_) async {});
    when(() => api.changePassword(any<ChangePasswordRequest>()))
        .thenAnswer((_) async {});
    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const ChangePasswordScreen()),
      ),
    );

    await tester.enterText(
        find.byKey(const Key('change-current')), 'old12345');
    await tester.enterText(
        find.byKey(const Key('change-new-password')), 'new12345');
    await tester.tap(find.byKey(const Key('change-submit')));
    await tester.pumpAndSettle();

    // Navigation vers le stub d'accueil.
    expect(find.text('HOME'), findsOneWidget);
    verify(() => api.changePassword(any<ChangePasswordRequest>())).called(1);
  });
}
