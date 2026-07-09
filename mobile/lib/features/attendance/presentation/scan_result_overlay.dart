import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_buttons.dart';
import '../application/scan_state.dart';

/// Overlay **modal** de résultat de scan (aller-retour API) : succès / déjà
/// présente / erreur. Suspend la détection jusqu'à sa fermeture (FR-005/FR-014).
class ScanResultOverlay extends StatelessWidget {
  const ScanResultOverlay({
    super.key,
    required this.result,
    required this.onDismiss,
  });

  final ScanResultView result;
  final VoidCallback onDismiss;

  @override
  Widget build(BuildContext context) {
    final isError = result.isError;
    final isOffline = result.kind == ScanResultKind.offlineQueued;
    // Hors ligne : ton neutre (indigo) — ce n'est pas une erreur (FR-001).
    final Color badgeBg = isError
        ? AppColors.dangerSoft
        : isOffline
            ? AppColors.primarySoft
            : AppColors.successSoft;
    final Color badgeFg = isError
        ? AppColors.danger
        : isOffline
            ? AppColors.primary
            : AppColors.success;
    final IconData icon = isError
        ? Icons.error_outline
        : isOffline
            ? Icons.cloud_off_outlined
            : Icons.check;

    return Positioned.fill(
      child: ColoredBox(
        color: const Color(0x8C221F1A), // rgba(34,31,26,0.55)
        child: Center(
          child: Container(
            width: 280,
            margin: const EdgeInsets.symmetric(horizontal: 24),
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(20),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 56,
                  height: 56,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(color: badgeBg, shape: BoxShape.circle),
                  child: Icon(icon, color: badgeFg, size: 28),
                ),
                const SizedBox(height: 12),
                Text(
                  result.title,
                  key: const Key('scan-result-title'),
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppColors.ink),
                ),
                if (result.subtitle != null) ...[
                  const SizedBox(height: 6),
                  Text(
                    result.subtitle!,
                    key: const Key('scan-result-subtitle'),
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 13, color: AppColors.ink2),
                  ),
                ],
                const SizedBox(height: 20),
                LumPrimaryButton(
                  key: const Key('scan-result-dismiss'),
                  label: isError ? 'Scanner à nouveau' : 'Fermer',
                  onPressed: onDismiss,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
