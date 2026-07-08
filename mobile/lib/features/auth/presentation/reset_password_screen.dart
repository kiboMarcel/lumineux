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

/// US3 — Réinitialisation via le jeton reçu par e-mail (design Lumineux Mobile).
class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({super.key});

  @override
  ConsumerState<ResetPasswordScreen> createState() =>
      _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _token = TextEditingController();
  final _newPassword = TextEditingController();
  bool _submitting = false;
  bool _done = false;
  String? _error;

  @override
  void dispose() {
    _token.dispose();
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
      await ref.read(authApiProvider).resetPassword(
            ResetPasswordRequest(
              token: _token.text.trim(),
              newPassword: _newPassword.text,
            ),
          );
      setState(() => _done = true);
    } on ApiException catch (e) {
      setState(() =>
          _error = messageForApiException(e, context: ErrorContext.reset));
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
          key: const Key('reset-back'),
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(Routes.login),
        ),
        title: const Text('Réinitialisation'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
          child: _done ? _buildSuccess(context) : _buildForm(context),
        ),
      ),
    );
  }

  Widget _buildSuccess(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Text(
          'Votre mot de passe a été réinitialisé.',
          key: Key('reset-success'),
          style: TextStyle(
              fontSize: 16, fontWeight: FontWeight.w700, color: AppColors.ink),
        ),
        const SizedBox(height: 20),
        LumPrimaryButton(
          key: const Key('reset-to-login'),
          label: 'Se connecter',
          onPressed: () => context.go(Routes.login),
        ),
      ],
    );
  }

  Widget _buildForm(BuildContext context) {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Collez le jeton reçu par e-mail puis choisissez un nouveau mot de '
            'passe.',
            style: TextStyle(fontSize: 14, color: AppColors.ink2, height: 1.5),
          ),
          const SizedBox(height: 16),
          LumField(
            label: 'Jeton de réinitialisation',
            fieldKey: const Key('reset-token'),
            controller: _token,
            enabled: !_submitting,
            validator: (v) =>
                (v == null || v.trim().isEmpty) ? 'Jeton requis' : null,
          ),
          const SizedBox(height: 16),
          LumField(
            label: 'Nouveau mot de passe',
            fieldKey: const Key('reset-new-password'),
            controller: _newPassword,
            enabled: !_submitting,
            obscureText: true,
            validator: (v) => PasswordPolicy.validate(v ?? ''),
          ),
          if (_error != null) ...[
            const SizedBox(height: 16),
            Text(
              _error!,
              key: const Key('reset-error'),
              style: const TextStyle(color: AppColors.danger, fontSize: 13),
            ),
          ],
          const SizedBox(height: 20),
          LumPrimaryButton(
            key: const Key('reset-submit'),
            label: 'Réinitialiser',
            loading: _submitting,
            onPressed: _submit,
          ),
        ],
      ),
    );
  }
}
