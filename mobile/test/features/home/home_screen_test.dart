import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/application/session_state.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:lumineux_mobile/features/home/presentation/home_shell.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  Future<UncontrolledProviderScope> authenticatedApp(
    MockAuthApi api,
    MockTokenStore store, {
    List<String> permissions = const [],
  }) async {
    final holder = TokenHolder();
    when(() => store.clear()).thenAnswer((_) async {});
    when(() => store.save(any())).thenAnswer((_) async {});
    when(() => api.login(any())).thenAnswer((_) async => TokenResponse(
          accessToken: 'jwt',
          tokenType: 'Bearer',
          expiresAt: DateTime.now().add(const Duration(hours: 1)),
        ));
    when(() => api.me()).thenAnswer((_) async => CurrentUser(
        memberId: '1', displayName: 'Jean Dupont', permissions: permissions));

    final container = makeContainer(api: api, store: store, holder: holder);
    await container
        .read(sessionControllerProvider.notifier)
        .login('M-1', 'password1');

    return UncontrolledProviderScope(
      container: container,
      child: routerApp(const HomeShell()),
    );
  }

  testWidgets('onglet Accueil : salutation avec le prénom', (tester) async {
    final app = await authenticatedApp(MockAuthApi(), MockTokenStore());
    await tester.pumpWidget(app);
    await tester.pump();

    expect(find.text('Bonjour, Jean'), findsOneWidget);
  });

  testWidgets('déconnexion depuis l\'onglet Profil → session anonyme',
      (tester) async {
    final store = MockTokenStore();
    final app = await authenticatedApp(MockAuthApi(), store);
    await tester.pumpWidget(app);
    await tester.pump();

    // Basculer vers l'onglet Profil puis se déconnecter.
    await tester.tap(find.byKey(const Key('nav-profile')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('profile-logout')));
    await tester.pump();

    expect(
      app.container.read(sessionControllerProvider).status,
      SessionStatus.anonymous,
    );
    verify(() => store.clear()).called(1);
  });

  testWidgets('onglet Profil : droit de gestion listé', (tester) async {
    final app = await authenticatedApp(
      MockAuthApi(),
      MockTokenStore(),
      permissions: ['manage_attendance'],
    );
    await tester.pumpWidget(app);
    await tester.pump();

    await tester.tap(find.byKey(const Key('nav-profile')));
    await tester.pumpAndSettle();

    expect(find.text('Jean Dupont'), findsOneWidget);
    expect(find.text('Gérer les présences'), findsOneWidget);
  });
}
