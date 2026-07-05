import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { MemberProfilesApi } from '../../../core/api/member-profiles-api';
import { messageForError } from '../../../core/http/error-messages';
import { SessionStore } from '../../../core/session/session-store';
import { BureauProfileSummary, MemberProfilesResponse } from '../bureau-profile.models';

/**
 * Profils & droits effectifs d'un membre (feature 011, US3). Attribution idempotente et révocation
 * (avec garde-fou dernier administrateur). Actions d'écriture selon le droit d'administration.
 */
@Component({
  selector: 'app-member-profiles',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="lx-card">
      @if (data(); as d) {
        <h1 class="lx-title">Profils & droits — {{ d.member.fullName }}</h1>
        <p class="lx-muted">{{ d.member.reference }} ({{ d.member.status }})</p>

        @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }

        <h2 class="lx-title" style="font-size:1.1rem;">Droits effectifs</h2>
        @if (d.effectivePermissions.length === 0) { <p class="lx-muted">Aucun droit effectif.</p> }
        <ul>@for (p of d.effectivePermissions; track p) { <li>{{ p }}</li> }</ul>

        <h2 class="lx-title" style="font-size:1.1rem;">Profils attribués</h2>
        @if (d.profiles.length === 0) { <p class="lx-muted">Aucun profil attribué.</p> }
        <ul>
          @for (p of d.profiles; track p.id) {
            <li>
              {{ p.name }}
              @if (canWrite()) {
                <button type="button" class="lx-btn lx-btn-link" (click)="revoke(p.id)">Révoquer</button>
              }
            </li>
          }
        </ul>

        @if (canWrite()) {
          <h2 class="lx-title" style="font-size:1.1rem;">Attribuer un profil</h2>
          <div style="display:flex; gap:0.5rem;">
            <select [(ngModel)]="toAssign" name="toAssign" style="flex:1; padding:0.5rem;">
              <option [ngValue]="null">— Sélectionner un profil —</option>
              @for (p of assignable(); track p.id) { <option [ngValue]="p.id">{{ p.name }}</option> }
            </select>
            <button type="button" class="lx-btn" [disabled]="toAssign === null || busy()" (click)="assign()">Attribuer</button>
          </div>
        }

        <div class="lx-links"><a [routerLink]="['/members', d.member.id]">Retour à la fiche</a></div>
      } @else {
        <p class="lx-muted">Chargement…</p>
      }
    </div>
  `,
})
export class MemberProfilesComponent {
  private readonly api = inject(MemberProfilesApi);
  private readonly profilesApi = inject(BureauProfilesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly session = inject(SessionStore);

  private readonly memberId = Number(this.route.snapshot.paramMap.get('id'));

  readonly data = signal<MemberProfilesResponse | null>(null);
  readonly allProfiles = signal<BureauProfileSummary[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  toAssign: number | null = null;

  readonly canWrite = computed(() => this.session.hasPermission('manage_bureau_profiles'));
  readonly assignable = computed(() => {
    const assignedIds = new Set((this.data()?.profiles ?? []).map((p) => p.id));
    return this.allProfiles().filter((p) => !assignedIds.has(p.id));
  });

  constructor() {
    this.reload();
    this.profilesApi.list().subscribe((p) => this.allProfiles.set(p));
  }

  private reload(): void {
    this.api.get(this.memberId).subscribe((d) => this.data.set(d));
  }

  assign(): void {
    if (this.toAssign === null) {
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    // Idempotent côté API : réattribuer un profil déjà présent ne produit pas d'erreur.
    this.api.assign(this.memberId, this.toAssign).subscribe({
      next: () => { this.busy.set(false); this.toAssign = null; this.reload(); },
      error: (err: HttpErrorResponse) => { this.busy.set(false); this.error.set(messageForError(err)); },
    });
  }

  revoke(profileId: number): void {
    if (!confirm('Révoquer ce profil du membre ?')) {
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.api.revoke(this.memberId, profileId).subscribe({
      next: () => { this.busy.set(false); this.reload(); },
      // 409 last_administrator : erreur bloquante restituée.
      error: (err: HttpErrorResponse) => { this.busy.set(false); this.error.set(messageForError(err)); },
    });
  }
}
