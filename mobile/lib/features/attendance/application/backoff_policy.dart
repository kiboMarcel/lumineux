import 'dart:math';

import 'pending_capture.dart';

/// Politique de réessai et plafond d'abandon (research.md D3/D4, FR-013).
///
/// - **Backoff exponentiel** (délai croissant entre tentatives), plafonné, avec
///   *jitter* pour éviter les pics synchronisés côté serveur.
/// - **Plafond combiné** : un élément passe en **échec définitif** dès que le
///   nombre de tentatives OU l'ancienneté dépasse le seuil — garantissant
///   qu'aucune présence ne reste indéfiniment « coincée » (SC-004).
///
/// Les valeurs par défaut sont les **valeurs retenues** en conception
/// (configurables) : 8 tentatives / 7 jours ; backoff 2 s ×2, plafond 5 min.
class BackoffPolicy {
  const BackoffPolicy({
    this.maxAttempts = 8,
    this.maxAge = const Duration(days: 7),
    this.initialDelay = const Duration(seconds: 2),
    this.factor = 2.0,
    this.maxDelay = const Duration(minutes: 5),
    this.jitter = 0.2,
  });

  final int maxAttempts;
  final Duration maxAge;
  final Duration initialDelay;
  final double factor;
  final Duration maxDelay;

  /// Amplitude relative du jitter (0.2 = ±20 %).
  final double jitter;

  /// Vrai si l'élément a atteint le plafond (tentatives OU âge) → échec définitif.
  bool isExhausted(PendingCapture capture, DateTime now) {
    if (capture.attemptCount >= maxAttempts) return true;
    if (now.difference(capture.firstCapturedAt) >= maxAge) return true;
    return false;
  }

  /// Délai avant la prochaine tentative pour un élément ayant déjà subi
  /// [attemptCount] échecs (0 → [initialDelay]). Plafonné à [maxDelay], jitter
  /// appliqué. Un [Random] peut être injecté en test.
  Duration delayFor(int attemptCount, {Random? random}) {
    final rnd = random ?? Random();
    final baseMs = initialDelay.inMilliseconds *
        pow(factor, attemptCount < 0 ? 0 : attemptCount);
    final cappedMs = min(baseMs.toDouble(), maxDelay.inMilliseconds.toDouble());
    // Jitter symétrique : ±jitter autour de la valeur de base.
    final delta = (rnd.nextDouble() * 2 - 1) * jitter; // ∈ [-jitter, +jitter]
    final withJitter = cappedMs * (1 + delta);
    return Duration(milliseconds: withJitter.round().clamp(0, maxDelay.inMilliseconds));
  }
}
