import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { TrustBadgeComponent } from './trust-badge.component';

/**
 * Trust badge item configuration
 */
export interface TrustBadgeItem {
  variant: 'security' | 'shipping' | 'warranty' | 'support' | 'certification' | 'guarantee';
  icon?: string;
  title: string;
  description?: string;
}

/**
 * Default trust badges displayed in the strip
 */
const DEFAULT_BADGES: TrustBadgeItem[] = [
  {
    variant: 'security',
    icon: 'shield-check',
    title: 'trust.secureCheckout',
    description: 'trust.sslEncrypted'
  },
  {
    variant: 'shipping',
    icon: 'truck',
    title: 'trust.freeShipping',
    description: 'trust.freeShippingThreshold'
  },
  {
    variant: 'warranty',
    icon: 'badge-check',
    title: 'trust.warrantyIncluded',
    description: 'trust.upToYears'
  },
  {
    variant: 'support',
    icon: 'headphones',
    title: 'trust.expertSupport',
    description: 'trust.supportHours'
  }
];

/**
 * TrustBadgeStripComponent - A horizontal strip of trust badges.
 * 
 * Used in footer, checkout pages, and other areas to reinforce trust.
 * Displays a configurable set of badges in a responsive horizontal layout.
 * 
 * @example
 * ```html
 * <!-- Default badges -->
 * <app-trust-badge-strip />
 * 
 * <!-- Custom badges -->
 * <app-trust-badge-strip [badges]="customBadges" />
 * 
 * <!-- Compact mode for footer -->
 * <app-trust-badge-strip [compact]="true" />
 * ```
 */
@Component({
  selector: 'app-trust-badge-strip',
  standalone: true,
  imports: [CommonModule, TranslateModule, TrustBadgeComponent],
  template: `
    <div 
      class="trust-strip" 
      [class.trust-strip--compact]="compact()"
      [class.trust-strip--centered]="centered()"
      [attr.aria-label]="'trust.sectionTitle' | translate"
      data-testid="trust-badge-strip"
    >
      @for (badge of displayBadges(); track badge.title) {
        <app-trust-badge
          [variant]="badge.variant"
          [icon]="badge.icon || ''"
          [title]="badge.title"
          [description]="compact() ? '' : (badge.description || '')"
          [size]="compact() ? 'sm' : 'md'"
        />
      }
    </div>
  `,
  styles: [`
    .trust-strip {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      padding: 1rem 0;

      &--compact {
        gap: 0.75rem;
        padding: 0.75rem 0;
      }

      &--centered {
        justify-content: center;
      }
    }

    // Responsive: scroll horizontally on mobile
    @media (max-width: 768px) {
      .trust-strip {
        flex-wrap: nowrap;
        overflow-x: auto;
        padding-bottom: 0.5rem;
        scrollbar-width: none;
        -webkit-overflow-scrolling: touch;

        &::-webkit-scrollbar {
          display: none;
        }

        ::ng-deep app-trust-badge {
          flex-shrink: 0;
        }
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrustBadgeStripComponent {
  /**
   * Custom badges to display (defaults to standard trust badges)
   */
  readonly badges = input<TrustBadgeItem[]>();

  /**
   * Compact mode - smaller badges without descriptions
   */
  readonly compact = input<boolean>(false);

  /**
   * Center the badges horizontally
   */
  readonly centered = input<boolean>(false);

  /**
   * Returns custom badges or defaults
   */
  displayBadges(): TrustBadgeItem[] {
    return this.badges() || DEFAULT_BADGES;
  }
}
