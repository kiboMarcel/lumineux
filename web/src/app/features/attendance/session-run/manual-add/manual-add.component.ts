import { Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MemberLookupApi } from '../../../../core/api/member-lookup-api';
import { AttendancesApi } from '../../../../core/api/attendances-api';
import { messageForError } from '../../../../core/http/error-messages';
import { MemberLookupItem } from '../../attendance.models';

/**
 * Ajout **manuel** d'une présence (feature 014, US3). Le membre est identifié via la **recherche
 * allégée** (feature 015), accessible au droit `manage_attendance`. L'ajout est **idempotent** côté
 * API (réajout sans doublon). Émet `added` pour que le parent rafraîchisse la liste.
 */
@Component({
  selector: 'app-manual-add',
  imports: [FormsModule],
  template: `
    <div class="lx-card" style="margin-top:1rem;">
      <h3 style="margin-top:0;">Ajout manuel d'une présence</h3>

      <form (ngSubmit)="search()" style="display:flex; gap:0.5rem;">
        <input type="text" [(ngModel)]="query" name="query" placeholder="Référence ou nom…"
               style="flex:1; padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
        <button type="submit" class="lx-btn" [disabled]="searching() || !query.trim()">Rechercher</button>
      </form>

      @if (error()) {
        <p class="lx-error">{{ error() }}</p>
      }

      @if (searching()) {
        <p class="lx-muted">Recherche…</p>
      } @else if (searched() && results().length === 0) {
        <p class="lx-muted">Aucun membre trouvé.</p>
      } @else if (results().length > 0) {
        <ul style="list-style:none; padding:0; margin:0.75rem 0 0;">
          @for (m of results(); track m.id) {
            <li style="display:flex; justify-content:space-between; align-items:center; padding:0.4rem 0; border-bottom:1px solid var(--lx-border);">
              <span>{{ m.fullName }} <span class="lx-muted">({{ m.reference }} — {{ m.status }})</span></span>
              <button type="button" class="lx-btn lx-btn-ghost" [disabled]="adding()" (click)="add(m)">Ajouter</button>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`.lx-error { color: var(--lx-danger, #c0392b); }`],
})
export class ManualAddComponent {
  private readonly lookupApi = inject(MemberLookupApi);
  private readonly attendancesApi = inject(AttendancesApi);

  readonly sessionId = input.required<number>();
  readonly added = output<void>();

  query = '';
  readonly results = signal<MemberLookupItem[]>([]);
  readonly searching = signal(false);
  readonly searched = signal(false);
  readonly adding = signal(false);
  readonly error = signal<string | null>(null);

  search(): void {
    const term = this.query.trim();
    if (!term || this.searching()) {
      return;
    }
    this.searching.set(true);
    this.error.set(null);
    this.lookupApi.lookup(term).subscribe({
      next: (list) => {
        this.results.set(list);
        this.searched.set(true);
        this.searching.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(messageForError(err));
        this.searching.set(false);
      },
    });
  }

  add(member: MemberLookupItem): void {
    if (this.adding()) {
      return;
    }
    this.adding.set(true);
    this.error.set(null);
    this.attendancesApi.addManual(this.sessionId(), { memberId: member.id }).subscribe({
      next: () => {
        this.adding.set(false);
        this.added.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(messageForError(err));
        this.adding.set(false);
      },
    });
  }
}
