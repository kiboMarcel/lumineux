import 'dart:math';

import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/backoff_policy.dart';
import 'package:lumineux_mobile/features/attendance/application/pending_capture.dart';

PendingCapture _capture({int attemptCount = 0, Duration age = Duration.zero}) {
  final now = DateTime.utc(2026, 7, 9, 14);
  return PendingCapture(
    clientOperationId: 'op',
    sessionId: 1,
    token: 't',
    clientArrivalTime: now,
    firstCapturedAt: now.subtract(age),
    attemptCount: attemptCount,
  );
}

void main() {
  const policy = BackoffPolicy();
  final now = DateTime.utc(2026, 7, 9, 14);

  group('plafond FR-013', () {
    test('non épuisé quand tentatives et âge sous le seuil', () {
      expect(policy.isExhausted(_capture(attemptCount: 3), now), isFalse);
    });

    test('épuisé au plafond de tentatives (>= 8)', () {
      expect(policy.isExhausted(_capture(attemptCount: 8), now), isTrue);
    });

    test('épuisé au plafond d\'âge (>= 7 jours)', () {
      expect(
        policy.isExhausted(_capture(age: const Duration(days: 7)), now),
        isTrue,
      );
    });

    test('épuisé si l\'un OU l\'autre seuil est atteint', () {
      expect(
        policy.isExhausted(
            _capture(attemptCount: 0, age: const Duration(days: 8)), now),
        isTrue,
      );
    });
  });

  group('backoff exponentiel', () {
    test('croît avec le nombre de tentatives (jitter neutralisé)', () {
      // Random déterministe → jitter constant, on compare les ordres de grandeur.
      Duration d(int n) => policy.delayFor(n, random: Random(42));
      expect(d(0) < d(1), isTrue);
      expect(d(1) < d(2), isTrue);
    });

    test('plafonné à maxDelay (5 min) pour de grands compteurs', () {
      final d = policy.delayFor(20, random: Random(1));
      expect(d.inMinutes, lessThanOrEqualTo(5));
    });

    test('délai initial ~2 s au premier échec (jitter ±20 %)', () {
      final d = policy.delayFor(0, random: Random(7));
      expect(d.inMilliseconds, inInclusiveRange(1600, 2400));
    });
  });
}
