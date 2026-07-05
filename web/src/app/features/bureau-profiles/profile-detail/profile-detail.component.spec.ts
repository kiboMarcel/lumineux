import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../../../../environments/environment';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { SessionStore } from '../../../core/session/session-store';
import { ProfileDetailComponent } from './profile-detail.component';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;
const api = { get: vi.fn(), remove: vi.fn() };

function setup(permissions: string[]) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: BureauProfilesApi, useValue: api },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '5' }) } } },
    ],
  });
  const store = TestBed.inject(SessionStore);
  const http = TestBed.inject(HttpTestingController);
  store.establish('tok').subscribe();
  http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'X', permissions });
  return TestBed.createComponent(ProfileDetailComponent).componentInstance;
}

describe('ProfileDetailComponent (US1/US2)', () => {
  beforeEach(() => { api.get.mockReset(); api.remove.mockReset(); });

  it('affiche le détail (droits + titulaires)', () => {
    api.get.mockReturnValue(of({ id: 5, name: 'Admin', description: null, permissions: ['x'], memberCount: 1, members: [{ id: 2, reference: 'LUM-2', fullName: 'Jane', status: 'Active' }] }));
    const comp = setup(['manage_members']);
    expect(comp.profile()?.name).toBe('Admin');
    expect(comp.canWrite()).toBe(false);
  });

  it('signale un profil introuvable (404)', () => {
    api.get.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    expect(setup(['manage_members']).notFound()).toBe(true);
  });

  it('supprime après confirmation et redirige', () => {
    api.get.mockReturnValue(of({ id: 5, name: 'P', description: null, permissions: [], memberCount: 0, members: [] }));
    api.remove.mockReturnValue(of(void 0));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = setup(['manage_bureau_profiles']);
    const navSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    comp.remove(5);
    expect(api.remove).toHaveBeenCalledWith(5);
    expect(navSpy).toHaveBeenCalledWith(['/bureau-profiles']);
  });

  it('restitue un garde-fou (409) comme erreur bloquante', () => {
    api.get.mockReturnValue(of({ id: 5, name: 'P', description: null, permissions: [], memberCount: 0, members: [] }));
    api.remove.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'last_administrator', detail: 'Impossible.' } })));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = setup(['manage_bureau_profiles']);
    comp.remove(5);
    expect(comp.error()).toBeTruthy();
  });
});
