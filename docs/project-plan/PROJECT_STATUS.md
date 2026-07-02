# ClimaSite — Project Status (Single Source of Truth)

**Date:** 2026-06-26 (DOC-02 verification — supersedes the 2026-06-11 snapshot) · **Updated 2026-07-02** for INV-01 (#98–#102) + the #92–#97 features (SEO B-044, a11y, B-016, B-038/B-039 Q&A)
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
| Search & navigation | ✅ Complete | **Postgres FTS (SEARCH-01-fts)** — GIN-indexed trigger-maintained `search_vector` (base + tags + translations), `simple`+`unaccent` config, `ts_rank_cd` relevance + exact-SKU boost, pg_trgm substring fallback (recall superset). Both public paths unified through `IProductSearchService`. |
| Cart (guest + merge-on-login) | ✅ Complete | BUG-03 **fixed** — FE posts `/merge?guestSessionId=` (query) matching `[FromQuery]` (`cart.service.ts:236`). |
| Checkout (address/shipping/payment/review) | 🟡 Partial | Flow complete; **currency consistent EUR (BUG-11)** + **free standard shipping over €50 (DEC-SHIPPING)** with the order-summary reflecting the selected method (displayed==charged); **reserves stock at checkout-start (INV-01 A2, #100) — the losing last-unit buyer is blocked BEFORE the card is charged**; guest cart/checkout keyed on a server-minted signed guest-session cookie (INV-01 A0/A1, #98/#99). Remaining gap: no field-level validation errors (UX). |
| Payments (Stripe) | ✅ Complete | **Money path fixed** (BUG-01/02/18): server-side EUR amount, verified-intent-before-persist, atomic stock decrement w/ `stock>=qty` guard, idempotent (unique `payment_intent_id`), webhook reconcile, orphan-charge refund. |
| Orders (create/track/cancel/invoice/reorder, guest token) | ✅ Complete | Guest token-gated confirmation lookup (SEC-02). |
| Inventory + stock reservations | ✅ Complete | **INV-01 (5 waves, #98–#102)** — stock reserved at checkout-start (20-min TTL hold placed BEFORE the Stripe charge), `stock_reservations` ledger + denormalized `reserved_quantity` counter honored by every stock writer (per-variant FOR-UPDATE serialization, clock-independent counter, background sweeper + drift reconciler), reservation-aware availability on PDP/cart, bank-transfer hold-with-expiry + auto-restock. Follow-up: admin STATUS-endpoint cancel of a **paid** order does not yet restock/release (route through `CancelOrderCommand`). |
| Reviews & ratings + Q&A | ✅ Complete | Verified-purchase, moderation, auto-approve. Q&A votes now use a per-voter ledger (B-039, #94); answered-state derived solely from approved answers (B-038, #97). |
| Wishlist | ✅ Complete | Add/remove/share-token/public view + **guest-merge exists** (`wishlist.service.ts` + `auth.service.ts`). |
| In-app notifications | ✅ Complete | **Old "zero producers/frontend" claim REFUTED** — order-status producers + header bell + `NotificationService` + dropdown all present. |
| Admin panel (products/customers/orders/dashboard) | ✅ Complete | **Old "12-line stub" claim REFUTED** — real CRUD pages + admin API + KPI dashboard + role guard. |
| Installation requests | ✅ Complete | Business email (outbox) + admin list/manage. |
| Contact | ✅ Complete | Real `POST /api/contact` persists + outbox email. |
| Legal pages + cookie consent | ✅ Complete | terms/privacy/cookies/returns/shipping/FAQ + Impressum routes + consent banner. |
| Payment methods (card + bank transfer) | ✅ Complete | Real bank transfer (IBAN/reference + outbox email). |
| Email outbox | ✅ Complete | Durable Postgres outbox + worker (retry/backoff). R-007: per-instance worker (multi-instance locking deferred). |
| GDPR (export/delete) | ✅ Complete | Export + delete work (BUG-26 fixed); **SEC-14 done** — Orders PII anonymized on erasure, invoice record retained (ADR-0004, Art. 17(3)(b)), integration-tested. Follow-up: scheduled 7y retention-sweep + guest-order policy. |
| Home v3 (configurator + recommendations) | ✅ Complete | Real recommendations endpoint; recommendation spec-key aliasing + BTU recalibration (B-016, #96). |
| SEO (titles/meta/canonical/JSON-LD, robots+sitemap) | ✅ Complete | **B-044 (#92/#93)** — `SeoService` + `TitleStrategy` (translated per-route title/meta-desc/self-canonical/OG/Twitter/robots), `StructuredDataService` JSON-LD (Org/WebSite/Product/ItemList/FAQ/Breadcrumb), dynamic `~/robots.txt` + `~/sitemap.xml` (2285 URLs), fail-closed host-injection-safe base URL. Full SSR/prerender crawlability (DEC-SSR) is a separate owner decision. |
| Accessibility (axe) | ✅ Complete | **`A11Y_ENFORCE=1` live in CI** (hard gate); UX-15 contrast fixed; scans under reduced-motion; a11y dialogs/radiogroups hardened (B-001/B-014/B-042, #95). |
| Performance / Lighthouse | 🟡 Partial | Lighthouse runs every PR but **reporting-only** (warn + `\|\| true`), not enforced; some images lack `loading=lazy`. "Verified" was overstated. |
| Security posture | 🟡 Partial | JWT/lockout/rate-limiter/forwarded-headers + **response security headers done** (SEC-08); committed JWT secret removed + centralized issuance (SEC-05/B-011, #86); MediatR command-log credential redaction (#83); Stripe idempotency keys (PAY-IDEM, #84); per-user rate-limit partition on vote endpoints (#97). Remaining: deploy-time CORS allowlist + AllowedHosts + frontend Stripe-CSP (OPS-08), SEC-12/13. |
| Testing / CI | ✅ Complete | 6 required checks, 80/70 coverage enforced, Plan-19 E2E hardening (NetworkIdle purge + trace + guarded retry). |
| Deployment / ops | ❌ Absent | **OPS-08** nothing deployed; OPS-05 observability + OPS-11 merge queue (plan-blocked) outstanding. |

## 3. Disputed claims — resolved by this verification
- **Admin panel** = real CRUD (NOT 12-line stubs). **Notifications** = real producers + frontend bell (NOT "zero"). **Contact/Legal/Installation/GDPR-delete/Payments** = all verified complete. These were built in the M2 GAP work + later, *after* the 2026-06-11 snapshot.
- **Corrected verifier errors** (hand-checked): **BUG-03 cart-merge is FIXED**; **wishlist guest-merge EXISTS**. (Two agents claimed otherwise; the code says fixed/present.)
- **Confirmed still-open**: Lighthouse-reporting-only. *(inventory-has-no-reservations — now **FIXED**, INV-01 #98–#102; search-is-ILIKE — **FIXED**, Postgres FTS, SEARCH-01-fts; BUG-11 checkout-USD, SEC-14, SEC-08 — all **FIXED** since this snapshot.)*

## 4. Real remaining gaps (all already in `PRIORITIZED_BACKLOG.md`)
- ~~BUG-11 checkout/cart USD~~ ✅ **FIXED 2026-06-26** (single EUR + shipping labels match the server). Remaining currency work: **UX-16** dual EUR/BGN transitional display (P3, pre-BG-launch) + **DEC-SHIPPING** owner question (standard free vs €5.99).
- ~~**Inventory reservations** don't exist~~ ✅ **DONE 2026-07-02** (INV-01, #98–#102) — reserve-before-charge at checkout-start (FOR-UPDATE + clock-independent counter), bank-transfer hold-with-expiry, reservation-aware PDP/cart availability. Follow-up: paid-order admin-status cancel restock.
- ~~**Search** ILIKE → FTS~~ ✅ **DONE 2026-06-28** (SEARCH-01-fts — Postgres FTS, trigger-maintained `search_vector`, ts_rank_cd + pg_trgm hybrid; recall superset). Meilisearch not needed at this scale.
- ~~**SEC-14** GDPR Orders-PII anonymization~~ ✅ done (ADR-0004, #69); ~~**SEC-08** security headers~~ ✅ done. Remaining: **SEC-12/13** Angular advisories / gitleaks-enforce.
- **Lighthouse** flip warn→enforce + image lazy-load. **OPS-08** deploy · **OPS-05** observability · **OPS-11** merge queue (paid-plan-blocked).
- Plan-19 **B2/B3** (more Angular component specs). **DOC-05/06** tail (more ADRs, AGENTS.md regen).

## 5. Launch blockers (must clear before production)
1. **OPS-08** deploy + **OPS-05** observability (currently nothing is live).
2. **DEC-CURRENCY** — single EUR display is now consistent end-to-end (BUG-11 fixed); dual EUR/BGN (UX-16) only needed for a BG launch.
3. **SEC-12** dependency advisories (Angular 19 + Microsoft.OpenApi/Swashbuckle). *(SEC-14 GDPR Orders-PII + SEC-08 security headers are done.)*
4. **SEC-11** pre-launch pen-test pass + **PRODUCTION_READINESS_CHECKLIST.md** green.

> Inventory-reservation (INV-01) and search-FTS (SEARCH-01-fts) both shipped — the remaining launch blockers are deploy/observability/pen-test, not feature gaps.
