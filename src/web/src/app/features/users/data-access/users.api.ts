import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../../../core/config/app-config';
import { UserProfileResponse } from './users.contracts';

/**
 * The profile endpoint. Only {@link UsersStore} calls this - the page talks to the store,
 * never to `HttpClient`.
 */
@Injectable()
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(APP_CONFIG).apiBaseUrl}/users`;

  /** One person with the preferences they have saved. */
  getProfile(userId: number): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.baseUrl}/${userId}`);
  }
}
