import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_widgets.dart';
import '../../auth/application/permissions.dart';
import '../../auth/application/providers.dart';

/// Onglet Accueil : salutation + badge de rôle effectif.
///
/// Le bloc « session ouverte » et l'historique « mes présences » du design
/// dépendent d'endpoints hors périmètre M0 ; un état vide est présenté à la place.
class HomeTab extends ConsumerWidget {
  const HomeTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(sessionControllerProvider).user;
    final displayName = user?.displayName ?? '';
    final firstName =
        displayName.isEmpty ? '' : displayName.split(RegExp(r'\s+')).first;
    final permissions = user?.permissions ?? const <String>[];
    final bureau = hasManagementRights(permissions);

    return SafeArea(
      bottom: false,
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        firstName.isEmpty ? 'Bonjour' : 'Bonjour, $firstName',
                        key: const Key('home-display-name'),
                        style: const TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                            color: AppColors.ink),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        bureau ? 'Membre du bureau' : 'Membre',
                        style: const TextStyle(
                            fontSize: 13, color: AppColors.ink2),
                      ),
                    ],
                  ),
                ),
                LumPill(
                  text: roleLabel(permissions),
                  background:
                      bureau ? AppColors.accentWarmSoft : AppColors.primarySoft,
                  foreground:
                      bureau ? AppColors.accentWarmText : AppColors.primary,
                ),
              ],
            ),
            const SizedBox(height: 20),
            const SectionLabel('Mes présences récentes'),
            const SizedBox(height: 10),
            const LumCard(
              child: Text(
                'Aucune présence à afficher pour le moment.',
                style: TextStyle(fontSize: 14, color: AppColors.ink2),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
