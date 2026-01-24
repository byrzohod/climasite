import { 
  Component, 
  inject, 
  signal, 
  computed, 
  effect, 
  OnDestroy, 
  HostBinding,
  PLATFORM_ID 
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

import { IconComponent } from '../icon';
import { CartService } from '../../../core/services/cart.service';
import { AnimationService } from '../../../core/services/animation.service';

/**
 * Navigation item configuration
 */
export interface BottomNavItem {
  icon: string;
  label: string;
  route?: string;
  action?: 'search' | 'menu';
  showBadge?: boolean;
  testId: string;
}

/**
 * BottomNavComponent - Mobile bottom navigation bar
 * 
 * A fixed bottom navigation bar for mobile devices with 5 items:
 * - Home, Categories, Search, Cart (with badge), Account
 * 
 * Features:
 * - Safe area insets for notched devices
 * - Hide on scroll down, show on scroll up
 * - Active state highlighting
 * - Badge for cart item count
 * - Reduced motion support
 * - Mobile-only visibility (hidden on tablet+)
 */
@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, TranslateModule, IconComponent],
  template: `
    <nav 
      class="bottom-nav"
      [class.hidden]="isHidden()"
      role="navigation"
      aria-label="Mobile navigation"
      data-testid="bottom-nav"
    >
      @for (item of navItems; track item.testId) {
        @if (item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: item.route === '/' }"
            class="nav-item"
            [attr.data-testid]="item.testId"
            [attr.aria-label]="item.label | translate"
          >
            <span class="nav-icon">
              <app-icon [name]="item.icon" size="md" />
              @if (item.showBadge && cartItemCount() > 0) {
                <span 
                  class="badge"
                  [class.pulse]="badgePulse()"
                  aria-live="polite"
                  [attr.aria-label]="cartItemCount() + ' items in cart'"
                >
                  {{ cartItemCount() > 99 ? '99+' : cartItemCount() }}
                </span>
              }
            </span>
            <span class="nav-label">{{ item.label | translate }}</span>
          </a>
        } @else {
          <button
            type="button"
            class="nav-item"
            [attr.data-testid]="item.testId"
            [attr.aria-label]="item.label | translate"
            (click)="handleAction(item.action)"
          >
            <span class="nav-icon">
              <app-icon [name]="item.icon" size="md" />
            </span>
            <span class="nav-label">{{ item.label | translate }}</span>
          </button>
        }
      }
    </nav>
  `,
  styles: [`
    /* Host - Only show on mobile devices */
    :host {
      display: none;
    }

    @media (max-width: 767px) {
      :host {
        display: block;
        position: fixed;
        bottom: 0;
        left: 0;
        right: 0;
        z-index: 1000;
      }
    }

    .bottom-nav {
      display: flex;
      justify-content: space-around;
      align-items: stretch;
      height: 56px;
      background: var(--glass-bg-heavy);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border-top: 1px solid var(--color-border-primary);
      padding-bottom: env(safe-area-inset-bottom, 0);
      box-shadow: 0 -2px 10px var(--shadow-color);
      transform: translateY(0);
      transition: transform 0.3s ease-out;
    }

    .bottom-nav.hidden {
      transform: translateY(100%);
    }

    @media (prefers-reduced-motion: reduce) {
      .bottom-nav {
        transition: none;
      }
      .bottom-nav.hidden {
        visibility: hidden;
      }
    }

    .nav-item {
      all: unset;
      box-sizing: border-box;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 2px;
      min-width: 44px;
      min-height: 44px;
      padding: 6px 12px;
      flex: 1;
      cursor: pointer;
      -webkit-tap-highlight-color: transparent;
      touch-action: manipulation;
      color: var(--color-text-secondary);
      text-decoration: none;
      transition: color 0.15s ease, transform 0.1s ease;
    }

    @media (hover: hover) {
      .nav-item:hover {
        color: var(--color-primary);
      }
    }

    .nav-item:focus-visible {
      outline: 2px solid var(--color-border-focus);
      outline-offset: -2px;
      border-radius: 8px;
    }

    .nav-item:active {
      transform: scale(0.95);
    }

    .nav-item.active {
      color: var(--color-primary);
    }

    .nav-item.active .nav-label {
      font-weight: 600;
    }

    @media (prefers-reduced-motion: reduce) {
      .nav-item {
        transition: none;
      }
      .nav-item:active {
        transform: none;
      }
    }

    .nav-icon {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      height: 24px;
    }

    .nav-label {
      font-size: 10px;
      font-weight: 500;
      line-height: 1.2;
      text-align: center;
      white-space: nowrap;
      user-select: none;
    }

    .badge {
      position: absolute;
      top: -6px;
      right: -8px;
      min-width: 16px;
      height: 16px;
      padding: 0 4px;
      border-radius: 9999px;
      background-color: var(--color-error);
      color: var(--color-text-inverse);
      font-size: 10px;
      font-weight: 700;
      line-height: 16px;
      text-align: center;
      z-index: 1;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
    }

    .badge.pulse {
      animation: badge-pulse 0.6s ease-out;
    }

    @keyframes badge-pulse {
      0% { transform: scale(1); }
      50% { transform: scale(1.3); }
      100% { transform: scale(1); }
    }

    @media (prefers-reduced-motion: reduce) {
      .badge.pulse {
        animation: none;
      }
    }

    /* Small mobile screens (< 375px) */
    @media (max-width: 374px) {
      .bottom-nav {
        height: 52px;
      }
      .nav-item {
        padding: 4px 8px;
      }
      .nav-label {
        font-size: 9px;
      }
      .badge {
        min-width: 14px;
        height: 14px;
        font-size: 9px;
        line-height: 14px;
        top: -4px;
        right: -6px;
      }
    }

    /* Landscape orientation */
    @media (max-width: 767px) and (orientation: landscape) {
      .bottom-nav {
        height: 48px;
      }
      .nav-item {
        flex-direction: row;
        gap: 6px;
      }
      .nav-icon {
        height: 20px;
      }
      .nav-label {
        font-size: 11px;
      }
    }
  `]
})
export class BottomNavComponent implements OnDestroy {
  private readonly router = inject(Router);
  private readonly cartService = inject(CartService);
  private readonly animationService = inject(AnimationService);
  private readonly platformId = inject(PLATFORM_ID);
  
