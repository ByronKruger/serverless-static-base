import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { toApiError } from './api-error';

/**
 * Maps every HTTP failure to an `ApiError` once, here, so no page ever inspects a
 * status code. It deliberately does not toast: whether a failure is worth
 * interrupting the user is a per-call decision (Docs/ARCHITECTURE.md).
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? toApiError(error) : error)),
    ),
  );
