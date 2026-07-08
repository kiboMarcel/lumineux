import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:lumineux_mobile/features/auth/presentation/reset_password_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  Future<void> pumpScreen(
    WidgetTester tester,
    MockAuthApi api,
    MockTokenStore store,
  ) async {
    when(() => store.clear()).thenAnswer((_) async {});
    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const ResetPasswordScreen()),
      ),
    );
  }

  testWidgets('réinitialisation réussie → écran de succès', (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    when(() => api.resetPassword(any<ResetPasswordRequest>()))
        .thenAnswer((_) async {});
    await pumpScreen(tester, api, store);

    await tester.enterText(find.byKey(const Key('reset-token')), 'token-123');
    await tester.enterText(
        find.byKey(const Key('reset-new-password')), 'abcd1234');
    await tester.tap(find.byKey(const Key('reset-submit')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('reset-success')), findsOneWidget);
  });

  testWidgets('jeton invalide → message clair', (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    when(() => api.resetPassword(any<ResetPasswordRequest>()))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));
    await pumpScreen(tester, api, store);

    await tester.enterText(find.byKey(const Key('reset-token')), 'bad');
    await tester.enterText(
        find.byKey(const Key('reset-new-password')), 'abcd1234');
    await tester.tap(find.byKey(const Key('reset-submit')));
    await tester.pumpAndSettle();

    expect(find.text('Jeton invalide ou expiré.'), findsOneWidget);
  });
}
