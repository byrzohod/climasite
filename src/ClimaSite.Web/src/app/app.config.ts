import { ApplicationConfig, importProvidersFrom, inject, provideAppInitializer, provideZoneChangeDetection, DEFAULT_CURRENCY_CODE } from '@angular/core';
import { provideRouter, Router, TitleStrategy } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { TranslateModule, TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { Observable } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './auth/interceptors/auth.interceptor';
import { SeoTitleStrategy, resetRouteOwnedJsonLdOnNavigation } from './core/services/seo-title.strategy';
import { StructuredDataService } from './core/services/structured-data.service';

class CustomHttpLoader implements TranslateLoader {
  constructor(private http: HttpClient) {}

  getTranslation(lang: string): Observable<TranslationObject> {
    const url = `/assets/i18n/${lang}.json`;
    return this.http.get<TranslationObject>(url);
  }
}

export function createTranslateLoader(http: HttpClient) {
  return new CustomHttpLoader(http);
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    // DEC-CURRENCY: the store charges in EUR — make the default `| currency` pipe render € (not the
    // framework default USD $). Prefer explicit `| currency:'EUR'`; this is the safety net.
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'EUR' },
    provideRouter(routes),
    // B-044: SeoTitleStrategy is the single brand-title authority and applies
    // default per-route meta/canonical/robots on every navigation.
    { provide: TitleStrategy, useClass: SeoTitleStrategy },
    // B-044: clear route-owned JSON-LD on NavigationStart (before the next route's
    // component synchronously emits its own — e.g. home's constructor). This lives in an
    // app-initializer, NOT the TitleStrategy, so it can inject Router without the NG0200
    // circular dependency (the Router builds the TitleStrategy during its own init).
    provideAppInitializer(() => {
      // Block body (returns void): the helper returns a Subscription, which is not a valid
      // app-initializer return type. The subscription lives for the app's lifetime.
      resetRouteOwnedJsonLdOnNavigation(inject(Router), inject(StructuredDataService));
    }),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        loader: {
          provide: TranslateLoader,
          useFactory: createTranslateLoader,
          deps: [HttpClient]
        }
      })
    )
  ]
};
