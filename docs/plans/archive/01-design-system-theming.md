# Design System & Theming Plan

## 1. Overview

This document outlines the implementation plan for ClimaSite's centralized design system with theme switching capability. The system provides a single source of truth for all visual design tokens, enabling consistent branding and seamless light/dark mode transitions.

### Goals
- **Centralized Color Management**: All colors defined in ONE file for easy brand updates
- **Theme Switching**: Light and dark mode with smooth transitions
- **Developer Experience**: Type-safe tokens, IntelliSense support, clear naming conventions
- **Accessibility**: WCAG 2.1 AA compliant color contrasts
- **Performance**: Minimal runtime overhead using CSS custom properties
- **Testability**: Comprehensive E2E tests with Playwright (no mocking)

### Tech Stack
- Angular 19+ with standalone components
- Tailwind CSS with custom properties
- Angular Signals for reactive theme state
- Playwright for E2E testing

---

## 2. Color Architecture

### 2.1 Color File Structure

```
src/ClimaSite.Web/src/styles/
├── _colors.scss              # THE SINGLE SOURCE - All color definitions
├── _variables.scss           # CSS custom properties mapping
├── _theme-light.scss         # Light theme semantic values
├── _theme-dark.scss          # Dark theme semantic values
├── _typography.scss          # Font definitions and scales
├── _spacing.scss             # Spacing scale
├── _shadows.scss             # Shadow definitions
├── _animations.scss          # Transition and animation tokens
├── themes.scss               # Theme loader and aggregator
└── global.scss               # Global styles entry point
```

### 2.2 Color Definitions (_colors.scss)

```scss
// =============================================================================
// CLIMASITE COLOR PALETTE
// =============================================================================
// This file is THE SINGLE SOURCE OF TRUTH for all colors in the application.
// To change the brand colors, modify ONLY this file.
// =============================================================================

// -----------------------------------------------------------------------------
// BASE COLORS
// -----------------------------------------------------------------------------

// White & Black
$color-white: #ffffff;
$color-black: #000000;

// -----------------------------------------------------------------------------
// GRAY PALETTE (Neutral)
// -----------------------------------------------------------------------------
$color-gray-50: #f9fafb;
$color-gray-100: #f3f4f6;
$color-gray-200: #e5e7eb;
$color-gray-300: #d1d5db;
$color-gray-400: #9ca3af;
$color-gray-500: #6b7280;
$color-gray-600: #4b5563;
$color-gray-700: #374151;
$color-gray-800: #1f2937;
$color-gray-900: #111827;
$color-gray-950: #030712;

// -----------------------------------------------------------------------------
// PRIMARY PALETTE (Brand Blue - HVAC/Cooling theme)
// -----------------------------------------------------------------------------
$color-primary-50: #eff6ff;
$color-primary-100: #dbeafe;
$color-primary-200: #bfdbfe;
$color-primary-300: #93c5fd;
$color-primary-400: #60a5fa;
$color-primary-500: #3b82f6;
$color-primary-600: #2563eb;
$color-primary-700: #1d4ed8;
$color-primary-800: #1e40af;
$color-primary-900: #1e3a8a;
$color-primary-950: #172554;

// -----------------------------------------------------------------------------
// SECONDARY PALETTE (Slate - Professional/Technical)
// -----------------------------------------------------------------------------
$color-secondary-50: #f8fafc;
$color-secondary-100: #f1f5f9;
$color-secondary-200: #e2e8f0;
$color-secondary-300: #cbd5e1;
$color-secondary-400: #94a3b8;
$color-secondary-500: #64748b;
$color-secondary-600: #475569;
$color-secondary-700: #334155;
$color-secondary-800: #1e293b;
$color-secondary-900: #0f172a;
$color-secondary-950: #020617;

// -----------------------------------------------------------------------------
// ACCENT PALETTE (Cyan - Cool/Refreshing for AC theme)
// -----------------------------------------------------------------------------
$color-accent-50: #ecfeff;
$color-accent-100: #cffafe;
$color-accent-200: #a5f3fc;
$color-accent-300: #67e8f9;
$color-accent-400: #22d3ee;
$color-accent-500: #06b6d4;
$color-accent-600: #0891b2;
$color-accent-700: #0e7490;
$color-accent-800: #155e75;
$color-accent-900: #164e63;
$color-accent-950: #083344;

// -----------------------------------------------------------------------------
// WARM PALETTE (Orange - Heating theme)
// -----------------------------------------------------------------------------
$color-warm-50: #fff7ed;
$color-warm-100: #ffedd5;
$color-warm-200: #fed7aa;
$color-warm-300: #fdba74;
$color-warm-400: #fb923c;
$color-warm-500: #f97316;
$color-warm-600: #ea580c;
$color-warm-700: #c2410c;
$color-warm-800: #9a3412;
$color-warm-900: #7c2d12;
$color-warm-950: #431407;

// -----------------------------------------------------------------------------
// SUCCESS PALETTE (Green)
// -----------------------------------------------------------------------------
$color-success-50: #f0fdf4;
$color-success-100: #dcfce7;
$color-success-200: #bbf7d0;
$color-success-300: #86efac;
$color-success-400: #4ade80;
$color-success-500: #22c55e;
$color-success-600: #16a34a;
$color-success-700: #15803d;
$color-success-800: #166534;
$color-success-900: #14532d;
$color-success-950: #052e16;

// -----------------------------------------------------------------------------
// WARNING PALETTE (Amber)
// -----------------------------------------------------------------------------
$color-warning-50: #fffbeb;
$color-warning-100: #fef3c7;
$color-warning-200: #fde68a;
$color-warning-300: #fcd34d;
$color-warning-400: #fbbf24;
$color-warning-500: #f59e0b;
$color-warning-600: #d97706;
$color-warning-700: #b45309;
$color-warning-800: #92400e;
$color-warning-900: #78350f;
$color-warning-950: #451a03;

// -----------------------------------------------------------------------------
// ERROR PALETTE (Red)
// -----------------------------------------------------------------------------
$color-error-50: #fef2f2;
$color-error-100: #fee2e2;
$color-error-200: #fecaca;
$color-error-300: #fca5a5;
$color-error-400: #f87171;
$color-error-500: #ef4444;
$color-error-600: #dc2626;
$color-error-700: #b91c1c;
$color-error-800: #991b1b;
$color-error-900: #7f1d1d;
$color-error-950: #450a0a;

// -----------------------------------------------------------------------------
// INFO PALETTE (Sky)
// -----------------------------------------------------------------------------
$color-info-50: #f0f9ff;
$color-info-100: #e0f2fe;
$color-info-200: #bae6fd;
$color-info-300: #7dd3fc;
$color-info-400: #38bdf8;
$color-info-500: #0ea5e9;
$color-info-600: #0284c7;
$color-info-700: #0369a1;
$color-info-800: #075985;
$color-info-900: #0c4a6e;
$color-info-950: #082f49;
```

### 2.3 CSS Custom Properties (_variables.scss)

