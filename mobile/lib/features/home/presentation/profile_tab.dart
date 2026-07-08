import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_buttons.dart';
import '../../../core/widgets/lum_widgets.dart';
import '../../../routing/app_router.dart';
import '../../auth/application/permissions.dart';
import '../../auth/application/providers.dart';

/// Onglet Profil : identité, droits effectifs (`GET /auth/me`), actions compte.
class ProfileTab extends ConsumerWidget {
  const ProfileTab({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(sessionControllerProvider).user;
    final displayName = user?.displayName ?? '';
    final permissions = user?.permissions ?? const <String>[];
    final labels = permissionLabels(permissions);

    return SafeArea(
      bottom: false,
      child: SingleChildScrollView(
        padding: const EdgeInsets.only(bottom: 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const SizedBox(height: 20),
            Center(
              child: Column(
                children: [
                  LumAvatar(
                    initials: initialsOf(displayName),
                    size: 76,
                    fontSize: 26,
                  ),
                  const SizedBox(height: 10),
                  Text(
                    displayName,
                    key: const Key('profile-display-name'),
                    style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                        color: AppColors.ink),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    roleLabel(permissions),
                    style: const TextStyle(fontSize: 13, color: AppColors.ink2),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: LumCard(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 6),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SectionLabel('Droits'),
                    const SizedBox(height: 4),
                    if (labels.isEmpty)
                      _rightRow('Aucun droit de gestion', muted: true)
                    else
                      ...labels.map((l) => _rightRow(l)),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: LumCard(
                padding: EdgeInsets.zero,
                child: InkWell(
                  key: const Key('profile-change-password'),
                  borderRadius: BorderRadius.circular(16),
                  onTap: () => context.go(Routes.changePassword),
                  child: const Padding(
                    padding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                    child: Row(
                      children: [
                        Icon(Icons.lock_outline,
                            size: 20, color: AppColors.ink2),
                        SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            'Changer mon mot de passe',
                            style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w600,
                                color: AppColors.ink),
                          ),
                        ),
                        Icon(Icons.chevron_right, color: AppColors.ink3),
                      ],
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: LumOutlineButton(
                key: const Key('profile-logout'),
                label: 'Se déconnecter',
                color: AppColors.danger,
                icon: Icons.logout,
                onPressed: () =>
                    ref.read(sessionControllerProvider.notifier).logout(),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _rightRow(String label, {bool muted = false}) {
    return Container(
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: AppColors.bgOuter)),
      ),
      padding: const EdgeInsets.symmetric(vertical: 9),
      child: Row(
        children: [
          Container(
            width: 20,
            height: 20,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: muted ? AppColors.bgOuter : AppColors.successSoft,
              shape: BoxShape.circle,
            ),
            child: Icon(
              muted ? Icons.remove : Icons.check,
              size: 12,
              color: muted ? AppColors.ink3 : AppColors.success,
            ),
          ),
          const SizedBox(width: 10),
          Text(label,
              style: const TextStyle(fontSize: 13, color: AppColors.ink)),
        ],
      ),
    );
  }
}
