import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
import 'package:lumineux_mobile/core/storage/secure_token_store.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/application/session_state.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:mocktail/mocktail.dart';

import 'support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  test('reprise d\'app : jeton expiré entre-temps → purge (SC-004)', () async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    final holder = TokenHolder();
    when(() => store.clear()).thenAnswer((_) async {});
    when(() => store.save(any())).thenAnswer((_) async {});
    when(() => api.login(any())).thenAnswer((_) async => TokenResponse(
          accessToken: 'jwt',
          tokenType: 'Bearer',
          expiresAt: DateTime.now().add(const Duration(hours: 1)),
        ));
    when(() => api.me()).thenAnswer((_) async =>
        const CurrentUser(memberId: '1', displayName: 'Jean', permissions: []));

    final container = makeContainer(api: api, store: store, holder: holder);
    final controller = container.read(sessionControllerProvider.notifier);

    await controller.login('M-1', 'password1');
    expect(container.read(sessionControllerProvider).status,
        SessionStatus.authenticated);

    // Simule l'expiration du jeton pendant l'arrière-plan.
    holder.current = AuthToken(
      value: 'jwt',
      type: 'Bearer',
      expiresAt: DateTime.now().subtract(const Duration(seconds: 1)),
    );

    await controller.recheckOnResume();

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.anonymous);
    expect(holder.current, isNull);
  });

  test('reprise d\'app : jeton encore valide → session conservée', () async {
    final api = MockAuthApi();
    final store = MockTokenStore();
    final holder = TokenHolder();
    when(() => store.clear()).thenAnswer((_) async {});
    when(() => store.save(any())).thenAnswer((_) async {});
    when(() => api.login(any())).thenAnswer((_) async => TokenResponse(
          accessToken: 'jwt',
          tokenType: 'Bearer',
          expiresAt: DateTime.now().add(const Duration(hours: 1)),
        ));
    when(() => api.me()).thenAnswer((_) async =>
        const CurrentUser(memberId: '1', displayName: 'Jean', permissions: []));

    final container = makeContainer(api: api, store: store, holder: holder);
    final controller = container.read(sessionControllerProvider.notifier);

    await controller.login('M-1', 'password1');
    await controller.recheckOnResume();

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.authenticated);
  });
}