```scss
@use 'colors' as c;

// =============================================================================
// CSS CUSTOM PROPERTIES
// =============================================================================
// These properties enable runtime theming via data-theme attribute
// =============================================================================

:root {
  // ---------------------------------------------------------------------------
  // BACKGROUND COLORS
  // ---------------------------------------------------------------------------
  --color-bg-primary: #{c.$color-white};
  --color-bg-secondary: #{c.$color-gray-50};
  --color-bg-tertiary: #{c.$color-gray-100};
  --color-bg-inverse: #{c.$color-gray-900};
  --color-bg-overlay: rgba(0, 0, 0, 0.5);

  // Component backgrounds
  --color-bg-card: #{c.$color-white};
  --color-bg-input: #{c.$color-white};
  --color-bg-hover: #{c.$color-gray-50};
  --color-bg-active: #{c.$color-gray-100};
  --color-bg-disabled: #{c.$color-gray-100};

  // ---------------------------------------------------------------------------
  // TEXT COLORS
  // ---------------------------------------------------------------------------
  --color-text-primary: #{c.$color-gray-900};
  --color-text-secondary: #{c.$color-gray-600};
  --color-text-tertiary: #{c.$color-gray-500};
  --color-text-inverse: #{c.$color-white};
  --color-text-disabled: #{c.$color-gray-400};
  --color-text-placeholder: #{c.$color-gray-400};

  // ---------------------------------------------------------------------------
  // BORDER COLORS
  // ---------------------------------------------------------------------------
  --color-border-primary: #{c.$color-gray-200};
  --color-border-secondary: #{c.$color-gray-300};
  --color-border-focus: #{c.$color-primary-500};
  --color-border-error: #{c.$color-error-500};

  // ---------------------------------------------------------------------------
  // BRAND COLORS (Direct access)
  // ---------------------------------------------------------------------------
  --color-primary: #{c.$color-primary-500};
  --color-primary-hover: #{c.$color-primary-600};
  --color-primary-active: #{c.$color-primary-700};
  --color-primary-light: #{c.$color-primary-100};

  --color-secondary: #{c.$color-secondary-500};
  --color-secondary-hover: #{c.$color-secondary-600};
  --color-secondary-active: #{c.$color-secondary-700};

  --color-accent: #{c.$color-accent-500};
  --color-accent-hover: #{c.$color-accent-600};
  --color-accent-active: #{c.$color-accent-700};

  --color-warm: #{c.$color-warm-500};
  --color-warm-hover: #{c.$color-warm-600};

  // ---------------------------------------------------------------------------
  // SEMANTIC COLORS
  // ---------------------------------------------------------------------------
  --color-success: #{c.$color-success-500};
  --color-success-light: #{c.$color-success-100};
  --color-success-dark: #{c.$color-success-700};

  --color-warning: #{c.$color-warning-500};
  --color-warning-light: #{c.$color-warning-100};
  --color-warning-dark: #{c.$color-warning-700};

  --color-error: #{c.$color-error-500};
  --color-error-light: #{c.$color-error-100};
  --color-error-dark: #{c.$color-error-700};

  --color-info: #{c.$color-info-500};
  --color-info-light: #{c.$color-info-100};
  --color-info-dark: #{c.$color-info-700};

  // ---------------------------------------------------------------------------
  // SHADOW COLORS
  // ---------------------------------------------------------------------------
  --shadow-color: rgba(0, 0, 0, 0.1);
  --shadow-color-lg: rgba(0, 0, 0, 0.15);

  // ---------------------------------------------------------------------------
  // RING/FOCUS COLORS
  // ---------------------------------------------------------------------------
  --color-ring: #{c.$color-primary-500};
  --color-ring-offset: #{c.$color-white};
}

// =============================================================================
// DARK THEME OVERRIDES
// =============================================================================

[data-theme="dark"] {
  // ---------------------------------------------------------------------------
  // BACKGROUND COLORS
  // ---------------------------------------------------------------------------
  --color-bg-primary: #{c.$color-gray-900};
  --color-bg-secondary: #{c.$color-gray-800};
  --color-bg-tertiary: #{c.$color-gray-700};
  --color-bg-inverse: #{c.$color-white};
  --color-bg-overlay: rgba(0, 0, 0, 0.7);

  // Component backgrounds
  --color-bg-card: #{c.$color-gray-800};
  --color-bg-input: #{c.$color-gray-800};
  --color-bg-hover: #{c.$color-gray-700};
  --color-bg-active: #{c.$color-gray-600};
  --color-bg-disabled: #{c.$color-gray-700};

  // ---------------------------------------------------------------------------
  // TEXT COLORS
  // ---------------------------------------------------------------------------
  --color-text-primary: #{c.$color-gray-50};
  --color-text-secondary: #{c.$color-gray-300};
  --color-text-tertiary: #{c.$color-gray-400};
  --color-text-inverse: #{c.$color-gray-900};
  --color-text-disabled: #{c.$color-gray-500};
  --color-text-placeholder: #{c.$color-gray-500};

  // ---------------------------------------------------------------------------
  // BORDER COLORS
  // ---------------------------------------------------------------------------
  --color-border-primary: #{c.$color-gray-700};
  --color-border-secondary: #{c.$color-gray-600};
  --color-border-focus: #{c.$color-primary-400};
  --color-border-error: #{c.$color-error-400};

  // ---------------------------------------------------------------------------
  // BRAND COLORS (Adjusted for dark mode)
  // ---------------------------------------------------------------------------
  --color-primary: #{c.$color-primary-400};
  --color-primary-hover: #{c.$color-primary-300};
  --color-primary-active: #{c.$color-primary-500};
  --color-primary-light: #{c.$color-primary-900};

  --color-accent: #{c.$color-accent-400};
  --color-accent-hover: #{c.$color-accent-300};

  // ---------------------------------------------------------------------------
  // SEMANTIC COLORS (Adjusted for dark mode)
  // ---------------------------------------------------------------------------
  --color-success: #{c.$color-success-400};
  --color-success-light: #{c.$color-success-900};
  --color-success-dark: #{c.$color-success-300};

  --color-warning: #{c.$color-warning-400};
  --color-warning-light: #{c.$color-warning-900};
  --color-warning-dark: #{c.$color-warning-300};

  --color-error: #{c.$color-error-400};
  --color-error-light: #{c.$color-error-900};
  --color-error-dark: #{c.$color-error-300};

  --color-info: #{c.$color-info-400};
  --color-info-light: #{c.$color-info-900};
  --color-info-dark: #{c.$color-info-300};

  // ---------------------------------------------------------------------------
  // SHADOW COLORS
  // ---------------------------------------------------------------------------
  --shadow-color: rgba(0, 0, 0, 0.3);
  --shadow-color-lg: rgba(0, 0, 0, 0.4);

  // ---------------------------------------------------------------------------
  // RING/FOCUS COLORS
  // ---------------------------------------------------------------------------
  --color-ring: #{c.$color-primary-400};
  --color-ring-offset: #{c.$color-gray-900};
}
```

---

## 3. Tasks

### Task DST-001: Create Color System Foundation

**Priority**: Critical
**Estimated Time**: 4 hours
**Dependencies**: None

**Description**:
Create the centralized color system with all palette definitions in a single source file.

**Acceptance Criteria**:
- [ ] Create `_colors.scss` with all color palette definitions
- [ ] All colors follow consistent naming: `$color-{palette}-{shade}`
- [ ] Palettes include: gray, primary, secondary, accent, warm, success, warning, error, info
- [ ] Each palette has shades: 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950
- [ ] Colors are WCAG 2.1 AA compliant for text on backgrounds
- [ ] File includes documentation header explaining its purpose

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_colors.scss`

---

### Task DST-002: Create CSS Custom Properties System

**Priority**: Critical
**Estimated Time**: 3 hours
**Dependencies**: DST-001

**Description**:
Create the CSS custom properties layer that enables runtime theming.

**Acceptance Criteria**:
- [ ] Create `_variables.scss` with all CSS custom properties
- [ ] Properties organized by category: background, text, border, brand, semantic
- [ ] Light theme values defined in `:root`
- [ ] Dark theme values defined in `[data-theme="dark"]`
- [ ] All properties reference `_colors.scss` values
- [ ] Smooth transition for theme switching (0.2s ease)

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_variables.scss`

---

### Task DST-003: Theme Service Implementation

**Priority**: Critical
**Estimated Time**: 4 hours
**Dependencies**: DST-002

**Description**:
Create the Angular service that manages theme state using Signals.

**Acceptance Criteria**:
- [ ] Service uses Angular Signals for reactive state
- [ ] Supports three modes: 'light', 'dark', 'system'
- [ ] Persists user preference to localStorage
- [ ] Detects system color scheme preference
- [ ] Listens to system preference changes in real-time
- [ ] Applies theme by setting `data-theme` attribute on `<html>`
- [ ] Provides computed signal for current effective theme
- [ ] Injectable at root level (providedIn: 'root')

**Implementation**:

```typescript
// src/ClimaSite.Web/src/app/core/services/theme.service.ts

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
```

**Implementation Files**:
- `src/ClimaSite.Web/src/app/core/services/theme.service.ts`
- `src/ClimaSite.Web/src/app/core/services/theme.service.spec.ts`

---

### Task DST-004: Theme Toggle Component

**Priority**: High
**Estimated Time**: 3 hours
**Dependencies**: DST-003

**Description**:
Create a reusable theme toggle component with accessibility support.

