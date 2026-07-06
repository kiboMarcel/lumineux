import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { ReportsApi } from '../../../core/api/reports-api';
import { messageForError } from '../../../core/http/error-messages';
import { TimeSeriesGranularity, TimeSeriesPoint } from '../report.models';

const W = 680, H = 200, PAD_L = 8, PAD_R = 8, PAD_T = 12, PAD_B = 30;
const INNER_W = W - PAD_L - PAD_R;
const INNER_H = H - PAD_T - PAD_B;
const BASELINE = PAD_T + INNER_H;

interface ChartPoint { x: number; y: number; label: string; value: number; showLabel: boolean; }
interface Geometry { hasData: boolean; points: ChartPoint[]; polyline: string; area: string; }

/**
 * Panneau « Évolution » (feature 021) : courbe/aire SVG de l'affluence (présences valides) par
 * intervalle (semaine ISO / mois), consommant l'API série temporelle (020). **Aucun calcul
 * statistique côté client** : les points viennent de l'API ; le composant met à l'échelle les
 * coordonnées (y ∝ valeur) et trace. Réagit aux changements de période / antenne / granularité.
 */
@Component({
  selector: 'app-time-series-chart',
  template: `
    <div class="lx-card" style="margin-top:1rem;">
      <div class="lx-topbar" style="border:0; padding:0; margin-bottom:0.75rem;">
        <h2 style="margin:0; font-size:1.1rem;">Évolution de l'affluence</h2>
        <div class="lx-links" style="margin:0;">
          @for (g of granularities; track g.value) {
            <button type="button" class="lx-btn lx-btn-ghost"
                    [style.font-weight]="granularity() === g.value ? '700' : '400'"
                    (click)="setGranularity(g.value)">{{ g.label }}</button>
          }
        </div>
      </div>

      @if (error()) {
        <p class="lx-error">{{ error() }}</p>
      } @else if (loading()) {
        <p class="lx-muted">Chargement…</p>
      } @else if (!geometry().hasData) {
        <p class="lx-muted">Aucune donnée de présence sur cette période.</p>
      } @else {
        <svg [attr.viewBox]="viewBox" style="width:100%; height:auto;" role="img"
             aria-label="Courbe d'évolution des présences valides">
          <polygon [attr.points]="geometry().area" fill="var(--lx-accent, #2d7)" fill-opacity="0.12" />
          <polyline [attr.points]="geometry().polyline" fill="none" stroke="var(--lx-accent, #2d7)" stroke-width="2" />
          @for (p of geometry().points; track p.label) {
            <circle [attr.cx]="p.x" [attr.cy]="p.y" r="3.5" fill="var(--lx-accent, #2d7)"
                    (pointerenter)="hovered.set(p)" (pointerleave)="hovered.set(null)">
              <title>{{ p.label }} : {{ p.value }} présence(s) valide(s)</title>
            </circle>
            @if (p.showLabel) {
              <text [attr.x]="p.x" [attr.y]="baseline + 16" text-anchor="middle" font-size="10"
                    fill="var(--lx-muted, #888)">{{ p.label }}</text>
            }
          }
        </svg>

        <p class="lx-muted" style="min-height:1.2em;">
          @if (hovered(); as h) { {{ h.label }} : <strong>{{ h.value }}</strong> présence(s) valide(s) }
          @else { Survolez un point pour lire sa valeur. }
        </p>
      }
    </div>
  `,
  styles: [`.lx-error { color: var(--lx-danger, #c0392b); }`],
})
export class TimeSeriesChartComponent {
  private readonly reportsApi = inject(ReportsApi);

  readonly from = input.required<string>();
  readonly to = input.required<string>();
  readonly antennaId = input<number | null>(null);

  readonly granularities: { value: TimeSeriesGranularity; label: string }[] = [
    { value: 'Month', label: 'Par mois' },
    { value: 'Week', label: 'Par semaine' },
  ];

  readonly granularity = signal<TimeSeriesGranularity>('Month');
  readonly points = signal<TimeSeriesPoint[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly hovered = signal<ChartPoint | null>(null);

  readonly baseline = BASELINE;
  readonly viewBox = `0 0 ${W} ${H}`;

  constructor() {
    // Réactivité au contexte appliqué : recharge quand la période ou l'antenne change.
    // La granularité, elle, est pilotée par setGranularity() (lue en untracked dans reload) afin de
    // ne PAS devenir une dépendance de cet effet — sinon double rechargement à chaque bascule.
    effect(() => {
      this.from();
      this.to();
      this.antennaId();
      this.reload();
    });
  }

  setGranularity(value: TimeSeriesGranularity): void {
    this.granularity.set(value);
    this.reload();
  }

  reload(): void {
    const from = this.from();
    const to = this.to();
    if (!from || !to) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    // Lues hors suivi réactif : reload() est déclenché explicitement (effet OU setGranularity).
    const granularity = untracked(this.granularity);
    const antennaId = untracked(this.antennaId);
    this.reportsApi.timeSeries(from, to, granularity, antennaId).subscribe({
      next: (res) => { this.points.set(res.points); this.loading.set(false); },
      error: (err: HttpErrorResponse) => { this.error.set(messageForError(err)); this.loading.set(false); },
    });
  }

  /** Géométrie du tracé (présentation seule : coordonnées proportionnelles aux valeurs de l'API). */
  readonly geometry = computed<Geometry>(() => {
    const pts = this.points();
    if (pts.length === 0) {
      return { hasData: false, points: [], polyline: '', area: '' };
    }

    const max = Math.max(1, ...pts.map((p) => p.validAttendanceCount));
    const n = pts.length;
    const labelEvery = Math.max(1, Math.ceil(n / 8));

    const coords: ChartPoint[] = pts.map((p, i) => ({
      x: n > 1 ? PAD_L + i * (INNER_W / (n - 1)) : PAD_L + INNER_W / 2,
      y: PAD_T + INNER_H - (p.validAttendanceCount / max) * INNER_H,
      label: p.label,
      value: p.validAttendanceCount,
      showLabel: i % labelEvery === 0 || i === n - 1,
    }));

    const polyline = coords.map((c) => `${round(c.x)},${round(c.y)}`).join(' ');
    const area = `${round(coords[0].x)},${BASELINE} ${polyline} ${round(coords[n - 1].x)},${BASELINE}`;
    return { hasData: true, points: coords, polyline, area };
  });
}

function round(n: number): number {
  return Math.round(n * 100) / 100;
}
