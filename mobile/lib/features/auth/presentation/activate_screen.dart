import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../application/password_policy.dart';
import '../application/providers.dart';

/// US2 — Activation à la première connexion. Référence pré-remplie
/// (lecture seule) ; définition d'un nouveau mot de passe conforme.
class ActivateScreen extends ConsumerStatefulWidget {
  const ActivateScreen({super.key});

  @override
  ConsumerState<ActivateScreen> createState() => _ActivateScreenState();
}

class _ActivateScreenState extends ConsumerState<ActivateScreen> {
  final _formKey = GlobalKey<FormState>();
  final _temporary = TextEditingController();
  final _newPassword = TextEditingController();
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _temporary.dispose();
    _newPassword.dispose();
    super.dispose();
  }

  Future<void> _submit(String reference) async {
    if (_submitting) return;
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      await ref.read(sessionControllerProvider.notifier).activate(
            reference,
            _temporary.text,
            _newPassword.text,
          );
      // Succès → la garde de session redirige vers l'accueil.
    } on ApiException catch (e) {
      setState(() =>
          _error = messageForApiException(e, context: ErrorContext.activate));
    } catch (_) {
      setState(() => _error = 'Une erreur est survenue.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final reference = ref.watch(sessionControllerProvider).reference ?? '';

    return Scaffold(
      appBar: AppBar(title: const Text('Activation du compte')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Définissez votre nouveau mot de passe pour activer votre compte.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  key: const Key('activate-reference'),
                  initialValue: reference,
                  readOnly: true,
                  enabled: false,
                  decoration: const InputDecoration(
                    labelText: 'Référence',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  key: const Key('activate-temporary'),
                  controller: _temporary,
                  enabled: !_submitting,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Mot de passe temporaire',
                    border: OutlineInputBorder(),
                  ),
                  validator: (v) => (v == null || v.isEmpty)
                      ? 'Mot de passe temporaire requis'
                      : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  key: const Key('activate-new-password'),
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
                    key: const Key('activate-error'),
                    style: TextStyle(color: Theme.of(context).colorScheme.error),
                  ),
                ],
                const SizedBox(height: 24),
                FilledButton(
                  key: const Key('activate-submit'),
                  onPressed: _submitting ? null : () => _submit(reference),
                  child: _submitting
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text('Activer mon compte'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
