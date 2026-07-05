import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthApi } from '../../core/api/auth-api';
import { messageForError } from '../../core/http/error-messages';
import { SessionStore } from '../../core/session/session-store';
import {
  mustDifferValidator,
  mustMatchValidator,
  passwordPolicyHint,
  passwordPolicyValidator,
} from '../../shared/validators/password.validators';

/**
 * Activation / première connexion (feature 008, US2, FR-011). L'utilisateur avec un mot de passe
 * temporaire définit un nouveau mot de passe conforme et **différent** du temporaire, puis est
 * connecté.
 */
@Component({
  selector: 'app-activate',
  imports: [ReactiveFormsModule],
  template: `
    <div class="lx-auth-shell">
      <div class="lx-card lx-auth-card">
        <h1 class="lx-title">Première connexion</h1>
        <p class="lx-muted">Définissez un nouveau mot de passe pour activer votre compte.</p>
        @if (error()) {
          <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div>
        }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="lx-field">
            <label for="reference">Référence</label>
            <input id="reference" type="text" formControlName="reference" autocomplete="username" />
          </div>
          <div class="lx-field">
            <label for="temp">Mot de passe temporaire</label>
            <input id="temp" type="password" formControlName="temporaryPassword" autocomplete="current-password" />
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
            @if (form.errors?.['mustDiffer']) { <span class="lx-error">Le nouveau mot de passe doit différer du temporaire.</span> }
          </div>
          <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Activation…' : 'Activer mon compte' }}
          </button>
        </form>
      </div>
    </div>
  `,
})
export class ActivateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly hint = passwordPolicyHint();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group(
    {
      reference: [this.route.snapshot.queryParamMap.get('reference') ?? '', Validators.required],
      temporaryPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, passwordPolicyValidator()]],
      confirm: ['', Validators.required],
    },
    { validators: [mustMatchValidator('newPassword', 'confirm'), mustDifferValidator('newPassword', 'temporaryPassword')] },
  );

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const { reference, temporaryPassword, newPassword } = this.form.getRawValue();

    this.authApi.activate({ reference, temporaryPassword, newPassword }).subscribe({
      next: (token) => this.session.establish(token.accessToken).subscribe({
        next: () => void this.router.navigateByUrl('/'),
        error: () => { this.loading.set(false); this.error.set('Activation impossible pour le moment.'); },
      }),
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.error.set(messageForError(err));
      },
    });
  }
}
