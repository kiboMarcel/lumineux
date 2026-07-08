import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/features/auth/presentation/login_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  late MockAuthApi api;
  late MockTokenStore store;

  setUp(() {
    api = MockAuthApi();
    store = MockTokenStore();
    when(() => store.clear()).thenAnswer((_) async {});
  });

  testWidgets('champs vides → validation, aucun appel réseau', (tester) async {
    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const LoginScreen()),
      ),
    );

    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pump();

    expect(find.text('Référence requise'), findsOneWidget);
    expect(find.text('Mot de passe requis'), findsOneWidget);
    verifyNever(() => api.login(any()));
  });

  testWidgets('identifiants invalides → message clair', (tester) async {
    when(() => api.login(any()))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));
    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const LoginScreen()),
      ),
    );

    await tester.enterText(find.byKey(const Key('login-reference')), 'M-1');
    await tester.enterText(find.byKey(const Key('login-password')), 'wrong');
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pumpAndSettle();

    expect(find.text('Identifiants invalides.'), findsOneWidget);
  });

  testWidgets('réseau indisponible → message dédié', (tester) async {
    when(() => api.login(any()))
        .thenThrow(const ApiException(ApiErrorType.network));
    final container = makeContainer(api: api, store: store);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: routerApp(const LoginScreen()),
      ),
    );

    await tester.enterText(find.byKey(const Key('login-reference')), 'M-1');
    await tester.enterText(find.byKey(const Key('login-password')), 'secret12');
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pumpAndSettle();

    expect(find.text('Réseau indisponible, réessayez.'), findsOneWidget);
  });
}
