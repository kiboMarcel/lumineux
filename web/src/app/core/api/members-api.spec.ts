import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { MembersApi } from './members-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/members`;

describe('MembersApi', () => {
  let api: MembersApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(MembersApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('search encode query + pagination', () => {
    api.search('doe', 2, 20).subscribe();
    const req = http.expectOne((r) => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('query')).toBe('doe');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ page: 2, pageSize: 20, total: 0, items: [] });
  });

  it('search sans query n\'ajoute pas le paramètre', () => {
    api.search(null, 1, 20).subscribe();
    const req = http.expectOne((r) => r.url === BASE);
    expect(req.request.params.has('query')).toBe(false);
    req.flush({ page: 1, pageSize: 20, total: 0, items: [] });
  });

  it('get / create / update ciblent la bonne URL et méthode', () => {
    api.get(5).subscribe();
    http.expectOne({ url: `${BASE}/5`, method: 'GET' }).flush({});

    api.create({ lastName: 'D', firstName: 'J', gender: 'F', antennaId: 1 }).subscribe();
    http.expectOne({ url: BASE, method: 'POST' }).flush({});

    api.update(5, { lastName: 'D', firstName: 'J', gender: 'F', antennaId: 1 }).subscribe();
    http.expectOne({ url: `${BASE}/5`, method: 'PUT' }).flush({});
  });
});
