import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { SessionStore } from '../session/session-store';
import { authTokenInterceptor } from './auth-token.interceptor';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;

describe('authTokenInterceptor (FR-002)', () => {
  let http: HttpClient;
  let mock: HttpTestingController;
  let store: SessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authTokenInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    mock = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
  });

  it("n'ajoute aucun en-tête Authorization sans session", () => {
    http.get('/api/v1/data').subscribe();
    const req = mock.expectOne('/api/v1/data');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('ajoute Authorization: Bearer <jeton> quand une session est active', () => {
    store.establish('tok-123').subscribe();
    mock.expectOne(ME_URL).flush({ memberId: 1, displayName: 'X', permissions: [] });

    http.get('/api/v1/data').subscribe();
    const req = mock.expectOne('/api/v1/data');
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-123');
    req.flush({});
  });
});
