# ClimaSite — Consolidated Roadmap

**Date:** 2026-06-11
**Sources:** `docs/project-plan/PROJECT_STATUS.md`, `docs/project-plan/PRIORITIZED_BACKLOG.md`, and the ten verified review files in `docs/project-plan/_review/`; reconciled against `docs/plans/18-project-completion.md` and `docs/plans/00-master-overview.md`.

**How to use this document:** This is the single planning entry point from today to launch. It groups the deduplicated backlog (`PRIORITIZED_BACKLOG.md`) into four ordered milestones with explicit exit criteria, so you always know *what unblocks what* and *what "done" means* for each phase. Read `PROJECT_STATUS.md` first for the current state, then this for the order of work, then `PRIORITIZED_BACKLOG.md` for the task-level detail (every task ID below — `OPS-01`, `BUG-02`, `SEC-03`, …— is defined there with files, acceptance criteria, and evidence). When this roadmap and an older plan disagree about order or scope, this roadmap wins.

---

## Relationship to Plan 18 (it supersedes, it does not discard)

`docs/plans/18-project-completion.md` remains a valid feature plan and its task IDs (`SEC-100..107`, `PROD-100..107`, `PERF-100/104`, `NOT-100..111`, `ANIM-*`) are still referenced by the backlog and absorbed below. **This roadmap supersedes Plan 18 as the planning hub** for two concrete reasons established by the review:

1. **Plan 18 does not track the actual launch blockers.** The broken Stripe money path (`BUG-01/02/04/18`), the "Coming Soon" admin UIs (`GAP-01/02`), the publicly-seeded admin credential (`SEC-01`), the missing `UseForwardedHeaders` (`SEC-03`), and the always-400 cart merge (`BUG-03`) are *absent* from Plan 18's phases. They are the highest-priority work and they live here and in the backlog.
2. **Plan 18 Phases 0–1 are done; Phase 2 is half-done.** Home v3 shipped; the Wishlist slice is complete but **uncommitted** (`OPS-01`); Notifications (`GAP-09`) and the Animation Audit 21F remainder are still open. Those survive into M2/M4 below.

Plan 18's still-valid security/perf/prod tasks are mapped into M1–M3 by ID. Do **not** start Plan 18 Phase 5/6/7 as written before the M1 blockers clear — several of its tasks are no-ops until then (e.g. SEC-104 rate limits are pointless before SEC-03 forwarded headers land).

---

## Where we are (one paragraph)

ClimaSite is **structurally healthy but functionally pre-production**: clean Clean-Architecture/CQRS backend, signal-based Angular frontend, clean builds, a real green test skeleton, and a strong browse-to-cart customer experience. It is **not launchable** because the payment path is broken end-to-end (wrong amount/currency charged, payments never reconciled to orders), the shop cannot be operated (admin order/product/customer pages are stubs), no transactional emails are ever sent, every footer legal page 404s (an EU blocker), production startup seeds a publicly-known admin password, and the documentation actively misdescribes the test commands, ports, and several "Complete" features. The work below is sequenced so that each milestone removes a class of "cannot launch" reason.

---

## Blocking decisions (resolve these to unblock the milestones)

These are **owner decisions** (per the standing convention that the user decides stack/product-level choices); each is defined in `DECISIONS.md` §3 and gates the tasks named. Resolve the M1 set *now* — `DEC-CURRENCY` and `OPS-08` block the single most important wave of work.

| Ref | Decision | Blocks | Needed by |
|---|---|---|---|
| **DEC-CURRENCY** | Store currency: **EUR or BGN?** (code mixes EUR/BGN/USD today) | BUG-01, BUG-02, BUG-04, BUG-11, BUG-13 | **M1 (now)** |
| **OPS-08 / O-7** | Railway topology: is anything **deployed today**, which services/configs, backups, replica count? | SEC-01 urgency, SEC-03 proxy config, OPS-03/04, BUG-09 | **M1 (now)** |
| DEC-GUEST | Guest checkout in scope for v1? | GAP-07, TS-13 | M2 |
| O-1 | Background-job mechanism (BackgroundService+outbox vs Hangfire vs RabbitMQ) | ARCH-05, GAP-03 reliability, GAP-09 | M2 |
| O-4 | Observability vendor (Sentry suggested) + OpenTelemetry scope | OPS-05 | M3 |
| O-2 | API error contract: ProblemDetails vs ratify current shape | ARCH-04 | M3/M4 |
| O-3 | Query-caching owner: register `CachingBehavior` vs delete it | PERF-01 | M3 |
| O-6 | Shared-infra vs project-local docker-compose for local dev | OPS-10 | M3 |
| DEC-SEARCH | Real search backend (Postgres FTS / pg_trgm / Meilisearch) | PERF-05 | M4 |
| DEC-SSR | SSR/prerender for the storefront vs client-only meta | PERF-07, OPS-03 topology | M4 |
| O-5 | ADR 003 (Canvas 2D) + wishlist share-token ADR | DOC-05, ideally before OPS-01 merge | M1/M4 |

