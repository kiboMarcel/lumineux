import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AntennaResponse,
  CreateAntennaRequest,
  UpdateAntennaRequest,
} from '../../features/antennas/antenna.models';

/**
 * Accès à l'API de gestion des antennes (feature 016). Aucun appel HTTP hors de ce service.
 * Réservé (côté API) au droit `manage_referentials` — l'API reste l'autorité.
 */
@Injectable({ providedIn: 'root' })
export class AntennasApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/antennas`;

  /** Liste de gestion : toutes les antennes, inactives incluses. */
  list(): Observable<AntennaResponse[]> {
    return this.http.get<AntennaResponse[]>(this.base);
  }

  get(id: number): Observable<AntennaResponse> {
    return this.http.get<AntennaResponse>(`${this.base}/${id}`);
  }

  create(body: CreateAntennaRequest): Observable<AntennaResponse> {
    return this.http.post<AntennaResponse>(this.base, body);
  }

  update(id: number, body: UpdateAntennaRequest): Observable<AntennaResponse> {
    return this.http.put<AntennaResponse>(`${this.base}/${id}`, body);
  }

  deactivate(id: number): Observable<AntennaResponse> {
    return this.http.post<AntennaResponse>(`${this.base}/${id}/deactivate`, {});
  }

  activate(id: number): Observable<AntennaResponse> {
    return this.http.post<AntennaResponse>(`${this.base}/${id}/activate`, {});
  }
}
