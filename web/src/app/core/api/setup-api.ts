import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FirstAdminRequest, SetupStatus, TokenResponse } from './models';

/** Accès aux endpoints d'installation (feature 005/012). */
@Injectable({ providedIn: 'root' })
export class SetupApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/setup`;

  installFirstAdmin(body: FirstAdminRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/first-admin`, body);
  }

  /** Statut d'installation de l'instance (anonyme, feature 012). */
  status(): Observable<SetupStatus> {
    return this.http.get<SetupStatus>(`${this.base}/status`);
  }
}
