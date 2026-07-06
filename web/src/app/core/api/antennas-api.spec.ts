import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { AntennasApi } from './antennas-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/antennas`;

describe('AntennasApi', () => {
  let api: AntennasApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AntennasApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('list / get ciblent la bonne URL', () => {
    api.list().subscribe();
    http.expectOne({ url: BASE, method: 'GET' }).flush([]);

    api.get(5).subscribe();
    http.expectOne({ url: `${BASE}/5`, method: 'GET' }).flush({});
  });

  it('create (POST) / update (PUT) envoient le corps', () => {
    api.create({ code: 'ANT-1', label: 'A', districtId: 2 }).subscribe();
    const create = http.expectOne({ url: BASE, method: 'POST' });
    expect(create.request.body).toEqual({ code: 'ANT-1', label: 'A', districtId: 2 });
    create.flush({});

    api.update(5, { label: 'B', districtId: 3 }).subscribe();
    const update = http.expectOne({ url: `${BASE}/5`, method: 'PUT' });
    expect(update.request.body).toEqual({ label: 'B', districtId: 3 });
    update.flush({});
  });

  it('deactivate / activate ciblent les sous-ressources (POST)', () => {
    api.deactivate(5).subscribe();
    http.expectOne({ url: `${BASE}/5/deactivate`, method: 'POST' }).flush({});

    api.activate(5).subscribe();
    http.expectOne({ url: `${BASE}/5/activate`, method: 'POST' }).flush({});
  });
});
