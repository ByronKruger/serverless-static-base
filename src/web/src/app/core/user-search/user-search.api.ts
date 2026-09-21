import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { UserSummaryResponse } from './user-search.contracts';

/**
 * The user-search endpoint. Only {@link UserSearchStore} calls this - the component talks
 * to the store, never to `HttpClient`.
 */
@Injectable({ providedIn: 'root' })
export class UserSearchApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_CONFIG).apiBaseUrl}/users`;

  search(term: string, limit: number): Observable<readonly UserSummaryResponse[]> {
    const params = new HttpParams().set('q', term).set('limit', limit);

    return this.http.get<readonly UserSummaryResponse[]>(this.baseUrl, { params });
  }
}