  private routerSubscription: Subscription | null = null;
  private lastScrollY = 0;
  private scrollThreshold = 10; // Minimum scroll distance to trigger hide/show
  
  // Cart item count from service
  readonly cartItemCount = this.cartService.itemCount;
  
  // Badge pulse animation (triggered when cart count changes)
  readonly badgePulse = signal(false);
  
  // Navigation visibility state
  readonly isHidden = signal(false);
  
  // Navigation items configuration
  readonly navItems: BottomNavItem[] = [
    { 
      icon: 'home', 
      label: 'nav.bottom.home', 
      route: '/',
      testId: 'bottom-nav-home'
    },
    { 
      icon: 'grid-2x2', 
      label: 'nav.bottom.categories', 
      route: '/products',
      testId: 'bottom-nav-categories'
    },
    { 
      icon: 'search', 
      label: 'nav.bottom.search', 
      action: 'search',
      testId: 'bottom-nav-search'
    },
    { 
      icon: 'shopping-cart', 
      label: 'nav.bottom.cart', 
      route: '/cart',
      showBadge: true,
      testId: 'bottom-nav-cart'
    },
    { 
      icon: 'user', 
      label: 'nav.bottom.account', 
      route: '/account',
      testId: 'bottom-nav-account'
    }
  ];
  
  constructor() {
    // Set up scroll-based hide/show behavior
    if (isPlatformBrowser(this.platformId)) {
      this.setupScrollBehavior();
    }
    
    // Trigger badge pulse when cart count changes
    effect(() => {
      const count = this.cartItemCount();
      if (count > 0) {
        this.triggerBadgePulse();
      }
    });
    
    // Reset visibility on route change
    this.routerSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.isHidden.set(false);
    });
  }
  
  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
    if (isPlatformBrowser(this.platformId)) {
      window.removeEventListener('scroll', this.handleScroll);
    }
  }
  
  /**
   * Set up scroll-based visibility behavior
   * Hides nav when scrolling down, shows when scrolling up
   */
  private setupScrollBehavior(): void {
    this.lastScrollY = window.scrollY;
    window.addEventListener('scroll', this.handleScroll, { passive: true });
  }
  
  /**
   * Handle scroll events to show/hide navigation
   */
  private handleScroll = (): void => {
    // Don't hide if user prefers reduced motion
    if (this.animationService.prefersReducedMotion()) {
      return;
    }
    
    const currentScrollY = window.scrollY;
    const scrollDiff = currentScrollY - this.lastScrollY;
    
    // Only trigger if scroll distance exceeds threshold
    if (Math.abs(scrollDiff) < this.scrollThreshold) {
      return;
    }
    
    // Hide when scrolling down, show when scrolling up
    if (scrollDiff > 0 && currentScrollY > 100) {
      // Scrolling down - hide
      this.isHidden.set(true);
    } else if (scrollDiff < 0) {
      // Scrolling up - show
      this.isHidden.set(false);
    }
    
    // Also show when near top of page
    if (currentScrollY < 50) {
      this.isHidden.set(false);
    }
    
    this.lastScrollY = currentScrollY;
  };
  
  /**
   * Trigger badge pulse animation
   */
  private triggerBadgePulse(): void {
    if (this.animationService.prefersReducedMotion()) {
      return;
    }
    
    this.badgePulse.set(true);
    setTimeout(() => this.badgePulse.set(false), 600);
  }
  
  /**
   * Handle action button clicks (search, menu)
   */
  handleAction(action: 'search' | 'menu' | undefined): void {
    if (action === 'search') {
      // Navigate to products page with search focus
      // In the future, this could open a full-screen search modal
      this.router.navigate(['/products'], { queryParams: { search: 'open' } });
    } else if (action === 'menu') {
      // Future: Open mobile menu sheet
      console.log('Menu action triggered');
    }
  }
}
