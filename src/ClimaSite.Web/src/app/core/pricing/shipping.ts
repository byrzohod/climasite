/**
 * Client-side shipping cost — the SINGLE source of truth for the UI, mirroring the server's
 * `CheckoutPricing.GetShippingCost(method, subtotal)` so the displayed shipping == the charged shipping.
 * Standard shipping is FREE when the order subtotal is at/above the €50 threshold (DEC-SHIPPING),
 * otherwise €5.99; express €15.99 / overnight €19.99 are flat.
 *
 * KEEP IN SYNC with src/ClimaSite.Application/Common/Pricing/CheckoutPricing.cs (the authoritative
 * server value — the server always recomputes and verifies the charge; this is for display parity).
 */
export const FREE_SHIPPING_THRESHOLD = 50;
export const STANDARD_SHIPPING = 5.99;
export const EXPRESS_SHIPPING = 15.99;
export const OVERNIGHT_SHIPPING = 19.99;
const DEFAULT_SHIPPING = 9.99;

/** Threshold-aware shipping cost for a method, given the current cart subtotal. */
export function shippingCostFor(method: string | null | undefined, subtotal: number): number {
  switch ((method ?? '').toLowerCase()) {
    case 'standard':
      return subtotal >= FREE_SHIPPING_THRESHOLD ? 0 : STANDARD_SHIPPING;
    case 'express':
      return EXPRESS_SHIPPING;
    case 'overnight':
      return OVERNIGHT_SHIPPING;
    default:
      return DEFAULT_SHIPPING;
  }
}
