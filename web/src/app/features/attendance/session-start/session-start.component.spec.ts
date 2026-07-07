import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ReferenceApi } from '../../../core/api/reference-api';
import { AttendanceSessionsApi } from '../../../core/api/attendance-sessions-api';
import { SessionStartComponent } from './session-start.component';

describe('SessionStartComponent (US1/US2 + reprise 024)', () => {
  const refApi = { antennas: vi.fn() };
  const sessionsApi = { start: vi.fn(), myOpenSessions: vi.fn() };

  const openSession = {
    id: 77, antennaId: 1, meetingDate: '2026-07-05T00:00:00', startTime: '2026-07-05T09:30:00',
    status: 'Open', openedByMemberId: 42, attendanceCount: 0,
  };

  beforeEach(() => {
    refApi.antennas.mockReset();
    sessionsApi.start.mockReset();
    sessionsApi.myOpenSessions.mockReset();
    refApi.antennas.mockReturnValue(of([{ id: 1, code: 'A1', label: 'Antenne 1' }]));
    sessionsApi.myOpenSessions.mockReturnValue(of([])); // par défaut : aucune session en cours
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

  // --- Reprise (feature 024) ---

  it('affiche l\'encart des sessions en cours au chargement (US1)', () => {
    sessionsApi.myOpenSessions.mockReturnValue(of([openSession]));
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    expect(comp.openSessions()).toHaveLength(1);
    expect(comp.antennaLabel(1)).toBe('Antenne 1');
  });

  it('aucun encart si aucune session en cours', () => {
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    expect(comp.openSessions()).toHaveLength(0);
  });

  it('« Reprendre » navigue vers l\'écran d\'animation de la session (US1)', () => {
    const nav = vi.spyOn(TestBed.inject(Router), 'navigate');
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    comp.resume(openSession);
    expect(nav).toHaveBeenCalledWith(['/attendance/sessions', 77]);
  });

  it('la vérification est non bloquante : un échec n\'empêche pas le formulaire (US1)', () => {
    sessionsApi.myOpenSessions.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    expect(comp.openSessions()).toHaveLength(0);
    expect(comp.loadingRefs()).toBe(false); // formulaire disponible
  });

  it('conflit 409 → propose la reprise de la session correspondante (US2)', () => {
    sessionsApi.start.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { detail: 'Une session ouverte existe déjà pour cette antenne à ce créneau.' } })));
    sessionsApi.myOpenSessions.mockReturnValue(of([openSession])); // re-fetch sur conflit
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    comp.antennaId = 1;
    comp.meetingDate = '2026-07-05';

    comp.start();

    expect(comp.conflictResume()?.id).toBe(77);
    expect(comp.submitting()).toBe(false);
  });

  it('conflit 409 sans correspondance → message clair (US2)', () => {
    sessionsApi.start.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { detail: 'Une session ouverte existe déjà pour cette antenne à ce créneau.' } })));
    sessionsApi.myOpenSessions.mockReturnValue(of([])); // aucune session à moi (ex. ouverte par un autre)
    const comp = TestBed.createComponent(SessionStartComponent).componentInstance;
    comp.antennaId = 1;
    comp.meetingDate = '2026-07-05';

    comp.start();

    expect(comp.conflictResume()).toBeNull();
    expect(comp.error()).toContain('déjà');
  });
});
