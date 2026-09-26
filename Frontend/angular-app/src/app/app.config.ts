import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { RealtimeService } from './core/services/realtime.service';
import { ThemeService } from './core/services/theme.service';

import { routes } from './app.routes';

/** Start SignalR connection on app bootstrap (called once at startup). */
function startRealtime(): () => void {
  return () => {
    // RealtimeService is a root singleton; SignalR connection starts lazily
    // when the dashboard layout initializes (user is authenticated).
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    RealtimeService,
    ThemeService,
    {
      provide: APP_INITIALIZER,
      useFactory: startRealtime,
      multi: true
    }
  ]
};
