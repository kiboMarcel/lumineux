import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { SessionStore } from '../session/session-store';
import { authGuard, guestOnly, permissionGuard } from './guards';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;

function authenticate(permissions: string[] = []): void {
  const store = TestBed.inject(SessionStore);
  const http = TestBed.inject(HttpTestingController);
  store.establish('tok').subscribe();
  http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'X', permissions });
}

const routeWith = (data: Record<string, unknown> = {}) =>
  ({ data } as unknown as ActivatedRouteSnapshot);
const state = (url: string) => ({ url } as RouterStateSnapshot);

describe('gardes', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  it('authGuard : redirige vers /login (avec returnUrl) sans session', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(routeWith(), state('/account')));
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toContain('/login');
    expect((result as UrlTree).toString()).toContain('returnUrl');
  });

  it('authGuard : autorise avec une session', () => {
    authenticate();
    const result = TestBed.runInInjectionContext(() => authGuard(routeWith(), state('/')));
    expect(result).toBe(true);
  });

  it('permissionGuard : autorise si le droit requis est présent', () => {
    authenticate(['manage_members']);
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWith({ permission: 'manage_members' }), state('/members')),
    );
    expect(result).toBe(true);
  });

  it('permissionGuard : refuse (redirige) si le droit manque', () => {
    authenticate([]);
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWith({ permission: 'manage_members' }), state('/members')),
    );
    expect(result).toBeInstanceOf(UrlTree);
  });

  it('permissionGuard any-of : autorise si l\'un des droits est présent', () => {
    authenticate(['manage_members']);
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWith({ anyPermissions: ['manage_bureau_profiles', 'manage_members'] }), state('/bureau-profiles')),
    );
    expect(result).toBe(true);
  });

  it('permissionGuard any-of : refuse si aucun des droits n\'est présent', () => {
    authenticate([]);
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard(routeWith({ anyPermissions: ['manage_bureau_profiles', 'manage_members'] }), state('/bureau-profiles')),
    );
    expect(result).toBeInstanceOf(UrlTree);
  });

  it('guestOnly : redirige un utilisateur déjà connecté', () => {
    authenticate();
    const result = TestBed.runInInjectionContext(() => guestOnly(routeWith(), state('/login')));
    expect(result).toBeInstanceOf(UrlTree);
  });
});
