import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';

PendingCapture _sample() => PendingCapture(
      clientOperationId: 'op-123',
      sessionId: 42,
      token: 'secret-token',
      clientArrivalTime: DateTime.utc(2026, 7, 9, 14, 3, 12),
      firstCapturedAt: DateTime.utc(2026, 7, 9, 14, 3, 12),
    );

void main() {
  test('valeurs par défaut : pending, 0 tentative, sans lastAttemptAt', () {
    final c = _sample();
    expect(c.state, PendingState.pending);
    expect(c.attemptCount, 0);
    expect(c.lastAttemptAt, isNull);
  });

  test('sérialisation JSON aller-retour préserve tous les champs', () {
    final c = _sample().copyWith(
      attemptCount: 3,
      lastAttemptAt: DateTime.utc(2026, 7, 9, 15),
      state: PendingState.transientFailed,
    );

    final round = PendingCapture.fromJson(c.toJson());

    expect(round.clientOperationId, c.clientOperationId);
    expect(round.sessionId, c.sessionId);
    expect(round.token, c.token);
    expect(round.clientArrivalTime, c.clientArrivalTime);
    expect(round.firstCapturedAt, c.firstCapturedAt);
    expect(round.attemptCount, 3);
    expect(round.lastAttemptAt, DateTime.utc(2026, 7, 9, 15));
    expect(round.state, PendingState.transientFailed);
  });

  test('copyWith ne mute pas les champs immuables (opId, séance, jeton)', () {
    final c = _sample();
    final updated = c.copyWith(state: PendingState.inProgress);
    expect(updated.clientOperationId, c.clientOperationId);
    expect(updated.sessionId, c.sessionId);
    expect(updated.token, c.token);
    expect(updated.state, PendingState.inProgress);
    expect(c.state, PendingState.pending); // original inchangé
  });

  test('les heures sont normalisées en UTC à la désérialisation', () {
    final round = PendingCapture.fromJson(_sample().toJson());
    expect(round.clientArrivalTime.isUtc, isTrue);
    expect(round.firstCapturedAt.isUtc, isTrue);
  });
}
