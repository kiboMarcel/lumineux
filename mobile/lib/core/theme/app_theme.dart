import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Thème Material 3 du design Lumineux Mobile.
///
/// La police cible est **Manrope** (poids 400/500/600/700/800). Tant qu'elle
/// n'est pas embarquée/branchée, [fontFamily] reste `null` (police système) —
/// les tailles et graisses sont respectées quel que soit le typeface.
class AppTheme {
  const AppTheme._();

  /// Police du design : Manrope (police variable embarquée dans les assets).
  static const String fontFamily = 'Manrope';

  static ThemeData light() {
    const scheme = ColorScheme.light(
      primary: AppColors.primary,
      onPrimary: Colors.white,
      surface: AppColors.surface,
      onSurface: AppColors.ink,
      error: AppColors.danger,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: AppColors.bg,
      fontFamily: fontFamily,
      splashFactory: InkRipple.splashFactory,
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.bg,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
        foregroundColor: AppColors.ink,
        titleTextStyle: TextStyle(
          color: AppColors.ink,
          fontSize: 18,
          fontWeight: FontWeight.w700,
        ),
      ),
      textTheme: const TextTheme(
        headlineSmall: TextStyle(
            fontSize: 24, fontWeight: FontWeight.w800, color: AppColors.ink),
        titleLarge: TextStyle(
            fontSize: 20, fontWeight: FontWeight.w800, color: AppColors.ink),
        titleMedium: TextStyle(
            fontSize: 16, fontWeight: FontWeight.w700, color: AppColors.ink),
        bodyMedium: TextStyle(
            fontSize: 14, fontWeight: FontWeight.w400, color: AppColors.ink2),
        labelLarge: TextStyle(
            fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.ink),
      ),
      materialTapTargetSize: MaterialTapTargetSize.padded,
    );
  }
}
