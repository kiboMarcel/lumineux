import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportsApi } from '../../../core/api/reports-api';
import { TimeSeriesChartComponent } from './time-series-chart.component';

describe('TimeSeriesChartComponent (US1/US2)', () => {
  const api = { timeSeries: vi.fn() };

  const series = (points: { label: string; validAttendanceCount: number }[]) => ({
    from: '2026-01-01', to: '2026-03-31', granularity: 'Month',
    points: points.map((p) => ({ periodStart: '', sessionCount: 1, ...p })),
  });

  beforeEach(() => {
    api.timeSeries.mockReset();
    api.timeSeries.mockReturnValue(of(series([
      { label: '2026-01', validAttendanceCount: 3 },
      { label: '2026-02', validAttendanceCount: 0 }, // intervalle vide
      { label: '2026-03', validAttendanceCount: 6 }, // max
    ])));
    TestBed.configureTestingModule({ providers: [{ provide: ReportsApi, useValue: api }] });
  });

  function create() {
    const fixture = TestBed.createComponent(TimeSeriesChartComponent);
    fixture.componentRef.setInput('from', '2026-01-01');
    fixture.componentRef.setInput('to', '2026-03-31');
    return fixture.componentInstance;
  }

  it('charge la série et calcule des coordonnées proportionnelles aux valeurs', () => {
    const comp = create();
    comp.reload();

    const geo = comp.geometry();
    expect(geo.hasData).toBe(true);
    expect(geo.points).toHaveLength(3);
    // La valeur max (6) est en haut (y minimal) ; la valeur 3 est à mi-hauteur ; 0 est sur la ligne de base.
    const [jan, feb, mar] = geo.points;
    expect(mar.y).toBeLessThan(jan.y); // 6 plus haut que 3
    expect(feb.y).toBe(comp.baseline); // 0 → ligne de base (série continue)
    expect(mar.value).toBe(6);
  });

  it('granularité par défaut = Month', () => {
    expect(create().granularity()).toBe('Month');
  });

  it('recharge en changeant la granularité, sans double appel', () => {
    const comp = create();
    api.timeSeries.mockClear();

    comp.setGranularity('Week');

    expect(comp.granularity()).toBe('Week');
    expect(api.timeSeries).toHaveBeenCalledWith('2026-01-01', '2026-03-31', 'Week', null);
    expect(api.timeSeries).toHaveBeenCalledTimes(1); // un seul appel (pas de double via l'effet)
  });

  it('recharge (une seule fois) quand le filtre d\'antenne change — contexte appliqué', () => {
    const fixture = TestBed.createComponent(TimeSeriesChartComponent);
    fixture.componentRef.setInput('from', '2026-01-01');
    fixture.componentRef.setInput('to', '2026-03-31');
    fixture.detectChanges(); // l'effet déclenche le chargement initial
    api.timeSeries.mockClear();

    fixture.componentRef.setInput('antennaId', 5);
    fixture.detectChanges(); // l'effet réagit au changement d'antenne

    expect(api.timeSeries).toHaveBeenCalledTimes(1);
    expect(api.timeSeries).toHaveBeenCalledWith('2026-01-01', '2026-03-31', 'Month', 5);
  });

  it('affiche un état vide quand la série est vide', () => {
    api.timeSeries.mockReturnValue(of(series([])));
    const comp = create();
    comp.reload();
    expect(comp.geometry().hasData).toBe(false);
  });

  it('mappe une erreur de l\'API', () => {
    api.timeSeries.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { detail: 'Plage invalide' } })));
    const comp = create();
    comp.reload();
    expect(comp.error()).toBe('Plage invalide');
  });
});
