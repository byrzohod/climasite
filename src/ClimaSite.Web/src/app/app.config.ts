import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { TranslateModule, TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { LucideAngularModule } from 'lucide-angular';
import { Observable } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './auth/interceptors/auth.interceptor';
import { ICON_REGISTRY } from './shared/components/icon';

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
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(
      LucideAngularModule.pick(ICON_REGISTRY),
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
