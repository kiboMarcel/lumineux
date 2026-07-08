import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';

import 'api_exception.dart';

/// Routes protégées nécessitant l'en-tête `Authorization: Bearer`.
bool requiresAuth(String path) =>
    path.contains('/auth/me') || path.contains('/auth/change-password');

/// Convertit une [DioException] en [ApiException] typée (mapping centralisé
/// FR-004/005). Lit `code`/`title`/`detail` du `ProblemDetails` (RFC 7807),
/// en couvrant à la fois `extensions.code` (nesté) et `code` au niveau racine
/// (sérialisation ProblemDetails ASP.NET).
ApiException mapDioException(DioException e) {
  switch (e.type) {
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
    case DioExceptionType.connectionError:
    case DioExceptionType.transformTimeout:
      return const ApiException(ApiErrorType.network);
    case DioExceptionType.cancel:
      return const ApiException(ApiErrorType.unknown);
    case DioExceptionType.badCertificate:
      return const ApiException(ApiErrorType.network);
    case DioExceptionType.unknown:
      if (e.error is SocketException) {
        return const ApiException(ApiErrorType.network);
      }
      break;
    case DioExceptionType.badResponse:
      break;
  }

  final response = e.response;
  final status = response?.statusCode;
  if (status == null) {
    return const ApiException(ApiErrorType.network);
  }

  String? code;
  String? title;
  String? detail;
  final data = response?.data;
  if (data is Map) {
    title = data['title'] as String?;
    detail = data['detail'] as String?;
    final ext = data['extensions'];
    if (ext is Map && ext['code'] != null) {
      code = ext['code'].toString();
    } else if (data['code'] != null) {
      code = data['code'].toString();
    }
  }

  if (status == 401) {
    return ApiException(ApiErrorType.unauthorized,
        statusCode: status, code: code, title: title, detail: detail);
  }
  if (status == 403) {
    return ApiException(ApiErrorType.forbidden,
        statusCode: status, code: code, title: title, detail: detail);
  }
  if (status == 400) {
    return ApiException(ApiErrorType.validation,
        statusCode: status, code: code, title: title, detail: detail);
  }
  if (status >= 500) {
    return ApiException(ApiErrorType.server,
        statusCode: status, code: code, title: title, detail: detail);
  }
  return ApiException(ApiErrorType.unknown,
      statusCode: status, code: code, title: title, detail: detail);
}

/// Ajoute le jeton porteur aux seules routes protégées.
class BearerInterceptor extends Interceptor {
  BearerInterceptor(this.readToken);

  final String? Function() readToken;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (requiresAuth(options.path)) {
      final token = readToken();
      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }
}

/// Normalise les erreurs en [ApiException] et notifie la purge sur 401.
/// Ne journalise **jamais** les corps de requête/réponse (FR-010).
class ErrorInterceptor extends Interceptor {
  ErrorInterceptor(this.onUnauthorized);

  final void Function() onUnauthorized;

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final mapped = mapDioException(err);
    if (mapped.type == ApiErrorType.unauthorized) {
      onUnauthorized();
    }
    handler.reject(
      DioException(
        requestOptions: err.requestOptions,
        response: err.response,
        type: err.type,
        error: mapped,
      ),
    );
  }
}

/// Construit le client `dio` configuré (base HTTPS, intercepteurs).
///
/// [allowSelfSignedInDev] active une **exception TLS ciblée** pour le
/// certificat auto-signé de l'API de dev. En **prod**, HTTPS strict (FR-019) :
/// aucune exception n'est installée.
Dio buildDioClient({
  required String apiRoot,
  required String? Function() readToken,
  required void Function() onUnauthorized,
  bool allowSelfSignedInDev = false,
}) {
  final dio = Dio(
    BaseOptions(
      baseUrl: apiRoot,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
      sendTimeout: const Duration(seconds: 15),
      contentType: Headers.jsonContentType,
      // 2xx uniquement OK ; tout le reste passe par l'ErrorInterceptor.
      validateStatus: (status) => status != null && status >= 200 && status < 300,
    ),
  );

  dio.interceptors.add(BearerInterceptor(readToken));
  dio.interceptors.add(ErrorInterceptor(onUnauthorized));

  if (allowSelfSignedInDev) {
    // DEV UNIQUEMENT — jamais en production.
    dio.httpClientAdapter = IOHttpClientAdapter(
      createHttpClient: () {
        final client = HttpClient();
        client.badCertificateCallback = (cert, host, port) => true;
        return client;
      },
    );
  }

  return dio;
}
