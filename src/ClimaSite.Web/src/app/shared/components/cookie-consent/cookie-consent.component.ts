import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ConsentService } from '../../../core/services/consent.service';

/**
 * Fixed bottom cookie-consent banner. Shown only until the visitor decides; Accept and
 * Reject both persist the choice (via ConsentService) and dismiss the banner. Rendered
 * once in the root layout so it is global to the app.
 */
@Component({
  selector: 'app-cookie-consent',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  template: `
    @if (!consent.hasDecided()) {
      <div
        class="cookie-consent"
        role="dialog"
        aria-modal="false"
        [attr.aria-label]="'cookieConsent.ariaLabel' | translate"
        data-testid="cookie-consent"
      >
        <div class="cookie-consent__inner">
          <p class="cookie-consent__message" data-testid="cookie-consent-message">
            {{ 'cookieConsent.message' | translate }}
            <a routerLink="/cookies" class="cookie-consent__link" data-testid="cookie-consent-learn-more">
              {{ 'cookieConsent.learnMore' | translate }}
            </a>
          </p>
          <div class="cookie-consent__actions">
            <button
              type="button"
              class="cookie-consent__btn cookie-consent__btn--secondary"
              data-testid="cookie-consent-reject"
              (click)="reject()"
            >
              {{ 'cookieConsent.reject' | translate }}
            </button>
            <button
              type="button"
              class="cookie-consent__btn cookie-consent__btn--primary"
              data-testid="cookie-consent-accept"
              (click)="accept()"
            >
              {{ 'cookieConsent.accept' | translate }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .cookie-consent {
      position: fixed;
      left: 0;
      right: 0;
      bottom: 0;
      /* Banner layer (300): sits below the overlay (400) / modal (500) layers
         so an open drawer or modal is never blocked by the consent banner. */
      z-index: var(--z-banner, 300);
      padding: 1rem;
      background-color: var(--color-bg-secondary);
      border-top: 1px solid var(--color-border-primary);
      box-shadow: 0 -4px 16px rgba(0, 0, 0, 0.08);
    }

    /* On mobile, offset the banner above the 56px fixed bottom-nav so it does
       not cover the navigation. The bottom-nav already accounts for the safe
       area inset, so we add that here too to clear notched devices. */
    @media (max-width: 767px) {
      .cookie-consent {
        bottom: calc(56px + env(safe-area-inset-bottom, 0px));
      }
    }

    .cookie-consent__inner {
      max-width: 80rem;
      margin: 0 auto;
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 1rem;
    }

    .cookie-consent__message {
      margin: 0;
      font-size: 0.9375rem;
      line-height: 1.5;
      color: var(--color-text-secondary);
    }

    .cookie-consent__link {
      color: var(--color-primary);
      text-decoration: underline;
    }

    .cookie-consent__link:hover {
      color: var(--color-primary-hover);
    }

    .cookie-consent__actions {
      display: flex;
      gap: 0.75rem;
      flex-shrink: 0;
    }

    .cookie-consent__btn {
      padding: 0.625rem 1.25rem;
      border-radius: 0.5rem;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s ease, border-color 0.2s ease;
    }

    .cookie-consent__btn--primary {
      background-color: var(--color-primary);
      color: var(--color-text-inverse);
      border: 1px solid var(--color-primary);
    }

    .cookie-consent__btn--primary:hover {
      background-color: var(--color-primary-hover);
      border-color: var(--color-primary-hover);
    }

    .cookie-consent__btn--secondary {
      background-color: transparent;
      color: var(--color-text-primary);
      border: 1px solid var(--color-border-primary);
    }

    .cookie-consent__btn--secondary:hover {
      background-color: var(--color-bg-tertiary);
    }

    .cookie-consent__btn:focus-visible {
      outline: 2px solid var(--color-border-focus);
      outline-offset: 2px;
    }

    @media (min-width: 768px) {
      .cookie-consent__inner {
        flex-direction: row;
        align-items: center;
        justify-content: space-between;
      }

      .cookie-consent__message {
        flex: 1;
      }
    }
  `]
})
export class CookieConsentComponent {
  readonly consent = inject(ConsentService);

  accept(): void {
    this.consent.accept();
  }

  reject(): void {
    this.consent.reject();
  }
}
