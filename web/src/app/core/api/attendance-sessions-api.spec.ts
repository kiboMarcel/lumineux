import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { AttendanceSessionsApi } from './attendance-sessions-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/attendance-sessions`;

describe('AttendanceSessionsApi', () => {
  let api: AttendanceSessionsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AttendanceSessionsApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('start POST la requête de démarrage', () => {
    api.start({ antennaId: 1, meetingDate: '2026-07-05', qrStepSeconds: 30 }).subscribe();
    const req = http.expectOne({ url: BASE, method: 'POST' });
    expect(req.request.body).toEqual({ antennaId: 1, meetingDate: '2026-07-05', qrStepSeconds: 30 });
    req.flush({});
  });

  it('get / qr / close ciblent la bonne URL et méthode', () => {
    api.get(7).subscribe();
    http.expectOne({ url: `${BASE}/7`, method: 'GET' }).flush({});

    api.qr(7).subscribe();
    http.expectOne({ url: `${BASE}/7/qr`, method: 'GET' }).flush({});

    api.close(7).subscribe();
    http.expectOne({ url: `${BASE}/7/close`, method: 'POST' }).flush({});
  });
});
