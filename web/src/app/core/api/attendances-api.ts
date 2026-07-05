import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AttendanceListResponse,
  AttendanceResponse,
  AttendanceStatusFilter,
  ManualAttendanceRequest,
} from '../../features/attendance/attendance.models';

/**
 * Accès aux endpoints de présences d'une session (feature 001) : liste/décompte, ajout manuel
 * (idempotent), annulation. Aucun appel HTTP hors de ce service.
 */
@Injectable({ providedIn: 'root' })
export class AttendancesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/attendance-sessions`;

  /** Liste des présences d'une session, filtrée par statut (Valid / Cancelled / All). */
  list(sessionId: number, status: AttendanceStatusFilter): Observable<AttendanceListResponse> {
    const params = new HttpParams().set('status', status);
    return this.http.get<AttendanceListResponse>(`${this.base}/${sessionId}/attendances`, { params });
  }

  /** Ajout manuel d'une présence (source « manuel »). Idempotent côté API (réajout sans doublon). */
  addManual(sessionId: number, body: ManualAttendanceRequest): Observable<AttendanceResponse> {
    return this.http.post<AttendanceResponse>(`${this.base}/${sessionId}/attendances`, body);
  }

  /** Annule (retire) la présence d'un membre tant que la session est ouverte. */
  cancel(sessionId: number, memberId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${sessionId}/attendances/${memberId}`);
  }
}
