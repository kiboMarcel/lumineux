import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MemberLookupItem } from '../../features/attendance/attendance.models';

/**
 * Recherche membre **allégée** (feature 015) : référence/nom → identité minimale, accessible au droit
 * `manage_attendance` (ou `manage_members`). Alimente le sélecteur d'ajout manuel de présence.
 * N'expose aucune donnée sensible. Aucun appel HTTP hors de ce service.
 */
@Injectable({ providedIn: 'root' })
export class MemberLookupApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/members/lookup`;

  lookup(query: string): Observable<MemberLookupItem[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<MemberLookupItem[]>(this.base, { params });
  }
}
