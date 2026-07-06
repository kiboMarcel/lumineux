import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AntennasApi } from '../../../core/api/antennas-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { AntennaFormComponent } from './antenna-form.component';

describe('AntennaFormComponent (US2/US3)', () => {
  const api = { create: vi.fn(), update: vi.fn(), get: vi.fn() };
  const referenceApi = { districts: vi.fn() };
  let routeId: string | null = null;

  beforeEach(() => {
    Object.values(api).forEach((f) => f.mockReset());
    referenceApi.districts.mockReset();
    routeId = null;
    referenceApi.districts.mockReturnValue(of([{ id: 9, code: 'D9', label: 'District 9' }]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AntennasApi, useValue: api },
        { provide: ReferenceApi, useValue: referenceApi },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeId } } } },
      ],
    });
  });

  function create() {
    return TestBed.createComponent(AntennaFormComponent).componentInstance;
  }

  it('charge les districts et invalide un formulaire incomplet (création)', () => {
    const comp = create();
    expect(comp.isEdit()).toBe(false);
    expect(comp.districts()).toHaveLength(1);
    expect(comp.form.invalid).toBe(true);
  });

  it('crée une antenne et navigue vers la liste', () => {
    api.create.mockReturnValue(of({ id: 5, code: 'ANT-9', label: 'Neuf', districtId: 9, status: 'Active' }));
    const comp = create();
    const nav = vi.spyOn(TestBed.inject(Router), 'navigate');
    comp.form.setValue({ code: 'ANT-9', label: 'Neuf', districtId: 9 });

    comp.submit();

    expect(api.create).toHaveBeenCalledWith({ code: 'ANT-9', label: 'Neuf', districtId: 9 });
    expect(nav).toHaveBeenCalledWith(['/antennas']);
  });

  it('mappe le conflit duplicate_code', () => {
    api.create.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'duplicate_code' } })));
    const comp = create();
    comp.form.setValue({ code: 'ANT-DUP', label: 'X', districtId: 9 });

    comp.submit();

    expect(comp.error()).toContain('déjà utilisé');
  });

  it('en édition : préremplit, code en lecture seule, update sans le code', () => {
    routeId = '5';
    api.get.mockReturnValue(of({ id: 5, code: 'ANT-5', label: 'Cinq', districtId: 9, status: 'Active' }));
    api.update.mockReturnValue(of({ id: 5, code: 'ANT-5', label: 'Cinq modifié', districtId: 9, status: 'Active' }));
    const comp = create();
    const nav = vi.spyOn(TestBed.inject(Router), 'navigate');

    expect(comp.isEdit()).toBe(true);
    expect(comp.form.controls.code.disabled).toBe(true); // code immuable
    expect(comp.form.controls.label.value).toBe('Cinq');

    comp.form.controls.label.setValue('Cinq modifié');
    comp.submit();

    expect(api.update).toHaveBeenCalledWith(5, { label: 'Cinq modifié', districtId: 9 });
    expect(nav).toHaveBeenCalledWith(['/antennas']);
  });
});
