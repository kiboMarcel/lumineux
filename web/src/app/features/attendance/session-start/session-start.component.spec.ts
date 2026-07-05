import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReferenceApi } from '../../../core/api/reference-api';
import { AttendanceSessionsApi } from '../../../core/api/attendance-sessions-api';
import { SessionStartComponent } from './session-start.component';

describe('SessionStartComponent (US1)', () => {
  const refApi = { antennas: vi.fn() };
  const sessionsApi = { start: vi.fn() };

  beforeEach(() => {
    refApi.antennas.mockReset();
    sessionsApi.start.mockReset();
    refApi.antennas.mockReturnValue(of([{ id: 1, code: 'A1', label: 'Antenne 1' }]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ReferenceApi, useValue: refApi },
        { provide: AttendanceSessionsApi, useValue: sessionsApi },
      ],
    });
  });

  it('charge les antennes du référentiel', () => {
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    expect(comp.antennas()).toHaveLength(1);
    expect(comp.loadingRefs()).toBe(false);
  });

  it('empêche le démarrage si aucune antenne active', () => {
    refApi.antennas.mockReturnValue(of([]));
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    expect(comp.antennas()).toHaveLength(0);
    comp.start();
    expect(sessionsApi.start).not.toHaveBeenCalled();
  });

  it('démarre et navigue vers l\'écran d\'animation au succès', () => {
    sessionsApi.start.mockReturnValue(of({ id: 99, status: 'Open' }));
    const fixture = TestBed.createComponent(SessionStartComponent);
    const comp = fixture.componentInstance;
    const nav = vi.spyOn(TestBed.inject(Router), 'navigate');
    comp.antennaId = 1;
    comp.meetingDate = '2026-07-05';
    comp.start();
    expect(sessionsApi.start).toHaveBeenCalledWith({ antennaId: 1, meetingDate: '2026-07-05', qrStepSeconds: null });
    expect(nav).toHaveBeenCalledWith(['/attendance/sessions', 99]);
  });

  it('mappe l\'erreur de démarrage sans naviguer', () => {
    sessionsApi.start.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400, error: { detail: 'Date invalide' } })));
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    comp.antennaId = 1;
    comp.meetingDate = '2026-07-05';
    comp.start();
    expect(comp.error()).toBe('Date invalide');
    expect(comp.submitting()).toBe(false);
  });
});
