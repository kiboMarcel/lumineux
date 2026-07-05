import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import { SetupApi } from './setup-api';

const BASE = `${environment.apiBaseUrl.replace(/\/$/, '')}/api/v1/setup`;

describe('SetupApi', () => {
  let api: SetupApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(SetupApi);
    http = TestBed.inject(HttpTestingController);
  });

  it('status() interroge GET /setup/status', () => {
    api.status().subscribe();
    http.expectOne({ url: `${BASE}/status`, method: 'GET' }).flush({ installed: false });
  });
});
