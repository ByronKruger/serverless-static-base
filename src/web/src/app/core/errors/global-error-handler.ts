import { ErrorHandler, Injectable, inject } from '@angular/core';
import { APP_CONFIG } from '../config/app-config';
import { isApiError } from '../http/api-error';
import { ERROR_REPORTER } from './error-reporter';

/**
 * The third of the three error classes: the unexpected one. Validation errors
 * belong to the form, mapped `ApiError`s belong to the feature that made the call,
 * and whatever reaches here is a defect - it gets reported, never rendered.
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly reporter = inject(ERROR_REPORTER);
  private readonly config = inject(APP_CONFIG);

  handleError(error: unknown): void {
    // An ApiError that reaches here was never handled by its feature. That is a bug
    // in the feature, not a new error class, so it is reported like any other.
    this.reporter.report(error, {
      environment: this.config.environmentName,
      handled: isApiError(error),
      at: new Date().toISOString(),
    });
  }
}
