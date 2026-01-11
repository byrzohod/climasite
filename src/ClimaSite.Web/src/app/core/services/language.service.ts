import { Injectable, inject, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';

export type SupportedLanguage = 'en' | 'bg' | 'de';

export interface LanguageInfo {
  code: SupportedLanguage;
  name: string;
  nativeName: string;
  flag: string;
}

export const SUPPORTED_LANGUAGES: LanguageInfo[] = [
  { code: 'en', name: 'English', nativeName: 'English', flag: 'gb' },
  { code: 'bg', name: 'Bulgarian', nativeName: 'Български', flag: 'bg' },
  { code: 'de', name: 'German', nativeName: 'Deutsch', flag: 'de' },
];

const LANGUAGE_STORAGE_KEY = 'climasite-language';
const DEFAULT_LANGUAGE: SupportedLanguage = 'en';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly translate = inject(TranslateService);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly _currentLanguage = signal<SupportedLanguage>(this.getInitialLanguage());

  readonly currentLanguage = this._currentLanguage.asReadonly();
  readonly languages = SUPPORTED_LANGUAGES;

  constructor() {
    this.initializeTranslations();
  }

  private initializeTranslations(): void {
    // Set available languages
    this.translate.addLangs(SUPPORTED_LANGUAGES.map(l => l.code));
    this.translate.setDefaultLang(DEFAULT_LANGUAGE);

    // Use stored or detected language
    const initialLang = this._currentLanguage();
    this.translate.use(initialLang);
  }

  private getInitialLanguage(): SupportedLanguage {
    // Try localStorage first
    if (this.isBrowser) {
      const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
      if (stored && this.isValidLanguage(stored)) {
        return stored as SupportedLanguage;
      }

      // Try browser language
      const browserLang = navigator.language?.split('-')[0];
      if (browserLang && this.isValidLanguage(browserLang)) {
        return browserLang as SupportedLanguage;
      }
    }

    return DEFAULT_LANGUAGE;
  }

  private isValidLanguage(lang: string): boolean {
    return SUPPORTED_LANGUAGES.some(l => l.code === lang);
  }

  setLanguage(lang: SupportedLanguage): void {
    if (!this.isValidLanguage(lang)) {
      console.warn(`Invalid language: ${lang}`);
      return;
    }

    this._currentLanguage.set(lang);
    this.translate.use(lang);

    if (this.isBrowser) {
      localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
      // Update HTML lang attribute
      document.documentElement.lang = lang;
    }
  }

  getCurrentLanguageInfo(): LanguageInfo | undefined {
    return SUPPORTED_LANGUAGES.find(l => l.code === this._currentLanguage());
  }

  instant(key: string, params?: object): string {
    return this.translate.instant(key, params);
  }
}