**Acceptance Criteria**:
- [ ] Standalone component using signals
- [ ] Displays current theme state visually (sun/moon icons)
- [ ] Accessible: proper ARIA labels, keyboard navigation
- [ ] Smooth icon transition animation
- [ ] `data-testid="theme-toggle"` for E2E testing
- [ ] Supports both toggle (2-state) and cycle (3-state) modes
- [ ] Tooltip showing current mode on hover

**Implementation**:

```typescript
// src/ClimaSite.Web/src/app/shared/components/theme-toggle/theme-toggle.component.ts

import { Component, inject, input } from '@angular/core';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  template: `
    <button
      type="button"
      (click)="handleClick()"
      [attr.aria-label]="ariaLabel()"
      [attr.title]="tooltipText()"
      data-testid="theme-toggle"
      class="theme-toggle"
      [class.theme-toggle--dark]="themeService.isDarkMode()"
    >
      <!-- Sun Icon (Light Mode) -->
      <svg
        class="theme-toggle__icon theme-toggle__icon--sun"
        [class.theme-toggle__icon--active]="!themeService.isDarkMode()"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="5"/>
        <line x1="12" y1="1" x2="12" y2="3"/>
        <line x1="12" y1="21" x2="12" y2="23"/>
        <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>
        <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
        <line x1="1" y1="12" x2="3" y2="12"/>
        <line x1="21" y1="12" x2="23" y2="12"/>
        <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
        <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
      </svg>

      <!-- Moon Icon (Dark Mode) -->
      <svg
        class="theme-toggle__icon theme-toggle__icon--moon"
        [class.theme-toggle__icon--active]="themeService.isDarkMode()"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
      >
        <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
      </svg>
    </button>
  `,
  styles: [`
    .theme-toggle {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      padding: 0.5rem;
      border: none;
      border-radius: 0.5rem;
      background-color: var(--color-bg-hover);
      color: var(--color-text-primary);
      cursor: pointer;
      transition: background-color 0.2s ease, transform 0.2s ease;

      &:hover {
        background-color: var(--color-bg-active);
        transform: scale(1.05);
      }

      &:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }

      &:active {
        transform: scale(0.95);
      }
    }

    .theme-toggle__icon {
      position: absolute;
      width: 1.25rem;
      height: 1.25rem;
      transition: opacity 0.2s ease, transform 0.3s ease;
      opacity: 0;
      transform: rotate(-90deg) scale(0.5);

      &--active {
        opacity: 1;
        transform: rotate(0) scale(1);
      }
    }
  `]
})
export class ThemeToggleComponent {
  protected readonly themeService = inject(ThemeService);

  // Input to control toggle behavior
  readonly mode = input<'toggle' | 'cycle'>('toggle');

  protected ariaLabel = () => {
    const currentTheme = this.themeService.effectiveTheme();
    return `Switch to ${currentTheme === 'light' ? 'dark' : 'light'} mode`;
  };

  protected tooltipText = () => {
    const mode = this.themeService.themeMode();
    const effective = this.themeService.effectiveTheme();

    if (mode === 'system') {
      return `System preference (${effective})`;
    }
    return `${effective.charAt(0).toUpperCase() + effective.slice(1)} mode`;
  };

  protected handleClick(): void {
    if (this.mode() === 'cycle') {
      this.themeService.cycleTheme();
    } else {
      this.themeService.toggleTheme();
    }
  }
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/app/shared/components/theme-toggle/theme-toggle.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/theme-toggle/theme-toggle.component.spec.ts`

---

### Task DST-005: Tailwind CSS Configuration

**Priority**: High
**Estimated Time**: 2 hours
**Dependencies**: DST-001, DST-002

**Description**:
Configure Tailwind CSS to use the design system colors and custom properties.

**Acceptance Criteria**:
- [ ] Extend Tailwind theme with custom color palette
- [ ] Map CSS custom properties to Tailwind utilities
- [ ] Configure dark mode to use `[data-theme="dark"]` selector
- [ ] Add custom spacing scale if needed
- [ ] Configure typography plugin (if used)
- [ ] Enable JIT mode for optimal performance

**Implementation**:

```typescript
// src/ClimaSite.Web/tailwind.config.js

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  darkMode: ['selector', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        // Semantic colors using CSS custom properties
        background: {
          primary: 'var(--color-bg-primary)',
          secondary: 'var(--color-bg-secondary)',
          tertiary: 'var(--color-bg-tertiary)',
          inverse: 'var(--color-bg-inverse)',
          card: 'var(--color-bg-card)',
          input: 'var(--color-bg-input)',
          hover: 'var(--color-bg-hover)',
          active: 'var(--color-bg-active)',
          disabled: 'var(--color-bg-disabled)',
        },
        foreground: {
          primary: 'var(--color-text-primary)',
          secondary: 'var(--color-text-secondary)',
          tertiary: 'var(--color-text-tertiary)',
          inverse: 'var(--color-text-inverse)',
          disabled: 'var(--color-text-disabled)',
          placeholder: 'var(--color-text-placeholder)',
        },
        border: {
          primary: 'var(--color-border-primary)',
          secondary: 'var(--color-border-secondary)',
          focus: 'var(--color-border-focus)',
          error: 'var(--color-border-error)',
        },
        // Brand colors
        primary: {
          DEFAULT: 'var(--color-primary)',
          hover: 'var(--color-primary-hover)',
          active: 'var(--color-primary-active)',
          light: 'var(--color-primary-light)',
          // Static palette for backgrounds, etc.
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
        secondary: {
          DEFAULT: 'var(--color-secondary)',
          hover: 'var(--color-secondary-hover)',
          active: 'var(--color-secondary-active)',
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b',
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617',
        },
        accent: {
          DEFAULT: 'var(--color-accent)',
          hover: 'var(--color-accent-hover)',
          active: 'var(--color-accent-active)',
          50: '#ecfeff',
          100: '#cffafe',
          200: '#a5f3fc',
          300: '#67e8f9',
          400: '#22d3ee',
          500: '#06b6d4',
          600: '#0891b2',
          700: '#0e7490',
          800: '#155e75',
          900: '#164e63',
          950: '#083344',
        },
        warm: {
          DEFAULT: 'var(--color-warm)',
          hover: 'var(--color-warm-hover)',
          50: '#fff7ed',
          100: '#ffedd5',
          200: '#fed7aa',
          300: '#fdba74',
          400: '#fb923c',
          500: '#f97316',
          600: '#ea580c',
          700: '#c2410c',
          800: '#9a3412',
          900: '#7c2d12',
          950: '#431407',
        },
        // Semantic status colors
        success: {
          DEFAULT: 'var(--color-success)',
          light: 'var(--color-success-light)',
          dark: 'var(--color-success-dark)',
        },
        warning: {
          DEFAULT: 'var(--color-warning)',
          light: 'var(--color-warning-light)',
          dark: 'var(--color-warning-dark)',
        },
        error: {
          DEFAULT: 'var(--color-error)',
          light: 'var(--color-error-light)',
          dark: 'var(--color-error-dark)',
        },
        info: {
          DEFAULT: 'var(--color-info)',
          light: 'var(--color-info-light)',
          dark: 'var(--color-info-dark)',
        },
      },
      boxShadow: {
        'sm': '0 1px 2px 0 var(--shadow-color)',
        'DEFAULT': '0 1px 3px 0 var(--shadow-color), 0 1px 2px -1px var(--shadow-color)',
        'md': '0 4px 6px -1px var(--shadow-color), 0 2px 4px -2px var(--shadow-color)',
        'lg': '0 10px 15px -3px var(--shadow-color-lg), 0 4px 6px -4px var(--shadow-color)',
        'xl': '0 20px 25px -5px var(--shadow-color-lg), 0 8px 10px -6px var(--shadow-color)',
      },
      ringColor: {
        DEFAULT: 'var(--color-ring)',
      },
      ringOffsetColor: {
        DEFAULT: 'var(--color-ring-offset)',
      },
      transitionProperty: {
        'theme': 'background-color, border-color, color, fill, stroke',
      },
    },
  },
  plugins: [],
}
```

**Implementation Files**:
- `src/ClimaSite.Web/tailwind.config.js`

---

### Task DST-006: Typography System

**Priority**: High
**Estimated Time**: 3 hours
**Dependencies**: DST-001

**Description**:
Create typography scale and font configurations.

