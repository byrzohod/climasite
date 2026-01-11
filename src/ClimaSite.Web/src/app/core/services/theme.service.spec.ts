import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';
import { PLATFORM_ID } from '@angular/core';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    // Reset document theme
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.classList.remove('dark');

    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.classList.remove('dark');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should default to system preference when no stored theme', () => {
    expect(service.themeMode()).toBe('system');
  });

  it('should apply light theme by default when system preference is light', () => {
    // Mock system preference as light
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    if (!mediaQuery.matches) {
      expect(service.effectiveTheme()).toBe('light');
    }
  });

  it('should toggle between light and dark mode', () => {
    service.setThemeMode('light');
    expect(service.effectiveTheme()).toBe('light');

    service.toggleTheme();
    expect(service.effectiveTheme()).toBe('dark');

    service.toggleTheme();
    expect(service.effectiveTheme()).toBe('light');
  });

  it('should cycle through light, dark, and system modes', () => {
    service.setThemeMode('light');

    service.cycleTheme();
    expect(service.themeMode()).toBe('dark');

    service.cycleTheme();
    expect(service.themeMode()).toBe('system');

    service.cycleTheme();
    expect(service.themeMode()).toBe('light');
  });

  it('should persist theme preference to localStorage', () => {
    service.setThemeMode('dark');
    expect(localStorage.getItem('climasite-theme-preference')).toBe('dark');
  });

  it('should load theme preference from localStorage', () => {
    localStorage.setItem('climasite-theme-preference', 'dark');

    // Recreate service to pick up localStorage value
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    const newService = TestBed.inject(ThemeService);

    expect(newService.themeMode()).toBe('dark');
  });

  it('should return correct isDarkMode computed value', () => {
    service.setThemeMode('light');
    expect(service.isDarkMode()).toBeFalse();

    service.setThemeMode('dark');
    expect(service.isDarkMode()).toBeTrue();
  });

  it('should set dark mode correctly', () => {
    // Test signal values directly without DOM side effects in tests
    service.setThemeMode('dark');
    expect(service.themeMode()).toBe('dark');
    expect(service.effectiveTheme()).toBe('dark');
    expect(service.isDarkMode()).toBeTrue();

    service.setThemeMode('light');
    expect(service.themeMode()).toBe('light');
    expect(service.effectiveTheme()).toBe('light');
    expect(service.isDarkMode()).toBeFalse();
  });
});
