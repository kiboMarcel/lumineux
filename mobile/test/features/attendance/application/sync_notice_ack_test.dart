import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/sync_notice.dart';
import 'package:lumineux_mobile/features/attendance/data/sync_notice_store.dart';

import '../../../support/harness.dart';

SyncNotice _notice(String opId, {NoticeKind kind = NoticeKind.rejected}) =>
    SyncNotice(
      clientOperationId: opId,
      sessionId: 10,
      kind: kind,
      reason: 'Jeton QR invalide au moment du scan.',
      occurredAt: DateTime.utc(2026, 7, 9, 14),
    );

void main() {
  late SyncNoticeStore store;

  setUp(() => store = SyncNoticeStore(inMemorySecureStorage()));

  test('add persiste un avis et readAll le relit (SC-004)', () async {
    await store.add(_notice('op-1'));
    final all = await store.readAll();
    expect(all, hasLength(1));
    expect(all.single.reason, 'Jeton QR invalide au moment du scan.');
    expect(all.single.acknowledged, isFalse);
  });

  test('add est idempotent par clientOperationId (pas de doublon d\'avis)',
      () async {
    await store.add(_notice('op-1'));
    await store.add(_notice('op-1'));
    expect(await store.readAll(), hasLength(1));
  });

  test('acknowledge marque l\'avis et persiste l\'état', () async {
    await store.add(_notice('op-1'));
    await store.acknowledge('op-1');

    final all = await store.readAll();
    expect(all.single.acknowledged, isTrue);
    // Filtrage typique de l'UI : plus aucun avis actif.
    expect(all.where((n) => !n.acknowledged), isEmpty);
  });

  test('les avis contiennent une raison, jamais de jeton', () async {
    await store.add(_notice('op-1'));
    final json = (await store.readAll()).single.toJson();
    expect(json.containsKey('token'), isFalse);
    expect(json['reason'], isNotEmpty);
  });
}
