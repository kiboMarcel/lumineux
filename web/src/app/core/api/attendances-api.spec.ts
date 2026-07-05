import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { AttendancesApi } from './attendances-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/attendance-sessions`;

describe('AttendancesApi', () => {
  let api: AttendancesApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AttendancesApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('list applique le filtre de statut', () => {
    api.list(7, 'Valid').subscribe();
    const req = http.expectOne((r) => r.url === `${BASE}/7/attendances`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('status')).toBe('Valid');
    req.flush({ sessionId: 7, validCount: 0, items: [] });
  });

  it('addManual POST la présence manuelle', () => {
    api.addManual(7, { memberId: 42 }).subscribe();
    const req = http.expectOne({ url: `${BASE}/7/attendances`, method: 'POST' });
    expect(req.request.body).toEqual({ memberId: 42 });
    req.flush({});
  });

  it('cancel DELETE la présence par memberId', () => {
    api.cancel(7, 42).subscribe();
    http.expectOne({ url: `${BASE}/7/attendances/42`, method: 'DELETE' }).flush(null);
  });
});
