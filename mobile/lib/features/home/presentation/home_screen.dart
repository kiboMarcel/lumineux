import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../routing/app_router.dart';
import '../../auth/application/providers.dart';

/// US1/US4 — Accueil authentifié. Affiche l'identité du membre et donne accès
/// aux actions compte (changement de mot de passe, déconnexion).
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    final user = session.user;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Accueil'),
        actions: [
          IconButton(
            key: const Key('home-logout'),
            tooltip: 'Se déconnecter',
            icon: const Icon(Icons.logout),
            onPressed: () =>
                ref.read(sessionControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Bonjour,',
                style: Theme.of(context).textTheme.titleMedium,
              ),
              const SizedBox(height: 4),
              Text(
                user?.displayName ?? '',
                key: const Key('home-display-name'),
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 32),
              FilledButton.tonalIcon(
                key: const Key('home-change-password'),
                icon: const Icon(Icons.password),
                label: const Text('Changer mon mot de passe'),
                onPressed: () => context.go(Routes.changePassword),
              ),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                key: const Key('home-logout-button'),
                icon: const Icon(Icons.logout),
                label: const Text('Se déconnecter'),
                onPressed: () =>
                    ref.read(sessionControllerProvider.notifier).logout(),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
