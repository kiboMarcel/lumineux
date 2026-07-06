import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AntennaAttendanceSummaryResponse,
  AttendanceTimeSeriesResponse,
  MemberAttendanceRateResponse,
  TimeSeriesGranularity,
} from '../../features/reports/report.models';

/**
 * Accès à l'API de rapports de présence (feature 018). Aucun appel HTTP hors de ce service.
 * Réservé (côté API) au droit `manage_attendance` — l'API reste l'autorité. Le client ne recalcule
 * aucune statistique : il consomme les agrégats.
 */
@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/reports/attendance`;

  private periodParams(from: string, to: string, antennaId?: number | null): HttpParams {
    let params = new HttpParams().set('from', from).set('to', to);
    if (antennaId != null) {
      params = params.set('antennaId', antennaId);
    }
    return params;
  }

  /** Synthèse par antenne sur la période (JSON). */
  antennaSummary(from: string, to: string, antennaId?: number | null): Observable<AntennaAttendanceSummaryResponse> {
    return this.http.get<AntennaAttendanceSummaryResponse>(`${this.base}/antenna-summary`, {
      params: this.periodParams(from, to, antennaId),
    });
  }

  /** Export CSV de la synthèse (téléchargement authentifié — le jeton est porté par l'intercepteur). */
  antennaSummaryCsv(from: string, to: string, antennaId?: number | null): Observable<Blob> {
    return this.http.get(`${this.base}/antenna-summary.csv`, {
      params: this.periodParams(from, to, antennaId),
      responseType: 'blob',
    });
  }

  /** Taux d'assiduité d'un membre sur la période. */
  memberRate(memberId: number, from: string, to: string): Observable<MemberAttendanceRateResponse> {
    const params = new HttpParams().set('memberId', memberId).set('from', from).set('to', to);
    return this.http.get<MemberAttendanceRateResponse>(`${this.base}/member-rate`, { params });
  }

  /** Série temporelle des présences valides par intervalle (semaine ISO / mois). */
  timeSeries(
    from: string, to: string, granularity: TimeSeriesGranularity, antennaId?: number | null,
  ): Observable<AttendanceTimeSeriesResponse> {
    let params = this.periodParams(from, to, antennaId).set('granularity', granularity);
    return this.http.get<AttendanceTimeSeriesResponse>(`${this.base}/time-series`, { params });
  }
}
