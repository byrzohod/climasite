import { Component, computed, input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { IconComponent } from '../icon/icon.component';

/**
 * Trust badge variant types
 */
export type TrustBadgeVariant = 'security' | 'shipping' | 'warranty' | 'support' | 'certification' | 'guarantee';

/**
 * Trust badge size options
 */
export type TrustBadgeSize = 'sm' | 'md' | 'lg';

/**
 * Default icons for each badge variant
 */
const DEFAULT_ICONS: Record<TrustBadgeVariant, string> = {
  security: 'shield-check',
  shipping: 'truck',
  warranty: 'badge-check',
  support: 'headphones',
  certification: 'award',
  guarantee: 'check-circle'
};

/**
 * TrustBadgeComponent - A flexible badge component for various trust indicators.
 * 
 * Used throughout the site to build customer confidence with visual trust signals.
 * Supports multiple variants, sizes, and custom icons.
 * 
 * @example
 * ```html
 * <!-- Basic usage -->
 * <app-trust-badge variant="security" title="trust.secureCheckout" />
 * 
 * <!-- With description -->
 * <app-trust-badge 
 *   variant="shipping" 
 *   icon="truck" 
 *   title="trust.freeShipping" 
 *   description="trust.freeShippingDesc" 
 * />
 * 
 * <!-- Large size -->
 * <app-trust-badge variant="warranty" title="trust.warranty" size="lg" />
 * ```
 */
@Component({
  selector: 'app-trust-badge',
  standalone: true,
  imports: [CommonModule, TranslateModule, IconComponent],
  template: `
    <div 
      class="trust-badge"
      [class]="badgeClasses()"
      [attr.data-testid]="'trust-badge-' + variant()"
    >
      <div class="trust-badge__icon">
        <app-icon [name]="displayIcon()" [size]="iconSize()" />
      </div>
      <div class="trust-badge__content">
        <span class="trust-badge__title">{{ title() | translate }}</span>
        @if (description()) {
          <span class="trust-badge__description">{{ description() | translate }}</span>
        }
      </div>
    </div>
  `,
  styles: [`
    .trust-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      border-radius: 0.5rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border-primary);
      transition: border-color 0.2s ease, box-shadow 0.2s ease;

      &:hover {
        border-color: var(--color-border-secondary);
      }

      // Variant styles
      &--security {
        .trust-badge__icon {
          color: var(--color-success);
          background: var(--color-success-light);
        }
      }

      &--shipping {
        .trust-badge__icon {
          color: var(--color-primary);
          background: var(--color-primary-light);
        }
      }

      &--warranty {
        .trust-badge__icon {
          color: var(--color-aurora);
          background: var(--color-aurora-50);
        }
      }

      &--support {
        .trust-badge__icon {
          color: var(--color-accent);
          background: var(--color-accent-50);
        }
      }

      &--certification {
        .trust-badge__icon {
          color: var(--color-warning);
          background: var(--color-warning-light);
        }
      }

      &--guarantee {
        .trust-badge__icon {
          color: var(--color-success);
          background: var(--color-success-light);
        }
      }

      // Size variants
      &--sm {
        padding: 0.5rem 0.75rem;
        gap: 0.5rem;

        .trust-badge__icon {
          width: 28px;
          height: 28px;
        }

        .trust-badge__title {
          font-size: 0.75rem;
        }

        .trust-badge__description {
          font-size: 0.625rem;
        }
      }

      &--md {
        .trust-badge__icon {
          width: 36px;
          height: 36px;
        }

        .trust-badge__title {
          font-size: 0.875rem;
        }

        .trust-badge__description {
          font-size: 0.75rem;
        }
      }

      &--lg {
        padding: 1rem 1.25rem;
        gap: 1rem;

        .trust-badge__icon {
          width: 44px;
          height: 44px;
        }

        .trust-badge__title {
          font-size: 1rem;
        }

        .trust-badge__description {
          font-size: 0.875rem;
        }
      }

      &__icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border-radius: 50%;
        flex-shrink: 0;
      }

      &__content {
        display: flex;
        flex-direction: column;
        gap: 0.125rem;
        min-width: 0;
      }

      &__title {
        font-weight: 600;
        color: var(--color-text-primary);
        line-height: 1.3;
        white-space: nowrap;
      }

      &__description {
        color: var(--color-text-secondary);
        line-height: 1.3;
      }
    }

    // Dark mode adjustments
    :host-context([data-theme="dark"]),
    :host-context(.dark) {
      .trust-badge--warranty .trust-badge__icon {
        background: rgba(20, 184, 166, 0.15);
      }

      .trust-badge--support .trust-badge__icon {
        background: rgba(6, 182, 212, 0.15);
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrustBadgeComponent {
  /**
   * Badge variant that determines styling and default icon
   */
  readonly variant = input<TrustBadgeVariant>('security');

  /**
   * Custom icon name (overrides variant default)
   */
  readonly icon = input<string>('');

  /**
   * Badge title (i18n key)
   */
  readonly title = input.required<string>();

  /**
   * Optional description text (i18n key)
   */
  readonly description = input<string>('');

  /**
   * Badge size
   */
  readonly size = input<TrustBadgeSize>('md');

  /**
   * Computed icon based on custom icon or variant default
   */
  readonly displayIcon = computed(() => {
    const customIcon = this.icon();
    if (customIcon) return customIcon;
    return DEFAULT_ICONS[this.variant()];
  });

  /**
   * Computed icon size based on badge size
   */
  readonly iconSize = computed(() => {
    const sizeMap = { sm: 'sm', md: 'md', lg: 'lg' } as const;
    return sizeMap[this.size()];
  });

  /**
   * Computed CSS classes for the badge
   */
  readonly badgeClasses = computed(() => {
    return `trust-badge--${this.variant()} trust-badge--${this.size()}`;
  });
}
