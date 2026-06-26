# ClimaSite — Project Status (Single Source of Truth)

**Date:** 2026-06-26 (DOC-02 verification — supersedes the 2026-06-11 snapshot)
**Method:** Per-feature status **verified against the working tree** by a parallel code-read across 4
clusters (storefront, accounts/engagement, admin/ops/content, cross-cutting/platform), with the
highest-stakes / contradictory findings hand-spot-verified. Where this file and any older doc disagree,
**this file wins**; for actionable work see `PRIORITIZED_BACKLOG.md`; for the live resume contract see
`.planning/STATE.md`; for change history see `CHANGELOG.md`.

> **Why a refresh was needed:** the prior 2026-06-11 snapshot predated the M2 GAP features, the PROC-01
> SDLC-hardening initiative, the vault workflow adoption, and Plan 19 — so its "partial/missing" claims
> for Admin, Notifications, Contact, Legal, Installation, GDPR were stale. The removed CLAUDE.md tables
> had drifted the *other* way (overclaiming). This pass reconciles both against code.

---

## 1. Project overview

ASP.NET Core (.NET 10), Clean Architecture + CQRS/MediatR (`Api`/`Application`/`Core`/`Infrastructure`),
PostgreSQL 16 (EF Core 10), Redis, Stripe (EUR), MinIO, a durable Postgres email **outbox** + background
worker; Angular 19 standalone + Signals frontend, Tailwind, ngx-translate (EN/BG/DE), light/dark.
Testing: xUnit + Jasmine + Playwright-for-.NET E2E; CI gates 6 checks + 80/70 coverage + enforced a11y.
Repo `byrzohod/climasite` is **public**; **nothing is deployed yet (OPS-08)**.

## 2. Verified feature status (2026-06-26)

| Area | Status | Notes (verified against code) |
|---|---|---|
| Design system / theming (light+dark) | ✅ Complete | `_colors.scss` single source; `--z-*` in `_tokens.scss`. |
| i18n EN/BG/DE | ✅ Complete | ngx-translate; full key parity. |
| Auth (register/login/refresh/lockout/reset) | ✅ Complete | Password-reset email sent via outbox; token not logged (BUG-07). |
| Product catalog (PLP/PDP, variants, gallery) | ✅ Complete | Variants/images/translations/specs. |
| Search & navigation | 🟡 Partial | Works, but **multi-term ILIKE substring** (no tsvector/pg_trgm/Meilisearch). Fine for MVP; **P2 scale gap**. |
| Cart (guest + merge-on-login) | ✅ Complete | BUG-03 **fixed** — FE posts `/merge?guestSessionId=` (query) matching `[FromQuery]` (`cart.service.ts:236`). |
| Checkout (address/shipping/payment/review) | 🟡 Partial | Flow complete; gaps: **BUG-11** checkout `\| currency` defaults to **USD** (cards use `:'EUR'`) + no field-level validation errors. |
| Payments (Stripe) | ✅ Complete | **Money path fixed** (BUG-01/02/18): server-side EUR amount, verified-intent-before-persist, atomic stock decrement w/ `stock>=qty` guard, idempotent (unique `payment_intent_id`), webhook reconcile, orphan-charge refund. |
| Orders (create/track/cancel/invoice/reorder, guest token) | ✅ Complete | Guest token-gated confirmation lookup (SEC-02). |
| Inventory | 🟡 Partial | Stock tracked + atomic decrement guard exists; **"reservations" are NOT implemented** (no reserve/hold) — known gap. |
| Reviews & ratings + Q&A | ✅ Complete | Verified-purchase, moderation, auto-approve. |
| Wishlist | ✅ Complete | Add/remove/share-token/public view + **guest-merge exists** (`wishlist.service.ts` + `auth.service.ts`). |
| In-app notifications | ✅ Complete | **Old "zero producers/frontend" claim REFUTED** — order-status producers + header bell + `NotificationService` + dropdown all present. |
| Admin panel (products/customers/orders/dashboard) | ✅ Complete | **Old "12-line stub" claim REFUTED** — real CRUD pages + admin API + KPI dashboard + role guard. |
| Installation requests | ✅ Complete | Business email (outbox) + admin list/manage. |
| Contact | ✅ Complete | Real `POST /api/contact` persists + outbox email. |
| Legal pages + cookie consent | ✅ Complete | terms/privacy/cookies/returns/shipping/FAQ + Impressum routes + consent banner. |
| Payment methods (card + bank transfer) | ✅ Complete | Real bank transfer (IBAN/reference + outbox email). |
| Email outbox | ✅ Complete | Durable Postgres outbox + worker (retry/backoff). R-007: per-instance worker (multi-instance locking deferred). |
| GDPR (export/delete) | 🟡 Partial | Export + delete work (BUG-26 fixed); **SEC-14 open** — Orders PII not anonymized on erasure (needs an ADR). |
| Home v3 (configurator + recommendations) | ✅ Complete | Real recommendations endpoint. |
| Accessibility (axe) | ✅ Complete | **`A11Y_ENFORCE=1` live in CI** (hard gate); UX-15 contrast fixed; scans under reduced-motion. |
| Performance / Lighthouse | 🟡 Partial | Lighthouse runs every PR but **reporting-only** (warn + `\|\| true`), not enforced; some images lack `loading=lazy`. "Verified" was overstated. |
| Security posture | 🟡 Partial | JWT/lockout/rate-limiter/forwarded-headers done; **SEC-08** response security headers (CSP/X-Frame/etc.) thin; SEC-12/13 open. |
| Testing / CI | ✅ Complete | 6 required checks, 80/70 coverage enforced, Plan-19 E2E hardening (NetworkIdle purge + trace + guarded retry). |
| Deployment / ops | ❌ Absent | **OPS-08** nothing deployed; OPS-05 observability + OPS-11 merge queue (plan-blocked) outstanding. |

