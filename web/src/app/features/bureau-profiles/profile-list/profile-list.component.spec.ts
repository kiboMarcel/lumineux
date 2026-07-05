import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../../../../environments/environment';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { SessionStore } from '../../../core/session/session-store';
import { ProfileListComponent } from './profile-list.component';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;
const api = { list: vi.fn(() => of([{ id: 1, name: 'Admin', description: null, permissions: ['manage_bureau_profiles'], memberCount: 2 }])) };

function setup(permissions: string[]) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), { provide: BureauProfilesApi, useValue: api }],
  });
  const store = TestBed.inject(SessionStore);
  const http = TestBed.inject(HttpTestingController);
  store.establish('tok').subscribe();
  http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'X', permissions });
  return TestBed.createComponent(ProfileListComponent).componentInstance;
}

describe('ProfileListComponent (US1)', () => {
  beforeEach(() => api.list.mockClear());

  it('charge la liste des profils', () => {
    const comp = setup(['manage_members']);
    expect(comp.profiles()).toHaveLength(1);
  });

  it('autorise l\'écriture pour un administrateur des profils, pas pour un lecteur', () => {
    expect(setup(['manage_bureau_profiles']).canWrite()).toBe(true);
    expect(setup(['manage_members']).canWrite()).toBe(false);
  });
});
