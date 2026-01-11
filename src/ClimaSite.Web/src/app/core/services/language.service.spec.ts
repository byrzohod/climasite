import { TestBed } from '@angular/core/testing';
import { LanguageService, SUPPORTED_LANGUAGES } from './language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { PLATFORM_ID } from '@angular/core';

describe('LanguageService', () => {
  let service: LanguageService;
  let translateService: TranslateService;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot()],
      providers: [
        LanguageService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    service = TestBed.inject(LanguageService);
    translateService = TestBed.inject(TranslateService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have supported languages', () => {
    expect(service.languages.length).toBe(3);
    expect(service.languages.map(l => l.code)).toContain('en');
    expect(service.languages.map(l => l.code)).toContain('bg');
    expect(service.languages.map(l => l.code)).toContain('de');
  });

  it('should default to English when no stored preference', () => {
    expect(service.currentLanguage()).toBe('en');
  });

  it('should set language and persist to localStorage', () => {
    service.setLanguage('de');
    expect(service.currentLanguage()).toBe('de');
    expect(localStorage.getItem('climasite-language')).toBe('de');
  });

  it('should use stored language on initialization', () => {
    localStorage.setItem('climasite-language', 'bg');

    // Recreate service to pick up localStorage value
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot()],
      providers: [
        LanguageService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    const newService = TestBed.inject(LanguageService);
    expect(newService.currentLanguage()).toBe('bg');
  });

  it('should return current language info', () => {
    service.setLanguage('de');
    const info = service.getCurrentLanguageInfo();

    expect(info).toBeTruthy();
    expect(info?.code).toBe('de');
    expect(info?.name).toBe('German');
    expect(info?.nativeName).toBe('Deutsch');
  });

  it('should not set invalid language', () => {
    const originalLang = service.currentLanguage();
    service.setLanguage('invalid' as any);
    expect(service.currentLanguage()).toBe(originalLang);
  });

  it('should update translate service when language changes', () => {
    spyOn(translateService, 'use');
    service.setLanguage('bg');
    expect(translateService.use).toHaveBeenCalledWith('bg');
  });
});
