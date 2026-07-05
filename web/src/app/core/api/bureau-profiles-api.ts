import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BureauProfileDetail,
  BureauProfileSummary,
  BureauProfileWriteRequest,
} from '../../features/bureau-profiles/bureau-profile.models';

/** Accès aux endpoints des profils du bureau (feature 004). Aucun appel HTTP hors de ce service. */
@Injectable({ providedIn: 'root' })
export class BureauProfilesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/bureau-profiles`;

  list(): Observable<BureauProfileSummary[]> {
    return this.http.get<BureauProfileSummary[]>(this.base);
  }

  get(id: number): Observable<BureauProfileDetail> {
    return this.http.get<BureauProfileDetail>(`${this.base}/${id}`);
  }

  create(body: BureauProfileWriteRequest): Observable<BureauProfileDetail> {
    return this.http.post<BureauProfileDetail>(this.base, body);
  }

  update(id: number, body: BureauProfileWriteRequest): Observable<BureauProfileDetail> {
    return this.http.put<BureauProfileDetail>(`${this.base}/${id}`, body);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
