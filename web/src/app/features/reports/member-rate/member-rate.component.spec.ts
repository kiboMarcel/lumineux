import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemberLookupApi } from '../../../core/api/member-lookup-api';
import { ReportsApi } from '../../../core/api/reports-api';
import { MemberRateComponent } from './member-rate.component';

describe('MemberRateComponent (US3)', () => {
  const lookupApi = { lookup: vi.fn() };
  const reportsApi = { memberRate: vi.fn() };

  const member = { id: 42, reference: 'LUM-42', fullName: 'Jane Doe', status: 'Active' };

  beforeEach(() => {
    lookupApi.lookup.mockReset();
    reportsApi.memberRate.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: MemberLookupApi, useValue: lookupApi },
        { provide: ReportsApi, useValue: reportsApi },
      ],
    });
  });

  function create() {
    const fixture = TestBed.createComponent(MemberRateComponent);
    fixture.componentRef.setInput('from', '2026-06-01');
    fixture.componentRef.setInput('to', '2026-06-30');
    return fixture.componentInstance;
  }

  it('recherche un membre et n\'appelle pas le taux tant qu\'aucun n\'est choisi', () => {
    lookupApi.lookup.mockReturnValue(of([member]));
    const comp = create();
    comp.query = 'doe';
    comp.search();

    expect(lookupApi.lookup).toHaveBeenCalledWith('doe');
    expect(comp.results()).toHaveLength(1);
    expect(reportsApi.memberRate).not.toHaveBeenCalled();
  });

  it('affiche le taux en pourcentage après sélection', () => {
    reportsApi.memberRate.mockReturnValue(of({
      memberId: 42, memberFullName: 'Jane Doe', from: '', to: '',
      validAttendanceCount: 3, eligibleSessionCount: 4, rate: 0.75,
    }));
    const comp = create();

    comp.select(member);

    expect(reportsApi.memberRate).toHaveBeenCalledWith(42, '2026-06-01', '2026-06-30');
    expect(comp.percent()).toBe(75);
  });

  it('présente 0 % sans erreur pour un membre sans présence', () => {
    reportsApi.memberRate.mockReturnValue(of({
      memberId: 42, memberFullName: 'Jane Doe', from: '', to: '',
      validAttendanceCount: 0, eligibleSessionCount: 5, rate: 0,
    }));
    const comp = create();

    comp.select(member);

    expect(comp.percent()).toBe(0);
    expect(comp.error()).toBeNull();
  });

  it('mappe un membre introuvable (404)', () => {
    reportsApi.memberRate.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const comp = create();

    comp.select(member);

    expect(comp.error()).toContain('introuvable');
    expect(comp.selected()).toBeNull();
  });
});
