import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AttendanceSessionsApi } from '../../../../core/api/attendance-sessions-api';
import { QrPanelComponent } from './qr-panel.component';

describe('QrPanelComponent (US1 — QR rotatif)', () => {
  const sessionsApi = { qr: vi.fn() };

  beforeEach(() => {
    sessionsApi.qr.mockReset();
    TestBed.configureTestingModule({
      providers: [{ provide: AttendanceSessionsApi, useValue: sessionsApi }],
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  function createPanel() {
    const fixture = TestBed.createComponent(QrPanelComponent);
    fixture.componentRef.setInput('sessionId', 7);
    return fixture;
  }

  it('récupère le jeton et alimente la donnée du QR (jamais persistée)', () => {
    // Jeton en mémoire uniquement : rien n'est écrit dans un stockage exposé (SC-005).
    const setItem = vi.fn();
    vi.stubGlobal('localStorage', { setItem, getItem: vi.fn(), removeItem: vi.fn(), clear: vi.fn() });
    sessionsApi.qr.mockReturnValue(of({ token: 'tok-1', stepSeconds: 30, expiresAt: '2026-07-05T10:00:30Z' }));
    const fixture = createPanel();
    fixture.componentInstance.ngOnInit();
    // Charge versionnée consommée par le mobile (feature 026) : { v, s, t }.
    expect(fixture.componentInstance.qrData()).toBe('{"v":1,"s":7,"t":"tok-1"}');
    expect(setItem).not.toHaveBeenCalled();
  });

  it('regénère le QR AVANT expiration (au rythme du pas de rotation)', () => {
    vi.useFakeTimers();
    sessionsApi.qr
      .mockReturnValueOnce(of({ token: 'tok-1', stepSeconds: 30, expiresAt: '' }))
      .mockReturnValueOnce(of({ token: 'tok-2', stepSeconds: 30, expiresAt: '' }));
    const comp = createPanel().componentInstance;
    comp.ngOnInit();
    expect(comp.qrData()).toBe('{"v":1,"s":7,"t":"tok-1"}');
    // Avant l'expiration (30 s), un nouveau jeton est récupéré.
    vi.advanceTimersByTime(29_000);
    expect(sessionsApi.qr).toHaveBeenCalledTimes(2);
    expect(comp.qrData()).toBe('{"v":1,"s":7,"t":"tok-2"}');
  });

  it('arrête le cycle de rotation à la destruction (pas de fuite de timer)', () => {
    vi.useFakeTimers();
    sessionsApi.qr.mockReturnValue(of({ token: 'tok-1', stepSeconds: 30, expiresAt: '' }));
    const comp = createPanel().componentInstance;
    comp.ngOnInit();
    comp.ngOnDestroy();
    vi.advanceTimersByTime(120_000);
    expect(sessionsApi.qr).toHaveBeenCalledTimes(1);
  });
});
