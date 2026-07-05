import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MemberLookupApi } from '../../../../core/api/member-lookup-api';
import { AttendancesApi } from '../../../../core/api/attendances-api';
import { ManualAddComponent } from './manual-add.component';

describe('ManualAddComponent (US3 — ajout manuel via lookup)', () => {
  const lookupApi = { lookup: vi.fn() };
  const attendancesApi = { addManual: vi.fn() };

  beforeEach(() => {
    lookupApi.lookup.mockReset();
    attendancesApi.addManual.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: MemberLookupApi, useValue: lookupApi },
        { provide: AttendancesApi, useValue: attendancesApi },
      ],
    });
  });

  function createComp() {
    const fixture = TestBed.createComponent(ManualAddComponent);
    fixture.componentRef.setInput('sessionId', 7);
    return fixture.componentInstance;
  }

  const member = { id: 42, reference: 'LUM-42', fullName: 'Jane Doe', status: 'Active' };

  it('recherche un membre via le lookup allégé', () => {
    lookupApi.lookup.mockReturnValue(of([member]));
    const comp = createComp();
    comp.query = 'doe';
    comp.search();
    expect(lookupApi.lookup).toHaveBeenCalledWith('doe');
    expect(comp.results()).toHaveLength(1);
  });

  it('ajoute la présence et émet added (idempotent : réajout sans erreur)', () => {
    attendancesApi.addManual.mockReturnValue(of({ id: 1, memberId: 42 }));
    const comp = createComp();
    let addedCount = 0;
    comp.added.subscribe(() => addedCount++);

    comp.add(member);
    comp.add(member); // réajout du même membre

    expect(attendancesApi.addManual).toHaveBeenCalledWith(7, { memberId: 42 });
    expect(attendancesApi.addManual).toHaveBeenCalledTimes(2);
    expect(addedCount).toBe(2);
    expect(comp.error()).toBeNull();
  });

  it('mappe le refus 409 (session close)', () => {
    attendancesApi.addManual.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { detail: 'Session close' } })),
    );
    const comp = createComp();
    comp.add(member);
    expect(comp.error()).toBe('Session close');
  });
});
