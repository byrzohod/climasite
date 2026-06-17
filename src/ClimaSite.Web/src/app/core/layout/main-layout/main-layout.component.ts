import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { HeaderComponent } from '../header/header.component';
import { FooterComponent } from '../footer/footer.component';
import { ToastContainerComponent } from '../../../shared/components/toast/toast.component';
import { BottomNavComponent } from '../../../shared/components/bottom-nav';
import { CookieConsentComponent } from '../../../shared/components/cookie-consent/cookie-consent.component';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet, TranslateModule, HeaderComponent, FooterComponent, ToastContainerComponent, BottomNavComponent, CookieConsentComponent],
  template: `
    <div class="layout" data-testid="main-layout">
      @defer (on timer(3200ms)) {
        <app-header />
      } @placeholder {
        <header class="header-shell" data-testid="header">
          <div class="header-shell__top">
            <a href="tel:+359898567504" class="header-shell__contact" data-testid="header-shell-phone">0898 567 504</a>
            <a href="mailto:info@cdl.bg" class="header-shell__contact" data-testid="header-shell-email">{{ 'footer.email' | translate }}</a>
          </div>
          <div class="header-shell__main">
            <a routerLink="/" class="header-shell__logo" data-testid="header-logo">
              <span class="header-shell__logo-icon" aria-hidden="true"></span>
              <span>CDL</span>
            </a>
            <a routerLink="/products" class="header-shell__search" data-testid="header-shell-search">
              {{ 'common.search' | translate }}
            </a>
            <div class="header-shell__actions">
              <a routerLink="/wishlist" data-testid="header-shell-wishlist" [attr.aria-label]="'nav.wishlist' | translate">
                <span class="header-shell__icon header-shell__icon--wishlist" aria-hidden="true"></span>
              </a>
              <a routerLink="/cart" data-testid="header-shell-cart" [attr.aria-label]="'nav.cart' | translate">
                <span class="header-shell__icon header-shell__icon--cart" aria-hidden="true"></span>
              </a>
              <a routerLink="/account" data-testid="header-shell-account" [attr.aria-label]="'nav.account' | translate">
                <span class="header-shell__icon header-shell__icon--account" aria-hidden="true"></span>
              </a>
            </div>
          </div>
          <nav class="header-shell__nav">
            <a routerLink="/" data-testid="header-shell-nav-home">{{ 'nav.home' | translate }}</a>
            <a routerLink="/promotions" data-testid="header-shell-nav-promotions">{{ 'nav.promotions' | translate }}</a>
            <a routerLink="/products" data-testid="header-shell-nav-products">{{ 'nav.products' | translate }}</a>
            <a routerLink="/brands" data-testid="header-shell-nav-brands">{{ 'nav.brands' | translate }}</a>
            <a routerLink="/about" data-testid="header-shell-nav-about">{{ 'nav.about' | translate }}</a>
            <a routerLink="/resources" data-testid="header-shell-nav-resources">{{ 'nav.resources' | translate }}</a>
            <a routerLink="/contact" data-testid="header-shell-nav-contact">{{ 'nav.contact' | translate }}</a>
          </nav>
        </header>
      }
      <main class="main-content" data-testid="main-content">
        <ng-content></ng-content>
        <router-outlet />
      </main>
      @defer (on timer(3200ms)) {
        <app-footer />
      } @placeholder {
        <div class="footer-placeholder" data-testid="footer-placeholder" aria-hidden="true"></div>
      }
      <app-bottom-nav />
      <app-toast-container />
      <app-cookie-consent />
    </div>
  `,
  styles: [`
    .layout {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }

    .header-shell {
      display: block;
      min-height: 187px;
      background-color: var(--color-bg-primary);
      border-bottom: 1px solid var(--color-border-primary);
      color: var(--color-text-primary);
    }

    .header-shell__top,
    .header-shell__main,
    .header-shell__nav {
      max-width: 1248px;
      margin: 0 auto;
      padding: 0 2rem;
    }

    .header-shell__top {
      min-height: 58px;
      display: flex;
      align-items: center;
      gap: 1.5rem;
      color: var(--color-text-secondary);
      font-size: 0.875rem;
    }

    .header-shell__contact,
    .header-shell__nav a,
    .header-shell__actions a,
    .header-shell__search,
    .header-shell__logo {
      color: inherit;
      text-decoration: none;
    }

    .header-shell__main {
      min-height: 76px;
      display: grid;
      grid-template-columns: 11rem minmax(16rem, 34rem) 1fr;
      align-items: center;
      gap: 2rem;
    }

    .header-shell__logo {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      color: var(--color-primary);
      font-weight: 800;
      font-size: 1.25rem;
    }

    .header-shell__logo-icon {
      display: inline-grid;
      place-items: center;
      width: 2.5rem;
      height: 2.5rem;
      border-radius: 0.75rem;
      background-color: var(--color-primary);
      color: var(--color-white);
      font-size: 1.25rem;
      line-height: 1;
    }

    .header-shell__logo-icon::before {
      content: '';
      width: 1rem;
      height: 1rem;
      border: 2px solid currentColor;
      border-top: 0;
      transform: rotate(45deg) translate(0.05rem, 0.05rem);
    }

    .header-shell__search {
      display: flex;
      align-items: center;
      min-height: 3rem;
      padding: 0 1.25rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 0.75rem;
      color: var(--color-text-muted);
      background-color: var(--color-bg-secondary);
    }

    .header-shell__actions {
      display: flex;
      justify-content: flex-end;
      align-items: center;
      gap: 1rem;
      color: var(--color-text-secondary);
    }

    .header-shell__actions a {
      min-width: 2rem;
      min-height: 2rem;
      display: inline-grid;
      place-items: center;
      font-size: 1.1rem;
      font-weight: 700;
    }

    .header-shell__icon {
      position: relative;
      display: inline-block;
      width: 1.1rem;
      height: 1.1rem;
      color: currentColor;
    }

    .header-shell__icon--wishlist::before,
    .header-shell__icon--cart::before,
    .header-shell__icon--account::before {
      content: '';
      position: absolute;
      inset: 0;
      border: 2px solid currentColor;
    }

    .header-shell__icon--wishlist::before {
      border-radius: 50%;
      transform: scale(0.85);
    }

    .header-shell__icon--cart::before {
      height: 0.7rem;
      top: 0.25rem;
      border-top: 0;
      border-radius: 0 0 0.2rem 0.2rem;
    }

    .header-shell__icon--account::before {
      border-radius: 50%;
      transform: scale(0.8);
    }

    .header-shell__nav {
      min-height: 53px;
      display: flex;
      align-items: center;
      gap: 2rem;
      color: var(--color-text-secondary);
      font-size: 0.9375rem;
      border-top: 1px solid var(--color-border-primary);
    }

    .footer-placeholder {
      min-height: 1px;
    }

    .main-content {
      flex: 1;
      min-height: 100vh;
      position: relative;
      background-color: var(--color-bg-primary);
      transition: var(--theme-transition);
      /* Ensure content doesn't overflow during animation */
      overflow-x: hidden;
    }

    /* Add padding at the bottom on mobile to account for bottom navigation */
    @media (max-width: 767px) {
      .header-shell {
        min-height: 61px;
      }

      .header-shell__top,
      .header-shell__nav,
      .header-shell__actions {
        display: none;
      }

      .header-shell__main {
        min-height: 61px;
        padding: 0 1rem;
        display: flex;
        justify-content: space-between;
        gap: 1rem;
      }

      .header-shell__logo {
        font-size: 1.25rem;
      }

      .header-shell__logo-icon {
        width: 2.5rem;
        height: 2.5rem;
      }

      .header-shell__search {
        position: relative;
        min-height: 2.5rem;
        width: 2.5rem;
        padding: 0;
        justify-content: center;
        overflow: hidden;
        font-size: 0;
        border: 0;
        background-color: transparent;
      }

      .header-shell__search::before {
        content: '';
        position: absolute;
        top: 0.65rem;
        left: 0.65rem;
        width: 1rem;
        height: 1rem;
        border: 2px solid var(--color-text-primary);
        border-radius: 50%;
      }

      .header-shell__search::after {
        content: '';
        position: absolute;
        top: 1.55rem;
        left: 1.55rem;
        width: 0.5rem;
        height: 2px;
        background-color: var(--color-text-primary);
        transform: rotate(45deg);
        transform-origin: center;
      }

      .main-content {
        /* Bottom nav height (56px) + safe area inset */
        padding-bottom: calc(56px + env(safe-area-inset-bottom, 0px));
      }
    }
  `]
})
export class MainLayoutComponent {
  constructor() {
    inject(ThemeService);
  }
}
