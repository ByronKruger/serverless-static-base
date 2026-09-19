import { InjectionToken } from '@angular/core';

/** Where unexpected errors go once the UI has stopped caring about them. */
export interface ErrorReporter {
  report(error: unknown, context: Readonly<Record<string, unknown>>): void;
}

/**
 * Swap this token's provider for a real sink (Application Insights, Sentry, ...)
 * in `app.config.ts`. The default writes to the console.
 */
export const ERROR_REPORTER = new InjectionToken<ErrorReporter>('ERROR_REPORTER', {
  providedIn: 'root',
  factory: () => new ConsoleErrorReporter(),
});

export class ConsoleErrorReporter implements ErrorReporter {
  report(error: unknown, context: Readonly<Record<string, unknown>>): void {
    console.error('[coffeeg]', error, context);
  }
}
