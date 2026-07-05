import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthApi } from '../../core/api/auth-api';
import { messageForError } from '../../core/http/error-messages';
import {
  mustDifferValidator,
  mustMatchValidator,
  passwordPolicyHint,
  passwordPolicyValidator,
} from '../../shared/validators/password.validators';

/**
 * Changement de mot de passe par un utilisateur connecté (feature 008, US4, FR-014). Route protégée
 * (`authGuard`). Nouveau mot de passe conforme et **différent** de l'actuel.
 */
@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-card lx-auth-card">
      <h1 class="lx-title">Changer mon mot de passe</h1>
      @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }
      @if (success()) { <div class="lx-alert lx-alert-success" role="status">Votre mot de passe a été mis à jour.</div> }
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="lx-field">
          <label for="current">Mot de passe actuel</label>
          <input id="current" type="password" formControlName="currentPassword" autocomplete="current-password" />
        </div>
        <div class="lx-field">
          <label for="new">Nouveau mot de passe</label>
          <input id="new" type="password" formControlName="newPassword" autocomplete="new-password" />
          <span class="lx-hint">{{ hint }}</span>
        </div>
        <div class="lx-field">
          <label for="confirm">Confirmer</label>
          <input id="confirm" type="password" formControlName="confirm" autocomplete="new-password" />
          @if (form.errors?.['mismatch']) { <span class="lx-error">Les mots de passe ne correspondent pas.</span> }
          @if (form.errors?.['mustDiffer']) { <span class="lx-error">Le nouveau mot de passe doit différer de l'actuel.</span> }
        </div>
        <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Enregistrement…' : 'Changer le mot de passe' }}
        </button>
      </form>
      <div class="lx-links"><a routerLink="/">Retour à l'accueil</a></div>
    </div>
  `,
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);

  readonly hint = passwordPolicyHint();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordPolicyValidator()]],
      confirm: ['', Validators.required],
    },
    { validators: [mustMatchValidator('newPassword', 'confirm'), mustDifferValidator('newPassword', 'currentPassword')] },
  );

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.success.set(false);
    const { currentPassword, newPassword } = this.form.getRawValue();

    this.authApi.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => { this.loading.set(false); this.success.set(true); this.form.reset(); },
      error: (err: HttpErrorResponse) => { this.loading.set(false); this.error.set(messageForError(err)); },
    });
  }
}
