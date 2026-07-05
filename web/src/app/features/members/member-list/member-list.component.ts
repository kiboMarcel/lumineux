import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MembersApi } from '../../../core/api/members-api';
import { MemberListItem } from '../member.models';

/** Recherche + liste paginée des membres (feature 009, US1, FR-002/003). */
@Component({
  selector: 'app-member-list',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="lx-card">
      <div class="lx-topbar" style="border:0; padding:0; margin-bottom:1rem;">
        <h1 class="lx-title" style="margin:0;">Membres</h1>
        <a class="lx-btn" routerLink="/members/new">Nouveau membre</a>
      </div>

      <form (ngSubmit)="search()" style="display:flex; gap:0.5rem; margin-bottom:1rem;">
        <input type="text" [(ngModel)]="query" name="query" placeholder="Nom, référence ou contact…"
               style="flex:1; padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
        <button type="submit" class="lx-btn" [disabled]="loading()">Rechercher</button>
      </form>

      @if (loading()) {
        <p class="lx-muted">Chargement…</p>
      } @else if (items().length === 0) {
        <p class="lx-muted">Aucun membre trouvé.</p>
      } @else {
        <div style="overflow-x:auto;">
          <table style="width:100%; border-collapse:collapse;">
            <thead>
              <tr>
                <th style="text-align:left;">Référence</th>
                <th style="text-align:left;">Nom</th>
                <th style="text-align:left;">Prénom</th>
                <th style="text-align:left;">Contact</th>
                <th style="text-align:left;">Statut</th>
              </tr>
            </thead>
            <tbody>
              @for (m of items(); track m.id) {
                <tr>
                  <td><a [routerLink]="['/members', m.id]">{{ m.reference }}</a></td>
                  <td>{{ m.lastName }}</td>
                  <td>{{ m.firstName }}</td>
                  <td>{{ m.email || m.mobile || '—' }}</td>
                  <td>{{ m.status }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="lx-links" style="align-items:center;">
          <button type="button" class="lx-btn lx-btn-ghost" [disabled]="page() <= 1" (click)="goto(page() - 1)">Précédent</button>
          <span class="lx-muted">Page {{ page() }} / {{ totalPages() }} — {{ total() }} membre(s)</span>
          <button type="button" class="lx-btn lx-btn-ghost" [disabled]="page() >= totalPages()" (click)="goto(page() + 1)">Suivant</button>
        </div>
      }
    </div>
  `,
})
export class MemberListComponent {
  private readonly api = inject(MembersApi);
  private readonly pageSize = 20;

  query = '';
  readonly items = signal<MemberListItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly loading = signal(false);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  constructor() {
    this.load();
  }

  search(): void {
    this.page.set(1);
    this.load();
  }

  goto(page: number): void {
    this.page.set(page);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.api.search(this.query.trim() || null, this.page(), this.pageSize).subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
