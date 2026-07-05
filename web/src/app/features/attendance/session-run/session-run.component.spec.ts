import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AttendanceSessionsApi } from '../../../core/api/attendance-sessions-api';
import { AttendancesApi } from '../../../core/api/attendances-api';
import { SessionRunComponent } from './session-run.component';

const openSession = { id: 7, antennaId: 1, meetingDate: '2026-07-05', startTime: 't', status: 'Open', openedByMemberId: 1, attendanceCount: 0 };
const closedSession = { ...openSession, status: 'Closed', endTime: '2026-07-05T12:00:00Z' };
const list = (validCount = 0, items: unknown[] = []) => ({ sessionId: 7, validCount, items });

describe('SessionRunComponent (US1/US2/US3/US4)', () => {
  const sessionsApi = { get: vi.fn(), close: vi.fn() };
  const attendancesApi = { list: vi.fn(), cancel: vi.fn() };

  beforeEach(() => {
    sessionsApi.get.mockReset();
    sessionsApi.close.mockReset();
    attendancesApi.list.mockReset();
    attendancesApi.cancel.mockReset();
    sessionsApi.get.mockReturnValue(of(openSession));
    attendancesApi.list.mockReturnValue(of(list()));
    TestBed.configureTestingModule({
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '7' } } } },
        { provide: AttendanceSessionsApi, useValue: sessionsApi },
        { provide: AttendancesApi, useValue: attendancesApi },
      ],
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  function run() {
    const comp = TestBed.createComponent(SessionRunComponent).componentInstance;
    comp.ngOnInit();
    return comp;
  }

  it('charge la session par identifiant et sa liste de présences (US1)', () => {
    attendancesApi.list.mockReturnValue(of(list(3)));
    const comp = run();
    expect(sessionsApi.get).toHaveBeenCalledWith(7);
    expect(comp.session()?.id).toBe(7);
    expect(comp.isClosed()).toBe(false);
    expect(comp.validCount()).toBe(3);
  });

  it('rafraîchit la liste par polling et sur changement de filtre (US2)', () => {
    vi.useFakeTimers();
    const comp = run();
    expect(attendancesApi.list).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(5000);
    expect(attendancesApi.list).toHaveBeenCalledTimes(2);

    comp.changeFilter('All');
    expect(comp.filter()).toBe('All');
    expect(attendancesApi.list).toHaveBeenLastCalledWith(7, 'All');

    comp.ngOnDestroy();
    vi.advanceTimersByTime(20000);
    // Plus aucun appel après destruction (compteur figé à celui d'avant destruction).
    const callsAfterDestroy = attendancesApi.list.mock.calls.length;
    vi.advanceTimersByTime(20000);
    expect(attendancesApi.list.mock.calls.length).toBe(callsAfterDestroy);
  });

  it('annule une présence après confirmation (US3)', () => {
    attendancesApi.cancel.mockReturnValue(of(void 0));
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = run();
    comp.cancel({ id: 1, sessionId: 7, memberId: 42, arrivalTime: 't', source: 'Manuel', status: 'Valid' });
    expect(confirmSpy).toHaveBeenCalled();
    expect(attendancesApi.cancel).toHaveBeenCalledWith(7, 42);
  });

  it('n\'annule pas si la confirmation est refusée (US3/SC-007)', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const comp = run();
    comp.cancel({ id: 1, sessionId: 7, memberId: 42, arrivalTime: 't', source: 'Manuel', status: 'Valid' });
    expect(attendancesApi.cancel).not.toHaveBeenCalled();
  });

  it('clôture après confirmation puis masque le QR et les écritures (US4/SC-006)', () => {
    vi.useFakeTimers();
    sessionsApi.close.mockReturnValue(of(closedSession));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = run();
    comp.close();
    expect(sessionsApi.close).toHaveBeenCalledWith(7);
    expect(comp.isClosed()).toBe(true);
    // Polling arrêté après clôture.
    const callsAfterClose = attendancesApi.list.mock.calls.length;
    vi.advanceTimersByTime(30000);
    expect(attendancesApi.list.mock.calls.length).toBe(callsAfterClose);
  });

  it('mappe une erreur de clôture sans changer d\'état', () => {
    sessionsApi.close.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = run();
    comp.close();
    expect(comp.isClosed()).toBe(false);
    expect(comp.closing()).toBe(false);
  });
});
