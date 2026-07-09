import 'sync_notice.dart';

/// Résultat du dernier cycle de synchronisation (data-model.md §4).
enum SyncOutcome { idle, running, success, partial, failed }

/// Agrégat d'état présenté au membre (FR-011, SC-006). Non persisté : dérivé de
/// la file et du store d'avis.
class SyncStatus {
  const SyncStatus({
    this.pendingCount = 0,
    this.inProgressCount = 0,
    this.notices = const [],
    this.lastSyncAt,
    this.lastSyncOutcome = SyncOutcome.idle,
  });

  /// Captures en attente (états `pending` + `transientFailed`).
  final int pendingCount;

  /// Captures en cours de synchronisation.
  final int inProgressCount;

  /// Avis non acquittés (rejets + échecs définitifs).
  final List<SyncNotice> notices;

  final DateTime? lastSyncAt;
  final SyncOutcome lastSyncOutcome;

  /// Rien à afficher : aucune capture, aucun avis.
  bool get isEmpty =>
      pendingCount == 0 && inProgressCount == 0 && notices.isEmpty;

  SyncStatus copyWith({
    int? pendingCount,
    int? inProgressCount,
    List<SyncNotice>? notices,
    DateTime? lastSyncAt,
    SyncOutcome? lastSyncOutcome,
  }) =>
      SyncStatus(
        pendingCount: pendingCount ?? this.pendingCount,
        inProgressCount: inProgressCount ?? this.inProgressCount,
        notices: notices ?? this.notices,
        lastSyncAt: lastSyncAt ?? this.lastSyncAt,
        lastSyncOutcome: lastSyncOutcome ?? this.lastSyncOutcome,
      );
}
