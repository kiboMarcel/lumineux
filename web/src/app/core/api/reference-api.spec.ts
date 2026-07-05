import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { ReferenceApi } from './reference-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/reference`;

describe('ReferenceApi', () => {
  let api: ReferenceApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ReferenceApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('chaque nomenclature cible son endpoint en GET', () => {
    api.antennas().subscribe();
    http.expectOne({ url: `${BASE}/antennas`, method: 'GET' }).flush([]);

    api.civilities().subscribe();
    http.expectOne({ url: `${BASE}/civilities`, method: 'GET' }).flush([]);

    api.cities().subscribe();
    http.expectOne({ url: `${BASE}/cities`, method: 'GET' }).flush([]);

    api.districts().subscribe();
    http.expectOne({ url: `${BASE}/districts`, method: 'GET' }).flush([]);

    api.countries().subscribe();
    http.expectOne({ url: `${BASE}/countries`, method: 'GET' }).flush([]);
  });
});
