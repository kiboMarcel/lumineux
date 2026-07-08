import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/application/session_state.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:lumineux_mobile/features/home/presentation/home_screen.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  Future<UncontrolledProviderScope> authenticatedApp(
    WidgetTester tester,
    MockAuthApi api,
    MockTokenStore store,
  ) async {
    final holder = TokenHolder();
    when(() => store.clear()).thenAnswer((_) async {});
    when(() => store.save(any())).thenAnswer((_) async {});
    when(() => api.login(any())).thenAnswer((_) async => TokenResponse(
          accessToken: 'jwt',
          tokenType: 'Bearer',
          expiresAt: DateTime.now().add(const Duration(hours: 1)),
        ));
    when(() => api.me()).thenAnswer((_) async => const CurrentUser(
        memberId: '1', displayName: 'Jean Dupont', permissions: []));

    final container = makeContainer(api: api, store: store, holder: holder);
    await container
        .read(sessionControllerProvider.notifier)
        .login('M-1', 'password1');

    return UncontrolledProviderScope(
      container: container,
      child: routerApp(const HomeScreen()),
    );
  }

  testWidgets('affiche l\'identité du membre', (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    await tester.pumpWidget(await authenticatedApp(tester, api, store));
    await tester.pump();

    expect(find.text('Jean Dupont'), findsOneWidget);
  });

  testWidgets('déconnexion → session anonyme', (tester) async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    final app = await authenticatedApp(tester, api, store);
    await tester.pumpWidget(app);
    await tester.pump();

    await tester.tap(find.byKey(const Key('home-logout-button')));
    await tester.pump();

    expect(
      app.container.read(sessionControllerProvider).status,
      SessionStatus.anonymous,
    );
    verify(() => store.clear()).called(1);
  });
}
