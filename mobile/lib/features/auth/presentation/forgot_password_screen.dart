import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../../../routing/app_router.dart';
import '../application/providers.dart';
import '../data/auth_dtos.dart';

/// US3 — Mot de passe oublié. Message **générique** (anti-énumération)
/// quel que soit le résultat.
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
      appBar: AppBar(title: const Text('Mot de passe oublié')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Saisissez votre référence pour recevoir un lien de réinitialisation.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  key: const Key('forgot-reference'),
                  controller: _reference,
                  enabled: !_submitting,
                  decoration: const InputDecoration(
                    labelText: 'Référence',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) => (v == null || v.trim().isEmpty)
                      ? 'Référence requise'
                      : null,
                ),
                if (_message != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _message!,
                    key: const Key('forgot-message'),
                    style: TextStyle(
                        color: Theme.of(context).colorScheme.primary),
                  ),
                ],
                if (_error != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    _error!,
                    key: const Key('forgot-error'),
                    style: TextStyle(color: Theme.of(context).colorScheme.error),
                  ),
                ],
                const SizedBox(height: 24),
                FilledButton(
                  key: const Key('forgot-submit'),
                  onPressed: _submitting ? null : _submit,
                  child: _submitting
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Envoyer'),
                ),
                const SizedBox(height: 8),
                TextButton(
                  key: const Key('forgot-to-reset'),
                  onPressed:
                      _submitting ? null : () => context.go(Routes.reset),
                  child: const Text('J\'ai déjà un jeton de réinitialisation'),
                ),
                TextButton(
                  key: const Key('forgot-to-login'),
                  onPressed:
                      _submitting ? null : () => context.go(Routes.login),
                  child: const Text('Retour à la connexion'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
