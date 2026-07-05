import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { PermissionsApi } from './permissions-api';

describe('PermissionsApi', () => {
  let api: PermissionsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(PermissionsApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('récupère le catalogue en GET', () => {
    api.list().subscribe();
    http.expectOne({ url: `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/permissions`, method: 'GET' }).flush([]);
  });
});
