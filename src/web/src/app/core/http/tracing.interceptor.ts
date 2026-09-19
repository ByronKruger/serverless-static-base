import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Stamps every outgoing request with a correlation id. The API echoes it back in
 * the Problem Details `traceId`, which is what support asks the user for.
 */
export const tracingInterceptor: HttpInterceptorFn = (request, next) =>
  next(request.clone({ setHeaders: { 'X-Correlation-Id': crypto.randomUUID() } }));
