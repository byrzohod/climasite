/**
 * Payment Icons Component Public API
 * Nordic Tech Design System - Payment Brand Icons
 *
 * Provides recognizable payment brand icons for checkout and cart components.
 *
 * @example
 * ```typescript
 * import { 
 *   PaymentIconComponent, 
 *   PaymentTrustStripComponent,
 *   PaymentBrand 
 * } from '@shared/components/payment-icons';
 *
 * // Single icon
 * <app-payment-icon brand="visa" size="md" />
 *
 * // Trust strip with multiple icons
 * <app-payment-trust-strip [showLabel]="true" [showSecureText]="true" />
 * ```
 */

export {
  PaymentIconComponent,
  type PaymentBrand,
  type PaymentIconSize,
  PAYMENT_ICON_SIZE_MAP
} from './payment-icon.component';

export { PaymentTrustStripComponent } from './payment-trust-strip.component';
