import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from '../../core/api/auth-api';
import { ForgotPasswordComponent } from './forgot-password.component';

describe('ForgotPasswordComponent (US3, FR-012 anti-énumération)', () => {
  const authApi = { forgotPassword: vi.fn() };

  beforeEach(() => {
    authApi.forgotPassword.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthApi, useValue: authApi },
      ],
    });
  });

  it('affiche le message générique de succès', () => {
    authApi.forgotPassword.mockReturnValue(of({ message: 'Message serveur générique' }));
    const comp = TestBed.createComponent(ForgotPasswordComponent).componentInstance;
    comp.form.setValue({ reference: 'LUM-1' });
    comp.submit();
    expect(comp.message()).toBe('Message serveur générique');
  });

  it('affiche un message générique MÊME en cas d\'erreur (ne rien divulguer)', () => {
    authApi.forgotPassword.mockReturnValue(throwError(() => new Error('boom')));
    const comp = TestBed.createComponent(ForgotPasswordComponent).componentInstance;
    comp.form.setValue({ reference: 'INEXISTANT' });
    comp.submit();
    expect(comp.message()).toContain('Si un compte correspond');
  });
});
