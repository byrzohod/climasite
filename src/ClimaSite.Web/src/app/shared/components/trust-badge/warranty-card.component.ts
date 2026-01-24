import { Component, input, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../icon/icon.component';

/**
 * Warranty type options
 */
export type WarrantyType = 'standard' | 'extended' | 'premium';

/**
 * Warranty feature item
 */
export interface WarrantyFeature {
  key: string;
  included: boolean;
}

/**
 * Default warranty features by type
 */
const WARRANTY_FEATURES: Record<WarrantyType, string[]> = {
  standard: [
    'warranty.features.parts',
    'warranty.features.labor',
    'warranty.features.manufacturer'
  ],
  extended: [
    'warranty.features.parts',
    'warranty.features.labor',
    'warranty.features.manufacturer',
    'warranty.features.accidentalDamage'
  ],
  premium: [
    'warranty.features.parts',
    'warranty.features.labor',
    'warranty.features.manufacturer',
    'warranty.features.accidentalDamage',
    'warranty.features.replacement',
    'warranty.features.prioritySupport'
  ]
};

/**
 * WarrantyCardComponent - Detailed warranty display for product pages.
 * 
 * Shows warranty duration, type, and coverage details with expandable section.
 * Provides visual reassurance to customers about product protection.
 * 
 * @example
 * ```html
 * <!-- Basic usage -->
 * <app-warranty-card [years]="2" type="standard" />
 * 
 * <!-- Extended warranty with custom features -->
 * <app-warranty-card 
 *   [years]="5" 
 *   type="extended" 
 *   [features]="['Parts', 'Labor', 'Compressor']"
 * />
 * 
 * <!-- Premium warranty -->
 * <app-warranty-card [years]="7" type="premium" />
 * ```
 */
@Component({
  selector: 'app-warranty-card',
  standalone: true,
  imports: [CommonModule, TranslateModule, RouterLink, IconComponent],
  template: `
    <div 
      class="warranty-card" 
      [class.warranty-card--expanded]="expanded()"
      [class]="'warranty-card--' + type()"
      data-testid="warranty-card"
    >
      <!-- Header (always visible) -->
      <button 
        class="warranty-card__header"
        (click)="toggleExpanded()"
        [attr.aria-expanded]="expanded()"
        type="button"
      >
        <div class="warranty-card__icon">
          <app-icon name="shield-check" size="lg" />
        </div>
        <div class="warranty-card__info">
          <span class="warranty-card__type">{{ getTypeLabel() | translate }}</span>
          <span class="warranty-card__duration">
            {{ years() }} {{ years() === 1 ? ('warranty.year' | translate) : ('warranty.years' | translate) }}
          </span>
        </div>
        @if (type() !== 'standard') {
          <span class="warranty-card__badge" [class]="'warranty-card__badge--' + type()">
            {{ type() === 'premium' ? ('warranty.premium' | translate) : ('warranty.extended' | translate) }}
          </span>
        }
        <div class="warranty-card__toggle">
          <app-icon [name]="expanded() ? 'chevron-up' : 'chevron-down'" size="sm" />
        </div>
      </button>

      <!-- Expandable details -->
      @if (expanded()) {
        <div class="warranty-card__details">
          <h4 class="warranty-card__coverage-title">{{ 'warranty.coverageIncludes' | translate }}</h4>
          <ul class="warranty-card__coverage-list">
            @for (feature of displayFeatures(); track feature) {
              <li class="warranty-card__coverage-item">
                <app-icon name="check" size="sm" class="warranty-card__check-icon" />
                <span>{{ feature | translate }}</span>
              </li>
            }
          </ul>
          
          @if (showTermsLink()) {
            <a 
              class="warranty-card__terms-link" 
              routerLink="/warranty-terms"
              data-testid="warranty-terms-link"
            >
              {{ 'warranty.viewFullTerms' | translate }}
              <app-icon name="external-link" size="xs" />
            </a>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .warranty-card {
      border-radius: 0.75rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border-primary);
      overflow: hidden;
      transition: border-color 0.2s ease, box-shadow 0.2s ease;

      &:hover {
        border-color: var(--color-success);
      }

      &--expanded {
        border-color: var(--color-success);
        box-shadow: 0 4px 12px var(--shadow-color);
      }

      &--premium {
        border-color: var(--color-warning);
        background: linear-gradient(
          135deg,
          var(--color-bg-secondary) 0%,
          rgba(245, 158, 11, 0.05) 100%
        );

        &:hover,
        &.warranty-card--expanded {
          border-color: var(--color-warning);
        }

        .warranty-card__icon {
          color: var(--color-warning);
          background: var(--color-warning-light);
        }
      }

      &--extended {
        .warranty-card__icon {
          color: var(--color-aurora);
          background: var(--color-aurora-50);
        }
      }

      &__header {
        display: flex;
        align-items: center;
        gap: 1rem;
        width: 100%;
        padding: 1rem 1.25rem;
        background: none;
        border: none;
        cursor: pointer;
        text-align: left;
        transition: background-color 0.2s ease;

        &:hover {
          background: var(--color-bg-hover);
        }

        &:focus-visible {
          outline: 2px solid var(--color-primary);
          outline-offset: -2px;
        }
      }

      &__icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 48px;
        height: 48px;
        border-radius: 50%;
        color: var(--color-success);
        background: var(--color-success-light);
        flex-shrink: 0;
      }

      &__info {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        flex-grow: 1;
        min-width: 0;
      }

      &__type {
        font-size: 0.875rem;
        font-weight: 500;
        color: var(--color-text-secondary);
      }

      &__duration {
        font-size: 1.25rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }

      &__badge {
        display: inline-flex;
        padding: 0.25rem 0.75rem;
        border-radius: 9999px;
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.025em;
        flex-shrink: 0;

        &--extended {
          background: var(--color-aurora-100);
          color: var(--color-aurora-700);
        }

        &--premium {
          background: var(--color-warning-100);
          color: var(--color-warning-700);
        }
      }

      &__toggle {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        border-radius: 50%;
        color: var(--color-text-secondary);
        transition: background-color 0.2s ease;

        &:hover {
          background: var(--color-bg-tertiary);
        }
      }

      &__details {
        padding: 0 1.25rem 1.25rem;
        border-top: 1px solid var(--color-border-primary);
        margin-top: -1px;
        animation: slideDown 0.2s ease-out;
      }

      &__coverage-title {
        margin: 1rem 0 0.75rem;
        font-size: 0.875rem;
        font-weight: 600;
        color: var(--color-text-primary);
      }

      &__coverage-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      &__coverage-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      &__check-icon {
        color: var(--color-success);
        flex-shrink: 0;
      }

      &__terms-link {
        display: inline-flex;
        align-items: center;
        gap: 0.375rem;
        margin-top: 1rem;
        font-size: 0.875rem;
        color: var(--color-primary);
        text-decoration: none;
        transition: color 0.2s ease;

        &:hover {
          color: var(--color-primary-hover);
          text-decoration: underline;
        }

        &:focus-visible {
          outline: 2px solid var(--color-primary);
          outline-offset: 2px;
        }
      }
    }

    @keyframes slideDown {
      from {
        opacity: 0;
        transform: translateY(-8px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    // Dark mode adjustments
    :host-context([data-theme="dark"]),
    :host-context(.dark) {
      .warranty-card--premium {
        background: linear-gradient(
          135deg,
          var(--color-bg-secondary) 0%,
          rgba(251, 191, 36, 0.08) 100%
        );
      }

      .warranty-card__badge--extended {
        background: rgba(20, 184, 166, 0.2);
        color: var(--color-aurora-300);
      }

      .warranty-card__badge--premium {
        background: rgba(251, 191, 36, 0.2);
        color: var(--color-warning-300);
      }
    }

    // Responsive
    @media (max-width: 480px) {
      .warranty-card__header {
        padding: 0.875rem 1rem;
        gap: 0.75rem;
      }

      .warranty-card__icon {
        width: 40px;
        height: 40px;
      }

      .warranty-card__duration {
        font-size: 1.125rem;
      }

      .warranty-card__badge {
        display: none;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WarrantyCardComponent {
  /**
   * Warranty duration in years
   */
  readonly years = input<number>(2);

  /**
   * Warranty type
   */
  readonly type = input<WarrantyType>('standard');

  /**
   * Custom warranty features (overrides defaults)
   */
  readonly features = input<string[]>();

  /**
   * Whether to show link to warranty terms page
   */
  readonly showTermsLink = input<boolean>(true);

  /**
   * Expanded state
   */
  expanded = signal(false);

  /**
   * Toggle expanded state
   */
  toggleExpanded(): void {
    this.expanded.update(v => !v);
  }

  /**
   * Get translated label for warranty type
   */
  getTypeLabel(): string {
    const labels: Record<WarrantyType, string> = {
      standard: 'warranty.manufacturerWarranty',
      extended: 'warranty.extendedWarranty',
      premium: 'warranty.premiumWarranty'
    };
    return labels[this.type()];
  }

  /**
   * Get features to display (custom or default based on type)
   */
  displayFeatures(): string[] {
    const customFeatures = this.features();
    if (customFeatures && customFeatures.length > 0) {
      return customFeatures;
    }
    return WARRANTY_FEATURES[this.type()];
  }
}
