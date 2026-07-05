import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { BureauProfilesApi } from './bureau-profiles-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/bureau-profiles`;

describe('BureauProfilesApi', () => {
  let api: BureauProfilesApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(BureauProfilesApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('cible les bonnes URL et méthodes', () => {
    api.list().subscribe();
    http.expectOne({ url: BASE, method: 'GET' }).flush([]);

    api.get(3).subscribe();
    http.expectOne({ url: `${BASE}/3`, method: 'GET' }).flush({});

    api.create({ name: 'A', description: null, permissions: ['manage_members'] }).subscribe();
    http.expectOne({ url: BASE, method: 'POST' }).flush({});

    api.update(3, { name: 'A', description: null, permissions: [] }).subscribe();
    http.expectOne({ url: `${BASE}/3`, method: 'PUT' }).flush({});

    api.remove(3).subscribe();
    http.expectOne({ url: `${BASE}/3`, method: 'DELETE' }).flush(null);
  });
});
