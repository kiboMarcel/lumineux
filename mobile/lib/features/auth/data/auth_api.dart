import 'package:dio/dio.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/dio_client.dart';
import 'auth_dtos.dart';

/// Couche d'accès aux contrats `/api/v1/auth/*` (features 003/006/007).
/// Ne réimplémente **aucune** règle métier : l'API fait autorité.
class AuthApi {
  AuthApi(this._dio);

  final Dio _dio;

  /// `POST /auth/login` → `TokenResponse` (200).
  /// Lève `ApiException(forbidden, code: password_change_required)` sur 403.
  Future<TokenResponse> login(LoginRequest request) => _guard(
        () => _dio.post('/auth/login', data: request.toJson()),
        (r) => TokenResponse.fromJson(r.data as Map<String, dynamic>),
      );

  /// `POST /auth/activate` → `TokenResponse` (200).
  Future<TokenResponse> activate(ActivateRequest request) => _guard(
        () => _dio.post('/auth/activate', data: request.toJson()),
        (r) => TokenResponse.fromJson(r.data as Map<String, dynamic>),
      );

  /// `POST /auth/forgot-password` → message **générique** (200), anti-énumération.
  Future<String> forgotPassword(ForgotPasswordRequest request) => _guard(
        () => _dio.post('/auth/forgot-password', data: request.toJson()),
        (r) {
          final data = r.data;
          if (data is Map && data['message'] is String) {
            return data['message'] as String;
          }
          return 'Si un compte correspond, un e-mail de réinitialisation a été envoyé.';
        },
      );

  /// `POST /auth/reset-password` → 204 (aucun corps).
  Future<void> resetPassword(ResetPasswordRequest request) => _guard(
        () => _dio.post('/auth/reset-password', data: request.toJson()),
        (_) {},
      );

  /// `GET /auth/me` → identité du membre (Bearer requis).
  Future<CurrentUser> me() => _guard(
        () => _dio.get('/auth/me'),
        (r) => CurrentUser.fromJson(r.data as Map<String, dynamic>),
      );

  /// `POST /auth/change-password` → 204 (Bearer requis).
  Future<void> changePassword(ChangePasswordRequest request) => _guard(
        () => _dio.post('/auth/change-password', data: request.toJson()),
        (_) {},
      );

  Future<T> _guard<T>(
    Future<Response<dynamic>> Function() call,
    T Function(Response<dynamic>) map,
  ) async {
    try {
      final response = await call();
      return map(response);
    } on DioException catch (e) {
      final err = e.error;
      throw err is ApiException ? err : mapDioException(e);
    }
  }
}
