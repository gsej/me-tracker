import {
  ApplicationConfig,
  provideZoneChangeDetection,
  isDevMode,
  inject,
  provideAppInitializer,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';
import { SettingsHttpService } from './settings/settings.http.service';
import { provideServiceWorker } from '@angular/service-worker';

export function initializeApp(settingsHttpService: SettingsHttpService) {
  return () => settingsHttpService.initializeApp();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(),
    provideAppInitializer(() => {
      const initializerFn = initializeApp(inject(SettingsHttpService));
      return initializerFn();
    }),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
