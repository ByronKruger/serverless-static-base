import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { APP_CONFIG } from '../config/app-config';
import { AuthStore } from './auth.store';

/**
 * Attaches the bearer token to calls that go to our own API, and only to those -
 * a token must never leak to a third-party host.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthStore).accessToken();
  const { apiBaseUrl } = inject(APP_CONFIG);

  if (token === null || !request.url.startsWith(apiBaseUrl)) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
