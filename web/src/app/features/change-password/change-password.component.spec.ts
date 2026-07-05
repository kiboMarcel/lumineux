import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from '../../core/api/auth-api';
import { ChangePasswordComponent } from './change-password.component';

describe('ChangePasswordComponent (US4, FR-014)', () => {
  const authApi = { changePassword: vi.fn() };

  beforeEach(() => {
    authApi.changePassword.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthApi, useValue: authApi },
      ],
    });
  });

  it('refuse un nouveau mot de passe identique à l\'actuel', () => {
    const comp = TestBed.createComponent(ChangePasswordComponent).componentInstance;
    comp.form.setValue({ currentPassword: 'Passw0rd', newPassword: 'Passw0rd', confirm: 'Passw0rd' });
    expect(comp.form.errors?.['mustDiffer']).toBe(true);
    expect(comp.form.invalid).toBe(true);
  });

  it('confirme le succès du changement', () => {
    authApi.changePassword.mockReturnValue(of(void 0));
    const comp = TestBed.createComponent(ChangePasswordComponent).componentInstance;
    comp.form.setValue({ currentPassword: 'Old12345', newPassword: 'New12345', confirm: 'New12345' });
    comp.submit();
    expect(comp.success()).toBe(true);
  });
});
