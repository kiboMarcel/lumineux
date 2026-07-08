import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/network/token_holder.dart';
import '../../../core/storage/secure_token_store.dart';
import '../data/auth_api.dart';
import '../data/auth_dtos.dart';
import 'providers.dart';
import 'session_state.dart';

/// Orchestre le cycle de vie de la session (restauration, connexion,
/// activation, expiration, déconnexion). Aucune règle métier : l'API décide,
/// le contrôleur oriente le parcours.
class SessionController extends Notifier<SessionState> {
  late final AuthApi _api;
  late final SecureTokenStore _store;
  late final TokenHolder _holder;

  @override
  SessionState build() {
    _api = ref.read(authApiProvider);
    _store = ref.read(secureTokenStoreProvider);
    _holder = ref.read(tokenHolderProvider);
    // Filet de sécurité : un 401 en cours d'usage purge la session (FR-009).
    _holder.onUnauthorized = _onUnauthorized;
    return const SessionState.unknown();
  }

  /// Au lancement : restaure une session si le jeton du coffre est valide.
  Future<void> restore() async {
    state = const SessionState.restoring();
    try {
      final token = await _store.read();
      if (token == null || !token.isPotentiallyValid) {
        await _purge();
        return;
      }
      _holder.current = token;
      final user = await _api.me();
      state = SessionState.authenticated(user);
    } on ApiException catch (e) {
      // 401 → jeton invalide : purge. Autre (réseau) → pas de session confirmée.
      await _purge(
        message: e.type == ApiErrorType.unauthorized
            ? null
            : 'Impossible de restaurer la session.',
      );
    } catch (_) {
      await _purge();
    }
  }

  /// Connexion (US1). Sur `403 password_change_required`, bascule en
  /// activation (référence pré-remplie) sans lever d'erreur. Les autres
  /// erreurs sont relancées pour affichage par l'écran.
  Future<void> login(String reference, String password) async {
    try {
      final tokens =
          await _api.login(LoginRequest(reference: reference, password: password));
      await _establish(tokens);
    } on ApiException catch (e) {
      if (e.type == ApiErrorType.forbidden &&
          e.code == 'password_change_required') {
        state = SessionState.passwordChangeRequired(reference);
        return;
      }
      rethrow;
    }
  }

  /// Activation à la première connexion (US2) : définit le nouveau mot de passe
  /// et établit la session. Relève les erreurs pour affichage.
  Future<void> activate(
    String reference,
    String temporaryPassword,
    String newPassword,
  ) async {
    final tokens = await _api.activate(ActivateRequest(
      reference: reference,
      temporaryPassword: temporaryPassword,
      newPassword: newPassword,
    ));
    await _establish(tokens);
  }

  /// Déconnexion (US4) : purge coffre + état.
  Future<void> logout() => _purge();

  /// Reprise d'app (T061) : re-vérifie la validité locale du jeton ; purge si
  /// expiré pendant que l'app était en arrière-plan (SC-004).
  Future<void> recheckOnResume() async {
    if (state.status != SessionStatus.authenticated) return;
    final token = _holder.current;
    if (token == null || !token.isPotentiallyValid) {
      await _purge(message: 'Session expirée. Veuillez vous reconnecter.');
    }
  }

  Future<void> _establish(TokenResponse tokens) async {
    final token = AuthToken(
      value: tokens.accessToken,
      type: tokens.tokenType,
      expiresAt: tokens.expiresAt,
    );
    _holder.current = token;
    await _store.save(token);
    final user = await _api.me();
    state = SessionState.authenticated(user);
  }

  Future<void> _purge({String? message}) async {
    _holder.current = null;
    await _store.clear();
    state = SessionState.anonymous(message: message);
  }

  void _onUnauthorized() {
    if (state.status == SessionStatus.authenticated ||
        state.status == SessionStatus.restoring) {
      _holder.current = null;
      // Purge asynchrone du coffre (best-effort) sans bloquer l'intercepteur.
      _store.clear();
      state = const SessionState.anonymous(
        message: 'Session expirée. Veuillez vous reconnecter.',
      );
    }
  }
}
