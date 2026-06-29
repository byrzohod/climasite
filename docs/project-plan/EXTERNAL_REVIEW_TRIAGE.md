# ClimaSite — External Review Triage Register

**Date:** 2026-06-28
**Source review:** `/Users/sarkisharalampiev/Projects/climasite-review-20260628-211745` — a 2026-06-28 external multi-agent code review; the deduplicated items (B-001..B-061) live in its `12_improvement_backlog/backlog_items.json`.
**Method:** 14 skeptical Claude verifiers re-checked every item against the **live code** (file:line evidence, runtime proofs where claimed behavior was in doubt); the high-stakes cluster (14 verdicts) then got a **Codex `gpt-5.5`@`xhigh` cross-check** (read-only advisor). The council agreed on all verdicts except two severity downgrades (B-007, B-016 → Medium) and added implementation refinements (B-010/B-037/B-040/B-053).

**Headline counts (61 items):**
- **Verdicts:** 57 CONFIRMED · 3 PARTIAL (B-008, B-023, B-026) · 1 ALREADY_FIXED (B-009).
- **Post-council real severity:** **2 High** (B-002, B-011) · **32 Medium** · **25 Low** · **2 None** (B-009, B-057).
- Final **High** = exactly **B-011** (committed JWT fallback secret) and **B-002** (admin price inversion). **Fix-first = B-011** (council pick: pre-deploy auth-bypass/admin-escalation risk).

> **This register VALIDATES the existing backlog and attaches `file:line` evidence to much of it — it is NOT a new parallel backlog.** Most items map onto existing `SEC/BUG/GAP/OPS/UX/PERF/ARCH/TS/DOC` tasks (see the "Maps to" column); only the genuinely-untracked items get new IDs (registered in `PRIORITIZED_BACKLOG.md` → "External review — newly-tracked items"). Nothing here is re-derived analysis — fixes are compressed from the triage notes; full evidence is in the cited file:line.

---

## Master register — all 61 items

Sorted by post-council real severity, then by ID. **Highs are bold.** Verdict key: C = CONFIRMED, P = PARTIAL, FIXED = ALREADY_FIXED.