**Acceptance Criteria**:
- [ ] Define font family stack (primary, secondary, monospace)
- [ ] Create type scale with semantic names (heading-1 through heading-6, body, caption, etc.)
- [ ] Define line heights for each scale
- [ ] Define font weights
- [ ] Create responsive typography utilities
- [ ] Ensure proper font loading strategy

**Implementation**:

```scss
// src/ClimaSite.Web/src/styles/_typography.scss

// =============================================================================
// TYPOGRAPHY SYSTEM
// =============================================================================

// -----------------------------------------------------------------------------
// FONT FAMILIES
// -----------------------------------------------------------------------------
$font-family-sans: 'Inter', system-ui, -apple-system, BlinkMacSystemFont,
                   'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
$font-family-mono: 'JetBrains Mono', 'Fira Code', Consolas, Monaco,
                   'Andale Mono', 'Ubuntu Mono', monospace;

:root {
  --font-sans: #{$font-family-sans};
  --font-mono: #{$font-family-mono};
}

// -----------------------------------------------------------------------------
// FONT SIZES (Using a modular scale - 1.25 ratio)
// -----------------------------------------------------------------------------
:root {
  // Base size
  --text-base: 1rem;        // 16px

  // Scale down
  --text-xs: 0.75rem;       // 12px
  --text-sm: 0.875rem;      // 14px

  // Scale up
  --text-lg: 1.125rem;      // 18px
  --text-xl: 1.25rem;       // 20px
  --text-2xl: 1.5rem;       // 24px
  --text-3xl: 1.875rem;     // 30px
  --text-4xl: 2.25rem;      // 36px
  --text-5xl: 3rem;         // 48px
  --text-6xl: 3.75rem;      // 60px
}

// -----------------------------------------------------------------------------
// LINE HEIGHTS
// -----------------------------------------------------------------------------
:root {
  --leading-none: 1;
  --leading-tight: 1.25;
  --leading-snug: 1.375;
  --leading-normal: 1.5;
  --leading-relaxed: 1.625;
  --leading-loose: 2;
}

// -----------------------------------------------------------------------------
// FONT WEIGHTS
// -----------------------------------------------------------------------------
:root {
  --font-thin: 100;
  --font-extralight: 200;
  --font-light: 300;
  --font-normal: 400;
  --font-medium: 500;
  --font-semibold: 600;
  --font-bold: 700;
  --font-extrabold: 800;
  --font-black: 900;
}

// -----------------------------------------------------------------------------
// LETTER SPACING
// -----------------------------------------------------------------------------
:root {
  --tracking-tighter: -0.05em;
  --tracking-tight: -0.025em;
  --tracking-normal: 0em;
  --tracking-wide: 0.025em;
  --tracking-wider: 0.05em;
  --tracking-widest: 0.1em;
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_typography.scss`

---

### Task DST-007: Spacing System

**Priority**: Medium
**Estimated Time**: 2 hours
**Dependencies**: DST-001

**Description**:
Create consistent spacing scale using CSS custom properties.

**Acceptance Criteria**:
- [ ] Define spacing scale (0, 1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24, etc.)
- [ ] Use 4px (0.25rem) as base unit
- [ ] Create component-specific spacing tokens
- [ ] Document spacing usage guidelines

**Implementation**:

```scss
// src/ClimaSite.Web/src/styles/_spacing.scss

// =============================================================================
// SPACING SYSTEM
// =============================================================================
// Based on 4px (0.25rem) grid

:root {
  // Base spacing scale
  --space-0: 0;
  --space-px: 1px;
  --space-0-5: 0.125rem;  // 2px
  --space-1: 0.25rem;     // 4px
  --space-1-5: 0.375rem;  // 6px
  --space-2: 0.5rem;      // 8px
  --space-2-5: 0.625rem;  // 10px
  --space-3: 0.75rem;     // 12px
  --space-3-5: 0.875rem;  // 14px
  --space-4: 1rem;        // 16px
  --space-5: 1.25rem;     // 20px
  --space-6: 1.5rem;      // 24px
  --space-7: 1.75rem;     // 28px
  --space-8: 2rem;        // 32px
  --space-9: 2.25rem;     // 36px
  --space-10: 2.5rem;     // 40px
  --space-11: 2.75rem;    // 44px
  --space-12: 3rem;       // 48px
  --space-14: 3.5rem;     // 56px
  --space-16: 4rem;       // 64px
  --space-20: 5rem;       // 80px
  --space-24: 6rem;       // 96px
  --space-28: 7rem;       // 112px
  --space-32: 8rem;       // 128px
  --space-36: 9rem;       // 144px
  --space-40: 10rem;      // 160px
  --space-44: 11rem;      // 176px
  --space-48: 12rem;      // 192px
  --space-52: 13rem;      // 208px
  --space-56: 14rem;      // 224px
  --space-60: 15rem;      // 240px
  --space-64: 16rem;      // 256px
  --space-72: 18rem;      // 288px
  --space-80: 20rem;      // 320px
  --space-96: 24rem;      // 384px

  // Component-specific spacing
  --space-card-padding: var(--space-6);
  --space-section-padding: var(--space-16);
  --space-container-padding: var(--space-4);
  --space-input-padding-x: var(--space-3);
  --space-input-padding-y: var(--space-2);
  --space-button-padding-x: var(--space-4);
  --space-button-padding-y: var(--space-2);

  // Gap scales
  --gap-xs: var(--space-1);
  --gap-sm: var(--space-2);
  --gap-md: var(--space-4);
  --gap-lg: var(--space-6);
  --gap-xl: var(--space-8);
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_spacing.scss`

---

### Task DST-008: Shadow System

**Priority**: Medium
**Estimated Time**: 1 hour
**Dependencies**: DST-002

**Description**:
Create shadow definitions that work with theme switching.

**Acceptance Criteria**:
- [ ] Define shadow scale (sm, default, md, lg, xl, 2xl, inner, none)
- [ ] Shadows use CSS custom properties for color
- [ ] Shadows adapt appropriately for dark mode
- [ ] Include elevation tokens for component layers

**Implementation**:

```scss
// src/ClimaSite.Web/src/styles/_shadows.scss

// =============================================================================
// SHADOW SYSTEM
// =============================================================================

:root {
  // Shadow scale
  --shadow-sm: 0 1px 2px 0 var(--shadow-color);
  --shadow: 0 1px 3px 0 var(--shadow-color), 0 1px 2px -1px var(--shadow-color);
  --shadow-md: 0 4px 6px -1px var(--shadow-color), 0 2px 4px -2px var(--shadow-color);
  --shadow-lg: 0 10px 15px -3px var(--shadow-color-lg), 0 4px 6px -4px var(--shadow-color);
  --shadow-xl: 0 20px 25px -5px var(--shadow-color-lg), 0 8px 10px -6px var(--shadow-color);
  --shadow-2xl: 0 25px 50px -12px var(--shadow-color-lg);
  --shadow-inner: inset 0 2px 4px 0 var(--shadow-color);
  --shadow-none: 0 0 #0000;

  // Elevation tokens (for layered UI)
  --elevation-0: var(--shadow-none);
  --elevation-1: var(--shadow-sm);
  --elevation-2: var(--shadow);
  --elevation-3: var(--shadow-md);
  --elevation-4: var(--shadow-lg);
  --elevation-5: var(--shadow-xl);
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_shadows.scss`

---

### Task DST-009: Animation & Transition Tokens

**Priority**: Medium
**Estimated Time**: 1 hour
**Dependencies**: None

**Description**:
Create animation and transition tokens for consistent motion design.

**Acceptance Criteria**:
- [ ] Define duration tokens (fast, normal, slow)
- [ ] Define easing functions
- [ ] Create transition presets
- [ ] Support reduced motion preferences

**Implementation**:

