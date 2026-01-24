import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { IconComponent } from '../icon/icon.component';

/**
 * Empty state variant types for different contexts
 */
export type EmptyStateVariant = 
  | 'cart' 
  | 'search' 
  | 'orders' 
  | 'wishlist' 
  | 'reviews' 
  | 'products'
  | 'error'
  | 'offline'
  | 'maintenance'
  | 'generic';

/**
 * Icon mapping for each variant
 */
const VARIANT_ICONS: Record<EmptyStateVariant, string> = {
  cart: 'shopping-cart',
  search: 'search',
  orders: 'package',
  wishlist: 'heart',
  reviews: 'message-square',
  products: 'grid-2x2',
  error: 'alert-triangle',
  offline: 'wifi-off',
  maintenance: 'wrench',
  generic: 'inbox'
};

/**
 * EmptyStateComponent
 * 
 * A reusable component for displaying empty states across the application.
 * Provides consistent styling and iconography for various empty state scenarios.
 * 
 * TASK-21H-044: Create EmptyStateComponent with illustration slots
 * 
 * @example
 * ```html
 * <!-- Basic usage -->
 * <app-empty-state
 *   variant="cart"
 *   [title]="'emptyState.cart.title' | translate"
 *   [description]="'emptyState.cart.description' | translate"
 *   [actionLabel]="'emptyState.cart.action' | translate"
 *   actionRoute="/products"
 * />
 * 
 * <!-- With custom icon visibility -->
 * <app-empty-state
 *   variant="search"
 *   [title]="'emptyState.search.title' | translate"
 *   [showIcon]="false"
 * />
 * ```
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, IconComponent],
  template: `
    <div 
      class="empty-state" 
      [class]="'empty-state--' + variant()"
      role="status"
      [attr.aria-label]="title()"
      data-testid="empty-state">
      
      @if (showIcon()) {
        <div class="empty-state__icon" [class.empty-state__icon--search]="variant() === 'search'">
          <app-icon 
            [name]="iconName()" 
            size="xl" 
            [strokeWidth]="1.5"
            [ariaHidden]="true"
          />
          @if (variant() === 'search') {
            <span class="empty-state__icon-badge">?</span>
          }
        </div>
      }
      
      @if (title()) {
        <h2 class="empty-state__title" data-testid="empty-state-title">
          {{ title() }}
        </h2>
      }
      
      @if (description()) {
        <p class="empty-state__description" data-testid="empty-state-description">
          {{ description() }}
        </p>
      }
      
      @if (actionLabel() && actionRoute()) {
        <a 
          [routerLink]="actionRoute()" 
          class="empty-state__action"
          data-testid="empty-state-action">
          {{ actionLabel() }}
        </a>
      }
      
      <!-- Slot for custom content -->
      <div class="empty-state__custom">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 3rem 2rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      min-height: 300px;
    }

    .empty-state__icon {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 80px;
      height: 80px;
      margin-bottom: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 50%;
      color: var(--color-text-tertiary);
      
      app-icon {
        --icon-size: 40px;
      }
    }

    .empty-state__icon--search {
      position: relative;
    }

    .empty-state__icon-badge {
      position: absolute;
      bottom: 4px;
      right: 4px;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      background: var(--color-warning);
      color: var(--color-text-inverse);
      border-radius: 50%;
      font-size: 0.875rem;
      font-weight: 700;
    }

    .empty-state__title {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--color-text-primary);
      margin: 0 0 0.5rem 0;
    }

    .empty-state__description {
      font-size: 0.9375rem;
      color: var(--color-text-secondary);
      margin: 0 0 1.5rem 0;
      max-width: 400px;
      line-height: 1.5;
    }

    .empty-state__action {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: 8px;
      font-size: 0.9375rem;
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s ease;

      &:hover {
        background: var(--color-primary-dark);
        transform: translateY(-1px);
      }

      &:active {
        transform: translateY(0);
      }
    }

    .empty-state__custom {
      margin-top: 1rem;
      
      &:empty {
        display: none;
      }
    }

    /* Variant-specific styles */
    .empty-state--cart .empty-state__icon {
      background: var(--color-primary-50, var(--color-bg-secondary));
      color: var(--color-primary);
    }

    .empty-state--wishlist .empty-state__icon {
      background: var(--color-error-light, var(--color-bg-secondary));
      color: var(--color-error);
    }

    .empty-state--orders .empty-state__icon {
      background: var(--color-accent-50, var(--color-bg-secondary));
      color: var(--color-accent);
    }

    .empty-state--search .empty-state__icon {
      background: var(--color-warning-light, var(--color-bg-secondary));
      color: var(--color-warning);
    }

    .empty-state--reviews .empty-state__icon {
      background: var(--color-success-light, var(--color-bg-secondary));
      color: var(--color-success);
    }

    .empty-state--products .empty-state__icon {
      background: var(--color-accent-50, var(--color-bg-secondary));
      color: var(--color-accent);
    }

    .empty-state--error .empty-state__icon {
      background: var(--color-error-light, var(--color-bg-secondary));
      color: var(--color-error);
    }

    .empty-state--offline .empty-state__icon {
      background: var(--color-warning-light, var(--color-bg-secondary));
      color: var(--color-text-tertiary);
    }

    .empty-state--maintenance .empty-state__icon {
      background: var(--color-info-light, var(--color-bg-secondary));
      color: var(--color-info);
    }

    /* Responsive adjustments */
    @media (max-width: 640px) {
      .empty-state {
        padding: 2rem 1.5rem;
        min-height: 250px;
      }

      .empty-state__icon {
        width: 64px;
        height: 64px;
        margin-bottom: 1rem;

        app-icon {
          --icon-size: 32px;
        }
      }

      .empty-state__icon-badge {
        width: 20px;
        height: 20px;
        font-size: 0.75rem;
      }

      .empty-state__title {
        font-size: 1.125rem;
      }

      .empty-state__description {
        font-size: 0.875rem;
      }

      .empty-state__action {
        width: 100%;
        max-width: 280px;
      }
    }

    /* Reduced motion support */
    @media (prefers-reduced-motion: reduce) {
      .empty-state__action {
        transition: none;

        &:hover {
          transform: none;
        }
      }
    }
  `]
})
export class EmptyStateComponent {
  /**
   * The variant type determines the icon and styling
   * @default 'generic'
   */
  readonly variant = input<EmptyStateVariant>('generic');

  /**
   * Main title text displayed in the empty state
   */
  readonly title = input<string>('');

  /**
   * Description text providing context about the empty state
   */
  readonly description = input<string>('');

  /**
   * Label for the optional action button
   */
  readonly actionLabel = input<string>('');

  /**
   * Router link for the action button
   */
  readonly actionRoute = input<string>('');

  /**
   * Whether to show the icon
   * @default true
   */
  readonly showIcon = input<boolean>(true);

  /**
   * Computed icon name based on variant
   */
  readonly iconName = computed(() => VARIANT_ICONS[this.variant()]);
}
