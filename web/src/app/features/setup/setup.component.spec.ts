import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SetupApi } from '../../core/api/setup-api';
import { SetupComponent } from './setup.component';

describe('SetupComponent (US5, FR-016)', () => {
  const setupApi = { installFirstAdmin: vi.fn() };

  beforeEach(() => {
    setupApi.installFirstAdmin.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SetupApi, useValue: setupApi },
      ],
    });
  });

  it('signale une instance déjà installée (409)', () => {
    setupApi.installFirstAdmin.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    const comp = TestBed.createComponent(SetupComponent).componentInstance;
    comp.form.setValue({ lastName: 'Doe', firstName: 'Jane', gender: 'F', email: '', password: 'Passw0rd' });
    comp.submit();
    expect(comp.error()).toBe('Une instance est déjà installée.');
  });

  it('valide le format du sexe (M/F)', () => {
    const comp = TestBed.createComponent(SetupComponent).componentInstance;
    comp.form.patchValue({ gender: 'X' });
    expect(comp.form.controls.gender.invalid).toBe(true);
  });
});
