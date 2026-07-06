import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AntennasApi } from '../../../core/api/antennas-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { ReferenceItem } from '../../../core/api/reference.models';
import { ProblemDetails } from '../../../core/api/models';
import { messageForError } from '../../../core/http/error-messages';

/**
 * Formulaire partagé création/édition d'une antenne (feature 017, US2/US3). En création : code +
 * libellé + district. En édition : préremplit ; le **code est en lecture seule** (immuable côté API,
 * FR-009). Le district provient du référentiel (feature 010).
 */
@Component({
  selector: 'app-antenna-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-card">
      <h1 class="lx-title">{{ isEdit() ? 'Modifier l\\'antenne' : 'Nouvelle antenne' }}</h1>

      @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }

      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="lx-field">
          <label for="code">Code *{{ isEdit() ? ' (non modifiable)' : '' }}</label>
          <input id="code" type="text" formControlName="code" />
        </div>
        <div class="lx-field">
          <label for="label">Libellé *</label>
          <input id="label" type="text" formControlName="label" />
        </div>
        <div class="lx-field">
          <label for="districtId">District *</label>
          <select id="districtId" formControlName="districtId">
            <option [ngValue]="null">— Sélectionner —</option>
            @for (d of districts(); track d.id) { <option [ngValue]="d.id">{{ d.label }}</option> }
          </select>
        </div>

        <button type="submit" class="lx-btn" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Enregistrement…' : (isEdit() ? 'Enregistrer' : 'Créer l\\'antenne') }}
        </button>
      </form>
      <div class="lx-links"><a routerLink="/antennas">Annuler</a></div>
    </div>
  `,
})
export class AntennaFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(AntennasApi);
  private readonly referenceApi = inject(ReferenceApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly editId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = signal<boolean>(this.editId !== null);

  readonly districts = signal<ReferenceItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.group({
    code: ['', Validators.required],
    label: ['', Validators.required],
    districtId: this.fb.control<number | null>(null, Validators.required),
  });

  constructor() {
    this.referenceApi.districts().subscribe((d) => this.districts.set(d));

    if (this.editId !== null) {
      this.form.controls.code.disable(); // code immuable (lecture seule)
      this.api.get(Number(this.editId)).subscribe((a) => {
        this.form.patchValue({ code: a.code, label: a.label, districtId: a.districtId });
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();

    if (this.isEdit()) {
      this.api.update(Number(this.editId), { label: v.label!, districtId: v.districtId! }).subscribe({
        next: () => void this.router.navigate(['/antennas']),
        error: (err: HttpErrorResponse) => this.handleError(err),
      });
      return;
    }

    this.api.create({ code: v.code!, label: v.label!, districtId: v.districtId! }).subscribe({
      next: () => void this.router.navigate(['/antennas']),
      error: (err: HttpErrorResponse) => this.handleError(err),
    });
  }

  private handleError(err: HttpErrorResponse): void {
    this.loading.set(false);
    const code = (err.error as ProblemDetails | null)?.code;
    if (err.status === 409 && code === 'duplicate_code') {
      this.error.set('Ce code d\'antenne est déjà utilisé. Choisissez-en un autre.');
      return;
    }
    this.error.set(messageForError(err));
  }
}
