import { InjectionToken } from '@angular/core';

/**
 * Settings that differ per deployed environment. They are fetched at runtime from
 * `config.json`, so one built artifact can be promoted from local to production
 * without a rebuild.
 */
export interface AppConfig {
  /** Free-text name of the environment, for display and error reports. */
  readonly environmentName: string;
  /** Base URL every API call is built from. Relative in local dev (dev-server proxy). */
  readonly apiBaseUrl: string;
  readonly auth: AuthConfig;
}

export interface AuthConfig {
  /** Token issuer / OIDC authority. */
  readonly authority: string;
  /** This application's client id at the authority. */
  readonly clientId: string;
  /** Audience the API expects in the access token. */
  readonly audience: string;
}

/** Injected everywhere the configuration is needed. Never read `window` directly. */
export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
