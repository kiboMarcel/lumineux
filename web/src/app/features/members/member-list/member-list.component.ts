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
    <div class="lx-page-head">
      <h1 class="lx-title">Membres</h1>
      <a class="lx-btn" routerLink="/members/new">Nouveau membre</a>
    </div>

    <div class="lx-card">
      <form (ngSubmit)="search()" class="lx-toolbar">
        <input type="text" [(ngModel)]="query" name="query" placeholder="Nom, référence ou contact…" />
        <button type="submit" class="lx-btn" [disabled]="loading()">Rechercher</button>
      </form>

      @if (loading()) {
        <p class="lx-empty">Chargement…</p>
      } @else if (items().length === 0) {
        <p class="lx-empty">Aucun membre trouvé.</p>
      } @else {
        <div class="lx-table-wrap">
          <table class="lx-table">
            <thead>
              <tr>
                <th>Référence</th>
                <th>Nom</th>
                <th>Prénom</th>
                <th>Contact</th>
                <th>Statut</th>
              </tr>
            </thead>
            <tbody>
              @for (m of items(); track m.id) {
                <tr>
                  <td><a [routerLink]="['/members', m.id]">{{ m.reference }}</a></td>
                  <td>{{ m.lastName }}</td>
                  <td>{{ m.firstName }}</td>
                  <td>{{ m.email || m.mobile || '—' }}</td>
                  <td>
                    <span class="lx-pill" [class.lx-pill-success]="m.status === 'Active'" [class.lx-pill-muted]="m.status !== 'Active'">{{ m.status }}</span>
                  </td>
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
