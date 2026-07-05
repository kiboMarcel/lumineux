import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MemberProfilesResponse } from '../../features/bureau-profiles/bureau-profile.models';

/** Accès à l'attribution des profils aux membres (feature 004). Aucun appel HTTP hors de ce service. */
@Injectable({ providedIn: 'root' })
export class MemberProfilesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/members`;

  get(memberId: number): Observable<MemberProfilesResponse> {
    return this.http.get<MemberProfilesResponse>(`${this.base}/${memberId}/bureau-profiles`);
  }

  assign(memberId: number, profileId: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${memberId}/bureau-profiles`, { profileId });
  }

  revoke(memberId: number, profileId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${memberId}/bureau-profiles/${profileId}`);
  }
}
