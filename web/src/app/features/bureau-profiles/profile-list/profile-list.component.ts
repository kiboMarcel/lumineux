import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { SessionStore } from '../../../core/session/session-store';
import { BureauProfileSummary } from '../bureau-profile.models';

/** Liste des profils du bureau (feature 011, US1, FR-002). Actions d'écriture selon le droit. */
@Component({
  selector: 'app-profile-list',
  imports: [RouterLink],
  template: `
    <div class="lx-page-head">
      <h1 class="lx-title">Profils du bureau</h1>
      @if (canWrite()) {
        <a class="lx-btn" routerLink="/bureau-profiles/new">Nouveau profil</a>
      }
    </div>

    <div class="lx-card">
      @if (loading()) {
        <p class="lx-empty">Chargement…</p>
      } @else if (profiles().length === 0) {
        <p class="lx-empty">Aucun profil.</p>
      } @else {
        <div class="lx-table-wrap">
          <table class="lx-table">
            <thead>
              <tr>
                <th>Nom</th>
                <th>Description</th>
                <th>Droits</th>
                <th>Titulaires</th>
              </tr>
            </thead>
            <tbody>
              @for (p of profiles(); track p.id) {
                <tr>
                  <td><a [routerLink]="['/bureau-profiles', p.id]">{{ p.name }}</a></td>
                  <td>{{ p.description || '—' }}</td>
                  <td><span class="lx-pill lx-pill-info lx-pill-plain">{{ p.permissions.length }}</span></td>
                  <td><span class="lx-pill lx-pill-muted lx-pill-plain">{{ p.memberCount }}</span></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class ProfileListComponent {
  private readonly api = inject(BureauProfilesApi);
  private readonly session = inject(SessionStore);

  readonly profiles = signal<BureauProfileSummary[]>([]);
  readonly loading = signal(true);
  readonly canWrite = computed(() => this.session.hasPermission('manage_bureau_profiles'));

  constructor() {
    this.api.list().subscribe({
      next: (p) => { this.profiles.set(p); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
