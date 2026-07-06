import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportsApi } from '../../../core/api/reports-api';
import { MemberLookupApi } from '../../../core/api/member-lookup-api';
import { MemberLookupItem } from '../../attendance/attendance.models';
import { messageForError } from '../../../core/http/error-messages';
import { MemberAttendanceRateResponse } from '../report.models';

/**
 * Panneau « taux d'assiduité d'un membre » (feature 019, US3). Sélection du membre via la recherche
 * allégée (015), affichage du taux en **pourcentage** (jauge légère). Aucun calcul statistique côté
 * client : les chiffres proviennent de l'API 018.
 */
@Component({
  selector: 'app-member-rate',
  imports: [FormsModule],
  template: `
    <div class="lx-card" style="margin-top:1rem;" [class.lx-print-hide]="!rate()">
      <h2 style="margin-top:0; font-size:1.1rem;">Taux d'assiduité d'un membre</h2>

      <form (ngSubmit)="search()" style="display:flex; gap:0.5rem;">
        <input type="text" [(ngModel)]="query" name="query" placeholder="Référence ou nom du membre…"
               style="flex:1; padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
        <button type="submit" class="lx-btn" [disabled]="searching() || !query.trim()">Rechercher</button>
      </form>

      @if (error()) { <p class="lx-error">{{ error() }}</p> }

      @if (results().length > 0 && !selected()) {
        <ul style="list-style:none; padding:0; margin:0.75rem 0 0;">
          @for (m of results(); track m.id) {
            <li style="display:flex; justify-content:space-between; align-items:center; padding:0.4rem 0; border-bottom:1px solid var(--lx-border);">
              <span>{{ m.fullName }} <span class="lx-muted">({{ m.reference }})</span></span>
              <button type="button" class="lx-btn lx-btn-ghost" (click)="select(m)">Voir le taux</button>
            </li>
          }
        </ul>
      }

      @if (rate(); as r) {
        <div style="margin-top:1rem;">
          <p><strong>{{ r.memberFullName }}</strong></p>
          <p class="lx-muted">{{ r.validAttendanceCount }} présence(s) valide(s) sur {{ r.eligibleSessionCount }} session(s) éligible(s).</p>
          <div style="display:flex; align-items:center; gap:0.75rem;">
            <div style="flex:1; height:14px; background:var(--lx-border); border-radius:7px; overflow:hidden;">
              <div [style.width.%]="percent()" style="height:100%; background:var(--lx-accent, #2d7);"></div>
            </div>
            <strong>{{ percent() }} %</strong>
          </div>
          <button type="button" class="lx-btn lx-btn-ghost" style="margin-top:0.75rem;" (click)="reset()">Choisir un autre membre</button>
        </div>
      } @else if (!results().length && !searching() && searched()) {
        <p class="lx-muted">Aucun membre trouvé.</p>
      }
    </div>
  `,
  styles: [`.lx-error { color: var(--lx-danger, #c0392b); }`],
})
export class MemberRateComponent {
  private readonly lookupApi = inject(MemberLookupApi);
  private readonly reportsApi = inject(ReportsApi);

  readonly from = input.required<string>();
  readonly to = input.required<string>();

  query = '';
  readonly results = signal<MemberLookupItem[]>([]);
  readonly searching = signal(false);
  readonly searched = signal(false);
  readonly selected = signal<MemberLookupItem | null>(null);
  readonly rate = signal<MemberAttendanceRateResponse | null>(null);
  readonly error = signal<string | null>(null);

  /** Taux affiché en pourcentage (mise en forme ; la fraction 0..1 vient de l'API). */
  readonly percent = computed(() => Math.round((this.rate()?.rate ?? 0) * 100));

  search(): void {
    const term = this.query.trim();
    if (!term || this.searching()) {
      return;
    }
    this.searching.set(true);
    this.error.set(null);
    this.selected.set(null);
    this.rate.set(null);
    this.lookupApi.lookup(term).subscribe({
      next: (list) => { this.results.set(list); this.searched.set(true); this.searching.set(false); },
      error: (err: HttpErrorResponse) => { this.error.set(messageForError(err)); this.searching.set(false); },
    });
  }

  select(member: MemberLookupItem): void {
    this.selected.set(member);
    this.error.set(null);
    this.reportsApi.memberRate(member.id, this.from(), this.to()).subscribe({
      next: (r) => this.rate.set(r),
      error: (err: HttpErrorResponse) => { this.error.set(messageForError(err)); this.selected.set(null); },
    });
  }

  reset(): void {
    this.selected.set(null);
    this.rate.set(null);
    this.results.set([]);
    this.searched.set(false);
    this.query = '';
  }
}
