import { Component, inject, signal, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ThemeToggleComponent } from '../../../shared/components/theme-toggle/theme-toggle.component';
import { LanguageSelectorComponent } from '../../../shared/components/language-selector/language-selector.component';
import { MegaMenuComponent } from '../../../shared/components/mega-menu/mega-menu.component';
import { CartService } from '../../services/cart.service';
import { WishlistService } from '../../services/wishlist.service';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    TranslateModule,
    ThemeToggleComponent,
    LanguageSelectorComponent,
    MegaMenuComponent
  ],
  template: `
    <header class="header" [class.header--sticky]="isSticky()" data-testid="header">
      <!-- Top Bar -->
      <div class="top-bar">
        <div class="top-bar-container">
          <div class="contact-info">
            <a href="tel:+359898567504" class="contact-link">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon-sm">
                <path fill-rule="evenodd" d="M2 3.5A1.5 1.5 0 013.5 2h1.148a1.5 1.5 0 011.465 1.175l.716 3.223a1.5 1.5 0 01-1.052 1.767l-.933.267c-.41.117-.643.555-.48.95a11.542 11.542 0 006.254 6.254c.395.163.833-.07.95-.48l.267-.933a1.5 1.5 0 011.767-1.052l3.223.716A1.5 1.5 0 0118 15.352V16.5a1.5 1.5 0 01-1.5 1.5H15c-1.149 0-2.263-.15-3.326-.43A13.022 13.022 0 012.43 8.326 13.019 13.019 0 012 5V3.5z" clip-rule="evenodd"/>
              </svg>
              0898 567 504
            </a>
            <a href="mailto:info@climasite.com" class="contact-link">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon-sm">
                <path d="M3 4a2 2 0 00-2 2v1.161l8.441 4.221a1.25 1.25 0 001.118 0L19 7.162V6a2 2 0 00-2-2H3z"/>
                <path d="M19 8.839l-7.77 3.885a2.75 2.75 0 01-2.46 0L1 8.839V14a2 2 0 002 2h14a2 2 0 002-2V8.839z"/>
              </svg>
              info&#64;climasite.com
            </a>
          </div>
          <div class="top-bar-actions">
            <app-language-selector />
            <app-theme-toggle />
          </div>
        </div>
      </div>

      <!-- Main Header -->
      <div class="main-header">
        <div class="header-container">
          <!-- Logo -->
          <a routerLink="/" class="header-logo" data-testid="header-logo">
            <span class="logo-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.47 3.84a.75.75 0 011.06 0l8.69 8.69a.75.75 0 101.06-1.06l-8.689-8.69a2.25 2.25 0 00-3.182 0l-8.69 8.69a.75.75 0 001.061 1.06l8.69-8.69z"/>
                <path d="M12 5.432l8.159 8.159c.03.03.06.058.091.086v6.198c0 1.035-.84 1.875-1.875 1.875H15a.75.75 0 01-.75-.75v-4.5a.75.75 0 00-.75-.75h-3a.75.75 0 00-.75.75V21a.75.75 0 01-.75.75H5.625a1.875 1.875 0 01-1.875-1.875v-6.198a2.29 2.29 0 00.091-.086L12 5.43z"/>
              </svg>
            </span>
            <span class="logo-text">ClimaSite</span>
          </a>

          <!-- Search Box -->
          <form class="search-box" (ngSubmit)="performSearch()">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="search-icon">
              <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd"/>
            </svg>
            <input
              type="text"
              [(ngModel)]="searchQuery"
              name="searchQuery"
              [placeholder]="'common.search' | translate"
              class="search-input"
              data-testid="search-input"
            />
            <button type="submit" class="search-btn" aria-label="Search" data-testid="search-button">
              {{ 'common.search' | translate }}
            </button>
          </form>

          <!-- Header Actions -->
          <div class="header-actions" data-testid="header-actions">
            <!-- NAV-002: Wishlist with badge count -->
            <a routerLink="/wishlist" class="action-btn action-btn--wishlist" [attr.aria-label]="'nav.wishlist' | translate" data-testid="wishlist-button">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 01-.383-.218 25.18 25.18 0 01-4.244-3.17C4.688 15.36 2.25 12.174 2.25 8.25 2.25 5.322 4.714 3 7.688 3A5.5 5.5 0 0112 5.052 5.5 5.5 0 0116.313 3c2.973 0 5.437 2.322 5.437 5.25 0 3.925-2.438 7.111-4.739 9.256a25.175 25.175 0 01-4.244 3.17 15.247 15.247 0 01-.383.219l-.022.012-.007.004-.003.001a.752.752 0 01-.704 0l-.003-.001z"/>
              </svg>
              @if (wishlistService.itemCount() > 0) {
                <span class="wishlist-badge" data-testid="wishlist-count">{{ wishlistService.itemCount() }}</span>
              }
            </a>

            <!-- Cart -->
            <a routerLink="/cart" class="action-btn action-btn--cart" [attr.aria-label]="'nav.cart' | translate" data-testid="cart-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M2.25 2.25a.75.75 0 000 1.5h1.386c.17 0 .318.114.362.278l2.558 9.592a3.752 3.752 0 00-2.806 3.63c0 .414.336.75.75.75h15.75a.75.75 0 000-1.5H5.378A2.25 2.25 0 017.5 15h11.218a.75.75 0 00.674-.421 60.358 60.358 0 002.96-7.228.75.75 0 00-.525-.965A60.864 60.864 0 005.68 4.509l-.232-.867A1.875 1.875 0 003.636 2.25H2.25zM3.75 20.25a1.5 1.5 0 113 0 1.5 1.5 0 01-3 0zM16.5 20.25a1.5 1.5 0 113 0 1.5 1.5 0 01-3 0z"/>
              </svg>
              @if (cartService.itemCount() > 0) {
                <span class="cart-badge" data-testid="cart-count">{{ cartService.itemCount() }}</span>
              }
            </a>

            <!-- User Menu -->
            @if (authService.isAuthenticated()) {
              <!-- Authenticated User Dropdown -->
              <div class="user-menu" data-testid="user-menu">
                <button
                  type="button"
                  class="user-menu-trigger"
                  (click)="toggleUserMenu()"
                  [attr.aria-expanded]="userMenuOpen()"
                  data-testid="user-menu-trigger"
                >
                  <div class="user-avatar">
                    {{ getUserInitials() }}
                  </div>
                  <span class="user-name">{{ authService.user()?.firstName }}</span>
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="chevron" [class.chevron--open]="userMenuOpen()">
                    <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/>
                  </svg>
                </button>

                @if (userMenuOpen()) {
                  <div class="user-dropdown" data-testid="user-dropdown">
                    <div class="dropdown-header">
                      <div class="dropdown-user-info">
                        <span class="dropdown-user-name">{{ authService.user()?.firstName }} {{ authService.user()?.lastName }}</span>
                        <span class="dropdown-user-email">{{ authService.user()?.email }}</span>
                      </div>
                    </div>
                    <nav class="dropdown-nav">
                      <a routerLink="/account" class="dropdown-link" (click)="closeUserMenu()" data-testid="account-link">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-5.5-2.5a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0zM10 12a5.99 5.99 0 00-4.793 2.39A6.483 6.483 0 0010 16.5a6.483 6.483 0 004.793-2.11A5.99 5.99 0 0010 12z" clip-rule="evenodd"/>
                        </svg>
                        {{ 'nav.account' | translate }}
                      </a>
                      <a routerLink="/account/orders" class="dropdown-link" (click)="closeUserMenu()" data-testid="orders-link">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M6 5v1H4.667a1.75 1.75 0 00-1.743 1.598l-.826 9.5A1.75 1.75 0 003.84 19H16.16a1.75 1.75 0 001.743-1.902l-.826-9.5A1.75 1.75 0 0015.333 6H14V5a4 4 0 00-8 0zm4-2.5A2.5 2.5 0 007.5 5v1h5V5A2.5 2.5 0 0010 2.5zM7.5 10a2.5 2.5 0 005 0V8.75a.75.75 0 011.5 0V10a4 4 0 01-8 0V8.75a.75.75 0 011.5 0V10z" clip-rule="evenodd"/>
                        </svg>
                        {{ 'nav.orders' | translate }}
                      </a>
                      <a routerLink="/account/wishlist" class="dropdown-link" (click)="closeUserMenu()" data-testid="wishlist-menu-link">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                          <path d="M9.653 16.915l-.005-.003-.019-.01a20.759 20.759 0 01-1.162-.682 22.045 22.045 0 01-2.582-1.9C4.045 12.733 2 10.352 2 7.5a4.5 4.5 0 018-2.828A4.5 4.5 0 0118 7.5c0 2.852-2.044 5.233-3.885 6.82a22.049 22.049 0 01-3.744 2.582l-.019.01-.005.003h-.002a.739.739 0 01-.69.001l-.002-.001z"/>
                        </svg>
                        {{ 'nav.wishlist' | translate }}
                      </a>
                      @if (authService.isAdmin()) {
                        <a routerLink="/admin" class="dropdown-link dropdown-link--admin" (click)="closeUserMenu()" data-testid="admin-link">
                          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                            <path fill-rule="evenodd" d="M4.5 2A1.5 1.5 0 003 3.5v13A1.5 1.5 0 004.5 18h11a1.5 1.5 0 001.5-1.5V7.621a1.5 1.5 0 00-.44-1.06l-4.12-4.122A1.5 1.5 0 0011.378 2H4.5zm2.25 8.5a.75.75 0 000 1.5h6.5a.75.75 0 000-1.5h-6.5zm0 3a.75.75 0 000 1.5h6.5a.75.75 0 000-1.5h-6.5z" clip-rule="evenodd"/>
                          </svg>
                          {{ 'nav.admin' | translate }}
                        </a>
                      }
                    </nav>
                    <div class="dropdown-footer">
                      <button type="button" class="logout-btn" (click)="logout()" data-testid="logout-button">
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd"/>
                          <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-.943a.75.75 0 10-1.004-1.114l-2.5 2.25a.75.75 0 000 1.114l2.5 2.25a.75.75 0 101.004-1.114l-1.048-.943h9.546A.75.75 0 0019 10z" clip-rule="evenodd"/>
                        </svg>
                        {{ 'auth.logout' | translate }}
                      </button>
                    </div>
                  </div>
                }
              </div>
            } @else {
              <!-- Login/Register Links -->
              <a routerLink="/login" class="action-btn" [attr.aria-label]="'nav.login' | translate" data-testid="login-button">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                  <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd"/>
                </svg>
              </a>
            }
          </div>

          <!-- Mobile Menu Toggle -->
          <button type="button" class="mobile-menu-btn" aria-label="Toggle menu" (click)="toggleMobileMenu()" data-testid="mobile-menu-toggle">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M3 6.75A.75.75 0 013.75 6h16.5a.75.75 0 010 1.5H3.75A.75.75 0 013 6.75zM3 12a.75.75 0 01.75-.75h16.5a.75.75 0 010 1.5H3.75A.75.75 0 013 12zm0 5.25a.75.75 0 01.75-.75h16.5a.75.75 0 010 1.5H3.75a.75.75 0 01-.75-.75z" clip-rule="evenodd"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- Navigation Bar -->
      <nav class="nav-bar" data-testid="header-nav">
        <div class="nav-container">
          <a routerLink="/" routerLinkActive="nav-link--active" [routerLinkActiveOptions]="{exact: true}" class="nav-link" data-testid="nav-home">
            {{ 'nav.home' | translate }}
          </a>
          <a routerLink="/promotions" routerLinkActive="nav-link--active" class="nav-link nav-link--promo" data-testid="nav-promotions">
            {{ 'nav.promotions' | translate }}
            <span class="promo-badge">🔥</span>
          </a>
          <app-mega-menu />
          <a routerLink="/brands" routerLinkActive="nav-link--active" class="nav-link" data-testid="nav-brands">
            {{ 'nav.brands' | translate }}
          </a>
          <a routerLink="/about" routerLinkActive="nav-link--active" class="nav-link" data-testid="nav-about">
            {{ 'nav.about' | translate }}
          </a>
          <a routerLink="/resources" routerLinkActive="nav-link--active" class="nav-link" data-testid="nav-resources">
            {{ 'nav.resources' | translate }}
          </a>
          <a routerLink="/contact" routerLinkActive="nav-link--active" class="nav-link" data-testid="nav-contact">
            {{ 'nav.contact' | translate }}
          </a>
        </div>
      </nav>
    </header>

    <!-- Mobile Menu Overlay -->
    @if (mobileMenuOpen()) {
      <div class="mobile-menu-overlay" (click)="closeMobileMenu()" data-testid="mobile-menu-overlay"></div>
      <div class="mobile-menu" data-testid="mobile-menu">
        <div class="mobile-menu-header">
          <span class="logo-text">ClimaSite</span>
          <button type="button" class="mobile-menu-close" (click)="closeMobileMenu()">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd"/>
            </svg>
          </button>
        </div>
        <nav class="mobile-nav">
          <a routerLink="/" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.home' | translate }}</a>
          <a routerLink="/promotions" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.promotions' | translate }}</a>
          <a routerLink="/products" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.products' | translate }}</a>
          <a routerLink="/brands" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.brands' | translate }}</a>
          <a routerLink="/about" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.about' | translate }}</a>
          <a routerLink="/resources" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.resources' | translate }}</a>
          <a routerLink="/contact" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.contact' | translate }}</a>
        </nav>
        <div class="mobile-menu-footer">
          @if (authService.isAuthenticated()) {
            <a routerLink="/account" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.account' | translate }}</a>
            <a routerLink="/account/orders" class="mobile-nav-link" (click)="closeMobileMenu()">{{ 'nav.orders' | translate }}</a>
            <button type="button" class="mobile-logout-btn" (click)="logout()">{{ 'auth.logout' | translate }}</button>
          } @else {
            <a routerLink="/login" class="mobile-login-btn" (click)="closeMobileMenu()">{{ 'auth.login.title' | translate }}</a>
            <a routerLink="/register" class="mobile-register-btn" (click)="closeMobileMenu()">{{ 'auth.register.title' | translate }}</a>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .header {
      position: sticky;
      top: 0;
      z-index: 100;
      background-color: var(--color-bg-primary);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      transition: var(--theme-transition);
    }

    .header--sticky {
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    }

    /* Top Bar */
    .top-bar {
      background-color: var(--color-bg-tertiary);
      border-bottom: 1px solid var(--color-border-primary);
      font-size: 0.8125rem;
      display: none;

      @media (min-width: 768px) {
        display: block;
      }
    }

    .top-bar-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      max-width: 80rem;
      margin: 0 auto;
      padding: 0.5rem 1rem;
    }

    .contact-info {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .contact-link {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      color: var(--color-text-secondary);
      text-decoration: none;
      transition: color 0.2s;

      &:hover {
        color: var(--color-primary);
      }
    }

    .icon-sm {
      width: 1rem;
      height: 1rem;
    }

    .top-bar-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    /* Main Header */
    .main-header {
      border-bottom: 1px solid var(--color-border-primary);
    }

    .header-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      max-width: 80rem;
      margin: 0 auto;
      padding: 0.75rem 1rem;
    }

    .header-logo {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      text-decoration: none;
      color: var(--color-text-primary);
      font-weight: 700;
      font-size: 1.25rem;
      flex-shrink: 0;

      &:hover {
        color: var(--color-primary);
      }
    }

    .logo-icon {
      color: var(--color-primary);

      svg {
        width: 2rem;
        height: 2rem;
      }
    }

    /* Search Box */
    .search-box {
      display: none;
      flex: 1;
      max-width: 500px;
      align-items: center;
      background-color: var(--color-bg-secondary);
      border: 1px solid var(--color-border-primary);
      border-radius: 0.5rem;
      padding: 0.25rem 0.25rem 0.25rem 0.75rem;
      transition: border-color 0.2s, box-shadow 0.2s;

      &:focus-within {
        border-color: var(--color-primary);
        box-shadow: 0 0 0 3px var(--color-primary-light);
      }

      @media (min-width: 768px) {
        display: flex;
      }
    }

    .search-icon {
      width: 1.25rem;
      height: 1.25rem;
      color: var(--color-text-tertiary);
      flex-shrink: 0;
    }

    .search-input {
      flex: 1;
      padding: 0.5rem 0.75rem;
      background: transparent;
      border: none;
      color: var(--color-text-primary);
      font-size: 0.875rem;

      &::placeholder {
        color: var(--color-text-placeholder);
      }

      &:focus {
        outline: none;
      }
    }

    /* THEME-001 FIX: Use CSS variable instead of hardcoded white */
    .search-btn {
      padding: 0.5rem 1rem;
      background-color: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: 0.375rem;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover {
        background-color: var(--color-primary-hover);
      }
    }

    /* Header Actions */
    .header-actions {
      display: none;
      align-items: center;
      gap: 0.25rem;

      @media (min-width: 768px) {
        display: flex;
      }
    }

    .action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      padding: 0.5rem;
      background: none;
      border: none;
      border-radius: 0.5rem;
      color: var(--color-text-secondary);
      cursor: pointer;
      text-decoration: none;
      transition: color 0.2s, background-color 0.2s;

      &:hover {
        color: var(--color-text-primary);
        background-color: var(--color-bg-hover);
      }

      svg {
        width: 1.25rem;
        height: 1.25rem;
      }

      &--cart,
      &--wishlist {
        position: relative;
      }
    }

    /* NAV-002: Wishlist badge styles */
    .wishlist-badge {
      position: absolute;
      top: 0.125rem;
      right: 0.125rem;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 1.125rem;
      height: 1.125rem;
      padding: 0 0.25rem;
      background-color: var(--color-error);
      color: white;
      font-size: 0.625rem;
      font-weight: 600;
      border-radius: 9999px;
    }

    .cart-badge {
      position: absolute;
      top: 0.125rem;
      right: 0.125rem;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 1.125rem;
      height: 1.125rem;
      padding: 0 0.25rem;
      background-color: var(--color-primary);
      color: white;
      font-size: 0.625rem;
      font-weight: 600;
      border-radius: 9999px;
    }

    /* User Menu */
    .user-menu {
      position: relative;
    }

    .user-menu-trigger {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.375rem 0.75rem 0.375rem 0.375rem;
      background: none;
      border: 1px solid transparent;
      border-radius: 9999px;
      cursor: pointer;
      transition: background-color 0.2s, border-color 0.2s;

      &:hover {
        background-color: var(--color-bg-hover);
        border-color: var(--color-border-primary);
      }
    }

    .user-avatar {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2rem;
      height: 2rem;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-hover) 100%);
      color: white;
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: 9999px;
    }

    .user-name {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-primary);
    }

    .chevron {
      width: 1rem;
      height: 1rem;
      color: var(--color-text-tertiary);
      transition: transform 0.2s;

      &--open {
        transform: rotate(180deg);
      }
    }

    .user-dropdown {
      position: absolute;
      top: calc(100% + 0.5rem);
      right: 0;
      min-width: 260px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border-primary);
      border-radius: 0.75rem;
      box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      overflow: hidden;
    }

    .dropdown-header {
      padding: 1rem;
      background: var(--color-bg-secondary);
      border-bottom: 1px solid var(--color-border-primary);
    }

    .dropdown-user-info {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .dropdown-user-name {
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .dropdown-user-email {
      font-size: 0.8125rem;
      color: var(--color-text-secondary);
    }

    .dropdown-nav {
      padding: 0.5rem;
    }

    .dropdown-link {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0.75rem;
      color: var(--color-text-primary);
      text-decoration: none;
      border-radius: 0.5rem;
      transition: background-color 0.15s;

      &:hover {
        background-color: var(--color-bg-hover);
      }

      svg {
        width: 1.25rem;
        height: 1.25rem;
        color: var(--color-text-tertiary);
      }

      &--admin {
        color: var(--color-primary);

        svg {
          color: var(--color-primary);
        }
      }
    }

    .dropdown-footer {
      padding: 0.5rem;
      border-top: 1px solid var(--color-border-primary);
    }

    .logout-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      width: 100%;
      padding: 0.625rem 0.75rem;
      background: transparent;
      border: 1px solid var(--color-border-primary);
      border-radius: 0.5rem;
      color: var(--color-text-primary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.15s;

      &:hover {
        background-color: var(--color-error-light);
        border-color: var(--color-error);
        color: var(--color-error);
      }

      svg {
        width: 1.125rem;
        height: 1.125rem;
      }
    }

    /* Navigation Bar */
    .nav-bar {
      background-color: var(--color-bg-primary);
      border-bottom: 1px solid var(--color-border-primary);
      display: none;

      @media (min-width: 768px) {
        display: block;
      }
    }

    .nav-container {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      max-width: 80rem;
      margin: 0 auto;
      padding: 0 1rem;
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.75rem 1rem;
      color: var(--color-text-secondary);
      text-decoration: none;
      font-weight: 500;
      font-size: 0.875rem;
      border-bottom: 2px solid transparent;
      transition: color 0.2s, border-color 0.2s;

      &:hover {
        color: var(--color-text-primary);
      }

      &--active {
        color: var(--color-primary);
        border-bottom-color: var(--color-primary);
      }

      &--promo {
        color: var(--color-warm);

        &:hover {
          color: var(--color-warm-hover);
        }
      }
    }

    .promo-badge {
      font-size: 0.75rem;
    }

    .nav-chevron {
      width: 1rem;
      height: 1rem;
    }

    /* Mobile Menu Button */
    .mobile-menu-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      padding: 0.5rem;
      background: none;
      border: none;
      border-radius: 0.5rem;
      color: var(--color-text-secondary);
      cursor: pointer;

      &:hover {
        background-color: var(--color-bg-hover);
      }

      svg {
        width: 1.5rem;
        height: 1.5rem;
      }

      @media (min-width: 768px) {
        display: none;
      }
    }

    /* Mobile Menu */
    .mobile-menu-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      z-index: 200;
    }

    .mobile-menu {
      position: fixed;
      top: 0;
      right: 0;
      width: 300px;
      max-width: 100%;
      height: 100vh;
      background: var(--color-bg-primary);
      z-index: 201;
      display: flex;
      flex-direction: column;
      box-shadow: -4px 0 20px rgba(0, 0, 0, 0.15);
    }

    .mobile-menu-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem;
      border-bottom: 1px solid var(--color-border-primary);
    }

    .mobile-menu-close {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      background: none;
      border: none;
      border-radius: 0.5rem;
      color: var(--color-text-secondary);
      cursor: pointer;

      svg {
        width: 1.5rem;
        height: 1.5rem;
      }
    }

    .mobile-nav {
      flex: 1;
      padding: 1rem;
      overflow-y: auto;
    }

    .mobile-nav-link {
      display: block;
      padding: 0.75rem 0;
      color: var(--color-text-primary);
      text-decoration: none;
      font-weight: 500;
      border-bottom: 1px solid var(--color-border-primary);
    }

    .mobile-menu-footer {
      padding: 1rem;
      border-top: 1px solid var(--color-border-primary);
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .mobile-login-btn,
    .mobile-register-btn {
      display: block;
      text-align: center;
      padding: 0.75rem 1rem;
      border-radius: 0.5rem;
      text-decoration: none;
      font-weight: 500;
    }

    .mobile-login-btn {
      background: var(--color-primary);
      color: white;
    }

    .mobile-register-btn {
      background: transparent;
      border: 1px solid var(--color-border-primary);
      color: var(--color-text-primary);
    }

    .mobile-logout-btn {
      width: 100%;
      padding: 0.75rem 1rem;
      background: transparent;
      border: 1px solid var(--color-border-primary);
      border-radius: 0.5rem;
      color: var(--color-text-primary);
      font-weight: 500;
      cursor: pointer;
    }
  `]
})
export class HeaderComponent {
  private readonly elementRef = inject(ElementRef);
  private readonly router = inject(Router);
  readonly cartService = inject(CartService);
  readonly wishlistService = inject(WishlistService);
  readonly authService = inject(AuthService);

  readonly isSticky = signal(false);
  readonly userMenuOpen = signal(false);
  readonly mobileMenuOpen = signal(false);
  searchQuery = '';

  private lastScrollY = 0;

  @HostListener('window:scroll')
  onScroll(): void {
    const currentScrollY = window.scrollY;
    this.isSticky.set(currentScrollY > 50);
    this.lastScrollY = currentScrollY;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const userMenu = this.elementRef.nativeElement.querySelector('.user-menu');
    if (userMenu && !userMenu.contains(event.target as Node)) {
      this.userMenuOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.userMenuOpen.set(false);
    this.mobileMenuOpen.set(false);
  }

  toggleUserMenu(): void {
    this.userMenuOpen.update(open => !open);
  }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update(open => !open);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  getUserInitials(): string {
    const user = this.authService.user();
    if (!user) return '?';
    const first = user.firstName?.charAt(0) || '';
    const last = user.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || user.email.charAt(0).toUpperCase();
  }

  logout(): void {
    this.closeUserMenu();
    this.closeMobileMenu();
    this.authService.logout().subscribe();
  }

  performSearch(): void {
    const query = this.searchQuery.trim();
    if (query) {
      this.router.navigate(['/products'], { queryParams: { search: query } });
    }
  }
}
