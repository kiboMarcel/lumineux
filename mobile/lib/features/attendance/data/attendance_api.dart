import 'package:dio/dio.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/dio_client.dart';
import 'offline_scan_dtos.dart';
import 'scan_dtos.dart';

/// Accès au contrat de scan `/api/v1/attendance-sessions/{id}/scan` (existant,
/// inchangé). Ne réimplémente aucune règle métier : l'API fait autorité.
class AttendanceApi {
  AttendanceApi(this._dio);

  final Dio _dio;

  /// `POST /attendance-sessions/{sessionId}/scan` avec `{ token }`.
  /// **201** → présence créée (`created=true`) ; **200** → déjà présente.
  Future<ScanOutcome> scan(int sessionId, String token) async {
    try {
      final response = await _dio.post<dynamic>(
        '/attendance-sessions/$sessionId/scan',
        data: {'token': token},
      );
      final attendance = AttendanceResponse.fromJson(
        (response.data as Map).cast<String, dynamic>(),
      );
      return ScanOutcome(
        attendance: attendance,
        created: response.statusCode == 201,
      );
    } on DioException catch (e) {
      final err = e.error;
      throw err is ApiException ? err : mapDioException(e);
    }
  }

  /// `POST /attendance-sessions/{sessionId}/scan/batch` (existant, feature 027).
  /// Synchronise un lot de scans hors ligne pour **une** séance ; renvoie une
  /// issue par élément. Ne réimplémente aucune règle métier : l'API fait autorité.
  Future<OfflineScanBatchResponse> syncBatch(
      int sessionId, List<OfflineScanItem> items) async {
    try {
      final response = await _dio.post<dynamic>(
        '/attendance-sessions/$sessionId/scan/batch',
        data: OfflineScanBatchRequest(items).toJson(),
      );
      return OfflineScanBatchResponse.fromJson(
        (response.data as Map).cast<String, dynamic>(),
      );
    } on DioException catch (e) {
      final err = e.error;
      throw err is ApiException ? err : mapDioException(e);
    }
  }
}
