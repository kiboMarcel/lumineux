import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthApi } from '../../core/api/auth-api';
import {
  mustMatchValidator,
  passwordPolicyHint,
  passwordPolicyValidator,
} from '../../shared/validators/password.validators';

/**
 * Réinitialisation via le lien reçu par e-mail (feature 008, US3, FR-013). Route **publique** lisant
 * le jeton depuis l'URL (`?token=`). Un jeton invalide/expiré/consommé → **échec générique** (sans
 * distinction de cause), avec possibilité de redemander un lien.
 */
@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-auth-shell">
      <div class="lx-card lx-auth-card">
        <h1 class="lx-title">Nouveau mot de passe</h1>
        @if (error()) {
          <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div>
        }
        @if (!token) {
          <p class="lx-muted">Lien invalide : jeton manquant.</p>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="lx-field">
              <label for="new">Nouveau mot de passe</label>
              <input id="new" type="password" formControlName="newPassword" autocomplete="new-password" />
              <span class="lx-hint">{{ hint }}</span>
            </div>
            <div class="lx-field">
              <label for="confirm">Confirmer</label>
              <input id="confirm" type="password" formControlName="confirm" autocomplete="new-password" />
              @if (form.errors?.['mismatch']) { <span class="lx-error">Les mots de passe ne correspondent pas.</span> }
            </div>
            <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
              {{ loading() ? 'Validation…' : 'Réinitialiser' }}
            </button>
          </form>
        }
        <div class="lx-links">
          <a routerLink="/auth/forgot-password">Redemander un lien</a>
          <a routerLink="/login">Connexion</a>
        </div>
      </div>
    </div>
  `,
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly hint = passwordPolicyHint();
  readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group(
    {
      newPassword: ['', [Validators.required, passwordPolicyValidator()]],
      confirm: ['', Validators.required],
    },
    { validators: [mustMatchValidator('newPassword', 'confirm')] },
  );

  submit(): void {
    if (this.form.invalid || !this.token) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const { newPassword } = this.form.getRawValue();

    this.authApi.resetPassword({ token: this.token, newPassword }).subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { reset: 'ok' } }),
      // Échec générique (FR-013) : ne pas distinguer invalide / expiré / consommé.
      error: () => {
        this.loading.set(false);
        this.error.set('Lien de réinitialisation invalide ou expiré. Vous pouvez en redemander un.');
      },
    });
  }
}
