import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../../../../environments/environment';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { MemberProfilesApi } from '../../../core/api/member-profiles-api';
import { SessionStore } from '../../../core/session/session-store';
import { MemberProfilesComponent } from './member-profiles.component';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;
const mpApi = { get: vi.fn(), assign: vi.fn(), revoke: vi.fn() };
const profilesApi = { list: vi.fn(() => of([{ id: 1, name: 'Admin', description: null, permissions: [], memberCount: 0 }, { id: 2, name: 'Bureau', description: null, permissions: [], memberCount: 0 }])) };

const memberData = {
  member: { id: 7, reference: 'LUM-7', fullName: 'Jane', status: 'Active' },
  profiles: [{ id: 1, name: 'Admin', description: null, permissions: ['manage_bureau_profiles'], memberCount: 1 }],
  effectivePermissions: ['manage_bureau_profiles'],
};

function setup(permissions: string[]) {
  TestBed.resetTestingModule();
  mpApi.get.mockReturnValue(of(memberData));
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: MemberProfilesApi, useValue: mpApi },
      { provide: BureauProfilesApi, useValue: profilesApi },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '7' }) } } },
    ],
  });
  const store = TestBed.inject(SessionStore);
  const http = TestBed.inject(HttpTestingController);
  store.establish('tok').subscribe();
  http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'X', permissions });
  return TestBed.createComponent(MemberProfilesComponent).componentInstance;
}

describe('MemberProfilesComponent (US3)', () => {
  beforeEach(() => { mpApi.get.mockReset(); mpApi.assign.mockReset(); mpApi.revoke.mockReset(); });

  it('affiche droits effectifs et profils attribués ; assignable exclut les déjà attribués', () => {
    const comp = setup(['manage_bureau_profiles']);
    expect(comp.data()?.effectivePermissions).toEqual(['manage_bureau_profiles']);
    expect(comp.assignable().map((p) => p.id)).toEqual([2]); // 1 déjà attribué → exclu
    expect(comp.canWrite()).toBe(true);
  });

  it('masque les actions d\'écriture pour un lecteur', () => {
    expect(setup(['manage_members']).canWrite()).toBe(false);
  });

  it('attribue un profil (idempotent côté API) puis recharge', () => {
    mpApi.assign.mockReturnValue(of(void 0));
    const comp = setup(['manage_bureau_profiles']);
    comp.toAssign = 2;
    comp.assign();
    expect(mpApi.assign).toHaveBeenCalledWith(7, 2);
    expect(mpApi.get).toHaveBeenCalledTimes(2); // chargement initial + reload
  });

  it('restitue le garde-fou dernier administrateur à la révocation', () => {
    mpApi.revoke.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'last_administrator', detail: 'Dernier administrateur.' } })));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = setup(['manage_bureau_profiles']);
    comp.revoke(1);
    expect(comp.error()).toBe('Dernier administrateur.');
  });
});
