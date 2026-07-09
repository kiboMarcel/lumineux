import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from '../../core/api/auth-api';
import { SetupApi } from '../../core/api/setup-api';
import { LoginComponent } from './login.component';

describe('LoginComponent (US1, FR-010)', () => {
  const authApi = { login: vi.fn() };
  const setupApi = { status: vi.fn() };

  beforeEach(() => {
    authApi.login.mockReset();
    setupApi.status.mockReset();
    setupApi.status.mockReturnValue(of({ installed: true })); // défaut : instance installée (pas de lien)
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthApi, useValue: authApi },
        { provide: SetupApi, useValue: setupApi },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    });
  });

  it('ne soumet pas un formulaire invalide', () => {
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    comp.submit();
    expect(authApi.login).not.toHaveBeenCalled();
  });

  it('affiche un message non révélateur sur identifiants invalides', () => {
    authApi.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    comp.form.setValue({ reference: 'LUM-1', password: 'wrong' });

    comp.submit();

    expect(comp.error()).toBe('Référence ou mot de passe invalide.');
  });

  it('bascule vers /auth/activate sur password_change_required', () => {
    authApi.login.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 403, error: { code: 'password_change_required' } })),
    );
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    comp.form.setValue({ reference: 'LUM-2', password: 'Temp1234' });

    comp.submit();

    expect(navSpy).toHaveBeenCalledWith(['/auth/activate'], { queryParams: { reference: 'LUM-2' } });
  });

  it('établit la session et redirige en cas de succès', () => {
    authApi.login.mockReturnValue(of({ accessToken: 'tok', tokenType: 'Bearer', expiresAt: '' }));
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    // establish() appellera /auth/me ; on court-circuite en mockant SessionStore.establish
    const session = (comp as unknown as { session: { establish: unknown } }).session;
    vi.spyOn(session as { establish: (t: string) => unknown }, 'establish').mockReturnValue(of({ memberId: 1, displayName: 'X', permissions: [] }));
    comp.form.setValue({ reference: 'LUM-3', password: 'Passw0rd' });

    comp.submit();

    expect(navSpy).toHaveBeenCalledWith('/');
  });

  // Feature 013 — découvrabilité de l'installation

  it('[US1] affiche le lien « Première installation » quand l\'instance n\'est pas installée', () => {
    setupApi.status.mockReturnValue(of({ installed: false }));
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    expect(comp.showSetupLink()).toBe(true);
  });

  it('[US2] masque le lien quand l\'instance est déjà installée', () => {
    setupApi.status.mockReturnValue(of({ installed: true }));
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    expect(comp.showSetupLink()).toBe(false);
  });

  it('affiche/masque le mot de passe : masqué par défaut, bascule à la demande', () => {
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    expect(comp.showPassword()).toBe(false); // masqué par défaut
    comp.showPassword.set(true);
    expect(comp.showPassword()).toBe(true); // affiché après bascule
    comp.showPassword.set(false);
    expect(comp.showPassword()).toBe(false);
  });

  it('[US2] masque le lien (défaut sûr) si le statut est indisponible, sans bloquer la connexion', () => {
    setupApi.status.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    authApi.login.mockReturnValue(of({ accessToken: 'tok', tokenType: 'Bearer', expiresAt: '' }));
    const comp = TestBed.createComponent(LoginComponent).componentInstance;
    expect(comp.showSetupLink()).toBe(false);

    // La connexion reste opérante malgré l'échec du statut.
    const session = (comp as unknown as { session: { establish: unknown } }).session;
    vi.spyOn(session as { establish: (t: string) => unknown }, 'establish').mockReturnValue(of({ memberId: 1, displayName: 'X', permissions: [] }));
    const navSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    comp.form.setValue({ reference: 'LUM-9', password: 'Passw0rd' });
    comp.submit();
    expect(navSpy).toHaveBeenCalledWith('/');
  });
});
