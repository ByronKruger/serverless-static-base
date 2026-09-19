import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { GlobalErrorHandler } from './core/errors/global-error-handler';
import { apiErrorInterceptor } from './core/http/api-error.interceptor';
import { tracingInterceptor } from './core/http/tracing.interceptor';
import { provideAppIcons } from './core/icons/icons.provider';

/**
 * Application-wide providers. Only true singletons belong here; feature stores and
 * feature services go on their route's `providers` so they die with the feature.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' }),
    ),
    // Interceptors are functional and ordered: trace, then authorize, then map
    // failures. None of them rewrites a success body.
    provideHttpClient(
      withFetch(),
      withInterceptors([tracingInterceptor, authInterceptor, apiErrorInterceptor]),
    ),
    provideNzI18n(en_US),
    provideAppIcons(),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
  ],
};
