import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SetupApi } from '../../core/api/setup-api';
import { messageForError } from '../../core/http/error-messages';
import { SessionStore } from '../../core/session/session-store';
import { passwordPolicyHint, passwordPolicyValidator } from '../../shared/validators/password.validators';

/**
 * Installation du premier administrateur (feature 008, US5, FR-016). Une instance déjà amorcée est
 * rejetée par l'API (409) → message adéquat. En cas de succès, l'administrateur est connecté.
 */
@Component({
  selector: 'app-setup',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-auth-shell">
      <div class="lx-card lx-auth-card">
        <h1 class="lx-title">Installation — premier administrateur</h1>
        @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="lx-field">
            <label for="lastName">Nom</label>
            <input id="lastName" type="text" formControlName="lastName" />
          </div>
          <div class="lx-field">
            <label for="firstName">Prénom</label>
            <input id="firstName" type="text" formControlName="firstName" />
          </div>
          <div class="lx-field">
            <label for="gender">Sexe (M/F)</label>
            <input id="gender" type="text" maxlength="1" formControlName="gender" />
          </div>
          <div class="lx-field">
            <label for="email">E-mail (optionnel)</label>
            <input id="email" type="email" formControlName="email" />
          </div>
          <div class="lx-field">
            <label for="password">Mot de passe</label>
            <input id="password" type="password" formControlName="password" autocomplete="new-password" />
            <span class="lx-hint">{{ hint }}</span>
          </div>
          <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Installation…' : 'Installer' }}
          </button>
        </form>
        <div class="lx-links"><a routerLink="/login">Retour à la connexion</a></div>
      </div>
    </div>
  `,
})
export class SetupComponent {
  private readonly fb = inject(FormBuilder);
  private readonly setupApi = inject(SetupApi);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  readonly hint = passwordPolicyHint();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    lastName: ['', Validators.required],
    firstName: ['', Validators.required],
    gender: ['', [Validators.required, Validators.pattern(/^[MFmf]$/)]],
    email: [''],
    password: ['', [Validators.required, passwordPolicyValidator()]],
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();

    this.setupApi
      .installFirstAdmin({
        lastName: v.lastName,
        firstName: v.firstName,
        gender: v.gender.toUpperCase(),
        password: v.password,
        email: v.email || null,
      })
      .subscribe({
        next: (token) => this.session.establish(token.accessToken).subscribe({
          next: () => void this.router.navigateByUrl('/'),
          error: () => { this.loading.set(false); this.error.set('Installation impossible pour le moment.'); },
        }),
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.error.set(err.status === 409 ? 'Une instance est déjà installée.' : messageForError(err));
        },
      });
  }
}
