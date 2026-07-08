import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
import 'package:lumineux_mobile/core/storage/secure_token_store.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/application/session_state.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  setUpAll(registerAuthFallbacks);

  late MockAuthApi api;
  late MockTokenStore store;
  late TokenHolder holder;

  setUp(() {
    api = MockAuthApi();
    store = MockTokenStore();
    holder = TokenHolder();
    when(() => store.clear()).thenAnswer((_) async {});
  });

  AuthToken validToken() => AuthToken(
        value: 't',
        type: 'Bearer',
        expiresAt: DateTime.now().add(const Duration(hours: 1)),
      );

  const user = CurrentUser(memberId: '1', displayName: 'Jean', permissions: []);

  test('restore : jeton valide + /me OK → authenticated', () async {
    when(() => store.read()).thenAnswer((_) async => validToken());
    when(() => api.me()).thenAnswer((_) async => user);
    final container = makeContainer(api: api, store: store, holder: holder);

    await container.read(sessionControllerProvider.notifier).restore();

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.authenticated);
    expect(holder.current, isNotNull);
  });

  test('restore : aucun jeton → anonymous', () async {
    when(() => store.read()).thenAnswer((_) async => null);
    final container = makeContainer(api: api, store: store, holder: holder);

    await container.read(sessionControllerProvider.notifier).restore();

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.anonymous);
    verifyNever(() => api.me());
  });

  test('restore : jeton expiré → anonymous + purge', () async {
    when(() => store.read()).thenAnswer((_) async => AuthToken(
          value: 't',
          type: 'Bearer',
          expiresAt: DateTime.now().subtract(const Duration(minutes: 1)),
        ));
    final container = makeContainer(api: api, store: store, holder: holder);

    await container.read(sessionControllerProvider.notifier).restore();

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.anonymous);
    verify(() => store.clear()).called(1);
  });

  test('restore : /me renvoie 401 → purge + anonymous', () async {
    when(() => store.read()).thenAnswer((_) async => validToken());
    when(() => api.me())
        .thenThrow(const ApiException(ApiErrorType.unauthorized));
    final container = makeContainer(api: api, store: store, holder: holder);

    await container.read(sessionControllerProvider.notifier).restore();

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.anonymous);
    expect(holder.current, isNull);
  });
}
