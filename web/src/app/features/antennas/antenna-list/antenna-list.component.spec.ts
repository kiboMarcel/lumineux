import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AntennasApi } from '../../../core/api/antennas-api';
import { ReferenceApi } from '../../../core/api/reference-api';
import { NotificationService } from '../../../shared/notifications/notification.service';
import { AntennaListComponent } from './antenna-list.component';

describe('AntennaListComponent (US1/US4)', () => {
  const api = { list: vi.fn(), deactivate: vi.fn(), activate: vi.fn() };
  const referenceApi = { districts: vi.fn() };
  const notifier = { error: vi.fn(), success: vi.fn(), info: vi.fn(), clear: vi.fn() };

  const active = { id: 1, code: 'ANT-1', label: 'Active', districtId: 9, status: 'Active' };
  const inactive = { id: 2, code: 'ANT-2', label: 'Inactive', districtId: 9, status: 'Inactive' };

  beforeEach(() => {
    Object.values(api).forEach((f) => f.mockReset());
    referenceApi.districts.mockReset();
    notifier.error.mockReset();
    api.list.mockReturnValue(of([active, inactive]));
    referenceApi.districts.mockReturnValue(of([{ id: 9, code: 'D9', label: 'District 9' }]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AntennasApi, useValue: api },
        { provide: ReferenceApi, useValue: referenceApi },
        { provide: NotificationService, useValue: notifier },
      ],
    });
  });

  afterEach(() => vi.restoreAllMocks());

  function create() {
    return TestBed.createComponent(AntennaListComponent).componentInstance;
  }

  it('charge toutes les antennes (actives et inactives) avec le libellé du district', () => {
    const comp = create();
    expect(comp.antennas()).toHaveLength(2);
    expect(comp.antennas().map((a) => a.status)).toEqual(['Active', 'Inactive']);
    expect(comp.districtLabel(9)).toBe('District 9');
  });

  it('désactive après confirmation puis recharge', () => {
    api.deactivate.mockReturnValue(of({ ...active, status: 'Inactive' }));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = create();

    comp.deactivate(active);

    expect(api.deactivate).toHaveBeenCalledWith(1);
    expect(api.list).toHaveBeenCalledTimes(2); // chargement initial + rechargement
  });

  it('ne désactive pas si la confirmation est refusée', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const comp = create();

    comp.deactivate(active);

    expect(api.deactivate).not.toHaveBeenCalled();
  });

  it('mappe le refus antenna_has_open_sessions en message clair', () => {
    api.deactivate.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'antenna_has_open_sessions' } })),
    );
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const comp = create();

    comp.deactivate(active);

    expect(notifier.error).toHaveBeenCalledWith(expect.stringContaining('session de présence est encore ouverte'));
  });

  it('réactive une antenne inactive', () => {
    api.activate.mockReturnValue(of({ ...inactive, status: 'Active' }));
    const comp = create();

    comp.activate(inactive);

    expect(api.activate).toHaveBeenCalledWith(2);
  });
});
