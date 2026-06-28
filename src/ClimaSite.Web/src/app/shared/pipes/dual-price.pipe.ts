import { Pipe, PipeTransform } from '@angular/core';
import { formatCurrency, formatNumber } from '@angular/common';

/**
 * Fixed Bulgarian lev peg: 1 EUR = 1.95583 BGN. This is the hard, legally-fixed conversion used for
 * Bulgaria's euro-adoption transitional dual display — NOT a live FX rate.
 */
export const BGN_PER_EUR = 1.95583;

/**
 * Renders a single EUR amount as the transitional dual display `€X.XX / Y.YY лв` (DEC-CURRENCY / UX-16).
 *
 * The store charges in EUR; BGN is shown alongside via the fixed {@link BGN_PER_EUR} peg. This is a drop-in
 * replacement for `| currency:'EUR'` on STORE prices (catalog / cart / checkout summary / admin KPIs). It is
 * deliberately NOT used for order-history/confirmation prices, which render the order's own charged currency.
 *
 * Pure (the peg is constant and `€`/`лв` are language-independent symbols), so it carries no perf cost over
 * the currency pipe it replaces. The EUR part matches today's `currency:'EUR'` output (en-US, e.g. `€1,234.56`).
 */
@Pipe({ name: 'dualPrice', standalone: true })
export class DualPricePipe implements PipeTransform {
  transform(eurAmount: number | null | undefined): string {
    if (eurAmount === null || eurAmount === undefined || Number.isNaN(eurAmount)) {
      return '';
    }
    const eur = formatCurrency(eurAmount, 'en-US', '€', 'EUR', '1.2-2');
    const bgn = formatNumber(eurAmount * BGN_PER_EUR, 'en-US', '1.2-2');
    return `${eur} / ${bgn} лв`;
  }
}
