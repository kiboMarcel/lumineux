import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from '../../core/api/auth-api';
import { ActivateComponent } from './activate.component';

describe('ActivateComponent (US2, FR-011)', () => {
  const authApi = { activate: vi.fn() };

  beforeEach(() => {
    authApi.activate.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthApi, useValue: authApi },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ reference: 'LUM-9' }) } } },
      ],
    });
  });

  it('pré-remplit la référence depuis l\'URL', () => {
    const comp = TestBed.createComponent(ActivateComponent).componentInstance;
    expect(comp.form.controls.reference.value).toBe('LUM-9');
  });

  it('refuse un nouveau mot de passe identique au temporaire', () => {
    const comp = TestBed.createComponent(ActivateComponent).componentInstance;
    comp.form.setValue({ reference: 'LUM-9', temporaryPassword: 'Temp1234', newPassword: 'Temp1234', confirm: 'Temp1234' });
    expect(comp.form.errors?.['mustDiffer']).toBe(true);
    comp.submit();
    expect(authApi.activate).not.toHaveBeenCalled();
  });
});
