import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { MemberProfilesApi } from './member-profiles-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/members`;

describe('MemberProfilesApi', () => {
  let api: MemberProfilesApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(MemberProfilesApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('get / assign / revoke ciblent la bonne URL et méthode', () => {
    api.get(7).subscribe();
    http.expectOne({ url: `${BASE}/7/bureau-profiles`, method: 'GET' }).flush({});

    api.assign(7, 2).subscribe();
    const post = http.expectOne({ url: `${BASE}/7/bureau-profiles`, method: 'POST' });
    expect(post.request.body).toEqual({ profileId: 2 });
    post.flush(null);

    api.revoke(7, 2).subscribe();
    http.expectOne({ url: `${BASE}/7/bureau-profiles/2`, method: 'DELETE' }).flush(null);
  });
});
