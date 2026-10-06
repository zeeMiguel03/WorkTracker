import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import localeEnGb from '@angular/common/locales/en-GB';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { LocalizedTitleStrategy } from './core/i18n/localized-title.strategy';

registerLocaleData(localePt, 'pt-PT');
registerLocaleData(localeEnGb, 'en-GB');

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: LOCALE_ID, useValue: 'pt-PT' },
    { provide: TitleStrategy, useClass: LocalizedTitleStrategy },
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