| B-ID | Title | Verdict | Real sev | Maps to existing | Fix (one line) |
|---|---|---|---|---|---|
| **B-002** ✅ FIXED 2026-06-29 | Admin product list inverts current vs compare-at price | C | **High** | BUG-06 (admin slice — not in BUG-06's files) | DONE: admin `SalePrice` now via `ProductPricing.GetSalePrice` (null unless on-sale) + template swapped so BasePrice prominent / compare-at struck. `/acceptance` PASS + Codex council clean. Both real Highs (B-011, B-002) now shipped. |
| **B-011** | JWT secret falls back to committed appsettings outside Production; duplicated token generators | C | **High** | SEC-05 | Delete committed `JwtSettings:Secret`; require a non-placeholder `JWT_SECRET` in every non-Dev/Test env; route all token issuance through `TokenService` (drop the 3 handler-local generators). `appsettings.json:38` / `JwtConfiguration.cs:21`. |
| B-001 | Address modals bypass the accessible shared modal pattern | C | Medium | — (new, NEW-UX) | Render both address dialogs via shared `<app-modal>` (role=dialog, Escape, focus-trap/restore). `addresses.component.ts:90,246`. |
| B-003 | CI does not fail on prod high/critical npm advisories | C | Medium | SEC-12 | Drop `\|\| true`; run `npm audit --omit=dev --audit-level=high` as a hard gate (needs the Angular major). `test.yml:356`. |
| B-004 | API container: hardcoded 8080 + runs as root | C | Medium | OPS-07 | Add a non-root `USER` to Dockerfile.api/web; honor `${PORT}`. `Dockerfile.api:36-40`. |
| B-005 | Checkout shipping form has no exposed validation feedback | C | Medium | UX-01 | Per-field errors (touched) + `aria-invalid`/`aria-describedby`; `markAllAsTouched()` + focus first invalid on submit. `checkout.component.ts:107-164,1182`. |
| B-006 | Concurrent 401 refreshes can clear a valid session | C | Medium | BUG-08 | Cache one shared refresh observable (`shareReplay`); clear auth only when the shared refresh itself errors. `auth.service.ts:240` / `auth.interceptor.ts:53`. |
| B-007 | Order email links include a literal `$` before the order ID | C | Medium (council ↓ from High) | — (new, NEW-BUG) | Drop the stray `$` → `$"{baseUrl}/account/orders/{orderId}"`. `EmailService.cs:66,76`. |
| B-008 | Raw `ArgumentException.Message` echoed to clients | P | Medium | ARCH-04 / SR-18 (residual) | Drop the broad `ArgumentException => arg.Message` arm to a generic "Invalid request"; remove it from the `detail` whitelist. `ExceptionHandlingMiddleware.cs:38,62`. **High-half (guest-token "throw") is a FALSE_POSITIVE.** |
| B-010 | Forwarded headers trust all proxies (XFF spoofing) | C | Medium | SEC-03 (done) + OPS-08 | Set `KnownNetworks`/`KnownProxies` to the trusted edge + `ForwardLimit=1`; stop deriving the audit IP from raw XFF. `Program.cs:219` / `AuthController.cs:214`. |
| B-012 | Legal pages still show pre-launch placeholder copy | C | Medium | GAP-04 (flagged follow-up) | Replace `legal.*` prose (en/bg/de) + remove approved pages from `PLACEHOLDER_PAGES`; content + human legal signoff. `legal-page.component.ts:24`. |
| B-013 | Blanket 5-min OutputCache; dead tagged policies | C | Medium | PERF-01 | `NoStore` the shared-wishlist GET (or evict on revoke); replace the blanket base policy with explicit named policies + `EvictByTag` on catalog mutations. `Program.cs:275` / `WishlistController.cs:28`. |
| B-014 | Payment/shipping radios hidden with `display:none` | C | Medium | — (new, NEW-UX) | Replace `display:none` with sr-only-but-focusable; wrap each group in `role=radiogroup`. `checkout.component.ts` CSS:695. |
| B-015 | Product detail hardcodes in-stock state | C | Medium | UX-10 | Compute `inStock` from `variants[].stockQuantity`; gate add-to-cart/qty + badge. `product-detail.component.ts:120,155,1021`. |
| B-016 | Recommendation scoring expects spec keys seeds don't use | C | Medium (council ↓ from High) | — (new, NEW-BUG) | Enforce a canonical HVAC spec schema at the seed/import/admin boundary (or make `Extract*` alias-aware). `DataSeeder.cs:247` vs `RecommendationScoringService.cs:41`. **Council: NOT "100% of products" — seeding is Dev/Test-gated.** |
| B-017 | Web `API_URL` defaults to localhost; health green while /api misrouted | C | Medium | OPS-07 | Fail hard in `docker-entrypoint.sh` when `API_URL` is unset/localhost; add a deploy smoke curl of `/api/products`. `docker-entrypoint.sh:8`. |
| B-018 | Account order load failures shown as empty history | C | Medium | UX-05 | Add a `loadError` signal + error/retry branch; reserve the empty-state for a true zero-count. `orders.component.ts:836`. |
| B-019 | MediatR `CachingBehavior`/Redis query-cache never registered | C | Medium | PERF-01 | Register `CachingBehavior` + mutation invalidation, OR delete the dead MediatR/Redis cache plumbing. `Application/DependencyInjection.cs:22`. |
| B-020 | Cart load failures converted into an empty-cart state | C | Medium | — (new, NEW-UX; UX-02 is wishlist) | Set `_error` on load failure (don't clobber a loaded cart with empty); the existing error branch then renders. `cart.service.ts:107`. |
| B-021 | Checkout async errors are visual-only (no role=alert) | C | Medium | UX-11 | Render checkout/stripe errors via `<app-alert>` (role=alert) or add `aria-live`. `checkout.component.ts:230,277`. |
| B-024 | Email/Outbox/SMTP/Bank/MinIO/AllowedHosts lack prod fail-fast | C | Medium | SEC-07 (follow-up) | Add `IValidateOptions`/`ValidateOnStart` prod fail-fast for Email/Bank/MinIO/AllowedHosts (mirror `StripeConfiguration`). `Program.cs:55`. |
| B-025 | Outbox batch claiming not atomic across instances | C | Medium | ARCH-05 / ADR-0001 (accepted tradeoff) | Claim rows atomically (`FOR UPDATE SKIP LOCKED` / `UPDATE…RETURNING`) before dispatch. `OutboxProcessor.cs:41`. Only bites at >1 replica. |
| B-028 | GDPR export omits same-email guest orders deletion erases | C | Medium | SEC-14 (residual) | Factor the deletion order-scope predicate and reuse it in `ExportUserDataQuery` (export == delete row set). `ExportUserDataQuery.cs:65` vs `DeleteUserDataCommand.cs:151`. |
| B-030 | Header/footer delayed by a fixed 3.2s timer | C | Medium | UX-04 | Render `<app-header />` eagerly (or `on idle`), keep the no-CLS placeholder. `main-layout.component.ts:18,61`. |
| B-034 | Installation request endpoint lacks the strict rate limit | C | Medium | — (new, NEW-SEC) | Add `[EnableRateLimiting("strict")]` to `CreateInstallationRequest`. `InstallationController.cs:33`. |
| B-036 | Pagination query bounds not enforced on public endpoints | C | Medium | PERF-02 | Clamp `pageNumber≥1` / `pageSize 1..100` / `count 1..24` before DB; guard `PaginatedList` for size≤0. `ProductsController.cs:28` / `PaginatedList.cs:13`. |
| B-038 | Pending answers mark a question answered before approval | C | Medium | — (new, NEW-BUG) | Don't `MarkAsAnswered` on a pending public answer — set `AnsweredAt` on first approval (or recompute from `Answers.Any(Approved)`). `AnswerQuestionCommand.cs:74`. |
| B-039 | Q&A votes anonymous, repeatable, no vote ledger | C | Medium | — (new, NEW-SEC) | Add `QuestionVote`/`AnswerVote` ledger (mirror `ReviewVote`); make votes idempotent per voter. `QuestionsController.cs:115,126`. |
| B-040 | Refresh tokens stored/queried in plaintext | C | Medium | SEC-09 | Store `SHA-256(token)`; query by hash in **Refresh AND Revoke** handlers. `ApplicationUser.cs:13` / `RefreshTokenCommandHandler.cs:36` / `RevokeTokenCommandHandler.cs:26`. |
| B-041 | Router scroll restoration not enabled | C | Medium | UX-03 | `provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration:'enabled', anchorScrolling:'enabled' }))`. `app.config.ts:30`. |
| B-042 | Saved address cards in checkout are click-only | C | Medium | — (new, NEW-UX) | Render saved-address cards as `<button>`/`role=radio` with keyboard activation + `aria-checked`. `checkout.component.ts:84`. |
| B-043 | Product DTOs expose hardcoded zero review aggregates | C | Medium | BUG-12 | Project approved-review aggregates (subquery or denormalized summary) into the 4 product DTOs. `GetProductsQueryHandler.cs:160` et al. |
| B-044 | Route SEO meta, robots.txt, sitemap, canonical, JSON-LD unwired | C | Medium | — (new, NEW-SEO; PERF-07 covers meta only) | Add robots.txt + sitemap.xml, a `TitleStrategy` + `SeoService` (title/meta/canonical), wire `StructuredDataService`, fix `index.html` title. `app.config.ts:30` / `index.html:5`. |
| B-045 | `MigrateAsync` runs on every API boot (not Dev/Test-gated) | C | Medium | OPS-04 | Gate `MigrateAsync` to Dev/Test (or a `MIGRATE_ON_STARTUP` flag); move prod migrations to an explicit pre-deploy step. `DataSeeder.cs:52`. |
| B-022 | CORS `AllowAnyHeader/Method` + wildcard `AllowedHosts` | C | Low | SEC-08 | Provide explicit prod `AllowedOrigins`/`AllowedHosts`; narrow `AllowAnyHeader`/`AllowAnyMethod` to what the SPA uses. `Program.cs:112` / `appsettings.json:74`. |
| B-023 | Agent/workflow docs carry stale status/endpoint assertions | P | Low | DOC-06 | Regenerate the `AGENTS.md` Generated/Commit/Branch stamp; remove the resolved API-014 KNOWN-ISSUES row. `AGENTS.md:3,92`. **Endpoint-table claim is a FALSE_POSITIVE.** |
| B-026 | One-command local E2E orchestrator + card-skip surfacing | P | Low | OPS-06 (partial) / process | Add `scripts/e2e-local.sh` orchestrator; surface card-E2E self-skip in the CI step summary. PAY-IDEM acceptance report is the in-flight unit's own gate. |
| B-027 | GDPR delete can't suppress an already-loaded outbox message | C | Low | SEC-14 (residual) | Re-validate the row exists immediately before send (lease/suppression flag) to close the sub-second TOCTOU. `OutboxProcessor.cs:57` / `DeleteUserDataCommand.cs:162`. |
| B-029 | Generic contact/installation bodies sent as unescaped HTML | C | Low | — (new, NEW-SEC) | Send Generic bodies as `text/plain` (or `HtmlEncode` user fields). `CreateContactMessageCommand.cs:59` / `EmailService.cs:127`. |
| B-031 | Header component SCSS exceeds the style budget | C | Low | ARCH-06 | Extract shared header SCSS / split the header to clear the 18 kB component-style warning. `header.component.ts:459-1664`. |
| B-032 | Header search inputs rely on placeholder-only labelling | C | Low | UX-11 | Add `role=search` + `aria-label` + `type=search`; `aria-hidden` the decorative icons. `header.component.ts:72,300,380`. |
| B-033 | Initial non-EN language doesn't set `documentElement.lang` | C | Low | — (new, NEW-UX/SEO) | Set `document.documentElement.lang = initialLang` in `initializeTranslations()`. `language.service.ts:40`. |
| B-035 | No deploy workflow for the documented Railway topology | C | Low | OPS-03 / OPS-08 | Add `deploy.yml` (gated on Test Summary, explicit migrate + smoke) when Railway creds land. Owner/OPS-08-gated. |
| B-037 | Duplicate-PaymentIntent guard depends on exception message text | C | Low | PAY-IDEM (in-flight) | Classify the duplicate via the structured Postgres error (keep the provider check in **Infrastructure** — no Npgsql in Application). `CreateOrderCommand.cs:342`. |
| B-046 | TestController shipped in API artifact, gated only by env name | C | Low | SEC-04 | Wrap TestController in `#if DEBUG`/opt-in flag; require non-default secrets on cleanup/seed. `TestController.cs:13,40,98`. Artifact pins Production → 404 today. |
| B-047 | `assets/config.json` advertises runtime API config but is unused | C | Low | — (new, NEW-CHORE) | Delete the dead `src/assets/config.json` (or wire a real runtime-config initializer). `config.json:1`. |
| B-048 | Breadcrumb JSON-LD rendered in template, not the head service | C | Low | — (new, NEW-SEO; relates B-044) | Move breadcrumb JSON-LD into `StructuredDataService` (de-dup with B-044). `breadcrumb.component.ts:77`. |
| B-049 | Cart removal toast says "Undo" but has no undo action | C | Low | — (new, NEW-UX) | Wire the toast action → `undoRemoval(item.id)` (or drop the "Undo" copy). `cart.component.ts:775,794`. |
| B-050 | Category descendant filtering queries one tree level at a time | C | Low | PERF-03 | Load the category set once + walk in memory (or recursive CTE) instead of one round-trip per node. `SearchProductsQuery.cs:139`. |
| B-051 | EF migration chain split across two live folders | C | Low | ARCH-03 | Add the split-folder policy to DEV_WORKFLOW + a CI clean-DB migration-apply/snapshot check; consolidate only via a tested plan. `DECISIONS.md` D-006. |
| B-052 | Energy-rating colors hardcoded in two divergent systems | C | Low | UX-13 | Centralize both energy palettes into named tokens (mark regulatory vs brand). `energy-rating.component.ts:130` / `product-card.component.ts:332`. |
| B-053 | Guest cart expiry enforced on read; no cleanup job | C | Low | — (new, NEW-CHORE; ARCH-05 infra) | Add a `BackgroundService` purging carts where `UserId IS NULL AND ExpiresAt<now`; **also enforce `ExpiresAt` on cart writes** (`AddToCartCommand.cs:134`). `Cart.cs:22` / `GetCartQuery.cs:41`. |
| B-054 | Home v3 delays useful content with fixed timers | C | Low | UX-04 | Fetch first recommendations immediately (`delayMs 0`); switch defers to `on viewport`/`on idle`. `home-v3.component.ts:62` / `.html:5`. |
| B-055 | Inbound correlation IDs logged/echoed without bounds | C | Low | — (new, NEW-SEC; OPS-05 adjacent) | Validate inbound `X-Correlation-Id` against `^[A-Za-z0-9._-]{1,128}$`, else new GUID, before echo/log. `CorrelationIdMiddleware.cs:22`. |
| B-056 | Shared UI components use `[innerHTML]` for icons | C | Low | — (new, NEW-CHORE) | Accept a typed icon-name (not an HTML string) + lint-forbid new data-driven `[innerHTML]`. `specs-table.component.ts:8` / `share-product.component.ts:64`. |
| B-058 | `ng lint` does not fail on warnings | C | Low | OPS-06 | Run `ng lint --max-warnings=0` (after clearing warnings; allow `any` only in `*.spec.ts`). `.eslintrc.json:36` / `test.yml:311`. |
| B-059 | Placeholder always-pass API test (`UnitTest1.cs`) | C | Low | TS-16 | Delete `tests/ClimaSite.Api.Tests/UnitTest1.cs` (empty always-pass stub). |
| B-060 | Product detail error state has no retry or 404 distinction | C | Low | UX-06 | Branch `404 → notFound` vs `loadError`; add retry + browse-products links (mirror product-list). `product-detail.component.ts:34,910`. |
| B-061 | Stripe create-intent doesn't accept/propagate a CancellationToken | C | Low | PAY-IDEM (in-flight) | Add an optional `CancellationToken` to `CreatePaymentIntentAsync`; forward to Stripe `CreateAsync`. `IPaymentService.cs:5` / `StripePaymentService.cs:60`. |
| B-009 | `dotnet format` gate / `CreateOrderCommand.cs` format violation | FIXED | None | — | **ALREADY_FIXED:** `dotnet format --verify-no-changes` is already a required gate and exits 0 on the live tree (`test.yml:297`). No action. |
| B-057 | Lighthouse budget report-only, outside Test Summary | C | None | — | **Intentional/by-design:** `lhci autorun \|\| true` with warn-level asserts until scores stabilize (`test.yml:452`). Ratchet later — no action now. |

---

## Corrections to the review (do NOT act on these)

The review over-stated a handful of claims; the triage corrected them:

- **B-009 — ALREADY_FIXED.** The requested `dotnet format` gate already exists as the required `lint-format` job and passes clean on the current tree; the captured violation was a transient dirty-branch snapshot. **No action.**
- **B-057 — intentional / None.** The report-only Lighthouse job (`lhci autorun || true`, warn-level asserts, absent from Test Summary) is a documented deliberate choice pending score stability — a future ratchet, not a defect.
- **B-008 — High-half is a FALSE_POSITIVE.** Runtime-proven on net10.0: `CryptographicOperations.FixedTimeEquals` returns **false** on differing lengths (it does **not** throw), so wrong-length guest tokens already return the generic "Order not found". Only the residual — raw `ArgumentException.Message` echoed verbatim (info disclosure) — is real (Medium).
- **B-023 — endpoint-table claim is a FALSE_POSITIVE.** The live `AGENTS.md` (141 lines) contains **no** endpoint/route table, so "/api/orders/* requires auth" doesn't exist there. The real (Low) drift: the stale Generated/Commit/Branch header stamp and the resolved API-014 KNOWN-ISSUES row.
- **B-016 — "100% of prod products broken" corrected (council).** Product seeding is gated to Dev/Testing, so this is **not** all production products. The real issue (Medium) is the absence of a canonical HVAC spec schema enforced at the seed/import/admin boundary, silently degrading recommendation fit-scoring for any display-key product.
- **B-026 — mostly process/tooling (PARTIAL, Low).** Local E2E *is* documented; the missing `.planning/acceptance/PAY-IDEM-stripe-keys.md` is the in-flight branch's own pre-merge gate (expected before `/trunk-merge`), not a standing bug.

---

## Recommended execution order (CONFIRMED-real items)

> **Deploy context:** nothing is deployed yet (OPS-08). Every "deploy-gated" item below is **latent, not an active exposure** — but several are P0 **pre-deploy gates** that must land before the first publicly-reachable origin ships.

### (a) Highs — do these first
1. **B-011 (= SEC-05) — committed JWT fallback secret.** S. Council's #1: pre-deploy auth-bypass / admin-escalation. Any non-`Production`-named env without `JWT_SECRET` signs+validates with the public repo key.
2. **B-002 (= BUG-06 admin slice) — admin price inversion.** S. Admins see the higher compare-at as the live price and the real selling price struck → wrong merchandising/discount decisions. (Not in BUG-06's current affected-files — extend BUG-06 to `GetAdminProductsQuery`.)

### (b) High-value quick wins (XS/S effort, real impact)
- **B-007** (NEW-BUG, XS) — order-email CTA `$<guid>` 404. One-char fix + a render test.
- **B-036** (= PERF-02, S) — clamp public pagination (`pageSize=0/-1/100000` DoS/garbage).
- **B-061** (= PAY-IDEM, XS) — thread `CancellationToken` into Stripe create-intent.
- **B-055** (NEW-SEC, XS) — bound/charset-validate inbound `X-Correlation-Id` (log-forging).
- **B-034** (NEW-SEC, XS) — `[EnableRateLimiting("strict")]` on the public installation-lead endpoint.
- **B-018 / B-020** (UX-05 / NEW-UX, S) — real error+retry states instead of fake "empty" history/cart.
- **B-038** (NEW-BUG, S) — stop flagging questions answered on a *pending* answer.
- **B-049** (NEW-UX, XS) — wire (or remove) the cart "Undo" toast action.
- **B-059** (= TS-16, XS) — delete the always-pass `UnitTest1.cs`.
- **B-008 residual** (ARCH-04/SR-18, XS) — stop echoing raw `ArgumentException.Message`; independently shippable ahead of the full O-2 error-contract rewrite.

### (c) Medium clusters by theme
- **GDPR/privacy** — B-028 (export==delete scope), B-027 (outbox TOCTOU). *(SEC-14 residuals.)*
- **A11y** — B-005 (UX-01 form errors), B-014 (radios `display:none`), B-021 (UX-11 role=alert), B-042 (saved-address keyboard), B-001 (address-modal dialog semantics).
- **Caching** — B-013 + B-019 (= PERF-01; decide the O-3 caching owner, kill the blanket 5-min cache).
- **Perf timers** — B-030 + B-054 (= UX-04; drop fixed `timer()` defers for `on idle`/`on viewport`).
- **SEO / i18n** — B-044 (robots/sitemap/title/JSON-LD), B-033 (`html lang` on first paint).
- **Catalog correctness** — B-043 (= BUG-12 review aggregates), B-015 (= UX-10 PDP stock), B-016 (canonical spec schema), B-039 (Q&A vote ledger).
- **Auth** — B-006 (= BUG-08 shared refresh), B-040 (= SEC-09 hash refresh tokens, incl. Revoke handler).
- **Deploy hardening (pre-deploy-gated by OPS-08)** — B-004 + B-017 (= OPS-07 non-root + API_URL), B-045 (= OPS-04 startup migrations), B-024 (= SEC-07 prod fail-fast for Email/Bank/MinIO/hosts), B-010 (SEC-03 follow-up proxy allowlist + audit-IP).

### (d) Defer / owner-gated
- **B-012** (= GAP-04) — legal copy is human legal-signoff content, not code. Pre-launch.
- **B-035** (= OPS-03/OPS-08) — deploy workflow; needs owner Railway creds.
- **B-003** (= SEC-12) — npm-audit blocking gate requires the breaking Angular major upgrade (Large).
- **B-057** — Lighthouse ratchet; no action until scores stabilize.
- Lower-priority Low items (B-022, B-026, B-029, B-031, B-032, B-037, B-046, B-047, B-048, B-050, B-051, B-052, B-053, B-056, B-058, B-060) — fold into the related existing task or pick up opportunistically; see "Maps to" above.

---

## See also

- `docs/project-plan/PRIORITIZED_BACKLOG.md` — the canonical task list; this register maps each B-ID onto it and registers the genuinely-new items.
- `docs/project-plan/PROJECT_STATUS.md` — per-feature status (verified against code 2026-06-26).
- `vault/Knowledge/climasite/` — decisions/risks/components knowledge graph.
