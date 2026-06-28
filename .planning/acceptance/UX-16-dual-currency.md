---
unit: UX-16-dual-currency
surface: ui
result: PASS
date: 2026-06-28
commit: feature/ux-16-dual-currency tip (final squash tip on merge)
driver: headless real-browser (node Playwright/chromium) against the running app — API
  (ASPNETCORE_ENVIRONMENT=Development, fresh seeded demo catalog) on :5029 + `ng serve` on :4200
---

# Acceptance — UX-16 (dual EUR/BGN price display)

## Environment
- Real running stack: Angular `ng serve` (:4200) → API in **Development** (:5029) against a fresh Postgres 16
  (real DataSeeder demo catalog). Driven headless via the web project's node Playwright (the Chrome extension
  was not connected).

## Scenarios
| # | Scenario | Result |
|---|---|---|
| 1 | **Product list** — every product price | ✅ all render `€X.XX / Y.YY лв` (`listDualOk: true`) |
| 2 | **Peg math (live render)** | ✅ `€149.99 / 293.35 лв`, `€999.99 / 1,955.81 лв` — matches 1.95583 |
| 3 | **On-sale card** | ✅ sale price + struck-through original BOTH dual (`€249.99 / 488.94 лв` + `€299.99 / 586.73 лв`) |
| 4 | **Product detail** | ✅ dual price present (`detailHasDual: true`) |
| 5 | **Cart page** | ✅ loads without error |
| 6 | **Layout (screenshot)** | ✅ grid/cards intact; single-price cards one line |
| 7 | **Order prices NOT converted** | ✅ (by construction — dynamic `currency:order.currency` left untouched; covered by unchanged order-confirmation USD spec) |

## Nits (tracked, non-blocking)
- **Sale-price wrap:** the dual string is ~2× longer, so on-sale prices wrap to two lines on cards (e.g.
  `€249.99 /` / `488.94 лв`). Readable and unbroken — acceptable for the transitional display. Polish
  follow-up: `white-space: nowrap` on the price (or a smaller BGN part) if tighter cards are wanted.

## Verdict
**PASS** — zero blocker, zero major; one minor (sale-price wrap) tracked as a follow-up. The transitional
dual EUR/BGN display renders correctly across catalog/detail/cart with correct peg math and on-sale handling;
order-currency renders are untouched. Automated: 1392 frontend tests + the pipe spec + production AOT build,
all green; council pass.
