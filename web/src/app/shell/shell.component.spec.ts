import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../environments/environment';
import { SessionStore } from '../core/session/session-store';
import { ShellComponent } from './shell.component';

const ME_URL = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth/me`;

function authenticate(permissions: string[]): void {
  const store = TestBed.inject(SessionStore);
  const http = TestBed.inject(HttpTestingController);
  store.establish('tok').subscribe();
  http.expectOne(ME_URL).flush({ memberId: 1, displayName: 'Jane', permissions });
}

describe('ShellComponent — navigation RBAC (SC-003)', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  it("affiche les modules autorisés (dont Profils en lecture élargie)", () => {
    // manage_members ouvre « Membres » et — en lecture élargie any-of — « Profils du bureau ».
    authenticate(['manage_members']);
    const comp = TestBed.createComponent(ShellComponent).componentInstance;
    expect(comp.visibleModules().map((m) => m.label)).toEqual(['Membres', 'Profils du bureau']);
  });

  it('affiche Profils du bureau pour un administrateur des profils', () => {
    authenticate(['manage_bureau_profiles']);
    const comp = TestBed.createComponent(ShellComponent).componentInstance;
    expect(comp.visibleModules().map((m) => m.label)).toEqual(['Profils du bureau']);
  });

  it("n'affiche aucun module de gestion sans droit", () => {
    authenticate([]);
    const comp = TestBed.createComponent(ShellComponent).componentInstance;
    expect(comp.visibleModules()).toHaveLength(0);
  });
});
