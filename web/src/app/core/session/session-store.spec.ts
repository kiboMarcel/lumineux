import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { SessionStore } from './session-store';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;

describe('SessionStore', () => {
  let store: SessionStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(SessionStore);
    http = TestBed.inject(HttpTestingController);
  });

  it('établit la session : jeton mémorisé + identité/droits chargés depuis /me', () => {
    store.establish('tok').subscribe();
    expect(store.accessToken()).toBe('tok');

    http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'Jane Doe', permissions: ['manage_members'] });

    expect(store.isAuthenticated()).toBe(true);
    expect(store.currentUser()?.displayName).toBe('Jane Doe');
    expect(store.permissions()).toEqual(['manage_members']);
    expect(store.hasPermission('manage_members')).toBe(true);
    expect(store.hasPermission('manage_attendance')).toBe(false);
  });

  it('clear() purge la session', () => {
    store.establish('tok').subscribe();
    http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'X', permissions: [] });

    store.clear();

    expect(store.accessToken()).toBeNull();
    expect(store.currentUser()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });
});
