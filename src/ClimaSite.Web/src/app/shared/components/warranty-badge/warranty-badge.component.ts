import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-warranty-badge',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="warranty-badges" data-testid="warranty-badges">
      <!-- Warranty -->
      @if (warrantyMonths() > 0) {
        <div class="badge warranty" data-testid="warranty-badge">
          <div class="icon-wrapper warranty-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
              <path d="m9 12 2 2 4-4"/>
            </svg>
          </div>
          <div class="content">
            <span class="value">{{ warrantyDisplay() }}</span>
            <span class="label">{{ 'products.warranty.title' | translate }}</span>
          </div>
        </div>
      }

      <!-- Return Policy -->
      @if (showReturnPolicy()) {
        <div class="badge return" data-testid="return-badge">
          <div class="icon-wrapper return-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/>
              <path d="M3 3v5h5"/>
            </svg>
          </div>
          <div class="content">
            <span class="value">{{ returnDays() }}</span>
            <span class="label">{{ 'products.returnPolicy.days' | translate: { count: returnDays() } }}</span>
          </div>
        </div>
      }

      <!-- Free Shipping -->
      @if (freeShipping()) {
        <div class="badge shipping" data-testid="shipping-badge">
          <div class="icon-wrapper shipping-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="1" y="3" width="15" height="13"/>
              <polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/>
              <circle cx="5.5" cy="18.5" r="2.5"/>
              <circle cx="18.5" cy="18.5" r="2.5"/>
            </svg>
          </div>
          <div class="content">
            <span class="label">{{ 'products.freeShipping' | translate }}</span>
          </div>
        </div>
      }

      <!-- In Stock -->
      @if (inStock()) {
        <div class="badge stock in-stock" data-testid="stock-badge">
          <div class="icon-wrapper stock-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
              <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
          </div>
          <div class="content">
            <span class="label">{{ 'products.stock.inStock' | translate }}</span>
          </div>
        </div>
      }

      <!-- Installation Available -->
      @if (installationAvailable()) {
        <div class="badge installation" data-testid="installation-badge">
          <div class="icon-wrapper installation-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>
            </svg>
          </div>
          <div class="content">
            <span class="label">{{ 'products.installation.available' | translate }}</span>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .warranty-badges {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }

    .badge {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      border: 1px solid var(--color-border);
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-primary-light);
      }
    }

    .icon-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      flex-shrink: 0;

      svg {
        width: 20px;
        height: 20px;
      }
    }

    .warranty-icon {
      background: var(--color-success-light);
      color: var(--color-success);
    }

    .return-icon {
      background: var(--color-primary-light);
      color: var(--color-primary);
    }

    .shipping-icon {
      background: var(--glow-primary);
      color: var(--color-primary);
    }

    .stock-icon {
      background: var(--color-success-light);
      color: var(--color-success);
    }

    .installation-icon {
      background: rgba(249, 115, 22, 0.1);
      color: #f97316;
    }

    .content {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .value {
      font-size: 1.125rem;
      font-weight: 700;
      color: var(--color-text-primary);
      line-height: 1.2;
    }

    .label {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      line-height: 1.2;
    }

    /* Compact mode for small screens */
    @media (max-width: 768px) {
      .warranty-badges {
        gap: 0.5rem;
      }

      .badge {
        padding: 0.5rem 0.75rem;
      }

      .icon-wrapper {
        width: 32px;
        height: 32px;

        svg {
          width: 16px;
          height: 16px;
        }
      }

      .value {
        font-size: 1rem;
      }

      .label {
        font-size: 0.625rem;
      }
    }

    /* Compact horizontal layout option */
    :host(.compact) {
      .warranty-badges {
        flex-direction: row;
        flex-wrap: nowrap;
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
        scrollbar-width: none;

        &::-webkit-scrollbar {
          display: none;
        }
      }

      .badge {
        flex-shrink: 0;
        padding: 0.5rem 0.75rem;
      }

      .icon-wrapper {
        width: 28px;
        height: 28px;

        svg {
          width: 14px;
          height: 14px;
        }
      }

      .content {
        flex-direction: row;
        align-items: baseline;
        gap: 0.25rem;
      }

      .value {
        font-size: 0.875rem;
      }

      .label {
        font-size: 0.75rem;
      }
    }
  `]
})
export class WarrantyBadgeComponent {
  warrantyMonths = input<number>(0);
  returnDays = input<number>(30);
  freeShipping = input<boolean>(false);
  inStock = input<boolean>(true);
  installationAvailable = input<boolean>(false);
  showReturnPolicy = input<boolean>(true);

  warrantyDisplay = computed(() => {
    const months = this.warrantyMonths();
    if (months >= 12 && months % 12 === 0) {
      return `${months / 12} ${months / 12 === 1 ? 'year' : 'years'}`;
    }
    return `${months} months`;
  });
}
