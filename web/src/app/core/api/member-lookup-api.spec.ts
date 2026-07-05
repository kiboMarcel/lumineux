import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { MemberLookupApi } from './member-lookup-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/members/lookup`;

describe('MemberLookupApi', () => {
  let api: MemberLookupApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(MemberLookupApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('lookup passe le terme de recherche', () => {
    api.lookup('doe').subscribe((res) => expect(res).toHaveLength(1));
    const req = http.expectOne((r) => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('query')).toBe('doe');
    req.flush([{ id: 1, reference: 'LUM-1', fullName: 'Jane Doe', status: 'Active' }]);
  });
});
