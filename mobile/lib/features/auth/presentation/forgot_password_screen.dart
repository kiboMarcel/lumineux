import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_buttons.dart';
import '../../../core/widgets/lum_field.dart';
import '../../../routing/app_router.dart';
import '../application/providers.dart';
import '../data/auth_dtos.dart';

/// US3 — Mot de passe oublié (design Lumineux Mobile). Message générique.
class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() =>
      _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _reference = TextEditingController();
  bool _submitting = false;
  String? _message;
  String? _error;

  @override
  void dispose() {
    _reference.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _submitting = true;
      _error = null;
      _message = null;
    });
    try {
      final message = await ref.read(authApiProvider).forgotPassword(
            ForgotPasswordRequest(reference: _reference.text.trim()),
          );
      setState(() => _message = message);
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
          key: const Key('forgot-back'),
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(Routes.login),
        ),
        title: const Text('Mot de passe oublié'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Saisissez votre référence pour recevoir un lien de '
                  'réinitialisation.',
                  style: TextStyle(
                      fontSize: 14, color: AppColors.ink2, height: 1.5),
                ),
                const SizedBox(height: 16),
                LumField(
                  label: 'Référence',
                  fieldKey: const Key('forgot-reference'),
                  controller: _reference,
                  enabled: !_submitting,
                  validator: (v) => (v == null || v.trim().isEmpty)
                      ? 'Référence requise'
                      : null,
                ),
                if (_message != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _message!,
                    key: const Key('forgot-message'),
                    style: const TextStyle(
                        color: AppColors.primary, fontSize: 14),
                  ),
                ],
                if (_error != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _error!,
                    key: const Key('forgot-error'),
                    style:
                        const TextStyle(color: AppColors.danger, fontSize: 13),
                  ),
                ],
                const SizedBox(height: 20),
                LumPrimaryButton(
                  key: const Key('forgot-submit'),
                  label: 'Envoyer',
                  loading: _submitting,
                  onPressed: _submit,
                ),
                const SizedBox(height: 10),
                Center(
                  child: TextButton(
                    key: const Key('forgot-to-reset'),
                    onPressed:
                        _submitting ? null : () => context.go(Routes.reset),
                    child: const Text(
                      'J\'ai déjà un jeton de réinitialisation',
                      style: TextStyle(color: AppColors.primary, fontSize: 13),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
