import 'dart:async';
import 'dart:developer' as developer;
import 'dart:math';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/time/clock.dart';
import '../data/attendance_api.dart';
import '../data/offline_queue_store.dart';
import '../data/offline_scan_dtos.dart';
import '../data/sync_notice_store.dart';
import 'backoff_policy.dart';
import 'pending_capture.dart';
import 'providers.dart';
import 'sync_notice.dart';
import 'sync_state.dart';

/// Orchestrateur de la synchronisation hors ligne (US2, FR-005..008/FR-013).
///
/// - Groupe les captures **par séance** et appelle l'endpoint de lot existant.
/// - **Réconcilie** chaque issue (retrait succès/rejets, conservation des seuls
///   échecs transitoires, échec définitif au plafond FR-013).
/// - Déclenche la synchro sur **retour de connectivité**, **relance manuelle**,
///   et par **backoff** tant que des éléments restent en file.
/// - Ne journalise **jamais** le jeton (FR-009).
class SyncController extends Notifier<SyncStatus> {
  late final OfflineQueueStore _queue;
  late final SyncNoticeStore _notices;
  late final AttendanceApi _api;
  late final Clock _clock;
  late final BackoffPolicy _backoff;

  StreamSubscription<bool>? _connSub;
  Timer? _retryTimer;
  bool _running = false;

  @override
  SyncStatus build() {
    _queue = ref.read(offlineQueueStoreProvider);
    _notices = ref.read(syncNoticeStoreProvider);
    _api = ref.read(attendanceApiProvider);
    _clock = ref.read(clockProvider);
    _backoff = ref.read(backoffPolicyProvider);

    // Déclencheur « retour de connectivité » (FR-006).
    _connSub = ref.read(connectivityFacadeProvider).onStatusChange.listen((online) {
      if (online) unawaited(sync());
    });

    ref.onDispose(() {
      _connSub?.cancel();
      _retryTimer?.cancel();
    });

    // Charge les compteurs initiaux sans bloquer la construction.
    unawaited(refreshStatus());
    return const SyncStatus();
  }

  /// Recalcule l'agrégat présenté (compteurs + avis non acquittés).
  Future<void> refreshStatus({SyncOutcome? outcome, DateTime? at}) async {
    final pending = await _queue.readAll();
    final notices =
        (await _notices.readAll()).where((n) => !n.acknowledged).toList();
    state = state.copyWith(
      pendingCount: pending.length,
      inProgressCount: 0,
      notices: notices,
      lastSyncOutcome: outcome,
      lastSyncAt: at,
    );
  }

  /// Relance manuelle immédiate (FR-006, bouton « Réessayer »).
  Future<void> syncNow() => sync();

  /// Acquitte un avis : il disparaît de la liste active et est persisté (SC-004).
  Future<void> acknowledgeNotice(String clientOperationId) async {
    await _notices.acknowledge(clientOperationId);
    await refreshStatus();
  }

  /// Un cycle de synchronisation. Réentrance protégée.
  Future<void> sync() async {
    if (_running) return;
    _running = true;
    _retryTimer?.cancel();
    try {
      final all = await _queue.readAll();
      if (all.isEmpty) {
        await refreshStatus(outcome: SyncOutcome.idle);
        return;
      }

      state = state.copyWith(
        lastSyncOutcome: SyncOutcome.running,
        inProgressCount: all.length,
        pendingCount: 0,
      );

      final now = _clock.utcNow();
      final bySession = <int, List<PendingCapture>>{};
      for (final c in all) {
        bySession.putIfAbsent(c.sessionId, () => []).add(c);
      }

      var sessionExpired = false;
      for (final entry in bySession.entries) {
        if (sessionExpired) break;
        await _syncSession(entry.key, entry.value, now, onExpired: () {
          sessionExpired = true;
        });
      }

      final remaining = await _queue.readAll();
      final SyncOutcome outcome;
      if (sessionExpired) {
        outcome = SyncOutcome.failed;
      } else if (remaining.isEmpty) {
        outcome = SyncOutcome.success;
      } else {
        outcome = SyncOutcome.partial;
      }
      developer.log(
        'Cycle de synchro : ${all.length} envoyé(s), ${remaining.length} restant(s), issue $outcome',
        name: 'attendance.sync',
      );
      await refreshStatus(outcome: outcome, at: now);

      if (!sessionExpired) _scheduleRetry(remaining);
    } finally {
      _running = false;
    }
  }

  Future<void> _syncSession(
    int sessionId,
    List<PendingCapture> items,
    DateTime now, {
    required void Function() onExpired,
  }) async {
    try {
      final response =
          await _api.syncBatch(sessionId, items.map(_toItem).toList());
      final byId = {for (final r in response.results) r.clientOperationId: r};
      for (final c in items) {
        final result = byId[c.clientOperationId];
        if (result == null) {
          await _markTransient(c, now);
          continue;
        }
        switch (result.outcome) {
          case OfflineScanOutcome.created:
          case OfflineScanOutcome.alreadyPresent:
            await _queue.remove(c.clientOperationId); // succès (idempotent)
          case OfflineScanOutcome.rejected:
            await _queue.remove(c.clientOperationId);
            await _notices.add(_notice(
              c,
              NoticeKind.rejected,
              result.reason ?? 'Présence refusée.',
              now,
            ));
          default:
            await _markTransient(c, now); // issue inconnue → prudence
        }
      }
    } on ApiException catch (e) {
      if (e.type == ApiErrorType.unauthorized) {
        // 401 : le socle purge la session ; captures conservées, pas d'incrément.
        onExpired();
        return;
      }
      if (e.type == ApiErrorType.network || e.type == ApiErrorType.server) {
        for (final c in items) {
          await _markTransient(c, now); // conservé pour réessai (borné FR-013)
        }
      } else {
        // 400/403/404/409/410/unknown : non transitoire → échec définitif.
        for (final c in items) {
          await _queue.remove(c.clientOperationId);
          await _notices.add(_notice(
            c,
            NoticeKind.permanentlyFailed,
            messageForApiException(e),
            now,
          ));
        }
      }
    }
  }

  /// Incrémente le compteur de tentatives ; passe en échec définitif au plafond
  /// (FR-013), sinon conserve l'élément en `transientFailed`.
  Future<void> _markTransient(PendingCapture c, DateTime now) async {
    final updated = c.copyWith(
      attemptCount: c.attemptCount + 1,
      lastAttemptAt: now,
      state: PendingState.transientFailed,
    );
    if (_backoff.isExhausted(updated, now)) {
      await _queue.remove(c.clientOperationId);
      await _notices.add(_notice(
        c,
        NoticeKind.permanentlyFailed,
        'Non synchronisée après plusieurs tentatives.',
        now,
      ));
      return;
    }
    await _queue.update(updated);
  }

  void _scheduleRetry(List<PendingCapture> remaining) {
    if (remaining.isEmpty) return;
    final minAttempts =
        remaining.map((c) => c.attemptCount).fold<int>(1 << 30, min);
    final delay = _backoff.delayFor(minAttempts);
    _retryTimer?.cancel();
    _retryTimer = Timer(delay, () => unawaited(sync()));
  }

  OfflineScanItem _toItem(PendingCapture c) => OfflineScanItem(
        clientOperationId: c.clientOperationId,
        token: c.token,
        clientArrivalTime: c.clientArrivalTime,
      );

  SyncNotice _notice(
          PendingCapture c, NoticeKind kind, String reason, DateTime now) =>
      SyncNotice(
        clientOperationId: c.clientOperationId,
        sessionId: c.sessionId,
        kind: kind,
        reason: reason,
        occurredAt: now,
      );
}
