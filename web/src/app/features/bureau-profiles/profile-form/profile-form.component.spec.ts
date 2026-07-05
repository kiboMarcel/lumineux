import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BureauProfilesApi } from '../../../core/api/bureau-profiles-api';
import { PermissionsApi } from '../../../core/api/permissions-api';
import { ProfileFormComponent } from './profile-form.component';

const api = { create: vi.fn(), update: vi.fn(), get: vi.fn() };
const permissionsApi = { list: vi.fn() };

function setup(opts: { id?: string; catalog?: { code: string; label: string }[] } = {}) {
  TestBed.resetTestingModule();
  permissionsApi.list.mockReturnValue(of(opts.catalog ?? [{ code: 'manage_members', label: 'Gérer les membres' }]));
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
      { provide: BureauProfilesApi, useValue: api },
      { provide: PermissionsApi, useValue: permissionsApi },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(opts.id ? { id: opts.id } : {}) } } },
    ],
  });
  return TestBed.createComponent(ProfileFormComponent).componentInstance;
}

describe('ProfileFormComponent (US2)', () => {
  beforeEach(() => { api.create.mockReset(); api.update.mockReset(); api.get.mockReset(); });

  it('crée un profil et redirige vers son détail', () => {
    api.create.mockReturnValue(of({ id: 9 }));
    const comp = setup();
    comp.form.setValue({ name: 'Nouveau', description: '' });
    comp.toggle('manage_members');
    const navSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    comp.submit();
    expect(api.create).toHaveBeenCalledWith(expect.objectContaining({ name: 'Nouveau', permissions: ['manage_members'] }));
    expect(navSpy).toHaveBeenCalledWith(['/bureau-profiles', 9]);
  });

  it('empêche la soumission si le catalogue est vide (G1)', () => {
    const comp = setup({ catalog: [] });
    comp.form.setValue({ name: 'X', description: '' });
    expect(comp.catalogEmpty()).toBe(true);
    comp.submit();
    expect(api.create).not.toHaveBeenCalled();
  });

  it('restitue un conflit (409 duplicate_name) comme erreur bloquante', () => {
    api.create.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'duplicate_name', detail: 'Nom déjà utilisé.' } })));
    const comp = setup();
    comp.form.setValue({ name: 'Doublon', description: '' });
    comp.toggle('manage_members');
    comp.submit();
    expect(comp.error()).toBe('Nom déjà utilisé.');
  });

  it('précharge le profil en édition', () => {
    api.get.mockReturnValue(of({ id: 3, name: 'Admin', description: 'desc', permissions: ['manage_members'], memberCount: 0, members: [] }));
    const comp = setup({ id: '3' });
    expect(comp.isEdit()).toBe(true);
    expect(comp.form.controls.name.value).toBe('Admin');
    expect(comp.selected().has('manage_members')).toBe(true);
  });
});
