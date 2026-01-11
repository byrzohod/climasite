import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  template: `
    <footer class="footer" data-testid="footer">
      <div class="footer-container">
        <!-- Top Section -->
        <div class="footer-grid">
          <!-- About -->
          <div class="footer-section">
            <h3 class="footer-title">{{ 'footer.about.title' | translate }}</h3>
            <p class="footer-description">{{ 'footer.about.description' | translate }}</p>
          </div>

          <!-- Shop Links -->
          <div class="footer-section">
            <h3 class="footer-title">{{ 'footer.shop.title' | translate }}</h3>
            <ul class="footer-links">
              <li><a routerLink="/products/air-conditioners">{{ 'footer.shop.airConditioners' | translate }}</a></li>
              <li><a routerLink="/products/heating-systems">{{ 'footer.shop.heatingSystems' | translate }}</a></li>
              <li><a routerLink="/products/ventilation">{{ 'footer.shop.ventilation' | translate }}</a></li>
              <li><a routerLink="/products/accessories">{{ 'footer.shop.accessories' | translate }}</a></li>
            </ul>
          </div>

          <!-- Support Links -->
          <div class="footer-section">
            <h3 class="footer-title">{{ 'footer.support.title' | translate }}</h3>
            <ul class="footer-links">
              <li><a routerLink="/contact">{{ 'footer.support.contact' | translate }}</a></li>
              <li><a routerLink="/faq">{{ 'footer.support.faq' | translate }}</a></li>
              <li><a routerLink="/shipping">{{ 'footer.support.shipping' | translate }}</a></li>
              <li><a routerLink="/returns">{{ 'footer.support.returns' | translate }}</a></li>
            </ul>
          </div>

          <!-- Legal Links -->
          <div class="footer-section">
            <h3 class="footer-title">{{ 'footer.legal.title' | translate }}</h3>
            <ul class="footer-links">
              <li><a routerLink="/terms">{{ 'footer.legal.terms' | translate }}</a></li>
              <li><a routerLink="/privacy">{{ 'footer.legal.privacy' | translate }}</a></li>
              <li><a routerLink="/cookies">{{ 'footer.legal.cookies' | translate }}</a></li>
            </ul>
          </div>

          <!-- Newsletter -->
          <div class="footer-section footer-section--newsletter">
            <h3 class="footer-title">{{ 'footer.newsletter.title' | translate }}</h3>
            <p class="footer-description">{{ 'footer.newsletter.description' | translate }}</p>
            <form class="newsletter-form" (submit)="$event.preventDefault()">
              <input
                type="email"
                [placeholder]="'footer.newsletter.placeholder' | translate"
                class="newsletter-input"
                data-testid="newsletter-input"
              />
              <button type="submit" class="newsletter-btn" data-testid="newsletter-submit">
                {{ 'footer.newsletter.subscribe' | translate }}
              </button>
            </form>
          </div>
        </div>

        <!-- Bottom Section -->
        <div class="footer-bottom">
          <p class="copyright">
            {{ 'footer.copyright' | translate:{ year: currentYear } }}
          </p>
          <div class="social-links">
            <a href="#" aria-label="Facebook" class="social-link">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
              </svg>
            </a>
            <a href="#" aria-label="Instagram" class="social-link">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zM12 0C8.741 0 8.333.014 7.053.072 2.695.272.273 2.69.073 7.052.014 8.333 0 8.741 0 12c0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98C8.333 23.986 8.741 24 12 24c3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98C15.668.014 15.259 0 12 0zm0 5.838a6.162 6.162 0 100 12.324 6.162 6.162 0 000-12.324zM12 16a4 4 0 110-8 4 4 0 010 8zm6.406-11.845a1.44 1.44 0 100 2.881 1.44 1.44 0 000-2.881z"/>
              </svg>
            </a>
            <a href="#" aria-label="LinkedIn" class="social-link">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.064 2.064 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
              </svg>
            </a>
          </div>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      background-color: var(--color-bg-secondary);
      border-top: 1px solid var(--color-border-primary);
      transition: var(--theme-transition);
    }

    .footer-container {
      max-width: 80rem;
      margin: 0 auto;
      padding: 3rem 1rem 1.5rem;
    }

    .footer-grid {
      display: grid;
      gap: 2rem;
      grid-template-columns: 1fr;

      @media (min-width: 640px) {
        grid-template-columns: repeat(2, 1fr);
      }

      @media (min-width: 1024px) {
        grid-template-columns: repeat(5, 1fr);
      }
    }

    .footer-section {
      &--newsletter {
        @media (min-width: 640px) {
          grid-column: span 2;
        }

        @media (min-width: 1024px) {
          grid-column: span 1;
        }
      }
    }

    .footer-title {
      color: var(--color-text-primary);
      font-size: 0.875rem;
      font-weight: 600;
      margin: 0 0 1rem 0;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .footer-description {
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      line-height: 1.5;
      margin: 0;
    }

    .footer-links {
      list-style: none;
      padding: 0;
      margin: 0;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;

      a {
        color: var(--color-text-secondary);
        text-decoration: none;
        font-size: 0.875rem;
        transition: color 0.2s ease;

        &:hover {
          color: var(--color-primary);
        }
      }
    }

    .newsletter-form {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-top: 0.75rem;

      @media (min-width: 640px) {
        flex-direction: row;
      }
    }

    .newsletter-input {
      flex: 1;
      padding: 0.625rem 0.75rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 0.375rem;
      background-color: var(--color-bg-input);
      color: var(--color-text-primary);
      font-size: 0.875rem;

      &::placeholder {
        color: var(--color-text-placeholder);
      }

      &:focus {
        outline: none;
        border-color: var(--color-border-focus);
        box-shadow: 0 0 0 3px var(--color-primary-light);
      }
    }

    .newsletter-btn {
      padding: 0.625rem 1rem;
      background-color: var(--color-primary);
      color: white;
      border: none;
      border-radius: 0.375rem;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.2s ease;
      white-space: nowrap;

      &:hover {
        background-color: var(--color-primary-hover);
      }
    }

    .footer-bottom {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      margin-top: 2.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border-primary);

      @media (min-width: 640px) {
        flex-direction: row;
        justify-content: space-between;
      }
    }

    .copyright {
      color: var(--color-text-tertiary);
      font-size: 0.875rem;
      margin: 0;
    }

    .social-links {
      display: flex;
      gap: 0.75rem;
    }

    .social-link {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2rem;
      height: 2rem;
      color: var(--color-text-tertiary);
      transition: color 0.2s ease;

      &:hover {
        color: var(--color-primary);
      }

      svg {
        width: 1.25rem;
        height: 1.25rem;
      }
    }
  `]
})
export class FooterComponent {
  readonly currentYear = new Date().getFullYear();
}
