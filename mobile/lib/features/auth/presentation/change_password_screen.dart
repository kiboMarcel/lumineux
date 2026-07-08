import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_buttons.dart';
import '../../../core/widgets/lum_field.dart';
import '../../../routing/app_router.dart';
import '../application/password_policy.dart';
import '../application/providers.dart';
import '../data/auth_dtos.dart';

/// US4 — Changement de mot de passe (design Lumineux Mobile).
class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  ConsumerState<ChangePasswordScreen> createState() =>
      _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends ConsumerState<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _newPassword = TextEditingController();
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _current.dispose();
    _newPassword.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      await ref.read(authApiProvider).changePassword(
            ChangePasswordRequest(
              currentPassword: _current.text,
              newPassword: _newPassword.text,
            ),
          );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mot de passe modifié.')),
      );
      context.go(Routes.home);
    } on ApiException catch (e) {
      setState(() => _error = messageForApiException(e));
    } catch (_) {
      setState(() => _error = 'Une erreur est survenue.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          key: const Key('change-back'),
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(Routes.home),
        ),
        title: const Text('Changer le mot de passe'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                LumField(
                  label: 'Mot de passe actuel',
                  fieldKey: const Key('change-current'),
                  controller: _current,
                  enabled: !_submitting,
                  obscureText: true,
                  validator: (v) => (v == null || v.isEmpty)
                      ? 'Mot de passe actuel requis'
                      : null,
                ),
                const SizedBox(height: 16),
                LumField(
                  label: 'Nouveau mot de passe',
                  fieldKey: const Key('change-new-password'),
                  controller: _newPassword,
                  enabled: !_submitting,
                  obscureText: true,
                  validator: (v) => PasswordPolicy.validate(v ?? ''),
                ),
                if (_error != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _error!,
                    key: const Key('change-error'),
                    style:
                        const TextStyle(color: AppColors.danger, fontSize: 13),
                  ),
                ],
                const SizedBox(height: 20),
                LumPrimaryButton(
                  key: const Key('change-submit'),
                  label: 'Enregistrer',
                  loading: _submitting,
                  onPressed: _submit,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