## 3. Disputed claims — resolved by this verification
- **Admin panel** = real CRUD (NOT 12-line stubs). **Notifications** = real producers + frontend bell (NOT "zero"). **Contact/Legal/Installation/GDPR-delete/Payments** = all verified complete. These were built in the M2 GAP work + later, *after* the 2026-06-11 snapshot.
- **Corrected verifier errors** (hand-checked): **BUG-03 cart-merge is FIXED**; **wishlist guest-merge EXISTS**. (Two agents claimed otherwise; the code says fixed/present.)
- **Confirmed still-open**: BUG-11 (checkout shows USD), search-is-ILIKE, inventory-has-no-reservations, Lighthouse-reporting-only, SEC-14 (GDPR Orders-PII), SEC-08 (security headers).

## 4. Real remaining gaps (all already in `PRIORITIZED_BACKLOG.md`)
- **BUG-11** checkout/cart `| currency` defaults to USD — set `DEFAULT_CURRENCY_CODE`/`:'EUR'`, kill bare pipes (ties to DEC-CURRENCY).
- **Inventory reservations** don't exist (oversell window narrowed by the atomic guard, but no hold/reserve).
- **Search** ILIKE → FTS/Meilisearch (P2 scale).
- **SEC-14** GDPR Orders-PII anonymization (ADR + handler scrub). **SEC-08** security headers. **SEC-12/13** Angular advisories / gitleaks-enforce.
- **Lighthouse** flip warn→enforce + image lazy-load. **OPS-08** deploy · **OPS-05** observability · **OPS-11** merge queue (paid-plan-blocked).
- Plan-19 **B2/B3** (more Angular component specs). **DOC-05/06** tail (more ADRs, AGENTS.md regen).

## 5. Launch blockers (must clear before production)
1. **OPS-08** deploy + **OPS-05** observability (currently nothing is live).
2. **BUG-11 / DEC-CURRENCY** — one consistent display currency end-to-end.
3. **SEC-14** GDPR Orders-PII (compliance) + **SEC-08** security headers + **SEC-12** advisories.
4. **SEC-11** pre-launch pen-test pass + **PRODUCTION_READINESS_CHECKLIST.md** green.

> Inventory-reservation + search-FTS are **scale/robustness**, not hard launch blockers for a small catalog.
