import 'dart:async';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
import 'package:lumineux_mobile/core/storage/secure_token_store.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/data/auth_api.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:mocktail/mocktail.dart';

class MockAuthApi extends Mock implements AuthApi {}

class MockTokenStore extends Mock implements SecureTokenStore {}

/// Adapter `dio` renvoyant une réponse figée, et enregistrant la dernière
/// requête (pour vérifier les en-têtes Bearer).
class FakeHttpAdapter implements HttpClientAdapter {
  FakeHttpAdapter(this.statusCode, this.body);

  final int statusCode;
  final String body;
  RequestOptions? lastOptions;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    lastOptions = options;
    return ResponseBody.fromString(
      body,
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

void registerAuthFallbacks() {
  registerFallbackValue(const LoginRequest(reference: 'r', password: 'p'));
  registerFallbackValue(const ActivateRequest(
    reference: 'r',
    temporaryPassword: 't',
    newPassword: 'n',
  ));
  registerFallbackValue(const ForgotPasswordRequest(reference: 'r'));
  registerFallbackValue(const ResetPasswordRequest(token: 't', newPassword: 'n'));
  registerFallbackValue(
      const ChangePasswordRequest(currentPassword: 'c', newPassword: 'n'));
  registerFallbackValue(
      AuthToken(value: 'v', type: 'Bearer', expiresAt: DateTime(2999)));
}

/// Conteneur Riverpod avec API + coffre moqués.
ProviderContainer makeContainer({
  required AuthApi api,
  required SecureTokenStore store,
  TokenHolder? holder,
}) {
  final container = ProviderContainer(
    overrides: [
      authApiProvider.overrideWithValue(api),
      secureTokenStoreProvider.overrideWithValue(store),
      tokenHolderProvider.overrideWithValue(holder ?? TokenHolder()),
    ],
  );
  addTearDown(container.dispose);
  return container;
}

/// Application minimale avec `go_router` incluant toutes les routes cibles
/// (stubs) afin que `context.go(...)` fonctionne en test.
Widget routerApp(Widget screen, {String initialLocation = '/'}) {
  final router = GoRouter(
    initialLocation: initialLocation,
    routes: [
      GoRoute(path: '/', builder: (context, state) => screen),
      _stub('/login', 'LOGIN'),
      _stub('/auth/forgot', 'FORGOT'),
      _stub('/auth/reset', 'RESET'),
      _stub('/home', 'HOME'),
      _stub('/account/change-password', 'CHANGE'),
    ],
  );
  return MaterialApp.router(routerConfig: router);
}

GoRoute _stub(String path, String label) => GoRoute(
      path: path,
      builder: (context, state) => Scaffold(body: Text(label)),
    );
