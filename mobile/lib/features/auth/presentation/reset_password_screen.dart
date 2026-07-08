import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../../../routing/app_router.dart';
import '../application/password_policy.dart';
import '../application/providers.dart';
import '../data/auth_dtos.dart';

/// US3 — Réinitialisation via le jeton reçu par e-mail (saisie/collage).
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
      appBar: AppBar(title: const Text('Réinitialisation')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: _done ? _buildSuccess(context) : _buildForm(context),
        ),
      ),
    );
  }

  Widget _buildSuccess(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Votre mot de passe a été réinitialisé.',
          key: const Key('reset-success'),
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 24),
        FilledButton(
          key: const Key('reset-to-login'),
          onPressed: () => context.go(Routes.login),
          child: const Text('Se connecter'),
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
          Text(
            'Collez le jeton reçu par e-mail puis choisissez un nouveau mot de passe.',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 16),
          TextFormField(
            key: const Key('reset-token'),
            controller: _token,
            enabled: !_submitting,
            decoration: const InputDecoration(
              labelText: 'Jeton de réinitialisation',
              border: OutlineInputBorder(),
            ),
            validator: (v) =>
                (v == null || v.trim().isEmpty) ? 'Jeton requis' : null,
          ),
          const SizedBox(height: 16),
          TextFormField(
            key: const Key('reset-new-password'),
            controller: _newPassword,
            enabled: !_submitting,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Nouveau mot de passe',
              border: OutlineInputBorder(),
            ),
            validator: (v) => PasswordPolicy.validate(v ?? ''),
          ),
          if (_error != null) ...[
            const SizedBox(height: 16),
            Text(
              _error!,
              key: const Key('reset-error'),
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ],
          const SizedBox(height: 24),
          FilledButton(
            key: const Key('reset-submit'),
            onPressed: _submitting ? null : _submit,
            child: _submitting
                ? const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Réinitialiser'),
          ),
        ],
      ),
    );
  }
}
