import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/core/network/dio_client.dart';
import 'package:lumineux_mobile/features/auth/data/auth_api.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';

import '../../support/harness.dart';

AuthApi apiWith(int status, dynamic body) {
  final dio = buildDioClient(
    apiRoot: 'https://x/api/v1',
    readToken: () => null,
    onUnauthorized: () {},
  );
  dio.httpClientAdapter = FakeHttpAdapter(status, jsonEncode(body));
  return AuthApi(dio);
}

void main() {
  test('login 200 → TokenResponse', () async {
    final api = apiWith(200, {
      'accessToken': 'jwt-value',
      'tokenType': 'Bearer',
      'expiresAt': '2999-01-01T00:00:00.000Z',
    });

    final token = await api.login(
        const LoginRequest(reference: 'M-1', password: 'p'));

    expect(token.accessToken, 'jwt-value');
    expect(token.tokenType, 'Bearer');
  });

  test('login 403 → ApiException forbidden + password_change_required',
      () async {
    final api = apiWith(403, {
      'title': 'Forbidden',
      'code': 'password_change_required',
    });

    await expectLater(
      api.login(const LoginRequest(reference: 'M-1', password: 'p')),
      throwsA(
        isA<ApiException>()
            .having((e) => e.type, 'type', ApiErrorType.forbidden)
            .having((e) => e.code, 'code', 'password_change_required'),
      ),
    );
  });

  test('login 401 → ApiException unauthorized', () async {
    final api = apiWith(401, {'title': 'Unauthorized'});
    await expectLater(
      api.login(const LoginRequest(reference: 'M-1', password: 'bad')),
      throwsA(isA<ApiException>()
          .having((e) => e.type, 'type', ApiErrorType.unauthorized)),
    );
  });

  test('forgotPassword → message générique', () async {
    final api = apiWith(200, {'message': 'Si un compte existe...'});
    final message =
        await api.forgotPassword(const ForgotPasswordRequest(reference: 'M-1'));
    expect(message, 'Si un compte existe...');
  });

  test('resetPassword 204 → complète sans erreur', () async {
    final api = apiWith(204, {});
    await expectLater(
      api.resetPassword(
          const ResetPasswordRequest(token: 'tk', newPassword: 'abcd1234')),
      completes,
    );
  });

  test('me 200 → CurrentUser', () async {
    final api = apiWith(200, {
      'memberId': '42',
      'displayName': 'Jean Dupont',
      'permissions': ['scan'],
    });
    final user = await api.me();
    expect(user.memberId, '42');
    expect(user.displayName, 'Jean Dupont');
    expect(user.permissions, contains('scan'));
  });

  test('changePassword 204 → complète', () async {
    final api = apiWith(204, {});
    await expectLater(
      api.changePassword(const ChangePasswordRequest(
          currentPassword: 'old12345', newPassword: 'new12345')),
      completes,
    );
  });

  test('erreur réseau (DioException sans réponse) → ApiException network',
      () async {
    final dio = buildDioClient(
      apiRoot: 'https://x/api/v1',
      readToken: () => null,
      onUnauthorized: () {},
    );
    // Adapter qui lève une erreur de connexion.
    dio.httpClientAdapter = _ThrowingAdapter();
    final api = AuthApi(dio);

    await expectLater(
      api.me(),
      throwsA(isA<ApiException>()
          .having((e) => e.type, 'type', ApiErrorType.network)),
    );
  });
}

class _ThrowingAdapter implements HttpClientAdapter {
  @override
  void close({bool force = false}) {}

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    throw DioException(
      requestOptions: options,
      type: DioExceptionType.connectionError,
      error: 'no network',
    );
  }
}
