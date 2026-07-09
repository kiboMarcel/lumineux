import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/data/offline_scan_dtos.dart';

void main() {
  test('OfflineScanItem.toJson : heure en ISO 8601 UTC', () {
    final item = OfflineScanItem(
      clientOperationId: 'op-1',
      token: 'tok',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14, 3, 12),
    );
    final json = item.toJson();
    expect(json['clientOperationId'], 'op-1');
    expect(json['token'], 'tok');
    expect(json['clientArrivalTime'], '2026-07-09T14:03:12.000Z');
  });

  test('OfflineScanBatchRequest.toJson enveloppe les items', () {
    final req = OfflineScanBatchRequest([
      OfflineScanItem(
        clientOperationId: 'op-1',
        token: 't',
        clientArrivalTime: DateTime.utc(2026),
      ),
    ]);
    expect((req.toJson()['items'] as List), hasLength(1));
  });

  test('OfflineScanBatchResponse.fromJson lit les 3 issues', () {
    final resp = OfflineScanBatchResponse.fromJson({
      'results': [
        {'clientOperationId': 'a', 'outcome': 'Created', 'attendanceId': 10},
        {'clientOperationId': 'b', 'outcome': 'AlreadyPresent', 'attendanceId': 11},
        {'clientOperationId': 'c', 'outcome': 'Rejected', 'reason': 'Jeton invalide'},
      ],
    });

    expect(resp.results, hasLength(3));
    expect(resp.results[0].outcome, OfflineScanOutcome.created);
    expect(resp.results[0].attendanceId, 10);
    expect(resp.results[1].outcome, OfflineScanOutcome.alreadyPresent);
    expect(resp.results[2].outcome, OfflineScanOutcome.rejected);
    expect(resp.results[2].reason, 'Jeton invalide');
    expect(resp.results[2].attendanceId, isNull);
  });

  test('fromJson tolère une réponse sans results', () {
    expect(OfflineScanBatchResponse.fromJson({}).results, isEmpty);
  });
}