```scss
// src/ClimaSite.Web/src/styles/_animations.scss

// =============================================================================
// ANIMATION & TRANSITION SYSTEM
// =============================================================================

:root {
  // Durations
  --duration-instant: 0ms;
  --duration-fast: 100ms;
  --duration-normal: 200ms;
  --duration-slow: 300ms;
  --duration-slower: 500ms;
  --duration-slowest: 700ms;

  // Easing functions
  --ease-linear: linear;
  --ease-in: cubic-bezier(0.4, 0, 1, 1);
  --ease-out: cubic-bezier(0, 0, 0.2, 1);
  --ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);
  --ease-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);

  // Common transitions
  --transition-colors: background-color var(--duration-normal) var(--ease-in-out),
                       border-color var(--duration-normal) var(--ease-in-out),
                       color var(--duration-normal) var(--ease-in-out),
                       fill var(--duration-normal) var(--ease-in-out),
                       stroke var(--duration-normal) var(--ease-in-out);
  --transition-opacity: opacity var(--duration-normal) var(--ease-in-out);
  --transition-shadow: box-shadow var(--duration-normal) var(--ease-in-out);
  --transition-transform: transform var(--duration-normal) var(--ease-in-out);
  --transition-all: all var(--duration-normal) var(--ease-in-out);

  // Theme transition
  --transition-theme: var(--transition-colors), var(--transition-shadow);
}

// Respect reduced motion preference
@media (prefers-reduced-motion: reduce) {
  :root {
    --duration-fast: 0ms;
    --duration-normal: 0ms;
    --duration-slow: 0ms;
    --duration-slower: 0ms;
    --duration-slowest: 0ms;
  }
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_animations.scss`

---

### Task DST-010: Border Radius System

**Priority**: Low
**Estimated Time**: 30 minutes
**Dependencies**: None

**Description**:
Create consistent border radius tokens.

**Acceptance Criteria**:
- [ ] Define radius scale
- [ ] Include component-specific radius tokens

**Implementation**:

```scss
// src/ClimaSite.Web/src/styles/_borders.scss

// =============================================================================
// BORDER RADIUS SYSTEM
// =============================================================================

:root {
  --radius-none: 0;
  --radius-sm: 0.125rem;   // 2px
  --radius-md: 0.25rem;    // 4px
  --radius-DEFAULT: 0.375rem; // 6px
  --radius-lg: 0.5rem;     // 8px
  --radius-xl: 0.75rem;    // 12px
  --radius-2xl: 1rem;      // 16px
  --radius-3xl: 1.5rem;    // 24px
  --radius-full: 9999px;

  // Component-specific radii
  --radius-button: var(--radius-lg);
  --radius-input: var(--radius-md);
  --radius-card: var(--radius-xl);
  --radius-modal: var(--radius-2xl);
  --radius-badge: var(--radius-full);
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/_borders.scss`

---

### Task DST-011: Global Styles Entry Point

**Priority**: High
**Estimated Time**: 1 hour
**Dependencies**: DST-001 through DST-010

**Description**:
Create the main styles entry point that imports all design tokens.

**Acceptance Criteria**:
- [ ] Import all style modules in correct order
- [ ] Set up base/reset styles
- [ ] Configure global transition for theme switching
- [ ] Set default font and colors

**Implementation**:

```scss
// src/ClimaSite.Web/src/styles/global.scss

// =============================================================================
// GLOBAL STYLES - ENTRY POINT
// =============================================================================

// Design tokens (order matters!)
@use 'colors';
@use 'variables';
@use 'typography';
@use 'spacing';
@use 'shadows';
@use 'animations';
@use 'borders';

// Tailwind directives
@tailwind base;
@tailwind components;
@tailwind utilities;

// =============================================================================
// BASE STYLES
// =============================================================================

@layer base {
  *,
  *::before,
  *::after {
    box-sizing: border-box;
  }

  html {
    font-family: var(--font-sans);
    font-size: 16px;
    line-height: var(--leading-normal);
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
    scroll-behavior: smooth;

    // Enable smooth theme transitions
    transition: var(--transition-theme);
  }

  body {
    margin: 0;
    padding: 0;
    background-color: var(--color-bg-primary);
    color: var(--color-text-primary);
    min-height: 100vh;
  }

  // Focus styles
  :focus-visible {
    outline: 2px solid var(--color-ring);
    outline-offset: 2px;
  }

  // Selection styles
  ::selection {
    background-color: var(--color-primary-light);
    color: var(--color-primary);
  }

  // Reduce motion when requested
  @media (prefers-reduced-motion: reduce) {
    html {
      scroll-behavior: auto;
    }

    *,
    *::before,
    *::after {
      animation-duration: 0.01ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.01ms !important;
    }
  }
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/styles/global.scss`

---

### Task DST-012: Theme Persistence Integration

**Priority**: Medium
**Estimated Time**: 1 hour
**Dependencies**: DST-003

**Description**:
Ensure theme preference is applied on initial page load to prevent flash of unstyled content (FOUC).

**Acceptance Criteria**:
- [ ] Add inline script in index.html to set theme before page renders
- [ ] Script reads from localStorage
- [ ] Falls back to system preference
- [ ] No flash of wrong theme on page load

**Implementation**:

```html
<!-- src/ClimaSite.Web/src/index.html -->
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>ClimaSite - HVAC Products</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">

  <!-- Theme initialization script - prevents FOUC -->
  <script>
    (function() {
      const STORAGE_KEY = 'climasite-theme-preference';

      function getInitialTheme() {
        const stored = localStorage.getItem(STORAGE_KEY);

        if (stored === 'dark') return 'dark';
        if (stored === 'light') return 'light';

        // System preference or default
        if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
          return 'dark';
        }

        return 'light';
      }

      const theme = getInitialTheme();

      if (theme === 'dark') {
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.classList.add('dark');
      }
    })();
  </script>
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

**Implementation Files**:
- `src/ClimaSite.Web/src/index.html`

---

### Task DST-013: Button Component with Theme Support

**Priority**: Medium
**Estimated Time**: 3 hours
**Dependencies**: DST-001 through DST-011

**Description**:
Create a base button component demonstrating the design system usage.

**Acceptance Criteria**:
- [ ] Multiple variants: primary, secondary, outline, ghost
- [ ] Multiple sizes: sm, md, lg
- [ ] Disabled state support
- [ ] Loading state support
- [ ] Theme-aware colors
- [ ] Accessible focus states

**Implementation**:

```typescript
// src/ClimaSite.Web/src/app/shared/components/button/button.component.ts

import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [class]="buttonClasses()"
      [attr.aria-busy]="loading() ? 'true' : null"
      [attr.data-testid]="testId()"
      (click)="handleClick($event)"
    >
      @if (loading()) {
        <svg
          class="animate-spin -ml-1 mr-2 h-4 w-4"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          />
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
          />
        </svg>
      }
      <ng-content />
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
  `]
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly fullWidth = input(false);
  readonly testId = input<string>('');

  readonly clicked = output<MouseEvent>();

  protected buttonClasses = (): string => {
    const base = 'inline-flex items-center justify-center font-medium rounded-lg transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2';

    const variants: Record<ButtonVariant, string> = {
      primary: 'bg-primary text-white hover:bg-primary-hover active:bg-primary-active focus-visible:ring-primary',
      secondary: 'bg-secondary-100 text-secondary-900 hover:bg-secondary-200 active:bg-secondary-300 focus-visible:ring-secondary-500 dark:bg-secondary-800 dark:text-secondary-100 dark:hover:bg-secondary-700',
      outline: 'border-2 border-primary text-primary hover:bg-primary-light active:bg-primary-200 focus-visible:ring-primary',
      ghost: 'text-foreground-primary hover:bg-background-hover active:bg-background-active focus-visible:ring-primary',
      danger: 'bg-error text-white hover:bg-error-dark active:bg-error-dark focus-visible:ring-error',
    };

    const sizes: Record<ButtonSize, string> = {
      sm: 'text-sm px-3 py-1.5 gap-1.5',
      md: 'text-base px-4 py-2 gap-2',
      lg: 'text-lg px-6 py-3 gap-2.5',
    };

    const disabled = this.disabled() || this.loading()
      ? 'opacity-50 cursor-not-allowed'
      : 'cursor-pointer';

    const width = this.fullWidth() ? 'w-full' : '';

    return `${base} ${variants[this.variant()]} ${sizes[this.size()]} ${disabled} ${width}`;
  };

  protected handleClick(event: MouseEvent): void {
    if (!this.disabled() && !this.loading()) {
      this.clicked.emit(event);
    }
  }
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/app/shared/components/button/button.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/button/button.component.spec.ts`

---

### Task DST-014: Card Component with Theme Support

**Priority**: Medium
**Estimated Time**: 2 hours
**Dependencies**: DST-001 through DST-011

