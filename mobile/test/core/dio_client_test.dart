import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/core/network/dio_client.dart';

import '../support/harness.dart';

void main() {
  group('requiresAuth', () {
    test('vrai pour les routes protégées', () {
      expect(requiresAuth('/auth/me'), isTrue);
      expect(requiresAuth('/auth/change-password'), isTrue);
    });
    test('faux pour les routes anonymes', () {
      expect(requiresAuth('/auth/login'), isFalse);
      expect(requiresAuth('/auth/forgot-password'), isFalse);
    });
  });

  group('mapDioException', () {
    DioException dioError(int status, dynamic data) => DioException(
          requestOptions: RequestOptions(path: '/x'),
          type: DioExceptionType.badResponse,
          response: Response(
            requestOptions: RequestOptions(path: '/x'),
            statusCode: status,
            data: data,
          ),
        );

    test('401 → unauthorized', () {
      expect(mapDioException(dioError(401, {})).type,
          ApiErrorType.unauthorized);
    });
    test('403 avec code racine → forbidden + code', () {
      final e = mapDioException(
          dioError(403, {'code': 'password_change_required'}));
      expect(e.type, ApiErrorType.forbidden);
      expect(e.code, 'password_change_required');
    });
    test('403 avec extensions.code → forbidden + code', () {
      final e = mapDioException(dioError(403, {
        'extensions': {'code': 'password_change_required'},
      }));
      expect(e.code, 'password_change_required');
    });
    test('400 → validation avec detail', () {
      final e = mapDioException(dioError(400, {'detail': 'Invalide'}));
      expect(e.type, ApiErrorType.validation);
      expect(e.detail, 'Invalide');
    });
    test('500 → server', () {
      expect(mapDioException(dioError(500, {})).type, ApiErrorType.server);
    });
    test('timeout → network', () {
      final e = mapDioException(DioException(
        requestOptions: RequestOptions(path: '/x'),
        type: DioExceptionType.connectionTimeout,
      ));
      expect(e.type, ApiErrorType.network);
    });
  });

  group('intercepteurs', () {
    test('ajoute le Bearer sur une route protégée', () async {
      final adapter = FakeHttpAdapter(
          200, jsonEncode({'memberId': '1', 'displayName': 'X'}));
      final dio = buildDioClient(
        apiRoot: 'https://x/api/v1',
        readToken: () => 'tok',
        onUnauthorized: () {},
      );
      dio.httpClientAdapter = adapter;

      await dio.get('/auth/me');

      expect(adapter.lastOptions!.headers['Authorization'], 'Bearer tok');
    });

    test('n\'ajoute pas le Bearer sur une route anonyme', () async {
      final adapter = FakeHttpAdapter(200, jsonEncode({'message': 'ok'}));
      final dio = buildDioClient(
        apiRoot: 'https://x/api/v1',
        readToken: () => 'tok',
        onUnauthorized: () {},
      );
      dio.httpClientAdapter = adapter;

      await dio.post('/auth/forgot-password', data: {'reference': 'r'});

      expect(adapter.lastOptions!.headers.containsKey('Authorization'), isFalse);
    });

    test('un 401 déclenche onUnauthorized et lève ApiException', () async {
      var purged = false;
      final adapter = FakeHttpAdapter(401, jsonEncode({'title': 'nope'}));
      final dio = buildDioClient(
        apiRoot: 'https://x/api/v1',
        readToken: () => 'tok',
        onUnauthorized: () => purged = true,
      );
      dio.httpClientAdapter = adapter;

      await expectLater(
        dio.get('/auth/me'),
        throwsA(isA<DioException>()),
      );
      expect(purged, isTrue);
    });
  });
}
