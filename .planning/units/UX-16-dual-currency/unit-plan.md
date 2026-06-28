---
unit: UX-16-dual-currency
type: NORMAL
status: approved
created: 2026-06-28
plan_status: approved
design: ../../design/DESIGN.md
---

# Unit plan — UX-16: transitional dual EUR/BGN price display

## Context / Definition of Ready
DEC-CURRENCY (owner, 2026-06-15): the store + Stripe **charge** currency is **EUR**; for Bulgaria's
euro-adoption transition the frontend must **also display BGN** alongside EUR at the **fixed peg
1 EUR = 1.95583 BGN** (a constant, not a live rate). BUG-11 (#64) made all store prices render single EUR
(`DEFAULT_CURRENCY_CODE='EUR'` + `| currency:'EUR'`). UX-16 adds the dual display via ONE shared formatter.

Scope (verified 2026-06-28):
- **78 static `| currency:'EUR'`** renders across 22 component files (catalog/cart/checkout/admin-KPIs/
  shared widgets) → these are the dual-display targets.
- **22 dynamic `| currency:order.currency`** renders (order-confirmation / account order-details / admin
  order-detail) → **NOT touched**: they render the order's OWN charged currency (the order-confirmation spec
  asserts a USD order), which is a different concern from store prices.

## Decision (format — the "small UX call", made per owner "no questions")
Inline `€X.XX / Y.YY лв` (EUR primary since the store charges EUR; BGN secondary via the peg). Standard,
legally-clear BG dual display. No tooltip. `лв` and `€` are fixed symbols (language-independent) → no new
i18n keys. EUR part formatted exactly like today's `currency:'EUR'` (`en-US` locale → `€1,234.56`).

## Scope / approach
1. **New shared pure pipe** `DualPricePipe` (`name: 'dualPrice'`, standalone) at
   `src/app/shared/pipes/dual-price.pipe.ts` + exported peg constant `BGN_PER_EUR = 1.95583`.
   `transform(eur: number|null|undefined): string` → `formatCurrency(eur,'en-US','€','EUR','1.2-2') + ' / ' +
   formatNumber(eur*BGN_PER_EUR,'en-US','1.2-2') + ' лв'`; null/undefined/NaN → `''`. Pure (peg is constant;
   symbols are language-independent), so it's a drop-in for `| currency:'EUR'` with no perf regression.
2. **Replace** all 78 `| currency:'EUR'` → `| dualPrice` (templates only; `app.config.ts`'s
   `DEFAULT_CURRENCY_CODE` provider is untouched). Add `DualPricePipe` to each affected standalone
   component's `imports: [...]` + the import statement.
3. **Leave** the 22 dynamic `| currency:order.currency` renders as-is.
4. Update the 2 specs that assert a `€`-only price string (`checkout`, `cart`) to the dual format.
5. New `dual-price.pipe.spec.ts` (peg math, formatting, grouping, null/zero).

## Acceptance criteria
- [ ] A single shared `DualPricePipe` is the only store-price formatter; **0** `| currency:'EUR'` remain
      (`grep` acceptance), and the 22 order-currency renders are unchanged.
- [ ] Every store price renders `€X.XX / Y.YY лв` at the 1.95583 peg (e.g. `€99.99 / 195.55 лв`).
- [ ] Pipe unit-tested (incl. peg math, grouping, null/zero); all existing frontend tests green; i18n check passes.
- [ ] Works in light/dark + EN/BG/DE (symbols are language-independent; no layout break — verify in /acceptance).

## Test / verification plan
- `dual-price.pipe.spec.ts`: `transform(99.99)` → `'€99.99 / 195.55 лв'`; `transform(1000)` →
  `'€1,000.00 / 1,955.83 лв'`; `transform(0)` → `'€0.00 / 0.00 лв'`; `null`/`undefined`/`NaN` → `''`;
  `BGN_PER_EUR === 1.95583`.
- Update `cart`/`checkout` specs to the dual format. Run `npm test -- --watch=false --browsers=ChromeHeadless`.
- `/acceptance`: drive the running app — product card/list/detail, cart, mini-cart, checkout summary all show
  `€ / лв`; no layout breakage on cards; light+dark; EN/BG/DE.

## Out of scope
Order-history/confirmation/admin-order prices (render the order's charged currency); a user currency-toggle;
live FX. Only needed before a BG launch (OPS-08).

## DoD gates
Green CI (6 checks) · cross-vendor council (non-trivial UI change, 23 files) · committed `/acceptance` PASS ·
update CHANGELOG / STATE / BACKLOG (mark UX-16 done).
