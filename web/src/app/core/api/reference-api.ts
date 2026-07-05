import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Country, ReferenceItem } from './reference.models';

/**
 * Accès aux endpoints de données de référence (feature 010) pour peupler les listes de sélection de
 * la fiche membre. Aucun appel HTTP hors de ce service.
 */
@Injectable({ providedIn: 'root' })
export class ReferenceApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/reference`;

  antennas(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.base}/antennas`);
  }

  civilities(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.base}/civilities`);
  }

  cities(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.base}/cities`);
  }

  districts(): Observable<ReferenceItem[]> {
    return this.http.get<ReferenceItem[]>(`${this.base}/districts`);
  }

  countries(): Observable<Country[]> {
    return this.http.get<Country[]>(`${this.base}/countries`);
  }
}
