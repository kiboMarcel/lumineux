import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { QrTokenResponse, SessionResponse, StartSessionRequest } from '../../features/attendance/attendance.models';

/**
 * Accès aux endpoints de sessions de présence (feature 001). Aucun appel HTTP hors de ce service.
 * Réservé (côté API) au droit `manage_attendance` — l'API reste l'autorité.
 */
@Injectable({ providedIn: 'root' })
export class AttendanceSessionsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/attendance-sessions`;

  start(body: StartSessionRequest): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(this.base, body);
  }

  get(sessionId: number): Observable<SessionResponse> {
    return this.http.get<SessionResponse>(`${this.base}/${sessionId}`);
  }

  /** Jeton QR courant (éphémère) : sert uniquement à générer l'image côté client. */
  qr(sessionId: number): Observable<QrTokenResponse> {
    return this.http.get<QrTokenResponse>(`${this.base}/${sessionId}/qr`);
  }

  close(sessionId: number): Observable<SessionResponse> {
    return this.http.post<SessionResponse>(`${this.base}/${sessionId}/close`, {});
  }
}
