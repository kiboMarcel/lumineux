import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MembersApi } from '../../../core/api/members-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { MemberFormComponent } from './member-form.component';

const membersApi = { create: vi.fn(), update: vi.fn(), get: vi.fn() };
const antenna = { id: 1, code: 'A1', label: 'Antenne 1' };
const referenceApi = {
  antennas: vi.fn(() => of([antenna])),
  civilities: vi.fn(() => of([])),
  cities: vi.fn(() => of([])),
  districts: vi.fn(() => of([])),
  countries: vi.fn(() => of([])),
};

type Ref = { id: number; code: string; label: string };

function setup(opts: { id?: string; antennas?: Ref[] } = {}) {
  TestBed.resetTestingModule();
  referenceApi.antennas.mockReturnValue(of(opts.antennas ?? [antenna]));
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      { provide: MembersApi, useValue: membersApi },
      { provide: ReferenceApi, useValue: referenceApi },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(opts.id ? { id: opts.id } : {}) } } },
    ],
  });
  return TestBed.createComponent(MemberFormComponent).componentInstance;
}

function fillValid(comp: MemberFormComponent) {
  comp.form.patchValue({ lastName: 'Doe', firstName: 'Jane', gender: 'F', antennaId: 1 });
}

describe('MemberFormComponent — création (US2)', () => {
  beforeEach(() => { membersApi.create.mockReset(); membersApi.update.mockReset(); });

  it('crée un membre et affiche la remise des identifiants', () => {
    membersApi.create.mockReturnValue(of({
      member: { id: 9, reference: 'LUM-9' }, loginId: 'LUM-9', credentialsDelivery: 'BureauHandout', temporaryPassword: 'Temp1234',
    }));
    const comp = setup();
    fillValid(comp);
    comp.submit(false);
    expect(comp.created()?.temporaryPassword).toBe('Temp1234');
  });

  it('propose la confirmation sur homonymie puis crée avec confirmDuplicate', () => {
    membersApi.create.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'duplicate_name' } })),
    );
    const comp = setup();
    fillValid(comp);
    comp.submit(false);
    expect(comp.homonym()).toBe(true);

    membersApi.create.mockReturnValueOnce(of({ member: { id: 9, reference: 'LUM-9' }, loginId: 'LUM-9', credentialsDelivery: 'EmailSent' }));
    comp.confirmHomonym();
    expect(membersApi.create.mock.calls[1][0]).toMatchObject({ confirmDuplicate: true });
    expect(comp.created()).not.toBeNull();
  });

  it('affiche une erreur bloquante sur contact déjà utilisé (non confirmable)', () => {
    membersApi.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'contact_in_use' } })),
    );
    const comp = setup();
    fillValid(comp);
    comp.submit(false);
    expect(comp.homonym()).toBe(false);
    expect(comp.error()).toBeTruthy();
  });

  it('empêche la création si aucune antenne active', () => {
    const comp = setup({ antennas: [] });
    fillValid(comp);
    expect(comp.noAntenna()).toBe(true);
    comp.submit(false);
    expect(membersApi.create).not.toHaveBeenCalled();
  });

  it('envoie la profession nettoyée (trim) — feature 030', () => {
    membersApi.create.mockReturnValue(of({ member: { id: 9, reference: 'LUM-9' }, loginId: 'LUM-9', credentialsDelivery: 'EmailSent' }));
    const comp = setup();
    fillValid(comp);
    comp.form.patchValue({ profession: '  Enseignant  ' });
    comp.submit(false);
    expect(membersApi.create.mock.calls[0][0]).toMatchObject({ profession: 'Enseignant' });
  });

  it('envoie profession null quand le champ est vide — feature 030', () => {
    membersApi.create.mockReturnValue(of({ member: { id: 9, reference: 'LUM-9' }, loginId: 'LUM-9', credentialsDelivery: 'EmailSent' }));
    const comp = setup();
    fillValid(comp);
    comp.submit(false);
    expect(membersApi.create.mock.calls[0][0].profession).toBeNull();
  });
});

describe('MemberFormComponent — édition (US3)', () => {
  beforeEach(() => { membersApi.update.mockReset(); membersApi.get.mockReset(); });

  it('précharge la fiche, expose la référence et enregistre via update', () => {
    membersApi.get.mockReturnValue(of({
      id: 5, reference: 'LUM-5', lastName: 'Doe', firstName: 'Jane', gender: 'F', antennaId: 1, status: 'Active',
    }));
    membersApi.update.mockReturnValue(of({ id: 5 }));
    const comp = setup({ id: '5' });

    expect(comp.isEdit()).toBe(true);
    expect(comp.reference()).toBe('LUM-5');

    const router = TestBed.inject(Router);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    comp.submit(false);
    expect(membersApi.update).toHaveBeenCalledWith(5, expect.objectContaining({ lastName: 'Doe' }));
    expect(navSpy).toHaveBeenCalledWith(['/members', 5]);
  });

  it('précharge la profession puis l\'efface en envoyant null — feature 030', () => {
    membersApi.get.mockReturnValue(of({
      id: 5, reference: 'LUM-5', lastName: 'Doe', firstName: 'Jane', gender: 'F', antennaId: 1, status: 'Active', profession: 'Infirmier',
    }));
    membersApi.update.mockReturnValue(of({ id: 5 }));
    const comp = setup({ id: '5' });

    expect(comp.form.getRawValue().profession).toBe('Infirmier'); // préchargée

    comp.form.patchValue({ profession: '' }); // effacement
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    comp.submit(false);
    expect(membersApi.update.mock.calls[0][1].profession).toBeNull();
  });
});
