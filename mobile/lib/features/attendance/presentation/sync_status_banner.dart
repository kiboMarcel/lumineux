import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../application/providers.dart';
import '../application/sync_controller.dart';
import '../application/sync_notice.dart';
import '../application/sync_state.dart';

/// Indicateur d'état de synchronisation (FR-011, SC-004/SC-006).
///
/// Affiche les compteurs (en attente / en cours), les **avis de rejet/échec**
/// avec leur raison (acquittables), et un bouton **Réessayer** (relance
/// manuelle). Masqué quand il n'y a rien à montrer. Aucun jeton n'est affiché.
class SyncStatusBanner extends ConsumerWidget {
  const SyncStatusBanner({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final status = ref.watch(syncControllerProvider);
    if (status.isEmpty) return const SizedBox.shrink();

    final notifier = ref.read(syncControllerProvider.notifier);
    final running = status.lastSyncOutcome == SyncOutcome.running ||
        status.inProgressCount > 0;

    return Container(
      key: const Key('sync-status-banner'),
      margin: const EdgeInsets.fromLTRB(20, 0, 20, 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        border: Border.all(color: AppColors.border),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _header(status, running, notifier),
          for (final notice in status.notices) ...[
            const SizedBox(height: 8),
            _NoticeTile(
              notice: notice,
              onAck: () => notifier.acknowledgeNotice(notice.clientOperationId),
            ),
          ],
        ],
      ),
    );
  }

  Widget _header(SyncStatus status, bool running, SyncController notifier) {
    if (running) {
      return const Row(
        children: [
          SizedBox(
            width: 16,
            height: 16,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
          SizedBox(width: 10),
          Text('Synchronisation…',
              style: TextStyle(fontSize: 14, color: AppColors.ink)),
        ],
      );
    }

    if (status.pendingCount > 0) {
      final label = status.pendingCount == 1
          ? '1 présence à synchroniser'
          : '${status.pendingCount} présences à synchroniser';
      return Row(
        children: [
          const Icon(Icons.cloud_upload_outlined,
              size: 18, color: AppColors.primary),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              label,
              key: const Key('sync-pending-count'),
              style: const TextStyle(fontSize: 14, color: AppColors.ink),
            ),
          ),
          TextButton(
            key: const Key('sync-retry'),
            onPressed: notifier.syncNow,
            child: const Text('Réessayer'),
          ),
        ],
      );
    }

    // Aucune capture en attente mais des avis subsistent.
    return const Text('Synchronisation à jour',
        style: TextStyle(fontSize: 14, color: AppColors.ink2));
  }
}

class _NoticeTile extends StatelessWidget {
  const _NoticeTile({required this.notice, required this.onAck});

  final SyncNotice notice;
  final VoidCallback onAck;

  @override
  Widget build(BuildContext context) {
    final title = notice.kind == NoticeKind.rejected
        ? 'Présence refusée'
        : 'Non synchronisée';
    return Container(
      key: Key('sync-notice-${notice.clientOperationId}'),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: AppColors.dangerSoft,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.error_outline, size: 18, color: AppColors.danger),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title,
                    style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w700,
                        color: AppColors.ink)),
                const SizedBox(height: 2),
                Text(notice.reason,
                    style: const TextStyle(fontSize: 12, color: AppColors.ink2)),
              ],
            ),
          ),
          TextButton(
            key: Key('sync-notice-ack-${notice.clientOperationId}'),
            onPressed: onAck,
            child: const Text('J\'ai compris'),
          ),
        ],
      ),
    );
  }
}
