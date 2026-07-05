import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { CurrentUser } from '../api/models';

/**
 * État de session **en mémoire** (feature 008, FR-003/004). Le jeton n'est JAMAIS persisté dans un
 * stockage exposé (pas de localStorage) : un rechargement complet déconnecte. L'identité et les
 * droits proviennent de `GET /auth/me` (feature 007) — la SPA ne décode jamais le jeton.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly authApi = inject(AuthApi);

  private readonly _accessToken = signal<string | null>(null);
  private readonly _currentUser = signal<CurrentUser | null>(null);

  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly permissions = computed(() => this._currentUser()?.permissions ?? []);
  readonly isAuthenticated = computed(
    () => this._accessToken() !== null && this._currentUser() !== null,
  );

  /** Indique si la session détient un droit donné (RBAC d'affichage — l'API reste l'autorité). */
  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  /**
   * Établit la session après réception d'un jeton (login / activation / installation) : mémorise le
   * jeton puis charge le profil de session. L'intercepteur porte alors le jeton sur l'appel `/me`.
   */
  establish(accessToken: string): Observable<CurrentUser> {
    this._accessToken.set(accessToken);
    return this.authApi.me().pipe(tap((user) => this._currentUser.set(user)));
  }

  /** Purge la session (déconnexion / refus d'authentification). */
  clear(): void {
    this._accessToken.set(null);
    this._currentUser.set(null);
  }
}
