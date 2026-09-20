import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../../../core/config/app-config';
import {
  BeveragePreferenceResponse,
  BeverageTypeDetailResponse,
  BeverageTypeResponse,
  CreateBeveragePreferenceRequest,
} from './preferences.contracts';

/**
 * The beverage endpoints. Only {@link PreferencesStore} calls this - pages talk to the
 * store, never to `HttpClient`.
 */
@Injectable()
export class PreferencesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(APP_CONFIG).apiBaseUrl;

  /** The catalogue a person picks from. Read-only: moderators own these rows. */
  listBeverageTypes(): Observable<readonly BeverageTypeResponse[]> {
    return this.http.get<readonly BeverageTypeResponse[]>(`${this.baseUrl}/beverage-types`);
  }

  /** One beverage type with the ingredients, variants and units the form renders from. */
  getBeverageType(id: number): Observable<BeverageTypeDetailResponse> {
    return this.http.get<BeverageTypeDetailResponse>(`${this.baseUrl}/beverage-types/${id}`);
  }

  listMine(): Observable<readonly BeveragePreferenceResponse[]> {
    return this.http.get<readonly BeveragePreferenceResponse[]>(`${this.baseUrl}/beverage-preferences`);
  }

  create(request: CreateBeveragePreferenceRequest): Observable<BeveragePreferenceResponse> {
    return this.http.post<BeveragePreferenceResponse>(`${this.baseUrl}/beverage-preferences`, request);
  }
}
