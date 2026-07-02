---
status: reconstructed   # reverse-engineered from the existing codebase by /project-adopt — NOT a designed-first, ratified doc. Ratify via /design-doc.
created: 2026-06-23
source: project-adopt (Explore-fanout inventory of the existing codebase)
project: climasite
---

# ClimaSite — Reconstructed Design (as-built)

ClimaSite is a production-grade HVAC e-commerce platform: air-conditioning, heating, and cooling
equipment, multi-language (EN/BG/DE), multi-theme (light/dark). This doc captures the **current,
as-built** architecture inventoried during adoption — not an aspirational design. Ratify (or correct)
it with `/design-doc` before the next major build phase.

## System shape

- **Backend** — ASP.NET Core **.NET 10**, Clean Architecture in four projects: **Api** (HTTP host) →
  **Application** (CQRS via MediatR + FluentValidation + Mapster + pipeline behaviors) → **Core**
  (domain entities, value objects, repository interfaces) ← **Infrastructure** (EF Core 10 + Npgsql,
  repository impls, external-service clients, DI). Dependencies point inward; Core has none.
- **Frontend** — **Angular 19** standalone components + Signals, Tailwind, ngx-translate (EN/BG/DE),
  light/dark theming, lazy routes. Talks to the API over HTTP (`/api/*`).
- **Data + infra** — **PostgreSQL 16** (snake_case, JSONB for specs), **Redis 7** cache,
  **Stripe** payments (EUR), **MinIO** object storage (product images), **SMTP** email via a durable
  Postgres **outbox** + background worker, **JWT** auth (ASP.NET Identity).

## Component map (as-built)

Architectural building blocks are seeded as Knowledge nodes (`Knowledge/climasite/Components/`); the
feature-level catalogue below is the full inventory.

**Backend layers / cross-cutting:** `api-layer`, `application-layer`, `core-layer`,
`infrastructure-layer`, `database-context`, `repositories`, `current-user-service`,
`token-service` (JWT), `cache-service` (Redis), `email-service` (SMTP), `email-outbox`
(transactional outbox + `EmailOutboxBackgroundService`), `payment-service` (Stripe), `storage-service`
(MinIO).

**Application feature areas:** auth, products (catalog + HVAC recommendation scoring + price history +
related), categories, brands, cart (guest + merge-on-login), orders (create/track/cancel/invoice/
reorder + guest token lookup), checkout, payments (intent + webhook), inventory (variants/stock),
reviews (verified-purchase + moderation), questions (product Q&A), wishlist (sharing + public token +
merge), notifications (in-app), contact, addresses, admin (dashboard/CRUD/customers/installation),
installation (HVAC install requests), promotions, gdpr (export Art.15 / delete Art.17).

**Frontend areas:** `core-services-web`, `auth-service-web`, `layout-web`, `shared-components-web`,
and feature areas home-v3, products, categories, cart, checkout, account, admin, wishlist.

## External integrations

| Integration | Kind | Config (env / appsettings) |
|---|---|---|
| PostgreSQL (EF Core + Npgsql, `EnableRetryOnFailure(3)`) | db | `DATABASE_URL` / `ConnectionStrings:DefaultConnection` |
| Redis (StackExchange.Redis, `IDistributedCache`, 30-min TTL) | cache | `REDIS_URL` / `ConnectionStrings:Redis` |
| Stripe (PaymentIntents in EUR, webhook `whsec`) | payment | `Stripe:SecretKey`, `Stripe:PublishableKey`, `Stripe:WebhookSecret` |
| SMTP via outbox (`EmailOutboxBackgroundService`, ~15s poll, backoff, 5 retries) | email | `Email:Smtp*`, `Email:From*`, `Email:UsePlaceholder` |
| MinIO (S3-compatible, product images) | storage | `Minio:Endpoint/AccessKey/SecretKey/UseSSL/PublicUrl` |
| JWT + ASP.NET Identity (15-min access, 7-day refresh, lockout 5/15min) | auth | `JWT_SECRET/JWT_ISSUER/JWT_AUDIENCE` |

