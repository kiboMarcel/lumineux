import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { ReportsApi } from './reports-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/reports/attendance`;

describe('ReportsApi', () => {
  let api: ReportsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ReportsApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('antennaSummary passe la période et le filtre d\'antenne', () => {
    api.antennaSummary('2026-06-01', '2026-06-30', 3).subscribe();
    const req = http.expectOne((r) => r.url === `${BASE}/antenna-summary`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('from')).toBe('2026-06-01');
    expect(req.request.params.get('to')).toBe('2026-06-30');
    expect(req.request.params.get('antennaId')).toBe('3');
    req.flush({ from: '2026-06-01', to: '2026-06-30', items: [] });
  });

  it('antennaSummary omet antennaId si absent', () => {
    api.antennaSummary('2026-06-01', '2026-06-30').subscribe();
    const req = http.expectOne((r) => r.url === `${BASE}/antenna-summary`);
    expect(req.request.params.has('antennaId')).toBe(false);
    req.flush({ from: '', to: '', items: [] });
  });

  it('antennaSummaryCsv demande un Blob', () => {
    api.antennaSummaryCsv('2026-06-01', '2026-06-30').subscribe();
    const req = http.expectOne((r) => r.url === `${BASE}/antenna-summary.csv`);
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['x'], { type: 'text/csv' }));
  });

  it('memberRate cible member-rate avec les paramètres', () => {
    api.memberRate(42, '2026-06-01', '2026-06-30').subscribe();
    const req = http.expectOne((r) => r.url === `${BASE}/member-rate`);
    expect(req.request.params.get('memberId')).toBe('42');
    req.flush({});
  });
});
