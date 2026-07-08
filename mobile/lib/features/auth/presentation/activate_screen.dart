import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/errors/error_messages.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_buttons.dart';
import '../../../core/widgets/lum_field.dart';
import '../../../core/widgets/lum_widgets.dart';
import '../../../routing/app_router.dart';
import '../application/password_policy.dart';
import '../application/providers.dart';

/// US2 — Activation à la première connexion (design Lumineux Mobile).
/// La référence est pré-remplie (lecture seule) si l'activation a été imposée
/// par l'API après un login ; sinon elle est saisissable (accès direct).
class ActivateScreen extends ConsumerStatefulWidget {
  const ActivateScreen({super.key});

  @override
  ConsumerState<ActivateScreen> createState() => _ActivateScreenState();
}

class _ActivateScreenState extends ConsumerState<ActivateScreen> {
  final _formKey = GlobalKey<FormState>();
  final _reference = TextEditingController();
  final _temporary = TextEditingController();
  final _newPassword = TextEditingController();
  final _confirm = TextEditingController();
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _reference.dispose();
    _temporary.dispose();
    _newPassword.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit(String? sessionReference) async {
    if (_submitting) return;
    if (!_formKey.currentState!.validate()) return;
    final reference = sessionReference ?? _reference.text.trim();
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      await ref
          .read(sessionControllerProvider.notifier)
          .activate(reference, _temporary.text, _newPassword.text);
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
    final sessionReference = ref.watch(sessionControllerProvider).reference;

    return Scaffold(
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _header(context),
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Text(
                        'Utilisez le mot de passe temporaire reçu par e-mail pour '
                        'définir votre nouveau mot de passe.',
                        style: TextStyle(
                            fontSize: 14, color: AppColors.ink2, height: 1.5),
                      ),
                      const SizedBox(height: 16),
                      LumField(
                        label: 'Référence',
                        fieldKey: const Key('activate-reference'),
                        controller: sessionReference == null ? _reference : null,
                        initialValue: sessionReference,
                        readOnly: sessionReference != null,
                        enabled: !_submitting,
                        validator: (v) {
                          if (sessionReference != null) return null;
                          return (v == null || v.trim().isEmpty)
                              ? 'Référence requise'
                              : null;
                        },
                      ),
                      const SizedBox(height: 16),
                      LumField(
                        label: 'Mot de passe temporaire',
                        fieldKey: const Key('activate-temporary'),
                        controller: _temporary,
                        enabled: !_submitting,
                        obscureText: true,
                        validator: (v) => (v == null || v.isEmpty)
                            ? 'Mot de passe temporaire requis'
                            : null,
                      ),
                      const SizedBox(height: 16),
                      LumField(
                        label: 'Nouveau mot de passe',
                        fieldKey: const Key('activate-new-password'),
                        controller: _newPassword,
                        enabled: !_submitting,
                        obscureText: true,
                        validator: (v) => PasswordPolicy.validate(v ?? ''),
                      ),
                      const SizedBox(height: 16),
                      LumField(
                        label: 'Confirmer le mot de passe',
                        fieldKey: const Key('activate-confirm'),
                        controller: _confirm,
                        enabled: !_submitting,
                        obscureText: true,
                        validator: (v) => (v != _newPassword.text)
                            ? 'Les mots de passe ne correspondent pas'
                            : null,
                      ),
                      const SizedBox(height: 16),
                      const _RequirementsCard(),
                      if (_error != null) ...[
                        const SizedBox(height: 16),
                        Text(
                          _error!,
                          key: const Key('activate-error'),
                          style: const TextStyle(
                              color: AppColors.danger, fontSize: 13),
                        ),
                      ],
                      const SizedBox(height: 20),
                      LumPrimaryButton(
                        key: const Key('activate-submit'),
                        label: 'Activer mon compte',
                        loading: _submitting,
                        onPressed: () => _submit(sessionReference),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _header(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 18, 20, 6),
      child: Row(
        children: [
          InkWell(
            key: const Key('activate-back'),
            borderRadius: BorderRadius.circular(10),
            onTap: () => context.go(Routes.login),
            child: const SizedBox(
              width: 32,
              height: 32,
              child: Icon(Icons.arrow_back, size: 20, color: AppColors.ink),
            ),
          ),
          const SizedBox(width: 12),
          const Text(
            'Activer votre compte',
            style: TextStyle(
                fontSize: 18, fontWeight: FontWeight.w700, color: AppColors.ink),
          ),
        ],
      ),
    );
  }
}

class _RequirementsCard extends StatelessWidget {
  const _RequirementsCard();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: AppColors.bgOuter,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionLabel('Exigences'),
          const SizedBox(height: 8),
          _rule('8 caractères minimum'),
          const SizedBox(height: 6),
          _rule('Une lettre et un chiffre'),
        ],
      ),
    );
  }

  Widget _rule(String text) {
    return Row(
      children: [
        const Icon(Icons.check, size: 16, color: AppColors.success),
        const SizedBox(width: 8),
        Text(text,
            style: const TextStyle(fontSize: 13, color: AppColors.ink)),
      ],
    );
  }
}
