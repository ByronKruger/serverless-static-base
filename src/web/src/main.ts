import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { appConfig } from './app/app.config';
import { APP_CONFIG } from './app/core/config/app-config';
import { loadAppConfig } from './app/core/config/config.loader';

// Runtime configuration is resolved before bootstrap so nothing has to cope with a
// half-configured application.
loadAppConfig()
  .then((config) =>
    bootstrapApplication(App, {
      ...appConfig,
      providers: [...appConfig.providers, { provide: APP_CONFIG, useValue: config }],
    }),
  )
  .catch((error: unknown) => {
    console.error('Coffeeg failed to start.', error);
    document.body.textContent = 'Coffeeg could not start. Please refresh or contact support.';
  });
