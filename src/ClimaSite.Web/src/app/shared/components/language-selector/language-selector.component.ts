import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService, SupportedLanguage, LanguageInfo } from '../../../core/services/language.service';

@Component({
  selector: 'app-language-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="language-selector"
         (mouseenter)="openDropdown()"
         (mouseleave)="closeDropdown()"
         data-testid="language-selector">
      <button
        type="button"
        class="language-toggle"
        [attr.aria-expanded]="isOpen()"
        aria-haspopup="listbox"
        (click)="toggleDropdown()"
        data-testid="language-toggle"
      >
        <span class="language-flag">{{ getCurrentFlag() }}</span>
        <span class="language-code">{{ languageService.currentLanguage() | uppercase }}</span>
        <svg
          class="dropdown-arrow"
          [class.dropdown-arrow--open]="isOpen()"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
        </svg>
      </button>

      @if (isOpen()) {
        <div class="language-dropdown" role="listbox" data-testid="language-dropdown">
          @for (lang of languageService.languages; track lang.code) {
            <button
              type="button"
              class="language-option"
              [class.language-option--active]="lang.code === languageService.currentLanguage()"
              [attr.aria-selected]="lang.code === languageService.currentLanguage()"
              role="option"
              (click)="selectLanguage(lang.code)"
              [attr.data-testid]="'language-' + lang.code"
            >
              <span class="language-flag">{{ getFlagEmoji(lang.flag) }}</span>
              <span class="language-name">{{ lang.nativeName }}</span>
              @if (lang.code === languageService.currentLanguage()) {
                <svg class="check-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
                </svg>
              }
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .language-selector {
      position: relative;
      display: inline-block;
    }

    .language-toggle {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.5rem 0.75rem;
      background-color: var(--color-bg-hover);
      border: none;
      border-radius: 0.5rem;
      color: var(--color-text-primary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: background-color 0.2s ease;

      &:hover {
        background-color: var(--color-bg-active);
      }

      &:focus-visible {
        outline: 2px solid var(--color-ring);
        outline-offset: 2px;
      }
    }

    .language-flag {
      font-size: 1rem;
    }

    .language-code {
      font-weight: 500;
    }

    .dropdown-arrow {
      width: 1rem;
      height: 1rem;
      transition: transform 0.2s ease;

      &--open {
        transform: rotate(180deg);
      }
    }

    .language-dropdown {
      position: absolute;
      top: calc(100% + 0.25rem);
      right: 0;
      min-width: 10rem;
      background-color: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 0.5rem;
      box-shadow: 0 4px 6px -1px var(--shadow-color), 0 2px 4px -2px var(--shadow-color);
      z-index: 50;
      overflow: hidden;
    }

    .language-option {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      width: 100%;
      padding: 0.625rem 0.75rem;
      background: none;
      border: none;
      color: var(--color-text-primary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: background-color 0.2s ease;

      &:hover {
        background-color: var(--color-bg-hover);
      }

      &--active {
        background-color: var(--color-primary-light);
        color: var(--color-primary);
      }
    }

    .language-name {
      flex: 1;
      text-align: left;
    }

    .check-icon {
      width: 1rem;
      height: 1rem;
      color: var(--color-primary);
    }
  `],
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'closeDropdown()'
  }
})
export class LanguageSelectorComponent {
  protected readonly languageService = inject(LanguageService);
  protected readonly isOpen = signal(false);

  protected toggleDropdown(): void {
    this.isOpen.update(v => !v);
  }

  protected openDropdown(): void {
    this.isOpen.set(true);
  }

  protected closeDropdown(): void {
    this.isOpen.set(false);
  }

  protected selectLanguage(code: SupportedLanguage): void {
    this.languageService.setLanguage(code);
    this.closeDropdown();
  }

  protected getCurrentFlag(): string {
    const info = this.languageService.getCurrentLanguageInfo();
    return info ? this.getFlagEmoji(info.flag) : '';
  }

  protected getFlagEmoji(countryCode: string): string {
    const codePoints = countryCode
      .toUpperCase()
      .split('')
      .map(char => 127397 + char.charCodeAt(0));
    return String.fromCodePoint(...codePoints);
  }

  protected onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.language-selector')) {
      this.closeDropdown();
    }
  }
}
