import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FirstAdminRequest, TokenResponse } from './models';

/** Accès à l'endpoint d'installation du premier administrateur (feature 005). */
@Injectable({ providedIn: 'root' })
export class SetupApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/setup`;

  installFirstAdmin(body: FirstAdminRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/first-admin`, body);
  }
}
