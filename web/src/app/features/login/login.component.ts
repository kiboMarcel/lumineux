import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthApi } from '../../core/api/auth-api';
import { SetupApi } from '../../core/api/setup-api';
import { isPasswordChangeRequired } from '../../core/http/error-messages';
import { SessionStore } from '../../core/session/session-store';

/**
 * Écran de connexion (feature 008/013, FR-010). Échec d'identification → message **non révélateur**.
 * Une première connexion (403 `password_change_required`) bascule vers l'activation.
 * Feature 013 : au chargement, consulte le statut d'installation et propose un lien « Première
 * installation » **uniquement** si l'instance n'est pas initialisée (défaut sûr = masqué).
 */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-auth-shell">
      <div class="lx-card lx-auth-card">
        <div style="text-align:center; margin-bottom:20px;">
          <img
            class="lx-auth-logo"
            src="logo-lockup-horizontal.svg"
            alt="Lumineux"
            style="display:block;height:46px;margin:0 auto 14px;"
          />
          <h1 class="lx-title" style="margin:0 0 4px;">Connexion</h1>
          <p class="lx-muted" style="margin:0;font-size:0.9rem;">Console bureau Lumineux</p>
        </div>
        @if (error()) {
          <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div>
        }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="lx-field">
            <label for="reference">Référence</label>
            <input id="reference" type="text" formControlName="reference" autocomplete="username" />
          </div>
          <div class="lx-field">
            <label for="password">Mot de passe</label>
            <div class="lx-password">
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                autocomplete="current-password"
              />
              <button
                type="button"
                class="lx-password-toggle"
                (click)="showPassword.set(!showPassword())"
                [attr.aria-label]="showPassword() ? 'Masquer le mot de passe' : 'Afficher le mot de passe'"
                [attr.aria-pressed]="showPassword()"
              >
                @if (showPassword()) {
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><path d="M1 1l22 22"/></svg>
                } @else {
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.9" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                }
              </button>
            </div>
          </div>
          <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Connexion…' : 'Se connecter' }}
          </button>
        </form>
        <div class="lx-links">
          <a routerLink="/auth/forgot-password">Mot de passe oublié ?</a>
          @if (showSetupLink()) {
            <a routerLink="/setup/first-admin">Première installation</a>
          }
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly setupApi = inject(SetupApi);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  /** Bascule d'affichage du mot de passe saisi (masqué par défaut). */
  readonly showPassword = signal(false);
  /** Lien « Première installation » : visible uniquement si l'instance n'est pas initialisée. */
  readonly showSetupLink = signal(false);

  readonly form = this.fb.nonNullable.group({
    reference: ['', Validators.required],
    password: ['', Validators.required],
  });

  constructor() {
    // Statut d'installation (anonyme). Défaut sûr : en cas d'échec, ne PAS afficher le lien et ne
    // jamais bloquer la connexion (FR-005).
    this.setupApi.status().subscribe({
      next: (s) => this.showSetupLink.set(s.installed === false),
      error: () => this.showSetupLink.set(false),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const { reference, password } = this.form.getRawValue();

    this.authApi.login({ reference, password }).subscribe({
      next: (token) => {
        this.session.establish(token.accessToken).subscribe({
          next: () => {
            const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
            void this.router.navigateByUrl(returnUrl);
          },
          error: () => {
            this.loading.set(false);
            this.error.set('Connexion impossible pour le moment. Veuillez réessayer.');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (isPasswordChangeRequired(err)) {
          void this.router.navigate(['/auth/activate'], { queryParams: { reference } });
          return;
        }
        // Message non révélateur (FR-010) : ne distingue pas les causes.
        this.error.set('Référence ou mot de passe invalide.');
      },
    });
  }
}
