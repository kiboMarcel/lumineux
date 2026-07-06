import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportsApi } from '../../../core/api/reports-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { MemberLookupApi } from '../../../core/api/member-lookup-api';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { ReportsDashboardComponent } from './reports-dashboard.component';

describe('ReportsDashboardComponent (US1/US2)', () => {
  const api = { antennaSummary: vi.fn(), antennaSummaryCsv: vi.fn() };
  const referenceApi = { antennas: vi.fn() };
  const lookupApi = { lookup: vi.fn() };
  const notifier = { error: vi.fn(), success: vi.fn(), info: vi.fn(), clear: vi.fn() };

  const summary = {
    from: '2026-06-01', to: '2026-06-30',
    items: [
      { antennaId: 1, antennaLabel: 'A1', sessionCount: 2, validAttendanceCount: 3, averageValidPerSession: 1.5 },
      { antennaId: 2, antennaLabel: 'A2', sessionCount: 4, validAttendanceCount: 6, averageValidPerSession: 1.5 },
    ],
  };

  beforeEach(() => {
    Object.values(api).forEach((f) => f.mockReset());
    referenceApi.antennas.mockReset();
    api.antennaSummary.mockReturnValue(of(summary));
    referenceApi.antennas.mockReturnValue(of([{ id: 1, code: 'A1', label: 'A1' }]));
    TestBed.configureTestingModule({
      providers: [
        { provide: ReportsApi, useValue: api },
        { provide: ReferenceApi, useValue: referenceApi },
        { provide: MemberLookupApi, useValue: lookupApi },
        { provide: NotificationService, useValue: notifier },
      ],
    });
  });

  afterEach(() => vi.unstubAllGlobals());

  function create() {
    return TestBed.createComponent(ReportsDashboardComponent).componentInstance;
  }

  it('charge la synthèse au démarrage et calcule des barres proportionnelles', () => {
    const comp = create();
    expect(api.antennaSummary).toHaveBeenCalled();
    expect(comp.summary()?.items).toHaveLength(2);
    // Max validAttendanceCount = 6 → 3 vaut 50 %, 6 vaut 100 %.
    expect(comp.barPercent(3)).toBe(50);
    expect(comp.barPercent(6)).toBe(100);
  });

  it('applique le filtre d\'antenne', () => {
    const comp = create();
    api.antennaSummary.mockClear();
    comp.antennaId = 2;
    comp.load();
    expect(api.antennaSummary).toHaveBeenLastCalledWith(comp.from, comp.to, 2);
  });

  it('gère l\'état vide', () => {
    api.antennaSummary.mockReturnValue(of({ from: '', to: '', items: [] }));
    const comp = create();
    expect(comp.hasData()).toBe(false);
  });

  it('bloque une plage invalide sans appeler l\'API', () => {
    const comp = create();
    api.antennaSummary.mockClear();
    comp.from = '2026-06-30';
    comp.to = '2026-06-01';
    comp.load();
    expect(comp.error()).toContain('postérieure');
    expect(api.antennaSummary).not.toHaveBeenCalled();
  });

  it('exportPdf déclenche l\'impression du navigateur', () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});
    const comp = create();

    comp.exportPdf();

    expect(printSpy).toHaveBeenCalled();
  });

  it('en-tête d\'impression : libellé d\'antenne appliqué et date de génération', () => {
    const comp = create();
    // Sans filtre → « Toutes ».
    comp.appliedAntennaId.set(null);
    expect(comp.appliedAntennaLabel()).toBe('Toutes');
    // Avec une antenne connue → son libellé.
    comp.antennas.set([{ id: 7, code: 'A7', label: 'Antenne 7' }]);
    comp.appliedAntennaId.set(7);
    expect(comp.appliedAntennaLabel()).toBe('Antenne 7');
    expect(comp.generatedAt).toBeTruthy();
  });

  it('exporte le CSV via un Blob authentifié et déclenche un téléchargement', () => {
    const createObjectURL = vi.fn(() => 'blob:x');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', { createObjectURL, revokeObjectURL });
    api.antennaSummaryCsv.mockReturnValue(of(new Blob(['x'], { type: 'text/csv' })));
    const comp = create();

    comp.exportCsv();

    expect(api.antennaSummaryCsv).toHaveBeenCalledWith(comp.from, comp.to, comp.antennaId);
    expect(createObjectURL).toHaveBeenCalled();
    expect(revokeObjectURL).toHaveBeenCalled();
  });
});
