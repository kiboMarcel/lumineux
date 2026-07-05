import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthApi } from '../../core/api/auth-api';
import { isPasswordChangeRequired } from '../../core/http/error-messages';
import { SessionStore } from '../../core/session/session-store';

/**
 * Écran de connexion (feature 008, US1, FR-010). Échec d'identification → message **non révélateur**
 * (indistinct référence inconnue / mot de passe erroné). Une première connexion (403
 * `password_change_required`) bascule vers l'écran d'activation.
 */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-auth-shell">
      <div class="lx-card lx-auth-card">
        <h1 class="lx-title">Connexion</h1>
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
            <input id="password" type="password" formControlName="password" autocomplete="current-password" />
          </div>
          <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Connexion…' : 'Se connecter' }}
          </button>
        </form>
        <div class="lx-links">
          <a routerLink="/auth/forgot-password">Mot de passe oublié ?</a>
        </div>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    reference: ['', Validators.required],
    password: ['', Validators.required],
  });

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
