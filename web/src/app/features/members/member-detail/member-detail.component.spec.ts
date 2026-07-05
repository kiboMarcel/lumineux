import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MembersApi } from '../../../core/api/members-api';
import { MemberDetailComponent } from './member-detail.component';

describe('MemberDetailComponent (US1)', () => {
  const api = { get: vi.fn() };

  function setup() {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: MembersApi, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '7' }) } } },
      ],
    });
    return TestBed.createComponent(MemberDetailComponent).componentInstance;
  }

  beforeEach(() => api.get.mockReset());

  it('affiche la fiche récupérée', () => {
    api.get.mockReturnValue(of({ id: 7, reference: 'LUM-7', firstName: 'Jane', lastName: 'Doe', status: 'Active' }));
    const comp = setup();
    expect(comp.member()?.reference).toBe('LUM-7');
    expect(comp.notFound()).toBe(false);
  });

  it('signale un membre introuvable sur 404', () => {
    api.get.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const comp = setup();
    expect(comp.notFound()).toBe(true);
  });
});
