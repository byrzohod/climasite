import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { PaymentIconComponent, PaymentBrand, PaymentIconSize } from './payment-icon.component';

/**
 * Payment Trust Strip Component
 * 
 * Displays a horizontal strip of accepted payment method icons.
 * Used to build trust in checkout, cart, and footer sections.
 * 
 * @example
 * ```html
 * <!-- Basic usage (shows all major cards) -->
 * <app-payment-trust-strip />
 * 
 * <!-- Custom brands -->
 * <app-payment-trust-strip [brands]="['visa', 'mastercard', 'paypal']" />
 * 
 * <!-- With label -->
 * <app-payment-trust-strip [showLabel]="true" />
 * 
 * <!-- Compact size -->
 * <app-payment-trust-strip size="sm" />
 * ```
 */
@Component({
  selector: 'app-payment-trust-strip',
  standalone: true,
  imports: [CommonModule, TranslateModule, PaymentIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div 
      class="payment-trust-strip"
      [class.compact]="size() === 'sm'"
      [class.vertical]="layout() === 'vertical'"
      [attr.data-testid]="'payment-trust-strip'"
      role="group"
      [attr.aria-label]="'checkout.payment.acceptedMethods' | translate"
    >
      @if (showLabel()) {
        <span class="trust-label">{{ 'checkout.payment.weAccept' | translate }}:</span>
      }
      <div class="payment-icons">
        @for (brand of brands(); track brand) {
          <app-payment-icon 
            [brand]="brand" 
            [size]="size()"
            [grayscale]="grayscale()"
          />
        }
      </div>
      @if (showSecureText()) {
        <span class="secure-text">
          <svg class="lock-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
            <path fill="currentColor" d="M8 1a4 4 0 0 0-4 4v2H3a1 1 0 0 0-1 1v6a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V8a1 1 0 0 0-1-1h-1V5a4 4 0 0 0-4-4zm2.5 6H5.5V5a2.5 2.5 0 0 1 5 0v2z"/>
          </svg>
          {{ 'checkout.payment.securePayment' | translate }}
        </span>
      }
    </div>
  `,
  styles: [`
    .payment-trust-strip {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .payment-trust-strip.vertical {
      flex-direction: column;
      align-items: flex-start;
    }

    .payment-trust-strip.compact {
      gap: 0.5rem;
    }

    .trust-label {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      white-space: nowrap;
    }

    .compact .trust-label {
      font-size: 0.75rem;
    }

    .payment-icons {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .compact .payment-icons {
      gap: 0.375rem;
    }

    .secure-text {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      white-space: nowrap;
    }

    .lock-icon {
      color: var(--color-success, #22c55e);
    }

    @media (max-width: 640px) {
      .payment-trust-strip {
        justify-content: center;
      }

      .payment-trust-strip.vertical {
        align-items: center;
      }
    }
  `]
})
export class PaymentTrustStripComponent {
  // ----- Optional Inputs -----

  /**
   * Payment brands to display
   * @default ['visa', 'mastercard', 'amex', 'paypal']
   */
  readonly brands = input<PaymentBrand[]>(['visa', 'mastercard', 'amex', 'paypal']);

  /**
   * Icon size
   * @default 'md'
   */
  readonly size = input<PaymentIconSize>('md');

  /**
   * Whether to show the "We accept:" label
   * @default false
   */
  readonly showLabel = input<boolean>(false);

  /**
   * Whether to show the secure payment text
   * @default false
   */
  readonly showSecureText = input<boolean>(false);

  /**
   * Whether to display icons in grayscale
   * @default false
   */
  readonly grayscale = input<boolean>(false);

  /**
   * Layout direction
   * @default 'horizontal'
   */
  readonly layout = input<'horizontal' | 'vertical'>('horizontal');
}
