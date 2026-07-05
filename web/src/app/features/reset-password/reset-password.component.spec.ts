import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from '../../core/api/auth-api';
import { ResetPasswordComponent } from './reset-password.component';

function configure(token: string) {
  const authApi = { resetPassword: vi.fn() };
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: AuthApi, useValue: authApi },
      { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ token }) } } },
    ],
  });
  return authApi;
}

describe('ResetPasswordComponent (US3, FR-013)', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('lit le jeton depuis l\'URL', () => {
    configure('jeton-abc');
    const comp = TestBed.createComponent(ResetPasswordComponent).componentInstance;
    expect(comp.token).toBe('jeton-abc');
  });

  it('affiche un échec générique sur jeton invalide/expiré', () => {
    const authApi = configure('jeton-abc');
    authApi.resetPassword.mockReturnValue(throwError(() => new Error('401')));
    const comp = TestBed.createComponent(ResetPasswordComponent).componentInstance;
    comp.form.setValue({ newPassword: 'Passw0rd', confirm: 'Passw0rd' });
    comp.submit();
    expect(comp.error()).toContain('invalide ou expiré');
  });
});
