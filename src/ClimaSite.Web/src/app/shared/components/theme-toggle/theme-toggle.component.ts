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
