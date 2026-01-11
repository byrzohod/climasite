import { Injectable, signal, computed, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type ThemeMode = 'light' | 'dark' | 'system';
export type EffectiveTheme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'climasite-theme-preference';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // User's selected preference (light, dark, or system)
  private readonly _themeMode = signal<ThemeMode>(this.getStoredTheme());

  // System preference (light or dark)
  private readonly _systemPreference = signal<EffectiveTheme>(this.getSystemPreference());

  // Public readonly signals
  readonly themeMode = this._themeMode.asReadonly();

  // Computed effective theme based on mode and system preference
  readonly effectiveTheme = computed<EffectiveTheme>(() => {
    const mode = this._themeMode();
    if (mode === 'system') {
      return this._systemPreference();
    }
    return mode;
  });

  // Computed boolean for quick checks
  readonly isDarkMode = computed(() => this.effectiveTheme() === 'dark');

  constructor() {
    // Apply theme changes to DOM
    effect(() => {
      this.applyTheme(this.effectiveTheme());
    });

    // Listen for system preference changes
    if (this.isBrowser) {
      this.watchSystemPreference();
    }
  }

  /**
   * Sets the theme mode and persists to localStorage
   */
  setThemeMode(mode: ThemeMode): void {
    this._themeMode.set(mode);
    if (this.isBrowser) {
      localStorage.setItem(THEME_STORAGE_KEY, mode);
    }
  }

  /**
   * Toggles between light and dark mode
   * If currently on 'system', switches to the opposite of effective theme
   */
  toggleTheme(): void {
    const current = this.effectiveTheme();
    this.setThemeMode(current === 'light' ? 'dark' : 'light');
  }

  /**
   * Cycles through: light -> dark -> system -> light
   */
  cycleTheme(): void {
    const modes: ThemeMode[] = ['light', 'dark', 'system'];
    const currentIndex = modes.indexOf(this._themeMode());
    const nextIndex = (currentIndex + 1) % modes.length;
    this.setThemeMode(modes[nextIndex]);
  }

  private getStoredTheme(): ThemeMode {
    if (!this.isBrowser) return 'light';

    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored;
    }
    return 'system'; // Default to system preference
  }

  private getSystemPreference(): EffectiveTheme {
    if (!this.isBrowser) return 'light';

    return window.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
  }

  private watchSystemPreference(): void {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    mediaQuery.addEventListener('change', (e) => {
      this._systemPreference.set(e.matches ? 'dark' : 'light');
    });
  }

  private applyTheme(theme: EffectiveTheme): void {
    if (!this.isBrowser) return;

    const html = document.documentElement;

    if (theme === 'dark') {
      html.setAttribute('data-theme', 'dark');
      html.classList.add('dark');
    } else {
      html.removeAttribute('data-theme');
      html.classList.remove('dark');
    }
  }
}
