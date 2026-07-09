import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MembersApi } from '../../../core/api/members-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { Country, ReferenceItem } from '../../../core/api/reference.models';
import { ProblemDetails } from '../../../core/api/models';
import { messageForError } from '../../../core/http/error-messages';
import { CreateMemberRequest, CredentialsDelivery, MemberCreatedResponse } from '../member.models';

/**
 * Formulaire partagé création/édition d'un membre (feature 009, US2/US3). En création : gère
 * l'homonymie confirmable et la remise des identifiants (mot de passe temporaire affiché une seule
 * fois, jamais persisté). En édition : préremplit, référence en lecture seule, pas de confirmation
 * d'homonyme (l'API ne la supporte pas). Le conflit de contact est bloquant dans les deux cas.
 */
@Component({
  selector: 'app-member-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="lx-card">
      <!-- Panneau de remise des identifiants (création réussie) -->
      @if (created(); as res) {
        <h1 class="lx-title">Membre créé</h1>
        <p>Référence : <strong>{{ res.member.reference }}</strong> — identifiant : <strong>{{ res.loginId }}</strong></p>
        @if (res.credentialsDelivery === email) {
          <div class="lx-alert lx-alert-success">Une invitation a été envoyée par e-mail.</div>
        } @else {
          <div class="lx-alert lx-alert-info">
            <p><strong>Remise bureau</strong> — transmettez ces identifiants en main propre. Ce mot de
            passe temporaire ne sera <strong>plus affiché</strong>.</p>
            <p>Mot de passe temporaire : <strong>{{ res.temporaryPassword }}</strong></p>
          </div>
        }
        <div class="lx-links">
          <a class="lx-btn" [routerLink]="['/members', res.member.id]">Voir la fiche</a>
          <a routerLink="/members">Retour à la liste</a>
        </div>
      } @else {
        <h1 class="lx-title">{{ isEdit() ? 'Modifier le membre' : 'Nouveau membre' }}</h1>

        @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }

        @if (noAntenna()) {
          <div class="lx-alert lx-alert-error" role="alert">
            Aucune antenne active n'est disponible : impossible de créer un membre pour le moment.
          </div>
        }

        <!-- Bannière homonymie (création) -->
        @if (homonym()) {
          <div class="lx-alert lx-alert-info" role="alert">
            Un membre de mêmes nom et prénom existe déjà. Confirmez pour créer un homonyme distinct.
            <div class="lx-links">
              <button type="button" class="lx-btn" (click)="confirmHomonym()">Confirmer la création</button>
              <button type="button" class="lx-btn lx-btn-ghost" (click)="homonym.set(false)">Annuler</button>
            </div>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="submit(false)">
          <div class="lx-form-grid">
          @if (isEdit()) {
            <div class="lx-field lx-field-full">
              <label>Référence (non modifiable)</label>
              <input type="text" [value]="reference()" disabled />
            </div>
          }
          <div class="lx-field">
            <label for="lastName">Nom *</label>
            <input id="lastName" type="text" formControlName="lastName" />
          </div>
          <div class="lx-field">
            <label for="firstName">Prénom *</label>
            <input id="firstName" type="text" formControlName="firstName" />
          </div>
          <div class="lx-field">
            <label for="gender">Sexe * (M/F)</label>
            <input id="gender" type="text" maxlength="1" formControlName="gender" />
          </div>
          <div class="lx-field">
            <label for="antennaId">Antenne d'origine *</label>
            <select id="antennaId" formControlName="antennaId">
              <option [ngValue]="null">— Sélectionner —</option>
              @for (a of antennas(); track a.id) { <option [ngValue]="a.id">{{ a.label }}</option> }
            </select>
          </div>
          <div class="lx-field">
            <label for="civilityId">Civilité</label>
            <select id="civilityId" formControlName="civilityId">
              <option [ngValue]="null">—</option>
              @for (c of civilities(); track c.id) { <option [ngValue]="c.id">{{ c.label }}</option> }
            </select>
          </div>
          <div class="lx-field">
            <label for="mobile">Mobile</label>
            <input id="mobile" type="text" formControlName="mobile" />
          </div>
          <div class="lx-field">
            <label for="email">E-mail</label>
            <input id="email" type="email" formControlName="email" />
          </div>
          <div class="lx-field">
            <label for="birthCityId">Ville de naissance</label>
            <select id="birthCityId" formControlName="birthCityId">
              <option [ngValue]="null">—</option>
              @for (c of cities(); track c.id) { <option [ngValue]="c.id">{{ c.label }}</option> }
            </select>
          </div>
          <div class="lx-field">
            <label for="districtId">District</label>
            <select id="districtId" formControlName="districtId">
              <option [ngValue]="null">—</option>
              @for (d of districts(); track d.id) { <option [ngValue]="d.id">{{ d.label }}</option> }
            </select>
          </div>
          <div class="lx-field">
            <label for="nationalityId">Nationalité</label>
            <select id="nationalityId" formControlName="nationalityId">
              <option [ngValue]="null">—</option>
              @for (c of countries(); track c.id) { <option [ngValue]="c.id">{{ c.nationality }}</option> }
            </select>
          </div>
          <div class="lx-field lx-field-full">
            <label for="address">Adresse</label>
            <input id="address" type="text" formControlName="address" />
          </div>
          </div>

          <button type="submit" class="lx-btn" [disabled]="form.invalid || loading() || noAntenna()">
            {{ loading() ? 'Enregistrement…' : (isEdit() ? 'Enregistrer' : 'Créer le membre') }}
          </button>
        </form>
        <div class="lx-links"><a routerLink="/members">Annuler</a></div>
      }
    </div>
  `,
})
export class MemberFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly membersApi = inject(MembersApi);
  private readonly referenceApi = inject(ReferenceApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly email = CredentialsDelivery.EmailSent;

  private readonly editId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = signal<boolean>(this.editId !== null);
  readonly reference = signal<string>('');

  readonly antennas = signal<ReferenceItem[]>([]);
  readonly civilities = signal<ReferenceItem[]>([]);
  readonly cities = signal<ReferenceItem[]>([]);
  readonly districts = signal<ReferenceItem[]>([]);
  readonly countries = signal<Country[]>([]);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly homonym = signal(false);
  readonly created = signal<MemberCreatedResponse | null>(null);
  readonly noAntenna = computed(() => this.antennas().length === 0 && !this.isEdit());

  readonly form = this.fb.group({
    lastName: ['', Validators.required],
    firstName: ['', Validators.required],
    gender: ['', [Validators.required, Validators.pattern(/^[MFmf]$/)]],
    antennaId: this.fb.control<number | null>(null, Validators.required),
    civilityId: this.fb.control<number | null>(null),
    mobile: [''],
    email: ['', Validators.email],
    birthCityId: this.fb.control<number | null>(null),
    districtId: this.fb.control<number | null>(null),
    nationalityId: this.fb.control<number | null>(null),
    address: [''],
  });

  constructor() {
    this.loadReferences();
    if (this.editId !== null) {
      this.loadMember(Number(this.editId));
    }
  }

  private loadReferences(): void {
    forkJoin({
      antennas: this.referenceApi.antennas(),
      civilities: this.referenceApi.civilities(),
      cities: this.referenceApi.cities(),
      districts: this.referenceApi.districts(),
      countries: this.referenceApi.countries(),
    }).subscribe((r) => {
      this.antennas.set(r.antennas);
      this.civilities.set(r.civilities);
      this.cities.set(r.cities);
      this.districts.set(r.districts);
      this.countries.set(r.countries);
    });
  }

  private loadMember(id: number): void {
    this.membersApi.get(id).subscribe((m) => {
      this.reference.set(m.reference);
      this.form.patchValue({
        lastName: m.lastName,
        firstName: m.firstName,
        gender: m.gender,
        antennaId: m.antennaId ?? null,
        civilityId: m.civilityId ?? null,
        mobile: m.mobile ?? '',
        email: m.email ?? '',
        birthCityId: m.birthCityId ?? null,
        districtId: m.districtId ?? null,
        nationalityId: m.nationalityId ?? null,
        address: m.address ?? '',
      });
    });
  }

  confirmHomonym(): void {
    this.homonym.set(false);
    this.submit(true);
  }

  submit(confirmDuplicate: boolean): void {
    if (this.form.invalid || this.noAntenna()) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    const body: CreateMemberRequest = {
      lastName: v.lastName!,
      firstName: v.firstName!,
      gender: v.gender!.toUpperCase(),
      antennaId: v.antennaId!,
      civilityId: v.civilityId,
      mobile: v.mobile || null,
      email: v.email || null,
      birthCityId: v.birthCityId,
      districtId: v.districtId,
      nationalityId: v.nationalityId,
      address: v.address || null,
    };

    if (this.isEdit()) {
      const { confirmDuplicate: _drop, ...update } = { ...body, confirmDuplicate: false };
      this.membersApi.update(Number(this.editId), update).subscribe({
        next: (m) => void this.router.navigate(['/members', m.id]),
        error: (err: HttpErrorResponse) => this.handleError(err),
      });
      return;
    }

    this.membersApi.create({ ...body, confirmDuplicate }).subscribe({
      next: (res) => { this.loading.set(false); this.created.set(res); },
      error: (err: HttpErrorResponse) => this.handleError(err),
    });
  }

  private handleError(err: HttpErrorResponse): void {
    this.loading.set(false);
    const code = (err.error as ProblemDetails | null)?.code;
    if (err.status === 409 && code === 'duplicate_name') {
      this.homonym.set(true); // homonymie confirmable (création)
      return;
    }
    // contact_in_use et autres conflits : erreur bloquante non confirmable.
    this.error.set(messageForError(err));
  }
}
