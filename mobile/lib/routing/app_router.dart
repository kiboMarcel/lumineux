import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/application/providers.dart';
import '../features/auth/application/session_state.dart';
import '../features/auth/presentation/activate_screen.dart';
import '../features/auth/presentation/change_password_screen.dart';
import '../features/auth/presentation/forgot_password_screen.dart';
import '../features/auth/presentation/login_screen.dart';
import '../features/auth/presentation/reset_password_screen.dart';
import '../features/home/presentation/home_shell.dart';
import '../features/home/presentation/splash_screen.dart';

/// Chemins de routes.
class Routes {
  const Routes._();
  static const String splash = '/splash';
  static const String login = '/login';
  static const String activate = '/auth/activate';
  static const String forgot = '/auth/forgot';
  static const String reset = '/auth/reset';
  static const String home = '/home';
  static const String changePassword = '/account/change-password';
}

/// Routes anonymes atteignables explicitement même sans session
/// (dont l'activation directe depuis « Première connexion »).
const Set<String> _anonymousRoutes = {
  Routes.login,
  Routes.activate,
  Routes.forgot,
  Routes.reset,
};

/// Routeur `go_router` avec **redirection globale** pilotée par l'état de
/// session (garde équivalente aux `guards` Angular du socle SPA).
final routerProvider = Provider<GoRouter>((ref) {
  final refresh = _SessionRefresh(ref);
  ref.onDispose(refresh.dispose);

  return GoRouter(
    initialLocation: Routes.splash,
    refreshListenable: refresh,
    redirect: (context, state) {
      final session = ref.read(sessionControllerProvider);
      final location = state.matchedLocation;

      switch (session.status) {
        case SessionStatus.unknown:
        case SessionStatus.restoring:
          return location == Routes.splash ? null : Routes.splash;

        case SessionStatus.passwordChangeRequired:
          return location == Routes.activate ? null : Routes.activate;

        case SessionStatus.anonymous:
          if (_anonymousRoutes.contains(location)) return null;
          return Routes.login;

        case SessionStatus.authenticated:
          if (location == Routes.home || location == Routes.changePassword) {
            return null;
          }
          return Routes.home;
      }
    },
    routes: [
      GoRoute(
        path: Routes.splash,
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: Routes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: Routes.activate,
        builder: (context, state) => const ActivateScreen(),
      ),
      GoRoute(
        path: Routes.forgot,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: Routes.reset,
        builder: (context, state) => const ResetPasswordScreen(),
      ),
      GoRoute(
        path: Routes.home,
        builder: (context, state) => const HomeShell(),
      ),
      GoRoute(
        path: Routes.changePassword,
        builder: (context, state) => const ChangePasswordScreen(),
      ),
    ],
  );
});

/// Ponte l'état Riverpod de session vers `refreshListenable` de go_router.
class _SessionRefresh extends ChangeNotifier {
  _SessionRefresh(Ref ref) {
    _sub = ref.listen<SessionState>(
      sessionControllerProvider,
      (_, _) => notifyListeners(),
      fireImmediately: false,
    );
  }

  late final ProviderSubscription<SessionState> _sub;

  @override
  void dispose() {
    _sub.close();
    super.dispose();
  }
}
