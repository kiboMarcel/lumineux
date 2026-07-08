import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:lumineux_mobile/features/auth/presentation/forgot_password_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  testWidgets('affiche le message générique après envoi', (tester) async {
    const generic = 'Si un compte correspond, un e-mail a été envoyé.';
    final api = MockAuthApi();
    final store = MockTokenStore();
    when(() => store.clear()).thenAnswer((_) async {});
    when(() => api.forgotPassword(any<ForgotPasswordRequest>()))
        .thenAnswer((_) async => generic);

    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const ForgotPasswordScreen()),
      ),
    );

    await tester.enterText(find.byKey(const Key('forgot-reference')), 'M-1');
    await tester.tap(find.byKey(const Key('forgot-submit')));
    await tester.pumpAndSettle();

    expect(find.text(generic), findsOneWidget);
  });
}