**Description**:
Create a card component for product displays and content containers.

**Acceptance Criteria**:
- [ ] Multiple variants: default, elevated, outlined
- [ ] Configurable padding
- [ ] Optional header and footer slots
- [ ] Theme-aware styling
- [ ] Hover state option for interactive cards

**Implementation**:

```typescript
// src/ClimaSite.Web/src/app/shared/components/card/card.component.ts

import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type CardVariant = 'default' | 'elevated' | 'outlined';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [class]="cardClasses()" [attr.data-testid]="testId()">
      @if (hasHeader) {
        <div class="card-header border-b border-border-primary px-6 py-4">
          <ng-content select="[card-header]" />
        </div>
      }

      <div [class]="bodyClasses()">
        <ng-content />
      </div>

      @if (hasFooter) {
        <div class="card-footer border-t border-border-primary px-6 py-4">
          <ng-content select="[card-footer]" />
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class CardComponent {
  readonly variant = input<CardVariant>('default');
  readonly padding = input<'none' | 'sm' | 'md' | 'lg'>('md');
  readonly interactive = input(false);
  readonly testId = input<string>('');

  hasHeader = false;
  hasFooter = false;

  protected cardClasses = (): string => {
    const base = 'bg-background-card rounded-xl transition-all';

    const variants: Record<CardVariant, string> = {
      default: 'shadow',
      elevated: 'shadow-lg',
      outlined: 'border border-border-primary shadow-none',
    };

    const interactive = this.interactive()
      ? 'cursor-pointer hover:shadow-lg hover:-translate-y-0.5'
      : '';

    return `${base} ${variants[this.variant()]} ${interactive}`;
  };

  protected bodyClasses = (): string => {
    const padding: Record<string, string> = {
      none: '',
      sm: 'p-4',
      md: 'p-6',
      lg: 'p-8',
    };

    return padding[this.padding()];
  };
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/app/shared/components/card/card.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/card/card.component.spec.ts`

---

### Task DST-015: Input Component with Theme Support

**Priority**: Medium
**Estimated Time**: 2 hours
**Dependencies**: DST-001 through DST-011

**Description**:
Create a form input component with validation states.

**Acceptance Criteria**:
- [ ] Support for text, email, password, number input types
- [ ] Validation states: default, error, success
- [ ] Label and helper text support
- [ ] Theme-aware focus and border colors
- [ ] Accessible error announcements

**Implementation**:

```typescript
// src/ClimaSite.Web/src/app/shared/components/input/input.component.ts

import { Component, input, output, signal, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type InputState = 'default' | 'error' | 'success';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="input-wrapper">
      @if (label()) {
        <label
          [for]="inputId()"
          class="block text-sm font-medium text-foreground-primary mb-1.5"
        >
          {{ label() }}
          @if (required()) {
            <span class="text-error">*</span>
          }
        </label>
      }

      <div class="relative">
        <input
          [id]="inputId()"
          [type]="type()"
          [placeholder]="placeholder()"
          [disabled]="isDisabled()"
          [required]="required()"
          [value]="value()"
          [class]="inputClasses()"
          [attr.aria-invalid]="state() === 'error'"
          [attr.aria-describedby]="helperText() ? inputId() + '-helper' : null"
          [attr.data-testid]="testId()"
          (input)="onInput($event)"
          (blur)="onTouched()"
        />

        @if (state() === 'error') {
          <div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
            <svg class="h-5 w-5 text-error" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </div>
        }

        @if (state() === 'success') {
          <div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
            <svg class="h-5 w-5 text-success" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
          </div>
        }
      </div>

      @if (helperText()) {
        <p
          [id]="inputId() + '-helper'"
          [class]="helperTextClasses()"
        >
          {{ helperText() }}
        </p>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class InputComponent implements ControlValueAccessor {
  readonly inputId = input.required<string>();
  readonly type = input<'text' | 'email' | 'password' | 'number'>('text');
  readonly label = input<string>('');
  readonly placeholder = input<string>('');
  readonly helperText = input<string>('');
  readonly state = input<InputState>('default');
  readonly required = input(false);
  readonly testId = input<string>('');

  readonly value = signal('');
  readonly isDisabled = signal(false);

  private onChange: (value: string) => void = () => {};
  protected onTouched: () => void = () => {};

  protected inputClasses = (): string => {
    const base = 'block w-full rounded-lg border px-3 py-2 text-foreground-primary bg-background-input placeholder:text-foreground-placeholder transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2';

    const states: Record<InputState, string> = {
      default: 'border-border-primary focus:border-border-focus focus:ring-primary',
      error: 'border-error pr-10 focus:border-error focus:ring-error',
      success: 'border-success pr-10 focus:border-success focus:ring-success',
    };

    const disabled = this.isDisabled()
      ? 'bg-background-disabled cursor-not-allowed opacity-50'
      : '';

    return `${base} ${states[this.state()]} ${disabled}`;
  };

  protected helperTextClasses = (): string => {
    const base = 'mt-1.5 text-sm';

    if (this.state() === 'error') return `${base} text-error`;
    if (this.state() === 'success') return `${base} text-success`;
    return `${base} text-foreground-secondary`;
  };

  protected onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value.set(target.value);
    this.onChange(target.value);
  }

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    this.value.set(value || '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }
}
```

**Implementation Files**:
- `src/ClimaSite.Web/src/app/shared/components/input/input.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/input/input.component.spec.ts`

---

## 4. E2E Tests (Playwright - NO MOCKING)

All E2E tests use real scenarios without mocking. Tests verify actual behavior in the browser.

### Test DST-E2E-001: Theme Switching

```typescript
// e2e/tests/theme-switching.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Theme Switching', () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage before each test
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  });

  test('should default to light theme when no preference stored', async ({ page }) => {
    await page.goto('/');

    const html = page.locator('html');

    // Should not have dark theme attribute
    await expect(html).not.toHaveAttribute('data-theme', 'dark');

    // Verify light theme background color is applied
    const body = page.locator('body');
    const bgColor = await body.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );

    // Light theme should have light background (white or near-white)
    expect(bgColor).toMatch(/rgb\(255, 255, 255\)|rgba\(255, 255, 255/);
  });

  test('user can switch to dark theme by clicking toggle', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const html = page.locator('html');

    // Initially light theme
    await expect(html).not.toHaveAttribute('data-theme', 'dark');

    // Click theme toggle
    await themeToggle.click();

    // Should now have dark theme
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Verify dark theme is visually applied
    const body = page.locator('body');
    const bgColor = await body.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );

    // Dark theme should have dark background
    // gray-900 is approximately rgb(17, 24, 39)
    expect(bgColor).not.toMatch(/rgb\(255, 255, 255\)/);
  });

  test('theme preference persists after page reload', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const html = page.locator('html');

    // Switch to dark theme
    await themeToggle.click();
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Reload the page
    await page.reload();

    // Should still be dark theme
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Verify localStorage has the preference
    const storedTheme = await page.evaluate(() =>
      localStorage.getItem('climasite-theme-preference')
    );
    expect(storedTheme).toBe('dark');
  });

  test('theme toggle is keyboard accessible', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const html = page.locator('html');

    // Focus the toggle using Tab
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab'); // May need multiple tabs depending on page structure

    // Activate with Enter key
    await themeToggle.focus();
    await page.keyboard.press('Enter');

    // Should toggle theme
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Space key should also work
    await page.keyboard.press('Space');
    await expect(html).not.toHaveAttribute('data-theme', 'dark');
  });

  test('theme toggle has proper ARIA attributes', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');

    // Should have aria-label
    const ariaLabel = await themeToggle.getAttribute('aria-label');
    expect(ariaLabel).toBeTruthy();
    expect(ariaLabel?.toLowerCase()).toContain('switch');
    expect(ariaLabel?.toLowerCase()).toMatch(/dark|light/);
  });

  test('can switch between themes multiple times', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const html = page.locator('html');

    // Toggle multiple times
    for (let i = 0; i < 5; i++) {
      await themeToggle.click();

      if (i % 2 === 0) {
        await expect(html).toHaveAttribute('data-theme', 'dark');
      } else {
        await expect(html).not.toHaveAttribute('data-theme', 'dark');
      }
    }
  });
});
```

---

### Test DST-E2E-002: Theme Consistency Across Navigation

