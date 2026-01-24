import { Component, computed, input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Supported payment brand types
 */
export type PaymentBrand =
  | 'visa'
  | 'mastercard'
  | 'amex'
  | 'paypal'
  | 'apple-pay'
  | 'google-pay'
  | 'bank'
  | 'card-generic';

/**
 * Icon size options
 */
export type PaymentIconSize = 'sm' | 'md' | 'lg';

/**
 * Size mapping to pixel heights
 * sm=20, md=28, lg=36
 */
export const PAYMENT_ICON_SIZE_MAP: Record<PaymentIconSize, number> = {
  sm: 20,
  md: 28,
  lg: 36
};

/**
 * Payment Icon Component - Displays recognizable payment brand logos
 *
 * This component renders SVG payment icons for common payment methods.
 * Uses official brand colors for recognition.
 *
 * @example
 * ```html
 * <!-- Basic usage -->
 * <app-payment-icon brand="visa" />
 *
 * <!-- With size -->
 * <app-payment-icon brand="mastercard" size="lg" />
 *
 * <!-- With grayscale for disabled state -->
 * <app-payment-icon brand="amex" [grayscale]="true" />
 * ```
 */
@Component({
  selector: 'app-payment-icon',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="payment-icon"
      [class.grayscale]="grayscale()"
      [style.height.px]="computedSize()"
      [attr.aria-label]="ariaLabel()"
      [attr.role]="'img'"
      [attr.data-testid]="'payment-icon-' + brand()"
    >
      @switch (brand()) {
        @case ('visa') {
          <svg viewBox="0 0 48 16" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- Visa Logo - Dark blue text -->
            <rect width="48" height="16" rx="2" fill="#1A1F71"/>
            <path d="M19.5 4.5L17.2 11.5H15L17.3 4.5H19.5ZM28.5 8.5L29.5 5.8L30.1 8.5H28.5ZM31.5 11.5H33.5L31.8 4.5H30C29.5 4.5 29.1 4.8 28.9 5.2L25.5 11.5H27.8L28.2 10.3H31L31.5 11.5ZM24.5 8.2C24.5 6.2 21.5 6.1 21.5 5.2C21.5 4.9 21.8 4.5 22.5 4.4C22.9 4.4 24 4.4 25.2 5L25.6 4.6C24.7 4.3 23.5 4 22.3 4C20.2 4 18.7 5.1 18.7 6.6C18.7 8.7 21.7 8.9 21.7 9.9C21.7 10.4 21.2 10.8 20.4 10.8C19.5 10.8 18.2 10.4 17.5 10L17.1 10.5C18 11 19.2 11.3 20.4 11.3C22.6 11.3 24.1 10.2 24.1 8.5L24.5 8.2ZM14.5 4.5L11 11.5H8.5L6.8 5.8C6.7 5.5 6.5 5.3 6.2 5.2C5.5 4.8 4.5 4.5 3.5 4.3L3.6 4.5H7.2C7.7 4.5 8.1 4.8 8.2 5.3L9 9.5L11.2 4.5H14.5Z" fill="white"/>
          </svg>
        }
        @case ('mastercard') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- Mastercard Logo - Two overlapping circles -->
            <rect width="48" height="30" rx="3" fill="#1A1A1A"/>
            <circle cx="18" cy="15" r="9" fill="#EB001B"/>
            <circle cx="30" cy="15" r="9" fill="#F79E1B"/>
            <path d="M24 8.5C25.8 10 27 12.4 27 15C27 17.6 25.8 20 24 21.5C22.2 20 21 17.6 21 15C21 12.4 22.2 10 24 8.5Z" fill="#FF5F00"/>
          </svg>
        }
        @case ('amex') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- American Express Logo -->
            <rect width="48" height="30" rx="3" fill="#006FCF"/>
            <path d="M5 18.5V11.5H9L10 13L11 11.5H43V17.5C43 17.5 42 18.5 41 18.5H26L25 17V18.5H21V16.5C21 16.5 20.5 17 19.5 17H18V18.5H12L11 17L10 18.5H5Z" fill="white"/>
            <path d="M5.5 12L3.5 17.5H5.5L6 16.5H8L8.5 17.5H11L8.5 12H5.5ZM6.5 14L7 15.5H6L6.5 14Z" fill="#006FCF"/>
            <path d="M11.5 12V17.5H14V16H16C17.5 16 18.5 15 18.5 13.5C18.5 12 17.5 12 16 12H11.5ZM14 14V13.5H16C16.5 13.5 16.5 14 16 14H14Z" fill="#006FCF"/>
            <text x="24" y="14" font-family="Arial, sans-serif" font-size="5" font-weight="bold" fill="white" text-anchor="middle">AMEX</text>
          </svg>
        }
        @case ('paypal') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- PayPal Logo -->
            <rect width="48" height="30" rx="3" fill="#003087"/>
            <text x="24" y="18" font-family="Arial, sans-serif" font-size="9" font-weight="bold" fill="white" text-anchor="middle">
              <tspan fill="#009CDE">Pay</tspan><tspan fill="white">Pal</tspan>
            </text>
          </svg>
        }
        @case ('apple-pay') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- Apple Pay Logo -->
            <rect width="48" height="30" rx="3" fill="#000000"/>
            <path d="M14 10C13.5 10.5 12.5 11 11.5 10.9C11.4 9.9 11.9 8.9 12.4 8.3C13 7.6 14 7.1 14.8 7C14.9 8.1 14.5 9.1 14 10ZM14.8 11.1C13.4 11 12.2 11.9 11.5 11.9C10.8 11.9 9.8 11.2 8.6 11.2C7.1 11.2 5.5 12.2 4.7 13.8C3 17 4.3 21.8 6 24.3C6.8 25.5 7.8 26.9 9.1 26.8C10.2 26.8 10.7 26.1 12.2 26.1C13.6 26.1 14.1 26.8 15.3 26.8C16.5 26.8 17.4 25.5 18.2 24.3C19.1 22.9 19.5 21.6 19.5 21.5C19.5 21.5 17 20.5 17 17.6C17 15.2 18.9 14.1 19 14C17.8 12.3 16 11.2 14.8 11.1Z" fill="white"/>
            <text x="32" y="19" font-family="Arial, sans-serif" font-size="8" font-weight="500" fill="white" text-anchor="middle">Pay</text>
          </svg>
        }
        @case ('google-pay') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- Google Pay Logo -->
            <rect width="48" height="30" rx="3" fill="#FFFFFF" stroke="#E0E0E0" stroke-width="0.5"/>
            <path d="M22 11H18V19H19.5V16H22C23.4 16 24.5 14.9 24.5 13.5C24.5 12.1 23.4 11 22 11ZM22 14.5H19.5V12.5H22C22.6 12.5 23 12.9 23 13.5C23 14.1 22.6 14.5 22 14.5Z" fill="#4285F4"/>
            <path d="M29 14C27.6 14 26.5 15.1 26.5 16.5C26.5 17.9 27.6 19 29 19C29.6 19 30.1 18.8 30.5 18.5V19H32V14H30.5V14.5C30.1 14.2 29.6 14 29 14ZM29 17.5C28.4 17.5 28 17.1 28 16.5C28 15.9 28.4 15.5 29 15.5C29.6 15.5 30 15.9 30 16.5C30 17.1 29.6 17.5 29 17.5Z" fill="#34A853"/>
            <path d="M36.5 14L35 17.5L33.5 14H32L34.3 19L33 22H34.5L38 14H36.5Z" fill="#4285F4"/>
            <path d="M14 15C14 12.8 12.2 11 10 11C7.8 11 6 12.8 6 15C6 17.2 7.8 19 10 19C11.2 19 12.3 18.5 13 17.7L11.8 16.5C11.4 17 10.7 17.5 10 17.5C8.6 17.5 7.5 16.4 7.5 15C7.5 13.6 8.6 12.5 10 12.5C11.1 12.5 12 13.2 12.3 14H10V15.5H14V15Z" fill="#EA4335"/>
          </svg>
        }
        @case ('bank') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- Bank Transfer Icon -->
            <rect width="48" height="30" rx="3" fill="#2D3748"/>
            <path d="M24 6L10 12V14H38V12L24 6Z" fill="#A0AEC0"/>
            <rect x="13" y="15" width="3" height="8" fill="#A0AEC0"/>
            <rect x="19" y="15" width="3" height="8" fill="#A0AEC0"/>
            <rect x="26" y="15" width="3" height="8" fill="#A0AEC0"/>
            <rect x="32" y="15" width="3" height="8" fill="#A0AEC0"/>
            <rect x="10" y="23" width="28" height="2" fill="#A0AEC0"/>
          </svg>
        }
        @case ('card-generic') {
          <svg viewBox="0 0 48 30" [attr.height]="computedSize()" preserveAspectRatio="xMidYMid meet" aria-hidden="true">
            <!-- Generic Credit Card Icon -->
            <rect width="48" height="30" rx="3" fill="#4A5568"/>
            <rect x="0" y="6" width="48" height="5" fill="#2D3748"/>
            <rect x="5" y="17" width="10" height="3" rx="1" fill="#A0AEC0"/>
            <rect x="18" y="17" width="6" height="3" rx="1" fill="#A0AEC0"/>
            <rect x="27" y="17" width="6" height="3" rx="1" fill="#A0AEC0"/>
            <rect x="36" y="17" width="6" height="3" rx="1" fill="#A0AEC0"/>
            <rect x="5" y="22" width="15" height="2" rx="0.5" fill="#718096"/>
          </svg>
        }
      }
    </span>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      line-height: 0;
    }

    .payment-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      transition: filter 0.2s ease-out;
    }

    .payment-icon svg {
      display: block;
      width: auto;
    }

    .payment-icon.grayscale {
      filter: grayscale(100%);
      opacity: 0.6;
    }

    @media (prefers-reduced-motion: reduce) {
      .payment-icon {
        transition: none;
      }
    }
  `]
})
export class PaymentIconComponent {
  // ----- Required Inputs -----

  /**
   * Payment brand to display
   * @example 'visa', 'mastercard', 'amex', 'paypal'
   */
  readonly brand = input.required<PaymentBrand>();

  // ----- Optional Inputs -----

  /**
   * Icon size using semantic scale
   * Maps to: sm=20px, md=28px, lg=36px
   * @default 'md'
   */
  readonly size = input<PaymentIconSize>('md');

  /**
   * Whether to display the icon in grayscale (for disabled state)
   * @default false
   */
  readonly grayscale = input<boolean>(false);

  /**
   * Accessible label for the icon
   * @default Brand name + ' payment'
   */
  readonly label = input<string>('');

  // ----- Computed Properties -----

  /**
   * Convert semantic size to pixel value
   */
  readonly computedSize = computed(() => PAYMENT_ICON_SIZE_MAP[this.size()]);

  /**
   * Computed aria-label for accessibility
   */
  readonly ariaLabel = computed(() => {
    const customLabel = this.label();
    if (customLabel) return customLabel;

    const brandLabels: Record<PaymentBrand, string> = {
      'visa': 'Visa',
      'mastercard': 'Mastercard',
      'amex': 'American Express',
      'paypal': 'PayPal',
      'apple-pay': 'Apple Pay',
      'google-pay': 'Google Pay',
      'bank': 'Bank Transfer',
      'card-generic': 'Credit Card'
    };

    return brandLabels[this.brand()];
  });
}