## Data flow

HTTP request → **Controller** instantiates a MediatR Query/Command → `IMediator.Send` runs the
Validation + Logging behaviors → **Handler** queries `ApplicationDbContext` (Postgres) through
repositories (eager-loads, filters, paginates) → DTOs (Mapster) → cached in Redis where applicable →
JSON. **State-changing** ops (orders, auth, contact, installation) enqueue `OutboxMessage` rows **in
the same DB transaction**; the background worker drains them to SMTP with retry/backoff. **Payments:**
the order total is computed **server-side in EUR** (`CheckoutPricing`); a Stripe PaymentIntent is
created/verified and `Order.PaymentIntentId` persisted only after server-side success verification
(idempotent via a filtered unique index). Guest carts use `Cart.SessionId` (7-day) and merge into the
user cart on login.

## Cross-cutting concerns

- **Auth/z** — JWT bearer; `[Authorize]`/role checks; `ICurrentUserService` from claims; guest flows
  use opaque tokens (orders confirmation, wishlist share).
- **i18n** — `?lang=en|bg|de` query param on the API (NOT Accept-Language); ngx-translate on the FE.
- **Theming** — all colors in `_colors.scss`; `--z-*` layering only in `_tokens.scss` (single source).
- **Validation/errors** — FluentValidation; errors currently a custom `{message}` shape (see gaps).
- **Logging** — Serilog console only (see gaps — no APM/metrics/tracing yet).
- **Testing** — xUnit (Core/Application/Api integration via Testcontainers, no mocks), Jasmine (FE),
  Playwright-for-.NET E2E; CI enforces 80/70 line coverage + the PROC-01 gate suite.

## Known gaps / divergences (feed Risks/Questions)

Open risks live in `Knowledge/climasite/Risks/` and the repo backlog `docs/project-plan/PRIORITIZED_BACKLOG.md`:

> _Gaps as of 2026-06-23; several since resolved — see `CHANGELOG.md` / `docs/project-plan/PRIORITIZED_BACKLOG.md` for current status._

- **R-001 / Q-002** No observability beyond console logging (no APM/metrics/tracing/error-monitoring).
- **R-002 (SEC-14) / Q-005** GDPR erasure leaves Orders PII (email/phone/addresses) — needs a retention ADR + scrub. — RESOLVED (SEC-14 anonymize-but-retain, ADR-0004, #69).
- **R-003 (SEC-12)** 7+ high-severity npm advisories (Angular 19 major upgrade pending).
- **R-004 (SEC-13)** gitleaks informational, not enforced.
- **R-005 (UX-15)** 3 serious dark-theme contrast violations (axe). — RESOLVED (fixed + A11Y_ENFORCE=1).
- **R-006** API errors are a custom shape, not RFC-7807 ProblemDetails.
- **R-007** Outbox worker is per-instance (no locking) — not safe for multi-instance deploys.
- **R-008 / Q-004** Config resolves from a mix of env vars + appsettings (dummy Stripe keys committed); prod-secret ownership undocumented. — PARTIAL (committed dummy Stripe keys removed via SEC-07; the config-precedence + secrets-ownership matrix is still undocumented — R-008 / Q-004 remain open).
- **Q-001** Nothing is deployed (Railway config exists, no live URL).
- **Q-003 (VERIFY)** Stock reservation: docs claim reservations but the field may be unused; confirm oversell protection. — RESOLVED (shipped as INV-01 #98–#102).
- **Q-006 (VERIFY)** SalePrice mapping flagged by the inventory pass; confirm against current code.

> **Already fixed — NOT open** (the adoption fanout read some stale paths; verified resolved in
> `CHANGELOG.md` / prior PRs): payment-intent persistence (BUG-02), server-side EUR charge amount
> (BUG-01/02/18), guest-cart merge contract (BUG-03), forgot-password email + no-token-logging (BUG-07),
> GDPR delete under the retry strategy (BUG-26). The money path is hardened (displayed == charged ==
> order total, verified-intent-before-persist, idempotent order creation).
