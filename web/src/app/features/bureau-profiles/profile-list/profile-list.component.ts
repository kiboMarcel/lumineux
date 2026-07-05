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
    <div class="lx-card">
      <div class="lx-topbar" style="border:0; padding:0; margin-bottom:1rem;">
        <h1 class="lx-title" style="margin:0;">Profils du bureau</h1>
        @if (canWrite()) {
          <a class="lx-btn" routerLink="/bureau-profiles/new">Nouveau profil</a>
        }
      </div>

      @if (loading()) {
        <p class="lx-muted">Chargement…</p>
      } @else if (profiles().length === 0) {
        <p class="lx-muted">Aucun profil.</p>
      } @else {
        <div style="overflow-x:auto;">
          <table style="width:100%; border-collapse:collapse;">
            <thead>
              <tr>
                <th style="text-align:left;">Nom</th>
                <th style="text-align:left;">Description</th>
                <th style="text-align:left;">Droits</th>
                <th style="text-align:left;">Titulaires</th>
              </tr>
            </thead>
            <tbody>
              @for (p of profiles(); track p.id) {
                <tr>
                  <td><a [routerLink]="['/bureau-profiles', p.id]">{{ p.name }}</a></td>
                  <td>{{ p.description || '—' }}</td>
                  <td>{{ p.permissions.length }}</td>
                  <td>{{ p.memberCount }}</td>
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
