import 'package:flutter/material.dart';

/// Écran de chargement affiché pendant la restauration de session
/// (états `unknown` / `restoring`). Aucune donnée protégée n'y est visible.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Image(
              image: AssetImage('assets/images/logo-icon.png'),
              width: 88,
              height: 88,
            ),
            SizedBox(height: 24),
            CircularProgressIndicator(),
            SizedBox(height: 16),
            Text('Chargement…'),
          ],
        ),
      ),
    );
  }
}
