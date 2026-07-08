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

  const user = CurrentUser(memberId: '1', displayName: 'Jean', permissions: []);

  test('activate 200 → authenticated', () async {
    when(() => api.activate(any())).thenAnswer((_) async => TokenResponse(
          accessToken: 'jwt',
          tokenType: 'Bearer',
          expiresAt: DateTime.now().add(const Duration(hours: 1)),
        ));
    when(() => api.me()).thenAnswer((_) async => user);
    final container = makeContainer(api: api, store: store, holder: holder);

    await container
        .read(sessionControllerProvider.notifier)
        .activate('M-1', 'temp1234', 'newpass1');

    expect(container.read(sessionControllerProvider).status,
        SessionStatus.authenticated);
  });

  test('activate en erreur → relance ApiException', () async {
    when(() => api.activate(any()))
        .thenThrow(const ApiException(ApiErrorType.unauthorized));
    final container = makeContainer(api: api, store: store, holder: holder);
    final controller = container.read(sessionControllerProvider.notifier);

    await expectLater(
      controller.activate('M-1', 'bad', 'newpass1'),
      throwsA(isA<ApiException>()),
    );
  });
}
