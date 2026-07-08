import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

/// Champ de saisie du design : label 13/600 au-dessus, champ blanc 48px,
/// radius 12, bordure fine `border` (focus → `primary`).
class LumField extends StatelessWidget {
  const LumField({
    super.key,
    required this.label,
    this.controller,
    this.initialValue,
    this.obscureText = false,
    this.enabled = true,
    this.readOnly = false,
    this.keyboardType,
    this.textInputAction,
    this.validator,
    this.onFieldSubmitted,
    this.fieldKey,
  });

  final String label;
  final TextEditingController? controller;
  final String? initialValue;
  final bool obscureText;
  final bool enabled;
  final bool readOnly;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final String? Function(String?)? validator;
  final void Function(String)? onFieldSubmitted;
  final Key? fieldKey;

  @override
  Widget build(BuildContext context) {
    const radius = BorderRadius.all(Radius.circular(12));
    OutlineInputBorder borderOf(Color color, [double width = 1]) =>
        OutlineInputBorder(
          borderRadius: radius,
          borderSide: BorderSide(color: color, width: width),
        );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: 6),
          child: Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.ink,
            ),
          ),
        ),
        TextFormField(
          key: fieldKey,
          controller: controller,
          initialValue: initialValue,
          obscureText: obscureText,
          enabled: enabled,
          readOnly: readOnly,
          keyboardType: keyboardType,
          textInputAction: textInputAction,
          validator: validator,
          onFieldSubmitted: onFieldSubmitted,
          style: const TextStyle(fontSize: 15, color: AppColors.ink),
          decoration: InputDecoration(
            isDense: true,
            filled: true,
            fillColor: readOnly || !enabled ? AppColors.bgOuter : AppColors.surface,
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
            enabledBorder: borderOf(AppColors.border),
            focusedBorder: borderOf(AppColors.primary, 1.5),
            disabledBorder: borderOf(AppColors.border),
            errorBorder: borderOf(AppColors.danger),
            focusedErrorBorder: borderOf(AppColors.danger, 1.5),
          ),
        ),
      ],
    );
  }
}
