import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { messageForError } from '../../../core/http/error-messages';
import { SessionStore } from '../../../core/session/session-store';
import { BureauProfileDetail } from '../bureau-profile.models';

/**
 * Détail d'un profil (feature 011, US1/US2, FR-003/009). Droits + titulaires ; actions d'écriture
 * (Modifier/Supprimer) conditionnées au droit d'administration. Suppression avec confirmation.
 */
@Component({
  selector: 'app-profile-detail',
  imports: [RouterLink],
  template: `
    <div class="lx-card">
      @if (notFound()) {
        <div class="lx-alert lx-alert-error" role="alert">Profil introuvable.</div>
        <a routerLink="/bureau-profiles">Retour à la liste</a>
      } @else if (profile(); as p) {
        <div class="lx-topbar" style="border:0; padding:0; margin-bottom:1rem;">
          <h1 class="lx-title" style="margin:0;">{{ p.name }}</h1>
          @if (canWrite()) {
            <div class="lx-links" style="margin:0;">
              <a class="lx-btn lx-btn-ghost" [routerLink]="['/bureau-profiles', p.id, 'edit']">Modifier</a>
              <button type="button" class="lx-btn lx-btn-ghost" (click)="remove(p.id)" [disabled]="deleting()">Supprimer</button>
            </div>
          }
        </div>

        @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }

        <p>{{ p.description || 'Aucune description.' }}</p>

        <h2 class="lx-title" style="font-size:1.1rem;">Droits</h2>
        @if (p.permissions.length === 0) { <p class="lx-muted">Aucun droit.</p> }
        <ul>@for (perm of p.permissions; track perm) { <li>{{ perm }}</li> }</ul>

        <h2 class="lx-title" style="font-size:1.1rem;">Titulaires ({{ p.memberCount }})</h2>
        @if (p.members.length === 0) { <p class="lx-muted">Aucun titulaire.</p> }
        <ul>@for (m of p.members; track m.id) { <li>{{ m.fullName }} — {{ m.reference }} ({{ m.status }})</li> }</ul>

        <div class="lx-links"><a routerLink="/bureau-profiles">Retour à la liste</a></div>
      } @else {
        <p class="lx-muted">Chargement…</p>
      }
    </div>
  `,
})
export class ProfileDetailComponent {
  private readonly api = inject(BureauProfilesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly session = inject(SessionStore);

  readonly profile = signal<BureauProfileDetail | null>(null);
  readonly notFound = signal(false);
  readonly error = signal<string | null>(null);
  readonly deleting = signal(false);
  readonly canWrite = computed(() => this.session.hasPermission('manage_bureau_profiles'));

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.get(id).subscribe({
      next: (p) => this.profile.set(p),
      error: (err: HttpErrorResponse) => { if (err.status === 404) this.notFound.set(true); },
    });
  }

  remove(id: number): void {
    if (!confirm('Supprimer ce profil ? Cette action est irréversible.')) {
      return;
    }
    this.deleting.set(true);
    this.error.set(null);
    this.api.remove(id).subscribe({
      next: () => void this.router.navigate(['/bureau-profiles']),
      // 409 profile_in_use / last_administrator : erreur bloquante restituée.
      error: (err: HttpErrorResponse) => { this.deleting.set(false); this.error.set(messageForError(err)); },
    });
  }
}
