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

/// US1 — Connexion (design Lumineux Mobile).
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _reference = TextEditingController();
  final _password = TextEditingController();
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _reference.dispose();
    _password.dispose();
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
      await ref
          .read(sessionControllerProvider.notifier)
          .login(_reference.text.trim(), _password.text);
    } on ApiException catch (e) {
      setState(() =>
          _error = messageForApiException(e, context: ErrorContext.login));
    } catch (_) {
      setState(() => _error = 'Une erreur est survenue.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 32),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _brand(context),
                  const SizedBox(height: 28),
                  LumField(
                    label: 'Identifiant',
                    fieldKey: const Key('login-reference'),
                    controller: _reference,
                    enabled: !_submitting,
                    textInputAction: TextInputAction.next,
                    validator: (v) => (v == null || v.trim().isEmpty)
                        ? 'Référence requise'
                        : null,
                  ),
                  const SizedBox(height: 14),
                  LumField(
                    label: 'Mot de passe',
                    fieldKey: const Key('login-password'),
                    controller: _password,
                    enabled: !_submitting,
                    obscureText: true,
                    textInputAction: TextInputAction.done,
                    onFieldSubmitted: (_) => _submit(),
                    validator: (v) =>
                        (v == null || v.isEmpty) ? 'Mot de passe requis' : null,
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 14),
                    Text(
                      _error!,
                      key: const Key('login-error'),
                      style: const TextStyle(
                          color: AppColors.danger, fontSize: 13),
                    ),
                  ],
                  const SizedBox(height: 20),
                  LumPrimaryButton(
                    key: const Key('login-submit'),
                    label: 'Se connecter',
                    loading: _submitting,
                    onPressed: _submit,
                  ),
                  const SizedBox(height: 10),
                  Center(
                    child: TextButton(
                      key: const Key('login-forgot-link'),
                      onPressed:
                          _submitting ? null : () => context.go(Routes.forgot),
                      child: const Text(
                        'Mot de passe oublié ?',
                        style: TextStyle(color: AppColors.primary, fontSize: 13),
                      ),
                    ),
                  ),
                  const SizedBox(height: 18),
                  _separator('Première connexion'),
                  const SizedBox(height: 18),
                  LumOutlineButton(
                    key: const Key('login-activate-link'),
                    label: 'Activer mon compte',
                    onPressed:
                        _submitting ? null : () => context.go(Routes.activate),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _brand(BuildContext context) {
    return Column(
      children: [
        Container(
          width: 64,
          height: 64,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: AppColors.primary,
            borderRadius: BorderRadius.circular(20),
          ),
          child: const Text(
            'L',
            style: TextStyle(
                color: Colors.white, fontSize: 28, fontWeight: FontWeight.w800),
          ),
        ),
        const SizedBox(height: 14),
        const Text(
          'Lumineux',
          style: TextStyle(
              fontSize: 24, fontWeight: FontWeight.w800, color: AppColors.ink),
        ),
        const SizedBox(height: 4),
        const Text(
          'Gestion des présences',
          style: TextStyle(fontSize: 14, color: AppColors.ink2),
        ),
      ],
    );
  }

  Widget _separator(String label) {
    return Row(
      children: [
        const Expanded(child: Divider(color: AppColors.border, height: 1)),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 10),
          child: Text(
            label,
            style: const TextStyle(color: AppColors.inkFaint, fontSize: 12),
          ),
        ),
        const Expanded(child: Divider(color: AppColors.border, height: 1)),
      ],
    );
  }
}