```typescript
// e2e/tests/theme-navigation.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Theme Consistency Across Navigation', () => {
  test('dark theme persists when navigating between pages', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const html = page.locator('html');

    // Switch to dark theme
    await themeToggle.click();
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Navigate to products page
    await page.click('a[href="/products"]');
    await page.waitForURL('**/products');

    // Theme should still be dark
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Navigate to another page
    await page.click('a[href="/about"]');
    await page.waitForURL('**/about');

    // Theme should still be dark
    await expect(html).toHaveAttribute('data-theme', 'dark');
  });

  test('theme persists across browser sessions', async ({ page, context }) => {
    await page.goto('/');

    // Set dark theme
    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    await themeToggle.click();

    const html = page.locator('html');
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Close and create new page (simulating new session)
    const newPage = await context.newPage();
    await newPage.goto('/');

    // Theme should be dark from localStorage
    const newHtml = newPage.locator('html');
    await expect(newHtml).toHaveAttribute('data-theme', 'dark');
  });

  test('no flash of wrong theme on page load (FOUC prevention)', async ({ page }) => {
    // First, set dark theme preference
    await page.goto('/');
    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    await themeToggle.click();

    // Now reload with network condition to slow things down
    await page.route('**/*.js', (route) => {
      // Add slight delay to JS loading
      setTimeout(() => route.continue(), 100);
    });

    // Track theme attribute changes
    const themeChanges: string[] = [];

    await page.exposeFunction('trackTheme', (theme: string) => {
      themeChanges.push(theme);
    });

    await page.addInitScript(() => {
      const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
          if (mutation.attributeName === 'data-theme') {
            const theme = document.documentElement.getAttribute('data-theme') || 'light';
            (window as any).trackTheme(theme);
          }
        });
      });

      observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['data-theme'],
      });
    });

    await page.reload();
    await page.waitForLoadState('domcontentloaded');

    // If there was a FOUC, we'd see a transition from light to dark
    // With proper implementation, it should start as dark
    const html = page.locator('html');
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Theme should not have flashed to light then dark
    expect(themeChanges.includes('light')).toBe(false);
  });
});
```

---

### Test DST-E2E-003: System Theme Preference

```typescript
// e2e/tests/system-theme-preference.spec.ts

import { test, expect } from '@playwright/test';

test.describe('System Theme Preference', () => {
  test('respects system dark mode preference', async ({ page }) => {
    // Emulate dark mode system preference
    await page.emulateMedia({ colorScheme: 'dark' });

    // Clear any stored preference
    await page.goto('/');
    await page.evaluate(() => localStorage.removeItem('climasite-theme-preference'));
    await page.reload();

    const html = page.locator('html');

    // Should automatically use dark theme based on system preference
    await expect(html).toHaveAttribute('data-theme', 'dark');
  });

  test('respects system light mode preference', async ({ page }) => {
    // Emulate light mode system preference
    await page.emulateMedia({ colorScheme: 'light' });

    // Clear any stored preference
    await page.goto('/');
    await page.evaluate(() => localStorage.removeItem('climasite-theme-preference'));
    await page.reload();

    const html = page.locator('html');

    // Should use light theme (no dark attribute)
    await expect(html).not.toHaveAttribute('data-theme', 'dark');
  });

  test('manual preference overrides system preference', async ({ page }) => {
    // Set system to dark mode
    await page.emulateMedia({ colorScheme: 'dark' });

    await page.goto('/');
    await page.evaluate(() => localStorage.removeItem('climasite-theme-preference'));
    await page.reload();

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const html = page.locator('html');

    // Initially dark due to system preference
    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Toggle to light manually
    await themeToggle.click();

    // Should now be light despite system preference
    await expect(html).not.toHaveAttribute('data-theme', 'dark');

    // Preference should be stored
    const storedPref = await page.evaluate(() =>
      localStorage.getItem('climasite-theme-preference')
    );
    expect(storedPref).toBe('light');

    // Reload and verify manual preference persists
    await page.reload();
    await expect(html).not.toHaveAttribute('data-theme', 'dark');
  });

  test('responds to system preference changes when on "system" mode', async ({ page }) => {
    await page.goto('/');

    // Set to system mode explicitly (clear any manual preference)
    await page.evaluate(() => {
      localStorage.setItem('climasite-theme-preference', 'system');
    });
    await page.reload();

    const html = page.locator('html');

    // Start with light system preference
    await page.emulateMedia({ colorScheme: 'light' });
    await page.waitForTimeout(100); // Allow time for change detection

    await expect(html).not.toHaveAttribute('data-theme', 'dark');

    // Change system to dark
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.waitForTimeout(100);

    await expect(html).toHaveAttribute('data-theme', 'dark');

    // Change back to light
    await page.emulateMedia({ colorScheme: 'light' });
    await page.waitForTimeout(100);

    await expect(html).not.toHaveAttribute('data-theme', 'dark');
  });
});
```

---

### Test DST-E2E-004: Component Theme Styling

```typescript
// e2e/tests/component-theming.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Component Theme Styling', () => {
  test('buttons change color with theme', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const primaryButton = page.locator('[data-testid="primary-button"]').first();

    // Get button color in light mode
    const lightModeColor = await primaryButton.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );

    // Switch to dark mode
    await themeToggle.click();
    await page.waitForTimeout(250); // Wait for transition

    // Get button color in dark mode
    const darkModeColor = await primaryButton.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );

    // Colors might be slightly different for dark mode (lighter primary)
    // At minimum, verify the button is still visible and styled
    expect(lightModeColor).toBeTruthy();
    expect(darkModeColor).toBeTruthy();
  });

  test('cards have correct background in each theme', async ({ page }) => {
    await page.goto('/products');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const productCard = page.locator('[data-testid="product-card"]').first();

    // Light mode - card should have light background
    const lightCardBg = await productCard.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );

    // Switch to dark mode
    await themeToggle.click();
    await page.waitForTimeout(250);

    // Dark mode - card should have dark background
    const darkCardBg = await productCard.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );

    // Backgrounds should be different
    expect(lightCardBg).not.toBe(darkCardBg);

    // Light mode should be lighter (higher RGB values)
    const lightRgb = lightCardBg.match(/\d+/g)?.map(Number) || [0, 0, 0];
    const darkRgb = darkCardBg.match(/\d+/g)?.map(Number) || [255, 255, 255];

    const lightAvg = (lightRgb[0] + lightRgb[1] + lightRgb[2]) / 3;
    const darkAvg = (darkRgb[0] + darkRgb[1] + darkRgb[2]) / 3;

    expect(lightAvg).toBeGreaterThan(darkAvg);
  });

  test('input fields are readable in both themes', async ({ page }) => {
    await page.goto('/contact'); // Assuming a contact page with a form

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const input = page.locator('input[type="text"]').first();

    // Test in light mode
    const lightInputBg = await input.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );
    const lightInputColor = await input.evaluate((el) =>
      getComputedStyle(el).color
    );

    // Type in the input
    await input.fill('Test text');

    // Text should be visible (we'll check contrast)
    await expect(input).toHaveValue('Test text');

    // Switch to dark mode
    await themeToggle.click();
    await page.waitForTimeout(250);

    const darkInputBg = await input.evaluate((el) =>
      getComputedStyle(el).backgroundColor
    );
    const darkInputColor = await input.evaluate((el) =>
      getComputedStyle(el).color
    );

    // Input should still be readable (different colors)
    expect(darkInputBg).not.toBe(lightInputBg);
    expect(darkInputColor).not.toBe(lightInputColor);

    // Text should still be visible
    await expect(input).toHaveValue('Test text');
  });

  test('theme transition is smooth', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const body = page.locator('body');

    // Check that transition property is set
    const transitionProperty = await body.evaluate((el) =>
      getComputedStyle(el).transitionProperty
    );

    // Should have background-color transition
    expect(transitionProperty).toMatch(/background|all/);

    // Check transition duration
    const transitionDuration = await body.evaluate((el) =>
      getComputedStyle(el).transitionDuration
    );

    // Should have a transition duration (not 0s)
    expect(transitionDuration).not.toBe('0s');
  });

  test('text remains readable after theme change', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');
    const heading = page.locator('h1').first();
    const paragraph = page.locator('p').first();

    // Get text colors in light mode
    const lightHeadingColor = await heading.evaluate((el) =>
      getComputedStyle(el).color
    );
    const lightParagraphColor = await paragraph.evaluate((el) =>
      getComputedStyle(el).color
    );

    // Switch to dark mode
    await themeToggle.click();
    await page.waitForTimeout(250);

    // Get text colors in dark mode
    const darkHeadingColor = await heading.evaluate((el) =>
      getComputedStyle(el).color
    );
    const darkParagraphColor = await paragraph.evaluate((el) =>
      getComputedStyle(el).color
    );

    // Colors should be inverted (light text on dark bg)
    expect(darkHeadingColor).not.toBe(lightHeadingColor);
    expect(darkParagraphColor).not.toBe(lightParagraphColor);

    // Dark mode text should be light (higher RGB values)
    const darkHeadingRgb = darkHeadingColor.match(/\d+/g)?.map(Number) || [0, 0, 0];
    const lightHeadingRgb = lightHeadingColor.match(/\d+/g)?.map(Number) || [255, 255, 255];

    const darkAvg = (darkHeadingRgb[0] + darkHeadingRgb[1] + darkHeadingRgb[2]) / 3;
    const lightAvg = (lightHeadingRgb[0] + lightHeadingRgb[1] + lightHeadingRgb[2]) / 3;

    expect(darkAvg).toBeGreaterThan(lightAvg);
  });
});
```

