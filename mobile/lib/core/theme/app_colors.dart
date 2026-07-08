import 'package:flutter/material.dart';

/// Jetons de couleur du design Lumineux Mobile (haute fidélité, figés).
/// Voir `template_mobile/README.md` — ne pas réinterpréter la palette.
class AppColors {
  const AppColors._();

  static const Color bg = Color(0xFFFAF7F2); // Fond d'écran
  static const Color bgOuter = Color(0xFFF1ECE3); // Fond neutre secondaire
  static const Color surface = Color(0xFFFFFFFF); // Cartes, champs, feuilles
  static const Color border = Color(0xFFECE7DE); // Bordures fines 1px
  static const Color ink = Color(0xFF221F1A); // Texte principal
  static const Color ink2 = Color(0xFF6B675F); // Texte secondaire
  static const Color ink3 = Color(0xFFADA89D); // Icônes inactifs, chevrons
  static const Color inkFaint = Color(0xFF8A8478); // Sur-titres discrets

  static const Color primary = Color(0xFF3B4FCC); // Indigo
  static const Color primaryDark = Color(0xFF2C3AA0); // Hover/pressed
  static const Color primarySoft = Color(0xFFE7E9FB); // Fond badges profils

  static const Color accentWarm = Color(0xFFD97A3F); // Terracotta (avatars membres)
  static const Color accentWarmSoft = Color(0xFFFBE7D6); // Fond badge rôle Bureau
  static const Color accentWarmText = Color(0xFFB85C22); // Texte badge Bureau

  static const Color success = Color(0xFF2F8F5B); // Actif / Validé
  static const Color successSoft = Color(0xFFE1F3E7);
  static const Color danger = Color(0xFFC1483A); // Inactif / Annulé / destructif
  static const Color dangerSoft = Color(0xFFFBE4E0);
}
