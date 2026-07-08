import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
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
    when(() => store.save(any())).thenAnswer((_) async {});
  });

  TokenResponse tokens() => TokenResponse(
        accessToken: 'jwt',
        tokenType: 'Bearer',
        expiresAt: DateTime.now().add(const Duration(hours: 1)),
      );

  const user = CurrentUser(memberId: '1', displayName: 'Jean', permissions: []);

  test('login 200 → authenticated + jeton établi', () async {
    when(() => api.login(any())).thenAnswer((_) async => tokens());
    when(() => api.me()).thenAnswer((_) async => user);
    final container = makeContainer(api: api, store: store, holder: holder);

    await container
        .read(sessionControllerProvider.notifier)
        .login('M-1', 'password1');

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.authenticated);
    expect(holder.current?.value, 'jwt');
    verify(() => store.save(any())).called(1);
  });

  test('login 403 password_change_required → passwordChangeRequired', () async {
    when(() => api.login(any())).thenThrow(const ApiException(
      ApiErrorType.forbidden,
      code: 'password_change_required',
    ));
    final container = makeContainer(api: api, store: store, holder: holder);

    await container
        .read(sessionControllerProvider.notifier)
        .login('M-1', 'temp');

    final state = container.read(sessionControllerProvider);
    expect(state.status, SessionStatus.passwordChangeRequired);
    expect(state.reference, 'M-1');
  });

  test('login 401 → relance ApiException, non authentifié', () async {
    when(() => api.login(any()))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));
    final container = makeContainer(api: api, store: store, holder: holder);
    final controller = container.read(sessionControllerProvider.notifier);

    await expectLater(
      controller.login('M-1', 'bad'),
      throwsA(isA<ApiException>()),
    );
    expect(container.read(sessionControllerProvider).status,
        isNot(SessionStatus.authenticated));
    expect(holder.current, isNull);
  });

  test('login réseau indisponible → relance ApiException network', () async {
    when(() => api.login(any()))
        .thenThrow(const ApiException(ApiErrorType.network));
    final container = makeContainer(api: api, store: store, holder: holder);
    final controller = container.read(sessionControllerProvider.notifier);

    await expectLater(
      controller.login('M-1', 'password1'),
      throwsA(isA<ApiException>()
          .having((e) => e.type, 'type', ApiErrorType.network)),
    );
  });
}
