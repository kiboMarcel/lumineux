import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateMemberRequest,
  MemberCreatedResponse,
  MemberListResponse,
  MemberResponse,
  UpdateMemberRequest,
} from '../../features/members/member.models';

/**
 * Accès aux endpoints membres de l'API (feature 002). Aucun appel HTTP hors de ce service.
 */
@Injectable({ providedIn: 'root' })
export class MembersApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/members`;

  search(query: string | null, page: number, pageSize: number): Observable<MemberListResponse> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (query) {
      params = params.set('query', query);
    }
    return this.http.get<MemberListResponse>(this.base, { params });
  }

  get(id: number): Observable<MemberResponse> {
    return this.http.get<MemberResponse>(`${this.base}/${id}`);
  }

  create(body: CreateMemberRequest): Observable<MemberCreatedResponse> {
    return this.http.post<MemberCreatedResponse>(this.base, body);
  }

  update(id: number, body: UpdateMemberRequest): Observable<MemberResponse> {
    return this.http.put<MemberResponse>(`${this.base}/${id}`, body);
  }
}
