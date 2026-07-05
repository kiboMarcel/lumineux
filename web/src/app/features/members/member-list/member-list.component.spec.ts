import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MembersApi } from '../../../core/api/members-api';
import { MemberListComponent } from './member-list.component';

describe('MemberListComponent (US1)', () => {
  const api = { search: vi.fn() };

  beforeEach(() => {
    api.search.mockReset();
    api.search.mockReturnValue(of({ page: 1, pageSize: 20, total: 0, items: [] }));
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: MembersApi, useValue: api }],
    });
  });

  it('charge une liste vide (état aucun résultat)', () => {
    const comp = TestBed.createComponent(MemberListComponent).componentInstance;
    expect(comp.items()).toHaveLength(0);
    expect(api.search).toHaveBeenCalledWith(null, 1, 20);
  });

  it('remplit la liste et calcule la pagination', () => {
    api.search.mockReturnValue(of({
      page: 1, pageSize: 20, total: 45,
      items: [{ id: 1, reference: 'LUM-1', lastName: 'Doe', firstName: 'Jane', status: 'Active' }],
    }));
    const comp = TestBed.createComponent(MemberListComponent).componentInstance;
    comp.search();
    expect(comp.items()).toHaveLength(1);
    expect(comp.total()).toBe(45);
    expect(comp.totalPages()).toBe(3);
  });
});