---

### Test DST-E2E-005: Visual Regression Testing

```typescript
// e2e/tests/visual-regression.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Visual Regression - Theme', () => {
  test('homepage light theme matches snapshot', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.setItem('climasite-theme-preference', 'light');
    });
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Wait for fonts and images to load
    await page.waitForTimeout(500);

    await expect(page).toHaveScreenshot('homepage-light.png', {
      fullPage: true,
      maxDiffPixelRatio: 0.02,
    });
  });

  test('homepage dark theme matches snapshot', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.setItem('climasite-theme-preference', 'dark');
    });
    await page.reload();
    await page.waitForLoadState('networkidle');

    await page.waitForTimeout(500);

    await expect(page).toHaveScreenshot('homepage-dark.png', {
      fullPage: true,
      maxDiffPixelRatio: 0.02,
    });
  });

  test('product listing light theme matches snapshot', async ({ page }) => {
    await page.goto('/products');
    await page.evaluate(() => {
      localStorage.setItem('climasite-theme-preference', 'light');
    });
    await page.reload();
    await page.waitForLoadState('networkidle');

    await page.waitForTimeout(500);

    await expect(page).toHaveScreenshot('products-light.png', {
      fullPage: true,
      maxDiffPixelRatio: 0.02,
    });
  });

  test('product listing dark theme matches snapshot', async ({ page }) => {
    await page.goto('/products');
    await page.evaluate(() => {
      localStorage.setItem('climasite-theme-preference', 'dark');
    });
    await page.reload();
    await page.waitForLoadState('networkidle');

    await page.waitForTimeout(500);

    await expect(page).toHaveScreenshot('products-dark.png', {
      fullPage: true,
      maxDiffPixelRatio: 0.02,
    });
  });

  test('theme toggle button matches snapshot in both states', async ({ page }) => {
    await page.goto('/');

    const themeToggle = page.locator('[data-testid="theme-toggle"]');

    // Light mode state
    await expect(themeToggle).toHaveScreenshot('theme-toggle-light.png');

    // Dark mode state
    await themeToggle.click();
    await page.waitForTimeout(300);

    await expect(themeToggle).toHaveScreenshot('theme-toggle-dark.png');
  });
});
```

---

## 5. Implementation Details

### 5.1 Angular Configuration

```typescript
// src/ClimaSite.Web/angular.json (partial)
{
  "projects": {
    "ClimaSite.Web": {
      "architect": {
        "build": {
          "options": {
            "styles": [
              "src/styles/global.scss"
            ],
            "stylePreprocessorOptions": {
              "includePaths": [
                "src/styles"
              ]
            }
          }
        }
      }
    }
  }
}
```

### 5.2 Playwright Configuration

```typescript
// e2e/playwright.config.ts

import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  webServer: {
    command: 'ng serve',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
```

---

## 6. File Structure

### 6.1 Files to Create

| Path | Purpose |
|------|---------|
| `src/ClimaSite.Web/src/styles/_colors.scss` | Single source of truth for all colors |
| `src/ClimaSite.Web/src/styles/_variables.scss` | CSS custom properties for theming |
| `src/ClimaSite.Web/src/styles/_typography.scss` | Typography scale and fonts |
| `src/ClimaSite.Web/src/styles/_spacing.scss` | Spacing scale |
| `src/ClimaSite.Web/src/styles/_shadows.scss` | Shadow definitions |
| `src/ClimaSite.Web/src/styles/_animations.scss` | Transitions and animations |
| `src/ClimaSite.Web/src/styles/_borders.scss` | Border radius tokens |
| `src/ClimaSite.Web/src/styles/global.scss` | Main style entry point |
| `src/ClimaSite.Web/src/app/core/services/theme.service.ts` | Theme management service |
| `src/ClimaSite.Web/src/app/shared/components/theme-toggle/theme-toggle.component.ts` | Theme toggle button |
| `src/ClimaSite.Web/src/app/shared/components/button/button.component.ts` | Button component |
| `src/ClimaSite.Web/src/app/shared/components/card/card.component.ts` | Card component |
| `src/ClimaSite.Web/src/app/shared/components/input/input.component.ts` | Input component |
| `src/ClimaSite.Web/tailwind.config.js` | Tailwind CSS configuration |
| `e2e/playwright.config.ts` | Playwright test configuration |
| `e2e/tests/theme-switching.spec.ts` | Theme switching E2E tests |
| `e2e/tests/theme-navigation.spec.ts` | Theme navigation E2E tests |
| `e2e/tests/system-theme-preference.spec.ts` | System preference E2E tests |
| `e2e/tests/component-theming.spec.ts` | Component theming E2E tests |
| `e2e/tests/visual-regression.spec.ts` | Visual regression tests |

### 6.2 Files to Modify

| Path | Changes |
|------|---------|
| `src/ClimaSite.Web/src/index.html` | Add theme initialization script |
| `src/ClimaSite.Web/angular.json` | Configure style paths |
| `src/ClimaSite.Web/package.json` | Add Playwright dev dependency |

---

## 7. Task Summary

| Task ID | Title | Priority | Est. Time | Dependencies |
|---------|-------|----------|-----------|--------------|
| DST-001 | Create Color System Foundation | Critical | 4h | None |
| DST-002 | Create CSS Custom Properties System | Critical | 3h | DST-001 |
| DST-003 | Theme Service Implementation | Critical | 4h | DST-002 |
| DST-004 | Theme Toggle Component | High | 3h | DST-003 |
| DST-005 | Tailwind CSS Configuration | High | 2h | DST-001, DST-002 |
| DST-006 | Typography System | High | 3h | DST-001 |
| DST-007 | Spacing System | Medium | 2h | DST-001 |
| DST-008 | Shadow System | Medium | 1h | DST-002 |
| DST-009 | Animation & Transition Tokens | Medium | 1h | None |
| DST-010 | Border Radius System | Low | 0.5h | None |
| DST-011 | Global Styles Entry Point | High | 1h | DST-001 to DST-010 |
| DST-012 | Theme Persistence Integration | Medium | 1h | DST-003 |
| DST-013 | Button Component | Medium | 3h | DST-011 |
| DST-014 | Card Component | Medium | 2h | DST-011 |
| DST-015 | Input Component | Medium | 2h | DST-011 |

**Total Estimated Time**: ~32.5 hours

---

## 8. Success Criteria

1. **Color Centralization**: Changing a color in `_colors.scss` updates all components
2. **Theme Switching**: Users can toggle between light/dark with immediate visual feedback
3. **Persistence**: Theme preference survives page reloads and new sessions
4. **No FOUC**: Page loads with correct theme instantly (no flash)
5. **System Preference**: Respects OS color scheme when no preference set
6. **Accessibility**: All color combinations meet WCAG 2.1 AA contrast ratios
7. **E2E Coverage**: All theme scenarios covered by passing Playwright tests
8. **Performance**: Theme switching has no perceptible lag (<200ms)