---

## Milestone M1 — Stabilize & Secure the core *(do first; this is "Now")*

**Goal:** Make the money path correct, stop the two guaranteed production incidents, stop silent customer-facing data loss, and turn CI from advisory into a real gate — so that no further work lands on a broken or unsafe foundation.

**Tasks (in execution order — this is the backlog's "Next 10" plus their immediate riders):**

| Order | Task | Why it's M1 | Complexity |
|---|---|---|---|
| 1 | **OPS-01** — commit/push/PR the wishlist slice (riders: SEC-10, BUG-09, BUG-10, UX-02) | 4 days of finished work is one `git checkout .` from total loss; PR's CI run closes TS-01 | Small |
| 2 | **SEC-01** — gate DataSeeder; bootstrap prod admin from env (with **OPS-08**) | Public repo publishes `admin@climasite.local`/`Admin123!`; active incident if anything is deployed | Small |
| 3 | **OPS-02** — protect `main`; fix CLAUDE.md direct-push mandate | Turns CI into a gate before the fix wave lands; red commits already reach main today | Small |
| 4 | **DOC-01** — fix test commands/ports/paths in CLAUDE.md + AGENTS.md | Every later task needs runnable commands; mandatory workflow is unexecutable as written | Small |
| 5 | **BUG-02** — server-side charge: correct amount, one currency, shipping included (needs **DEC-CURRENCY**) | Every honest order underpays ~49%; €0.01 manipulation is trivial | Medium |
| 6 | **BUG-01** — persist `paymentIntentId`; webhook reconciles orders (rider **BUG-18**) | 100% of card orders are charged-but-stuck-Pending with no recovery | Small |
| 7 | **TS-03** — regression tests bundled with BUG-01/02/03 | The bugs shipped under green suites; fixes must come with the tests that would have caught them | Medium |
| 8 | **BUG-03** — fix guest-cart merge 400 | Deterministic silent cart loss for every first-time customer at login | Small |
| 9 | **SEC-03** — `UseForwardedHeaders` before the rate limiter | Whole store shares one 100 req/min bucket behind nginx — guaranteed launch-day outage | Small |
| 10 | **BUG-07** — send password-reset email; stop logging the token | Users locked out forever; live reset tokens sit in logs (SR-05) | Small |
| 11 | **BUG-04** — order-before-charge / compensation + double-submit guard | Captured charges with no order, no refund; double-charge on double-click | Medium |
| 12 | **BUG-05** — stock-decrement concurrency (oversell) | Read-then-write decrement oversells the last unit; "reservations" are fictional | Medium |
| 13 | **BUG-06** — un-invert sale-price mapping sitewide | Higher price shown as the deal price everywhere; consumer-protection exposure | Medium |
| 14 | **SEC-02** — fix order-by-number IDOR / PII leak | Anonymous caller reads any customer's full PII; order numbers semi-predictable | Small |
| 15 | **ARCH-01** — delete dead `Features/Auth` tree; retarget its tests; add validators | Live auth handlers have zero unit coverage; must precede any other auth work | Medium |

**Exit criteria:**
- `DEC-CURRENCY` recorded as an ADR; for any cart, **displayed total == Stripe charge == `Order.Total`** in one currency, shipping included, verified by an automated test.
- A simulated `payment_intent.succeeded` flips an order to **Paid**; no card order remains stuck Pending; webhook reconciliation covered by integration tests.
- Production startup against an empty DB creates **no** `admin@climasite.local` and no demo catalog; `OPS-08` answered and any live exposure remediated same-day.
- `main` rejects direct pushes and requires the Test Suite checks; CLAUDE.md/AGENTS.md describe the same PR flow.
- Every command block in CLAUDE.md / AGENTS.md / `tests/ClimaSite.E2E/AGENTS.md` runs on a fresh checkout; `grep` for `npx playwright`/`localhost:5000`/`test-data-factory.ts` returns nothing outside archives.
- Guest adds items → logs in → items survive (E2E); rate-limit buckets are per-client behind nginx; password reset delivers a working link via MailHog; on-sale products show the correct active price; the wishlist slice is merged to main via PR with green CI.

---

## Milestone M2 — Operable & Compliant *(this is "Next")*

**Goal:** Make the shop runnable by a human operator and legally sellable in the EU (BG/DE). After M1 the money is correct; M2 makes orders fulfillable and the storefront compliant.

**Tasks:**
- **GAP-01** (P0, Large) — admin **orders** page: list, status transitions, tracking number. The minimum-operability slice; unblocks shipped-email notifications.
- **GAP-02** (P1, Large) — admin **products** CRUD + **customers** page + dashboard KPIs; wire the orphaned `product-translation-editor`/`related-products-manager` (closes BUG-14).
- **ARCH-05** (P2, Large, needs **O-1**) — background-job mechanism + email **outbox** processor. Do early in M2: it upgrades the email work from fire-and-forget to reliable and unblocks GAP-09.
- **GAP-03** (P1, Medium) — wire transactional emails: order confirmation, shipped, welcome, admin notify-customer (closes BUG-16).
- **GAP-04** (P1, Medium) — legal pages (terms/privacy/cookies/returns/shipping/FAQ + German Impressum) + cookie consent; fix dead footer/social links.
- **GAP-05** (P1, Small) — real `POST /api/contact` endpoint (form currently fakes success).
- **GAP-06** (P1, Small) — remove or finish fake PayPal/bank-transfer payment methods.
- **BUG-13** (P2, Medium) — country-based VAT (DE = 19%) on goods + shipping.
- **UX-01** (P1, Small) — checkout shipping form field-level validation errors (highest-value form, currently silent).
- **TS-04** (P1, Medium) — Stripe card-path E2E (test card via FrameLocator) + Payments/Webhooks integration tests.
- **TS-05** (P1, Medium) — GDPR integration tests (export completeness, irreversible-delete anonymization, authz) — land **before** GAP-10 exposes the delete button.
- **GAP-10** (P2, Medium) — GDPR self-service UI in `/account`.
- **GAP-12** (P2, Medium) — email-verification: enforce or remove (decide policy, SR-20).
- **GAP-07** (P1, decision+Medium, needs **DEC-GUEST**) — guest checkout: enable end-to-end or descope and fix docs.
- **GAP-08** (P1, Medium) — installation requests: notify the business + make them viewable (email-only can ship first).

**Exit criteria:**
- An admin can mark an order **Shipped with a tracking number** and the customer sees it in `/account/orders/:id`; the "via API since UI may be placeholder" E2E workarounds are replaced by real UI E2E.
- Placing an order sends a confirmation email (asserted in MailHog E2E); shipping sends a tracking email; failures are logged without breaking the order.
- All six footer legal links render translated content (EN/BG/DE, both themes); cookie consent blocks non-essential storage until accepted; the contact form persists/emails a real message.
- DE addresses are charged 19% VAT on goods + shipping; only payable payment methods are offered.
- Stripe card payment is covered by CI in test mode; GDPR delete is tested at every layer; the guest-checkout docs and behavior agree.

---

## Milestone M3 — Production-Ready *(this is "Soon")*

**Goal:** Make deploys safe, repeatable, observable, and reversible; raise the security floor to launch grade; close the test gates. After M3 the system can be shipped to production with confidence and operated when something breaks.

**Tasks:**
- **OPS-03** (P1, Medium, needs **OPS-08**) — `deploy.yml` + one canonical Dockerfile/Railway config per service; delete the duplicates.
- **OPS-04** (P1, Medium) — stop `MigrateAsync` at startup in prod; dedicated pre-deploy migration step + rollback story.
- **OPS-05** (P1, Medium, needs **O-4**) — observability floor: correlation IDs (`X-Correlation-Id`), structured JSON logs, error tracker, alerts, `Log.CloseAndFlush`.
- **OPS-06** (P2, Medium) — CI hardening: lint, analyzers, docker-build, dependency scanning (`npm audit`/`dotnet list package --vulnerable`/dependabot/CodeQL), concurrency, real-bundle E2E; fix the nonexistent-`develop`-branch trigger.
- **OPS-07** (P2, Small) — non-root containers; web entrypoint fails hard on unset `API_URL`.
- **OPS-09** (P2, Small) — cut the first `v0.x` tag via `/release`; wire image tagging.
- **OPS-10** (P2, Small, needs **O-6**) — resolve local-dev bootstrap (shared-infra vs project compose).
- **SEC-04..SEC-08** (P2) — exclude TestController from Release, remove committed JWT secret, gate Swagger, standardize secret env-var names + remove dummy Stripe keys, security-headers middleware + CORS/HSTS tightening (absorbs Plan 18 SEC-101/102/103).
- **SEC-11** (P1, Medium) — run the manual pen-test checklist on a deployed staging env.
- **PERF-01** (P1, Medium, needs **O-3**) — one caching owner: activate or delete the dead MediatR/Redis layer; kill the blanket 5-min OutputCache (also closes the revoked-share-link window).
- **PERF-02** (P1, Small) — facet-query projection + pagination clamp (DoS amplification fix).
- **TS-06** (P1, Medium) — integration tests wave 1 (Orders/Payments/Webhooks/Gdpr).
- **TS-07** (P2, Medium) — coverage measurement + thresholds (backend 80 / frontend 70), baseline then enforce.
- **TS-08** (P1, Large) — Application unit tests (Cart/Inventory/Reviews + sale-price mapper).
- **TS-09** (P2, Medium) — frontend specs for the riskiest units (auth interceptor/guard, payment, checkout, cart) — pairs with **BUG-08** (auth refresh storm).
- **TS-11** (P2, Medium) — E2E reliability (observable cleanup, parallel collections, NotExecuted mystery).
- **BUG-08** (P1, Small) — shared single in-flight token refresh (stop logging users out on concurrent 401s).
- **BUG-17** (P3, Small) — defuse hard-delete traps (cart `First()` crash, `SetNull` on non-nullable FKs).
- **DOC-02** (P1, Small) — status truth pass on CLAUDE.md + master overview.
- **DOC-03** (P2, Medium) — README + deployment runbook.

**Exit criteria:**
- Exactly one Dockerfile + one Railway config per service; `deploy.yml` runs only after the test suite passes; an explicit migration step runs pre-deploy (zero schema changes at app startup); a CI job applies all migrations to a clean Postgres and rolls back the latest.
- Every API response carries `X-Correlation-Id`; a thrown test exception appears in the error tracker; healthcheck-based uptime alerting is live.
- CI fails on lint errors, vulnerable packages, and broken Dockerfiles; coverage gates are enforced (after a baseline sprint); pen-test checklist items each have a recorded pass/fail + remediation ticket.
- Caching has one owner with a working invalidation story (or the dead code is gone); `pageSize=100000` is clamped; a `v0.x` tag and matching CHANGELOG section exist as the first rollback target.

---

## Milestone M4 — Feature-Complete & Polish *(this is "Later")*

**Goal:** Finish or formally descope the remaining features, pay down the identified tech debt, and complete performance depth and UI polish. Nothing here blocks launch; it raises quality and removes traps for future development.

**Tasks (grouped):**
- **Features / gaps:** GAP-09 (notifications system — needs O-1/ARCH-05 + GAP-01), GAP-11 (triage implemented-but-unreachable: compare/recently-viewed/price-history/inventory-admin), GAP-13 (coupons/discount codes + admin promotions).
- **Performance depth:** PERF-03 (query-shape fixes — bundle with BUG-06/BUG-12), PERF-04 (CD hygiene + route preloading + Plan 18 PERF-100 OnPush≥70%), PERF-05 (real search backend — DEC-SEARCH), PERF-06 (image pipeline: WebP/srcset/NgOptimizedImage), PERF-07 (SSR/prerender — DEC-SSR; ship per-route meta now regardless).
- **Architecture / debt:** ARCH-02 (dead-scaffolding sweep: repositories/Mapster/6 directives ≈1,800 lines), ARCH-03 (consolidate the two migrations folders), ARCH-04 (one API error contract — O-2), ARCH-06 (split the five god components), ARCH-07 (real-provider test fixture).
- **UI/UX polish:** UX-02..UX-14 (wishlist error toasts, scroll restoration, header/footer defer, account/PDP error states, WCAG error color, one breadcrumb, breakpoint tokens, OOS CTA, a11y polish, `/categories`, energy-label palette, offline state).
- **Security depth:** SEC-09 (hash refresh tokens — after ARCH-01), SEC-10 (drop owner UserId from shared wishlist DTO — ideally a rider on OPS-01 in M1).
- **Correctness/i18n:** BUG-10 (translate cart/wishlist product names), BUG-12 (real `AverageRating`/`ReviewCount`).
- **Testing breadth:** TS-10 (integration wave 2), TS-14 (integration wave 3 — after admin UI), TS-15 (remaining frontend specs), TS-16 (test hygiene), TS-17 (CI migration job).
- **Docs:** DOC-04 (tracker consolidation — issue registry, validation suite, plan numbering, Plan 12 reconciliation), DOC-05 (ADR backfill + repair ADR 002 integrity), DOC-06 (agent-context sync across `.claude`/`.codex`/`.opencode`).

**Exit criteria:** Plan 18 Phase 8's ≥8-ADR target met; every implemented-but-unreachable feature is either wired with an E2E test or documented as headless-by-design; OnPush ≥70%; one error contract; one migrations folder; one breadcrumb; the storefront serves per-route meta; the dead scaffolding is deleted and the build stays green. At this point CLAUDE.md's status table is true and `/release` can cut `v1.0.0`.

---

## Now / Next / Later (at a glance)

| Horizon | Milestone | Headline outcome | Gating decisions |
|---|---|---|---|
| **Now** | M1 — Stabilize & Secure | Money path correct; no published admin creds; CI is a gate; first-time carts survive login | DEC-CURRENCY, OPS-08 |
| **Next** | M2 — Operable & Compliant | Orders are fulfillable; emails send; EU legal floor present | DEC-GUEST, O-1 |
| **Soon** | M3 — Production-Ready | Safe repeatable observable deploys; security floor; test gates | O-4, O-3, O-6 |
| **Later** | M4 — Feature-Complete & Polish | Remaining features finished/descoped; debt paid; perf + UI polish | DEC-SEARCH, DEC-SSR, O-2 |

---

## Dependency callouts (the edges that bite if ignored)

- **DEC-CURRENCY gates the entire payment fix.** Do not start BUG-01/02/04/11/13 before it is decided and recorded as an ADR.
- **OPS-08 (is anything deployed?) changes SEC-01 from latent to active.** Answer it on day one; if a prod DB was ever seeded, rotate/delete `admin@climasite.local` immediately.
- **ARCH-01 must precede all other auth work** (SEC-09, BUG-08 backend) — today the unit tests target a dead duplicate auth tree, so changes to the live handlers are unverified.
- **SEC-03 before any rate-limit tuning** (Plan 18 SEC-104) — per-endpoint limits are meaningless while all clients share one IP bucket.
- **ARCH-05 (background jobs) before GAP-09 (notifications)** and ideally before GAP-03 hardening — there is no job infrastructure to emit/retry on today.
- **GAP-01 (admin orders) before GAP-03 shipped-emails** — you cannot send a "shipped" email until an operator can mark an order shipped.
- **ARCH-03 (consolidate migrations) before TS-17 and any new migration** (BUG-17, SEC-09) — otherwise new migrations land in the wrong folder.
- **OPS-02 (protect main) before the fix wave** — otherwise red commits keep landing on main during the busiest period of change.
- **TS-05 (GDPR tests) before GAP-10 (GDPR UI)** — never expose an untested irreversible delete to users.
- **Riders on OPS-01:** SEC-10, BUG-09, BUG-10, UX-02 are cheapest while the wishlist branch is still uncommitted — fold them into that PR if time allows.

---

## What NOT to do yet

- **Do not refactor the five god components (ARCH-06) or change the API error contract (ARCH-04) during M1/M2.** Both touch the money path and auth chain; they are M4 and gated on tests existing first.
- **Do not enable query caching (PERF-01) without the invalidation story** — a blanket cache with no eviction is exactly the bug that exists today.
- **Do not re-optimize the frontend bundle/lazy-loading** — the review verified it is already good (~73 KB gzip initial, all routes lazy). Spend the effort on OnPush/CD hygiene (PERF-04) instead.
- **Do not build the notifications UI (GAP-09) before background jobs (ARCH-05)** — it would have no reliable producer.
- **Do not touch the EF migration chain except via ARCH-03 and the `db-migrate` skill** — both folders are live halves of one chain; deleting either breaks history.
- **Do not start Plan 18 Phase 3 (104 color / i18n hardening) ahead of M1/M2** — it is real work but not a launch blocker; i18n is already at full EN/BG/DE parity, and color violations are cosmetic next to a broken checkout.
