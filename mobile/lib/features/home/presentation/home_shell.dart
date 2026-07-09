import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../attendance/presentation/scanner_screen.dart';
import 'home_tab.dart';
import 'profile_tab.dart';

/// Coquille des écrans principaux (post-authentification) : contenu par onglet
/// + barre de navigation basse. Périmètre membre → 3 onglets (Accueil, Scanner,
/// Profil) ; les fonctions bureau restent hors de l'app membre.
class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _index = 0;

  static const _tabs = [HomeTab(), ScannerScreen(), ProfileTab()];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(index: _index, children: _tabs),
      bottomNavigationBar: _BottomNav(
        index: _index,
        onSelect: (i) => setState(() => _index = i),
      ),
    );
  }
}

class _BottomNav extends StatelessWidget {
  const _BottomNav({required this.index, required this.onSelect});

  final int index;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.bg,
        border: Border(top: BorderSide(color: AppColors.border)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(8, 8, 8, 6),
          child: Row(
            children: [
              _NavItem(
                itemKey: const Key('nav-home'),
                label: 'Accueil',
                icon: Icons.home_rounded,
                selected: index == 0,
                onTap: () => onSelect(0),
              ),
              _NavItem(
                itemKey: const Key('nav-scanner'),
                label: 'Scanner',
                icon: Icons.qr_code_scanner_rounded,
                selected: index == 1,
                onTap: () => onSelect(1),
              ),
              _NavItem(
                itemKey: const Key('nav-profile'),
                label: 'Profil',
                icon: Icons.person_rounded,
                selected: index == 2,
                onTap: () => onSelect(2),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.itemKey,
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  final Key itemKey;
  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = selected ? AppColors.primary : AppColors.ink3;
    return Expanded(
      child: InkWell(
        key: itemKey,
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 22, color: color),
              const SizedBox(height: 4),
              Text(
                label,
                style: TextStyle(
                    fontSize: 11, fontWeight: FontWeight.w600, color: color),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
