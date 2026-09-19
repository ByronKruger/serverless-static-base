import { AppConfig } from './app-config';

/**
 * Loads `config.json` before the application bootstraps. A failure here is fatal:
 * an app that guesses its API base URL is worse than an app that refuses to start.
 */
export async function loadAppConfig(url = 'config.json'): Promise<AppConfig> {
  const response = await fetch(url, { cache: 'no-store' });

  if (!response.ok) {
    throw new Error(`Runtime configuration could not be loaded from ${url} (${response.status}).`);
  }

  return (await response.json()) as AppConfig;
}
