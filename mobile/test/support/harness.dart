import 'dart:async';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:lumineux_mobile/core/network/token_holder.dart';
import 'package:lumineux_mobile/core/storage/secure_token_store.dart';
import 'package:lumineux_mobile/core/time/clock.dart';
import 'package:lumineux_mobile/features/attendance/application/camera_permission_facade.dart';
import 'package:lumineux_mobile/features/attendance/application/connectivity_facade.dart';
import 'package:lumineux_mobile/features/attendance/application/providers.dart';
import 'package:lumineux_mobile/features/attendance/application/scanner_facade.dart';
import 'package:lumineux_mobile/features/attendance/data/attendance_api.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_scan_dtos.dart';
import 'package:lumineux_mobile/features/auth/application/providers.dart';
import 'package:lumineux_mobile/features/auth/data/auth_api.dart';
import 'package:lumineux_mobile/features/auth/data/auth_dtos.dart';
import 'package:mocktail/mocktail.dart';

class MockAuthApi extends Mock implements AuthApi {}

class MockTokenStore extends Mock implements SecureTokenStore {}

class MockAttendanceApi extends Mock implements AttendanceApi {}

class MockSecureStorage extends Mock implements FlutterSecureStorage {}

/// Horloge déterministe pour les tests (feature 027).
class FixedClock implements Clock {
  FixedClock(this._now);
  DateTime _now;
  set now(DateTime value) => _now = value;
  @override
  DateTime utcNow() => _now;
}

/// Connectivité contrôlable pour les tests (feature 027).
class FakeConnectivity implements ConnectivityFacade {
  FakeConnectivity({this.online = true});
  final _controller = StreamController<bool>.broadcast();
  bool online;

  /// Simule un changement d'état réseau.
  void emit(bool value) {
    online = value;
    _controller.add(value);
  }

  @override
  Stream<bool> get onStatusChange => _controller.stream;

  @override
  Future<bool> isOnline() async => online;

  void dispose() => _controller.close();
}

/// Enregistre le fallback mocktail pour `List<OfflineScanItem>` (arg de syncBatch).
void registerSyncFallbacks() {
  registerFallbackValue(<OfflineScanItem>[]);
}

/// Coffre sécurisé **en mémoire** pour les tests (feature 027) : simule la
/// persistance clé→valeur, permettant d'exercer le vrai `OfflineQueueStore`
/// sans canal plateforme. Enregistre le fallback `AndroidOptions`.
MockSecureStorage inMemorySecureStorage() {
  registerFallbackValue(const AndroidOptions());
  final storage = MockSecureStorage();
  final backing = <String, String>{};

  when(() => storage.read(
        key: any(named: 'key'),
        aOptions: any(named: 'aOptions'),
      )).thenAnswer((inv) async => backing[inv.namedArguments[#key] as String]);

  when(() => storage.write(
        key: any(named: 'key'),
        value: any(named: 'value'),
        aOptions: any(named: 'aOptions'),
      )).thenAnswer((inv) async {
    backing[inv.namedArguments[#key] as String] =
        inv.namedArguments[#value] as String;
  });

  when(() => storage.delete(
        key: any(named: 'key'),
        aOptions: any(named: 'aOptions'),
      )).thenAnswer((inv) async {
    backing.remove(inv.namedArguments[#key] as String);
  });

  return storage;
}

/// Scanner substitué en test : la vue est un placeholder ; `emit` simule un code.
class FakeScannerFacade implements ScannerFacade {
  void Function(String raw)? _onCode;
  bool started = false;

  void emit(String raw) => _onCode?.call(raw);

  @override
  Widget buildPreview({required void Function(String raw) onCode}) {
    _onCode = onCode;
    return const SizedBox.shrink();
  }

  @override
  Future<void> start() async => started = true;

  @override
  Future<void> stop() async => started = false;

  @override
  Future<void> dispose() async {}
}

/// Permission caméra substituée en test.
class FakeCameraPermission implements CameraPermissionFacade {
  FakeCameraPermission({this.granted = true});

  bool granted;
  bool settingsOpened = false;

  @override
  Future<bool> isGranted() async => granted;

  @override
  Future<bool> request() async => granted;

  @override
  Future<void> openSettings() async => settingsOpened = true;
}

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
      // Fakes par défaut : rendent `HomeShell` (qui instancie `ScannerScreen`)
      // montable sans caméra/permission réelles.
      scannerFacadeProvider.overrideWithValue(FakeScannerFacade()),
      cameraPermissionProvider.overrideWithValue(FakeCameraPermission()),
      // Feature 027 : le Scanner instancie le SyncController → éviter le coffre
      // et le plugin de connectivité réels en test.
      secureStorageProvider.overrideWithValue(inMemorySecureStorage()),
      connectivityFacadeProvider.overrideWithValue(FakeConnectivity()),
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
