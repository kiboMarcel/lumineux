import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActivateRequest,
  ChangePasswordRequest,
  CurrentUser,
  ForgotPasswordRequest,
  GenericMessage,
  LoginRequest,
  ResetPasswordRequest,
  TokenResponse,
} from './models';

/**
 * Accès aux endpoints d'authentification de l'API (feature 003/006/007). Aucun appel HTTP ne doit
 * être fait ailleurs que dans ce service (séparation des responsabilités).
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/auth`;

  login(body: LoginRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/login`, body);
  }

  activate(body: ActivateRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.base}/activate`, body);
  }

  forgotPassword(body: ForgotPasswordRequest): Observable<GenericMessage> {
    return this.http.post<GenericMessage>(`${this.base}/forgot-password`, body);
  }

  resetPassword(body: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/reset-password`, body);
  }

  changePassword(body: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/change-password`, body);
  }

  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.base}/me`);
  }
}
