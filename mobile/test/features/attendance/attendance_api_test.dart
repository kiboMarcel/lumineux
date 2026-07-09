import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/network/api_exception.dart';
import 'package:lumineux_mobile/core/network/dio_client.dart';
import 'package:lumineux_mobile/features/attendance/data/attendance_api.dart';

import '../../support/harness.dart';

Map<String, dynamic> attendanceJson() => {
      'id': 987,
      'sessionId': 123,
      'memberId': 42,
      'memberFullName': 'Aline Kouadio',
      'arrivalTime': '2026-07-09T14:32:11Z',
      'endTime': null,
      'source': 'Scan',
      'status': 'Valid',
      'originAntennaId': 3,
    };

({AttendanceApi api, FakeHttpAdapter adapter}) build(
  int status, {
  String? token,
}) {
  final dio = buildDioClient(
    apiRoot: 'https://x/api/v1',
    readToken: () => token,
    onUnauthorized: () {},
  );
  final adapter = FakeHttpAdapter(status, jsonEncode(attendanceJson()));
  dio.httpClientAdapter = adapter;
  return (api: AttendanceApi(dio), adapter: adapter);
}

void main() {
  test('scan 201 → ScanOutcome(created: true)', () async {
    final outcome = await build(201).api.scan(123, 'tok');
    expect(outcome.created, isTrue);
    expect(outcome.attendance.memberFullName, 'Aline Kouadio');
  });

  test('scan 200 → ScanOutcome(created: false) (déjà présente)', () async {
    final outcome = await build(200).api.scan(123, 'tok');
    expect(outcome.created, isFalse);
  });

  test('corps `{token}` et Bearer attaché sur la route de scan', () async {
    final built = build(201, token: 'jwt-tok');
    await built.api.scan(55, 'the-token');

    final opts = built.adapter.lastOptions!;
    expect(opts.path, '/attendance-sessions/55/scan');
    expect(opts.headers['Authorization'], 'Bearer jwt-tok');
    expect((opts.data as Map)['token'], 'the-token');
  });

  test('scan 410 → ApiException gone', () async {
    final dio = buildDioClient(
      apiRoot: 'https://x/api/v1',
      readToken: () => 'tok',
      onUnauthorized: () {},
    );
    dio.httpClientAdapter =
        FakeHttpAdapter(410, jsonEncode({'detail': 'Code QR expiré'}));
    final api = AttendanceApi(dio);

    await expectLater(
      api.scan(1, 'old'),
      throwsA(isA<ApiException>()
          .having((e) => e.type, 'type', ApiErrorType.gone)),
    );
  });

  test('scan sans réseau → ApiException network', () async {
    final dio = buildDioClient(
      apiRoot: 'https://x/api/v1',
      readToken: () => 'tok',
      onUnauthorized: () {},
    );
    dio.httpClientAdapter = _ThrowingAdapter();
    final api = AttendanceApi(dio);

    await expectLater(
      api.scan(1, 'tok'),
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
    );
  }
}
