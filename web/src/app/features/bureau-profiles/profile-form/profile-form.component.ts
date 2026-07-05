import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { PermissionsApi } from '../../../core/api/permissions-api';
import { PermissionDescriptor } from '../../../core/api/permission.models';
import { messageForError } from '../../../core/http/error-messages';

/**
 * Formulaire de création/édition d'un profil (feature 011, US2). Les droits sont choisis dans le
 * catalogue figé (cases à cocher). Catalogue vide → soumission empêchée (G1). Les conflits serveur
 * (`duplicate_name`, `last_administrator`, permission inconnue) sont restitués comme erreurs bloquantes.
 */
@Component({
  selector: 'app-profile-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-card lx-auth-card" style="max-width:560px;">
      <h1 class="lx-title">{{ isEdit() ? 'Modifier le profil' : 'Nouveau profil' }}</h1>
      @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }
      @if (catalogEmpty()) {
        <div class="lx-alert lx-alert-error" role="alert">
          Le catalogue de droits est indisponible : impossible de créer ou modifier un profil pour le moment.
        </div>
      }
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="lx-field">
          <label for="name">Nom *</label>
          <input id="name" type="text" formControlName="name" />
        </div>
        <div class="lx-field">
          <label for="description">Description</label>
          <input id="description" type="text" formControlName="description" />
        </div>
        <fieldset class="lx-field">
          <legend>Droits</legend>
          @for (perm of catalog(); track perm.code) {
            <label style="display:flex; align-items:center; gap:0.5rem; font-weight:normal;">
              <input type="checkbox" [checked]="selected().has(perm.code)" (change)="toggle(perm.code)" />
              {{ perm.label }} <span class="lx-muted">({{ perm.code }})</span>
            </label>
          }
          @if (selected().size === 0) { <span class="lx-hint">Sélectionnez au moins un droit (conseillé).</span> }
        </fieldset>
        <button type="submit" class="lx-btn" [disabled]="form.invalid || loading() || catalogEmpty()">
          {{ loading() ? 'Enregistrement…' : (isEdit() ? 'Enregistrer' : 'Créer le profil') }}
        </button>
      </form>
      <div class="lx-links"><a routerLink="/bureau-profiles">Annuler</a></div>
    </div>
  `,
})
export class ProfileFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BureauProfilesApi);
  private readonly permissionsApi = inject(PermissionsApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly editId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = signal(this.editId !== null);

  readonly catalog = signal<PermissionDescriptor[]>([]);
  readonly catalogLoaded = signal(false);
  readonly selected = signal<Set<string>>(new Set());
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly catalogEmpty = computed(() => this.catalogLoaded() && this.catalog().length === 0);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
  });

  constructor() {
    this.permissionsApi.list().subscribe({
      next: (c) => { this.catalog.set(c); this.catalogLoaded.set(true); },
      error: () => this.catalogLoaded.set(true),
    });
    if (this.editId !== null) {
      this.api.get(Number(this.editId)).subscribe((p) => {
        this.form.patchValue({ name: p.name, description: p.description ?? '' });
        this.selected.set(new Set(p.permissions));
      });
    }
  }

  toggle(code: string): void {
    const next = new Set(this.selected());
    if (next.has(code)) { next.delete(code); } else { next.add(code); }
    this.selected.set(next);
  }

  submit(): void {
    if (this.form.invalid || this.catalogEmpty()) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const { name, description } = this.form.getRawValue();
    const body = { name, description: description || null, permissions: [...this.selected()] };

    const request$ = this.isEdit()
      ? this.api.update(Number(this.editId), body)
      : this.api.create(body);

    request$.subscribe({
      next: (p) => void this.router.navigate(['/bureau-profiles', p.id]),
      // 409 duplicate_name / last_administrator, 400 permission inconnue : erreurs bloquantes.
      error: (err: HttpErrorResponse) => { this.loading.set(false); this.error.set(messageForError(err)); },
    });
  }
}
