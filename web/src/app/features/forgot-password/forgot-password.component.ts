import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthApi } from '../../core/api/auth-api';

/**
 * Demande de réinitialisation (feature 008, US3, FR-012). Affiche le **message générique** renvoyé
 * par l'API, identique quel que soit l'état du compte (anti-énumération). En cas d'erreur inattendue,
 * on affiche néanmoins un message générique pour ne rien divulguer.
 */
@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-auth-shell">
      <div class="lx-card lx-auth-card">
        <h1 class="lx-title">Mot de passe oublié</h1>
        @if (message()) {
          <div class="lx-alert lx-alert-info" role="status">{{ message() }}</div>
        } @else {
          <p class="lx-muted">Saisissez votre référence : si un compte correspond, un lien vous sera envoyé.</p>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="lx-field">
              <label for="reference">Référence</label>
              <input id="reference" type="text" formControlName="reference" autocomplete="username" />
            </div>
            <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
              {{ loading() ? 'Envoi…' : 'Envoyer le lien' }}
            </button>
          </form>
        }
        <div class="lx-links">
          <a routerLink="/login">Retour à la connexion</a>
        </div>
      </div>
    </div>
  `,
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);

  private readonly genericMessage =
    "Si un compte correspond à cette référence et qu'un email est enregistré, un lien de réinitialisation vient d'être envoyé.";

  readonly loading = signal(false);
  readonly message = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    reference: ['', [Validators.required, Validators.maxLength(60)]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.authApi.forgotPassword(this.form.getRawValue()).subscribe({
      next: (res) => this.message.set(res?.message ?? this.genericMessage),
      // Même en cas d'erreur : message générique (ne rien révéler, FR-012).
      error: () => this.message.set(this.genericMessage),
    });
  }
}
