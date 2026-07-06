import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportsApi } from '../../../core/api/reports-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { ReferenceItem } from '../../../core/api/reference.models';
import { messageForError } from '../../../core/http/error-messages';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { AntennaAttendanceSummaryResponse } from '../report.models';
import { MemberRateComponent } from '../member-rate/member-rate.component';
import { TimeSeriesChartComponent } from '../time-series-chart/time-series-chart.component';

/**
 * Tableau de bord des rapports (feature 019, US1/US2). Choix d'une période + filtre d'antenne, synthèse
 * par antenne (tableau + barres CSS proportionnelles), export CSV (téléchargement authentifié), et
 * panneau taux membre. Le client **ne recalcule aucune statistique** : il met en forme les valeurs de
 * l'API 018. Réservé au droit `manage_attendance` (garde de route).
 */
@Component({
  selector: 'app-reports-dashboard',
  imports: [FormsModule, MemberRateComponent, TimeSeriesChartComponent],
  template: `
    <div class="lx-card">
      <h1 class="lx-title" style="margin-top:0;">Rapports de présence</h1>

      <form (ngSubmit)="load()" style="display:flex; flex-wrap:wrap; gap:0.75rem; align-items:flex-end;">
        <label style="display:flex; flex-direction:column; gap:0.25rem;">
          <span>Du</span>
          <input type="date" [(ngModel)]="from" name="from" style="padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
        </label>
        <label style="display:flex; flex-direction:column; gap:0.25rem;">
          <span>Au</span>
          <input type="date" [(ngModel)]="to" name="to" style="padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;" />
        </label>
        <label style="display:flex; flex-direction:column; gap:0.25rem;">
          <span>Antenne</span>
          <select [(ngModel)]="antennaId" name="antennaId" style="padding:0.5rem; border:1px solid var(--lx-border); border-radius:8px;">
            <option [ngValue]="null">Toutes</option>
            @for (a of antennas(); track a.id) { <option [ngValue]="a.id">{{ a.label }}</option> }
          </select>
        </label>
        <button type="submit" class="lx-btn" [disabled]="loading()">Afficher</button>
        <button type="button" class="lx-btn lx-btn-ghost" [disabled]="!hasData()" (click)="exportCsv()">Exporter (CSV)</button>
      </form>

      @if (error()) { <div class="lx-alert lx-alert-error" role="alert">{{ error() }}</div> }

      @if (loading()) {
        <p class="lx-muted">Chargement…</p>
      } @else if (summary(); as s) {
        @if (s.items.length === 0) {
          <p class="lx-muted">Aucune donnée de présence sur cette période.</p>
        } @else {
          <div style="overflow-x:auto; margin-top:1rem;">
            <table style="width:100%; border-collapse:collapse;">
              <thead>
                <tr>
                  <th style="text-align:left;">Antenne</th>
                  <th style="text-align:right;">Sessions</th>
                  <th style="text-align:right;">Présences valides</th>
                  <th style="text-align:right;">Moyenne/séance</th>
                  <th style="width:35%;">Comparaison</th>
                </tr>
              </thead>
              <tbody>
                @for (item of s.items; track item.antennaId) {
                  <tr>
                    <td>{{ item.antennaLabel }}</td>
                    <td style="text-align:right;">{{ item.sessionCount }}</td>
                    <td style="text-align:right;">{{ item.validAttendanceCount }}</td>
                    <td style="text-align:right;">{{ item.averageValidPerSession }}</td>
                    <td>
                      <div style="height:12px; background:var(--lx-border); border-radius:6px; overflow:hidden;">
                        <div [style.width.%]="barPercent(item.validAttendanceCount)"
                             style="height:100%; background:var(--lx-accent, #2d7);"
                             [attr.aria-label]="item.validAttendanceCount + ' présences valides'"></div>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      }
    </div>

    @if (appliedFrom() && appliedTo()) {
      <app-time-series-chart [from]="appliedFrom()" [to]="appliedTo()" [antennaId]="appliedAntennaId()" />
    }

    @if (from && to) {
      <app-member-rate [from]="from" [to]="to" />
    }
  `,
  styles: [`.lx-alert-error { color: var(--lx-danger, #c0392b); }`],
})
export class ReportsDashboardComponent {
  private readonly api = inject(ReportsApi);
  private readonly referenceApi = inject(ReferenceApi);
  private readonly notifier = inject(NotificationService);

  readonly antennas = signal<ReferenceItem[]>([]);
  readonly summary = signal<AntennaAttendanceSummaryResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  from = firstDayOfMonth();
  to = today();
  antennaId: number | null = null;

  // Contexte « appliqué » (période + antenne validées) fourni au panneau d'évolution (feature 021),
  // afin qu'il ne se recharge pas à chaque frappe de date mais seulement à la validation.
  readonly appliedFrom = signal<string>('');
  readonly appliedTo = signal<string>('');
  readonly appliedAntennaId = signal<number | null>(null);

  readonly hasData = computed(() => (this.summary()?.items.length ?? 0) > 0);
  private readonly maxValid = computed(() =>
    Math.max(1, ...(this.summary()?.items.map((i) => i.validAttendanceCount) ?? [0])),
  );

  constructor() {
    this.referenceApi.antennas().subscribe((list) => this.antennas.set(list));
    this.load();
  }

  /** Largeur de barre en % (mise en forme proportionnelle ; aucune stat recalculée). */
  barPercent(validCount: number): number {
    return Math.round((validCount / this.maxValid()) * 100);
  }

  load(): void {
    const invalid = this.periodError();
    if (invalid) {
      this.error.set(invalid);
      this.summary.set(null);
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    // Applique le contexte validé au panneau d'évolution (feature 021).
    this.appliedFrom.set(this.from);
    this.appliedTo.set(this.to);
    this.appliedAntennaId.set(this.antennaId);
    this.api.antennaSummary(this.from, this.to, this.antennaId).subscribe({
      next: (res) => { this.summary.set(res); this.loading.set(false); },
      error: (err: HttpErrorResponse) => { this.error.set(messageForError(err)); this.loading.set(false); },
    });
  }

  exportCsv(): void {
    if (this.periodError()) {
      return;
    }
    this.api.antennaSummaryCsv(this.from, this.to, this.antennaId).subscribe({
      next: (blob) => this.triggerDownload(blob, `presence-antennes_${this.from}_${this.to}.csv`),
      error: (err: HttpErrorResponse) => this.notifier.error(messageForError(err)),
    });
  }

  /** Validation locale de la plage (l'API 018 reste l'autorité). */
  private periodError(): string | null {
    if (!this.from || !this.to) {
      return 'Veuillez renseigner les dates de début et de fin.';
    }
    if (this.to < this.from) {
      return 'La date de fin doit être postérieure ou égale à la date de début.';
    }
    return null;
  }

  private triggerDownload(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

function firstDayOfMonth(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}
