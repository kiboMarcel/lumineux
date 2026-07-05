import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermissionDescriptor } from './permission.models';

/** Accès au catalogue figé des droits fonctionnels (feature 004). */
@Injectable({ providedIn: 'root' })
export class PermissionsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/permissions`;

  list(): Observable<PermissionDescriptor[]> {
    return this.http.get<PermissionDescriptor[]>(this.base);
  }
}
