import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/config/env.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/network/token_holder.dart';
import '../../../core/storage/secure_token_store.dart';
import '../data/auth_api.dart';
import 'session_controller.dart';
import 'session_state.dart';

/// Coffre sécurisé (surchargé en test).
final secureStorageProvider = Provider<FlutterSecureStorage>((ref) {
  return const FlutterSecureStorage();
});

final secureTokenStoreProvider = Provider<SecureTokenStore>((ref) {
  return SecureTokenStore(ref.watch(secureStorageProvider));
});

/// Détenteur en mémoire du jeton (pont réseau ↔ session).
final tokenHolderProvider = Provider<TokenHolder>((ref) => TokenHolder());

/// Client `dio` : lit le jeton via le holder, purge sur 401, TLS dev-only.
final dioProvider = Provider<Dio>((ref) {
  final holder = ref.watch(tokenHolderProvider);
  return buildDioClient(
    apiRoot: Env.apiRoot,
    readToken: () => holder.current?.value,
    onUnauthorized: () => holder.onUnauthorized?.call(),
    allowSelfSignedInDev: Env.isDev,
  );
});

final authApiProvider = Provider<AuthApi>((ref) {
  return AuthApi(ref.watch(dioProvider));
});

final sessionControllerProvider =
    NotifierProvider<SessionController, SessionState>(SessionController.new);
