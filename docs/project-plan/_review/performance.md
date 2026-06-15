# Performance — Review Findings (2026-06-11)

## Summary

Performance posture is split: the frontend is in genuinely good shape, while the backend caching story is largely dead code and two server-side configuration issues threaten production behavior.

**Frontend:** every route is lazy-loaded, `@defer` is used for below-the-fold content, and the production build (rebuilt today, Jun 11) yields an initial payload of ~259 KB raw / ~73 KB gzipped — far inside the 650 KB budget. The documented Lighthouse 0.97/1.00 scores on `/` are plausible against the current config. Weaknesses are zone-based change detection with only 10/85 OnPush components, zero `runOutsideAngular` usage despite rAF/scroll loops in AnimationService/confetti/flying-cart, no responsive images (zero srcset, no NgOptimizedImage, no resize/WebP pipeline in front of MinIO), and no SSR/prerender for a public shop.

**Backend:** the MediatR CachingBehavior that 14 queries opt into via ICacheableQuery is never registered, so Redis is provisioned, health-checked, and required — but never used for application caching. Meanwhile an OutputCache base policy silently caches ALL anonymous GETs for 5 minutes with no invalidation anywhere (named "Products"/"Categories" policies and tags are defined but never referenced; `EvictByTagAsync` is never called). The most severe issue: the rate limiter partitions by RemoteIpAddress with no ForwardedHeaders processing, while the shipped nginx config proxies all /api traffic — in production every shopper shares one 100-req/min bucket, which will 429 the whole store under modest load.

**Query hygiene** is mixed: orders use projections, and slug/wishlist hydration is sane and batched, but search is naive multi-ILIKE (docs claim "full-text search"), facets load the entire product table into memory per request, recommendations score the whole in-stock catalog per call, and public pagination has no pageSize clamp. DB indexing is strong (GIN on tags/specifications, b-tree on all hot paths including wishlist share_token), though nothing in the schema supports the LIKE-based search.

## Findings

### 1. Rate limiter keys on RemoteIpAddress behind nginx proxy — all users share one 100 req/min bucket in production
- **Finding:** The rate limiter partitions by `RemoteIpAddress` with no ForwardedHeaders processing, while the shipped nginx config proxies all /api traffic — in production every shopper shares a single 100 req/min bucket.
- **Category:** backend-config
- **Severity/Priority:** P0 (High) — verification: confirmed
- **Evidence:** src/ClimaSite.Api/Program.cs:208-217 partitions the global limiter by `context.Connection.RemoteIpAddress` (100 req/min, QueueLimit 0, rejects with 429 at Program.cs:199-205). `grep ForwardedHeaders` over Program.cs returns nothing — X-Forwarded-For is never processed. The shipped web container proxies ALL browser API traffic: src/ClimaSite.Web/nginx.conf.template `location /api/ { proxy_pass ${API_URL}/api/ ... proxy_set_header X-Forwarded-For ... }`, so the API sees a single source IP (the nginx container) for every shopper. Deploys via railway.toml/Dockerfile.web use exactly this topology.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/nginx.conf.template
- **Why it matters:** A product-list page fires multiple API calls (products, filter options, categories, cart, wishlist). With every user sharing one fixed-window bucket of 100/min, roughly 5-15 concurrent shoppers will start receiving 429s store-wide. The per-IP auth brute-force policy (10/min) is similarly neutered as a security control and becomes a shared lockout. This only manifests in deployed (proxied) environments, which is why local testing never shows it.
- **Recommended fix:** Add `app.UseForwardedHeaders()` with `ForwardedHeadersOptions { ForwardedHeaders = XForwardedFor | XForwardedProto, KnownNetworks/KnownProxies configured for the internal network }` early in the pipeline, before UseRateLimiter. Verify the partition key then reflects the real client IP; alternatively partition explicitly on the parsed X-Forwarded-For first hop. (Effort: Small)
- **Acceptance criteria:** Integration/manual test through the nginx container: two clients with different X-Forwarded-For values get independent rate-limit buckets; a single client still gets limited; auth endpoints limit per real client IP.
- **Dependencies or follow-up:** Coordinate with security review — UseForwardedHeaders must restrict trusted proxies to avoid IP spoofing of the rate limiter.
- **Confidence:** verified. Verifier confirmed twice: all three limiters (global 100/min, auth 10/min, strict 5/min) key on RemoteIpAddress with QueueLimit 0; no ForwardedHeaders handling exists anywhere in code or deploy config (including the ASPNETCORE_FORWARDEDHEADERS_ENABLED escape hatch), and production traffic provably flows through the nginx proxy (environment.production.ts uses relative apiUrl). A deterministic store-wide 429 brownout at ~10-15 concurrent shoppers plus a neutered (and trivially exploitable site-wide login lockout) brute-force control makes P0/High justified, not inflated.

### 2. CachingBehavior is never registered — all 14 ICacheableQuery caches and the Redis query cache are dead code
- **Finding:** The MediatR CachingBehavior is never registered in the pipeline, so all 14 ICacheableQuery cache keys/TTLs are inert and Redis is provisioned and health-checked but never used for application caching.
- **Category:** backend-caching
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** src/ClimaSite.Application/DependencyInjection.cs:18-23 registers only ValidationBehavior and LoggingBehavior in the MediatR pipeline; `grep -rn AddBehavior src/` confirms no other registrations. CachingBehavior exists at src/ClimaSite.Application/Common/Behaviors/CachingBehavior.cs:13 and 14 queries implement ICacheableQuery with keys/durations (e.g. GetProductBySlugQuery.cs:15-16 '10 min', GetCategoryTreeQuery.cs:13-16 '1 hour', SearchProductsQuery.cs:21-22 '5 min'). ICacheService consumers are only CacheService itself and the unregistered behavior — no handler calls it directly. Redis is meanwhile mandatory: registered at src/ClimaSite.Infrastructure/DependencyInjection.cs:59-63 and health-checked at src/ClimaSite.Api/Program.cs:188-190.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/DependencyInjection.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Common/Behaviors/CachingBehavior.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/DependencyInjection.cs
- **Why it matters:** Every carefully designed cache key/duration in the codebase is inert: category tree, product-by-slug, search, brands, promotions all hit Postgres on every request (modulo the accidental output-cache, see separate finding). Redis adds an infra dependency, a health-check failure mode (Redis down → /health fails → Railway restarts an app that doesn't even need Redis), and zero benefit. Prior validation docs (docs/validation/areas/15-performance-ux.md, 17-platform-infrastructure.md) did not catch this.
- **Recommended fix:** Add `cfg.AddOpenBehavior(typeof(CachingBehavior<,>));` in AddApplicationServices (note: ICacheableQuery constraint means it only binds to caching queries). Then decide the invalidation story: product/category mutations must call ICacheService.RemoveAsync for affected keys, or use short TTLs deliberately. Alternatively, delete the behavior + ICacheableQuery plumbing and Redis if HTTP output caching is the chosen layer — but pick one consciously. (Effort: Medium)
- **Acceptance criteria:** Integration test: second identical GetCategoryTreeQuery within TTL does not hit the DB (assert via EF command interceptor or log); Redis keys ClimaSite_* appear; admin product update invalidates or expires the slug cache.
- **Dependencies or follow-up:** Decide overlap with the OutputCache base policy (next finding) to avoid double caching with two different TTLs.
- **Confidence:** verified. Verifier confirmed only Validation/Logging behaviors are registered, exactly 14 ICacheableQuery implementations exist, and Redis's only consumer chain ends at the unregistered behavior — its sole effect is gating /health. P1 calibrated: a fully inert caching subsystem plus a mandatory Redis dependency is a serious architectural/operational defect, not a P0 outage since anonymous GETs are accidentally shielded by the output cache.

### 3. OutputCache base policy silently caches ALL anonymous GETs for 5 min with zero invalidation; named policies/tags are unused
- **Finding:** A blanket OutputCache base policy caches every anonymous 200 GET for 5 minutes in per-instance memory, while the named "Products"/"Categories" policies and tags are never applied and no write path ever evicts.
- **Category:** backend-caching
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** src/ClimaSite.Api/Program.cs:245-250: `AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)))` plus named 'Products' (10 min, tag 'products') and 'Categories' (1 h) policies; middleware enabled at Program.cs:302. Base policies apply to every endpoint, so all anonymous 200 GET responses are cached 5 min in per-instance memory. But `grep OutputCache Controllers/` finds only two `[OutputCache(NoStore = true)]` usages (CartController.cs:13, ProductsController.cs:222) — the named policies are never applied, and `grep EvictByTag src/` returns nothing, so no write path ever invalidates.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Program.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/ProductsController.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/CartController.cs
- **Why it matters:** Two consequences: (1) stale storefront data — price/stock changes, new products, and review approvals are invisible to anonymous users for up to 5 minutes with no eviction hook; checkout-adjacent reads like product stock display can disagree with cart validation. (2) The configuration intent (differentiated 10-min/1-h policies with tags for eviction) was never wired, suggesting the global 5-min cache is accidental. Mitigations that currently prevent data leakage: language is a `lang` query param (ProductsController.cs:41) so it's part of the default cache key, authenticated requests (Authorization header) are excluded by default rules, and cart is NoStore.
- **Recommended fix:** Make caching explicit: remove the blanket Expire base policy; apply `[OutputCache(PolicyName = "Products")]` / `"Categories"` to the intended read endpoints; call `IOutputCacheStore.EvictByTagAsync("products", ...)` from product/category/inventory mutation handlers. Audit any future GET that varies by custom header (e.g. X-Session-Id) since the default key does not include headers. (Effort: Medium)
- **Acceptance criteria:** After an admin price update, an anonymous GET /api/products/{slug} reflects the new price within the documented TTL or immediately after tag eviction; endpoints not intended for caching return no cached responses (verify via repeated requests with a DB-side change).
- **Dependencies or follow-up:** Should be designed together with the CachingBehavior decision (one caching layer with clear ownership).
- **Confidence:** verified. Verifier confirmed the evidence exactly as cited (store is the default per-instance memory cache, so stale data is also instance-inconsistent and unpurgeable without restart) and that the cited mitigations hold — staleness, not leakage; it newly affects this branch's anonymous shared-wishlist GET (WishlistController.cs:28-29). P1 correctly calibrated.

### 4. GetFilterOptionsQuery loads the entire active product table into memory on every facet request
- **Finding:** The facet query materializes all active Product entities — including Description and jsonb Specifications — with no projection, aggregating in C# on every request, and its intended 30-min cache never runs.
- **Category:** backend-query
- **Severity/Priority:** P1 — verification: confirmed
- **Evidence:** src/ClimaSite.Application/Features/Products/Queries/GetFilterOptionsQuery.cs:57 `var products = await query.ToListAsync(...)` with no Take/projection — full Product entities including Description and Specifications jsonb are hydrated, then brands/price-range/specs/tags are aggregated in C# (lines 59-89). The query is ICacheableQuery (30 min, line 15) but that cache never runs (CachingBehavior unregistered). The facet endpoint is called by the public product-list page on each visit/category change.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetFilterOptionsQuery.cs
- **Why it matters:** O(catalog) memory and DB transfer per request on one of the hottest public endpoints. At 1k products with long descriptions and jsonb specs this is multiple MB per request; at 10k it's a per-request heap spike and GC pressure. The accidental 5-min output cache softens repeated identical URLs but every distinct categorySlug misses.
- **Recommended fix:** Short term: project only the needed columns (`.Select(p => new { p.Brand, p.BasePrice, p.Specifications, p.Tags })`). Medium term: push brand counts and price min/max into SQL GroupBy queries, keep only the jsonb spec aggregation in memory (or aggregate specs with jsonb_each in raw SQL), and ensure the 30-min cache actually executes. (Effort: Medium)
- **Acceptance criteria:** Facet request for the all-products case transfers only the 4 needed columns (verify via EF logged SQL); memory profile flat vs catalog size for brands/price; results identical to current output for seeded data.
- **Dependencies or follow-up:** Cache fix (CachingBehavior registration) multiplies the win.
- **Confidence:** verified. Verifier confirmed every cited fact and found the 5-min output-cache mitigation weaker than claimed: the Angular auth interceptor adds an Authorization header to all API calls and the default output-cache policy skips authorized requests, so every logged-in user triggers a full-table hydration per product-list visit/category change. P1 correctly calibrated.

### 5. Search is naive multi-ILIKE over name/description/translations — 'full-text search' claim in docs is not implemented and cannot use existing indexes
- **Finding:** Product search builds per-term lower-Contains predicates across ~10 text columns (including a correlated translations subquery), which no existing index can serve; the documented "full-text search" does not exist.
- **Category:** backend-query/db
- **Severity/Priority:** P2 — verification: adjusted (verifier downgraded P1 → P2)
- **Evidence:** src/ClimaSite.Application/Features/Products/Queries/SearchProductsQuery.cs:50-64 builds per-term `p.Name.ToLower().Contains(term)` across Name, ShortDescription, Description, Brand, Model, Tags AND a correlated `p.Translations.Any(...)` over three more text columns; GetProductsQueryHandler.cs:32-40 does the same for searchTerm. `grep tsvector|ILike|pg_trgm src/` returns nothing. ProductConfiguration.cs:153-162 has GIN indexes on Tags/Specifications and b-trees on scalar columns, but none can serve leading-wildcard LOWER(...) LIKE; ranking at SearchProductsQuery.cs:91-94 adds two more Contains per row. CLAUDE.md status table states 'Search & Navigation | Complete | Full-text search, facets, filters'.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/SearchProductsQuery.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/Data/Configurations/ProductConfiguration.cs
  - /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md
- **Why it matters:** Every search is a sequential scan of products joined to translations with ~10 string predicates per term — fine at the current seeded catalog size, but degrades linearly and the search box is a hot, unauthenticated endpoint. Relevance ranking by substring hit is also poor. This is a documented-vs-actual mismatch: no tsvector column, no pg_trgm, no Meilisearch (despite shared-infra Meilisearch being available per global instructions).
- **Recommended fix:** Pick one: (a) Postgres FTS — add a generated tsvector column over name/brand/model/description (+ per-language for translations), GIN index, query via EF.Functions.ToTsVector/Matches with ts_rank; (b) pg_trgm GIN indexes + EF.Functions.ILike for fuzzy prefix search; or (c) index into Meilisearch. Until then, correct the CLAUDE.md claim. (Effort: Large)
- **Acceptance criteria:** EXPLAIN ANALYZE on the search query shows index usage (bitmap/GIN) instead of Seq Scan on products; p95 search latency stays flat as catalog grows to 10k products (load test with seeded data); docs updated to match implementation.
- **Dependencies or follow-up:** None
- **Confidence:** verified. Verifier: evidence fully holds and is slightly worse than claimed (the search's own ICacheableQuery cache is also dead), but the project is pre-launch with at most a few hundred seeded products where a seq scan completes in milliseconds — prospective scalability debt plus a cheap docs correction, hence adjusted to P2.

### 6. No pageSize clamp or pagination validators on public list endpoints — ?pageSize=100000 dumps the catalog with Includes
- **Finding:** Public paginated endpoints bind pageSize/pageNumber straight from the query string with no clamping or validation, allowing a single request to dump the catalog with three Include joins.
- **Category:** backend-query
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** ProductsController.cs:28-29 binds `pageSize` straight from query; GetProductsQuery has no validator (only GetRecommendationsQueryValidator exists under Features/Products — verified by find). PaginatedList.CreateAsync (src/ClimaSite.Application/Common/Models/PaginatedList.cs:23-36) applies Skip/Take with no bounds; negative pageNumber yields negative Skip. GetProductsQueryHandler.cs:86-96 hydrates Translations + Images + Variants per row, so a huge Take is amplified ~4x in row count. Same pattern in SearchProductsQuery.cs:98-101 and reviews queries.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Api/Controllers/ProductsController.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Common/Models/PaginatedList.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetProductsQueryHandler.cs
- **Why it matters:** A single crafted anonymous request can pull the entire product table with three Include joins — cheap DoS amplification, made worse because the rate limiter is currently a shared global bucket (one attacker request also burns everyone's quota). Negative paging values can throw and 500.
- **Recommended fix:** Clamp in PaginatedList.CreateAsync (pageNumber >= 1, pageSize in [1, 100]) as a backstop, plus FluentValidation validators on public paginated queries so the existing ValidationBehavior rejects out-of-range values with 400. (Effort: Small)
- **Acceptance criteria:** GET /api/products?pageSize=100000 returns at most the documented max page size; pageNumber=0/-1 returns 400; unit tests cover the clamp.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 7. Recommendations endpoint scores the entire in-stock catalog in memory on every homepage wizard call, uncached
- **Finding:** GetRecommendationsQueryHandler loads all active in-stock products with variants and scores them in C# on every call, with no caching despite low-cardinality inputs, on the primary home-v3 endpoint.
- **Category:** backend-query
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** src/ClimaSite.Application/Features/Products/Queries/GetRecommendationsQueryHandler.cs:35-39 loads ALL active in-stock products with their variants (`ToListAsync` with no Take; the comment on line 34 says pre-score truncation is intentionally avoided), scores each in C# (lines 42-58), takes 3, then re-fetches details. GetRecommendationsQuery does not implement ICacheableQuery, and inputs (areaM2/roomType/climateZone) are low-cardinality and highly cacheable. This is the primary homepage (home-v3) endpoint per docs/plans/18-project-completion.md HOME-012.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetRecommendationsQueryHandler.cs
- **Why it matters:** Homepage is the hottest route; each wizard interaction triggers a full catalog hydration + scoring. The accidental 5-min output cache helps only for identical query strings. Fine at hundreds of products; a scaling cliff at thousands, and easily avoided given the tiny input space (~area buckets x 3 zones x room types).
- **Recommended fix:** Either pre-filter in SQL to plausible BTU range using the jsonb specifications (GIN index already exists on specifications, ProductConfiguration.cs:162) before materializing, or make the query ICacheableQuery with a 5-15 min TTL keyed on (areaBucket, zone, roomType, lang) once CachingBehavior is registered; project only the columns the scorer needs. (Effort: Medium)
- **Acceptance criteria:** DB rows materialized per recommendation call bounded (verify via EF logging); repeated identical wizard requests within TTL hit cache; recommendation output unchanged for the seeded test matrix.
- **Dependencies or follow-up:** CachingBehavior registration (finding 2) for the cache option.
- **Confidence:** verified

### 8. Zone-based change detection with 12% OnPush adoption and no runOutsideAngular around rAF/scroll loops
- **Finding:** The app runs zone-based change detection with only 10/85 OnPush components, and all rAF/scroll animation loops (AnimationService, confetti, flying-cart, header scroll handler) run inside the zone, triggering app-wide CD every frame.
- **Category:** frontend-change-detection
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** app.config.ts uses `provideZoneChangeDetection({ eventCoalescing: true })` with zone.js polyfill (angular.json polyfills) — not zoneless. Only 10 of 85 components use ChangeDetectionStrategy.OnPush (grep count; mostly home-v3 + trust badges). `grep -rln runOutsideAngular src/app` returns zero files, yet: AnimationService registers window scroll/resize listeners with a rAF callback updating signals (animation.service.ts:72-77, 95-103); ConfettiService runs a continuous rAF loop (~line 198-248); FlyingCartService animates via rAF; HeaderComponent has a `@HostListener('window:scroll')` rAF-throttled handler (header.component.ts:1437-1452). All run inside the zone, so every scroll frame and every confetti/fly-to-cart frame triggers app-wide change detection across ~75 Default-strategy components. Plan 18 PERF-100 (OnPush >= 70%) at docs/plans/18-project-completion.md:300-301 is still open, while CLAUDE.md marks 'Performance Optimizations | Complete'.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/animation.service.ts
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/confetti.service.ts
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/services/flying-cart.service.ts
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/core/layout/header/header.component.ts
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/app.config.ts
- **Why it matters:** Scroll on long pages (product list) = change detection on every animation frame across the whole tree; confetti on order confirmation = ~60 CD cycles/sec for the animation duration. Lighthouse lab scores on `/` stay high (TBT 0 claimed) because the homepage is light, but INP/scroll smoothness on mid/low-end mobiles degrades, and it caps how far the app can scale in template complexity.
- **Recommended fix:** Wrap rAF loops and scroll listeners in `NgZone.runOutsideAngular` (signals updated from outside the zone still propagate to OnPush/signal consumers in v19 via signal change notification; re-enter zone only where needed). Continue PERF-100: convert presentational/list components (product-card, lists, layout) to OnPush — signal-based inputs make this low-risk. (Effort: Medium)
- **Acceptance criteria:** Chrome DevTools performance trace while scrolling /products shows no per-frame application-wide CD (no recurring 'Run change detection' tasks tied to scroll); OnPush adoption >= 70% per plan 18; confetti animation produces no CD churn.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 9. No image optimization pipeline: zero srcset/NgOptimizedImage, originals served straight from MinIO
- **Finding:** There is no responsive-image or format-conversion pipeline — zero srcset/NgOptimizedImage usage, and admin-uploaded originals are served straight from MinIO with no resizing or WebP/AVIF conversion.
- **Category:** frontend-images
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** `grep NgOptimizedImage|ngSrc` over src/ClimaSite.Web/src: 0 hits; `grep srcset`: 0 hits; `loading="lazy"`: 20 hits across 18 components (coverage is now good — cart/brands/recently-viewed flagged 'Needs fix' in docs/performance/performance-audit.md have been fixed); `fetchpriority="high"` used in product-gallery.component.ts:36 and brand-detail.component.ts:30. Image URLs come from uploaded files via MinIO (Infrastructure/DependencyInjection.cs:73-81, PublicUrl config) with no resizing/format conversion service in the repo, and nginx only proxies/caches — no transformation.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/features/products/product-card/product-card.component.ts
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Infrastructure/DependencyInjection.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/nginx.conf.template
- **Why it matters:** Product-grid pages download admin-uploaded originals at full resolution for ~300px cards, in whatever format was uploaded (likely JPEG/PNG, no WebP/AVIF). On a 12-product grid this is typically the dominant LCP/bandwidth cost — currently masked because seeded data has few/small images. No CDN is configured in railway deploy, so MinIO/origin serves every image.
- **Recommended fix:** Introduce sized variants at upload time (thumb/card/detail widths, WebP) in MinioStorageService, expose them in ProductImageDto, and adopt NgOptimizedImage with ngSrcset in product-card/gallery. Alternatively front MinIO with an image-resizing proxy (imgproxy/thumbor) and build srcset URLs. (Effort: Large)
- **Acceptance criteria:** Product list page serves card images <= 2x display width in WebP/AVIF (verify via DevTools network panel); LCP image on product detail uses fetchpriority=high and an appropriately sized candidate.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 10. No SSR/prerender for a public e-commerce storefront — SEO, social previews, and first-load LCP depend entirely on client JS
- **Finding:** The storefront ships as a pure client-side SPA — no @angular/ssr, no prerendered routes, static generic meta tags only — so SEO freshness, link unfurling, and first-load LCP all depend on client JS execution.
- **Category:** frontend-architecture
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** package.json has no @angular/ssr; dist/clima-site.web/prerendered-routes.json contains `{"routes": {}}`; angular.json build has no server/prerender options; index.html ships static meta only ('CDL - Climate & Water Solutions', generic description). Product/category meta tags and JSON-LD (structured-data.service.ts exists) are injected client-side. nginx serves index.html for all routes (nginx.conf.template `try_files ... /index.html`).
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/package.json
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/angular.json
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/index.html
- **Why it matters:** Googlebot renders JS, but rendering-queue delays hurt crawl freshness for price/stock; non-Google crawlers and link unfurlers (Facebook/WhatsApp/Slack) see only the generic title/description for every product URL — significant for a shop whose products get shared. First-load LCP on product pages requires JS boot + API round trip; current Lighthouse 0.97 was measured on `/` only (plan 18 HOME-012), and PERF/A11Y-104 (5 routes) is still open.
- **Recommended fix:** Decision-level (per user rules, ask before changing architecture): options are Angular SSR with hydration for product/category/home routes, prerender for static routes + dynamic meta via edge function, or accept the trade-off and document it. At minimum implement per-route Title/Meta service usage now (cheap, partial). (Effort: Large)
- **Acceptance criteria:** curl (no JS) of /products/{slug} returns product-specific title/meta/OG tags; Lighthouse LCP on product detail (cold, mobile) < 2.5s; decision recorded in docs/adr/.
- **Dependencies or follow-up:** User decision required (architecture change); affects deploy topology (node server vs static nginx).
- **Confidence:** verified

### 11. Brief-DTO list queries hydrate full entities (Description, all variants, all translations) instead of SQL projections
- **Finding:** Product list/search handlers materialize whole Product entities with Translations + Images + Variants and then map ~10 scalar fields to ProductBriefDto in memory, transferring large text columns and variant rows the DTO never uses.
- **Category:** backend-query
- **Severity/Priority:** P2 — verification: unverified (P2/P3)
- **Evidence:** GetProductsQueryHandler.cs:86-96 includes Translations + Images(IsPrimary) + Variants and materializes whole Product entities, then maps ~10 scalar fields to ProductBriefDto in memory (lines 98-122); SearchProductsQuery.cs:41-47 includes unfiltered Images + Variants + Translations for the same brief DTO. Both transfer Description/full text and all variant rows that the DTO never uses; AverageRating/ReviewCount are hardcoded 0 (GetProductsQueryHandler.cs:114-115, SearchProductsQuery.cs:119-120, WishlistApplicationService.cs:179-180) instead of being aggregated. Counter-example showing the right pattern exists in-repo: GetUserOrdersQuery.cs:88-103 projects server-side.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetProductsQueryHandler.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/SearchProductsQuery.cs
- **Why it matters:** Per-page-of-12 this multiplies transferred rows ~4x (12 products x N variants x M translations x images) and serializes large text columns for no reason. GetTranslatedContent forces client-side evaluation today, but a projection can still select only (Name, Slug, ShortDescription, prices, Brand, primary image URL, any-stock flag, matching translation row).
- **Recommended fix:** Rewrite list/search handlers as `.Select()` projections (translated fields via `p.Translations.Where(t => t.LanguageCode == lang).Select(...).FirstOrDefault() ?? p.Name` pattern), compute InStock via `Any()` subquery, and join review aggregates (or maintain denormalized rating columns) instead of hardcoding 0. (Effort: Medium)
- **Acceptance criteria:** EF-logged SQL for product list selects only DTO columns (no description/specifications), one query (or one + count); payload for a 12-product page measurably smaller; ratings show real values.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 12. Recursive category-descendant lookup issues one query per tree node (N+1 by depth)
- **Finding:** A duplicated `GetDescendantIdsAsync` helper recursively issues one DB query per category node, costing K+1 round trips per search/facet request for a K-node subtree.
- **Category:** backend-query
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** Duplicated helper in SearchProductsQuery.cs:150-163 and GetFilterOptionsQuery.cs:106-119: `GetDescendantIdsAsync` recursively awaits one `Categories.Where(ParentId == id)` query per node. For a category tree of K nodes under the requested slug, that is K+1 round trips per search/facet request.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/SearchProductsQuery.cs
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Products/Queries/GetFilterOptionsQuery.cs
- **Why it matters:** Category trees are small today so impact is modest, but it sits on two hot public endpoints and is trivially batchable; it also duplicates logic in two files.
- **Recommended fix:** Load (Id, ParentId) for all active categories in one query and walk in memory (GetCategoryTreeQueryHandler already does this pattern), or use a recursive CTE via raw SQL. Extract to a shared service. (Effort: Small)
- **Acceptance criteria:** One DB query for descendant resolution regardless of tree depth (verify via EF logs); shared implementation used by both handlers.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 13. Admin dashboard KPIs execute 13 sequential queries per load
- **Finding:** GetDashboardKpisQuery issues 13 awaited CountAsync/SumAsync calls back-to-back on one DbContext, adding ~150-400ms of avoidable sequential round trips per dashboard view.
- **Category:** backend-query
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** GetDashboardKpisQuery.cs:30-73 issues 13 awaited CountAsync/SumAsync calls back-to-back (orders x4, revenue x4, customers x4, pending, low-stock) on one DbContext. The dashboard page also fires GetRevenueChart/GetOrderStatusChart/GetRecentOrders/GetTopSellingProducts/GetLowStockProducts queries separately.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Admin/Dashboard/Queries/GetDashboardKpisQuery.cs
- **Why it matters:** Admin-only and indexed (orders.created_at/status indexes exist, OrderConfiguration.cs:144-149), so it's latency (13 RTTs) rather than load. ~150-400ms of avoidable sequential round trips per dashboard view.
- **Recommended fix:** Collapse to 2-3 grouped queries (single pass over orders since prevWeekStart with conditional aggregation `GroupBy(1).Select(g => new { Today = g.Count(o => ...), ... })`; same for users), or accept and cache for 60s. (Effort: Small)
- **Acceptance criteria:** Dashboard KPI handler executes <= 4 queries (EF logs); values byte-identical to current output for seeded data.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 14. Stale/contradictory performance documentation: audit doc out of date, CLAUDE.md 'Complete' vs open plan-18 perf tasks
- **Finding:** The performance audit doc contradicts current code (budgets, fetchpriority, lazy-loading), and CLAUDE.md marks performance "Complete" while plan-18 tasks PERF-100 and PERF/A11Y-104 remain genuinely open.
- **Category:** docs-vs-implementation
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** docs/performance/performance-audit.md (dated 2026-01-24) states budgets of 500kB/8kB warning, but angular.json:66-77 now has 650kB/18kB; the doc claims 'No images in the codebase use fetchpriority=high' which is false now (product-gallery.component.ts:36, brand-detail.component.ts:30); its lazy-loading 'Needs fix' table (cart, brands-list, recently-viewed, orders) is resolved in code. CLAUDE.md marks 'Performance Optimizations | Complete' while docs/plans/18-project-completion.md:300-301 (PERF-100 OnPush >= 70%, actual ~12%) and :327 (PERF/A11Y-104 Lighthouse on 5 routes; only `/` measured per HOME-012 at :156-160) remain open. Lighthouse 0.97/1.00 claim itself IS consistent with current config: production dist rebuilt 2026-06-11 measures ~259KB raw / ~73KB gzip initial payload (main 34.0KB + polyfills 34.6KB + styles 55.8KB + 10 modulepreload chunks), with inlineCritical, deferred fonts, and @defer below-fold content.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/docs/performance/performance-audit.md
  - /Users/sarkisharalampiev/Projects/climasite/CLAUDE.md
  - /Users/sarkisharalampiev/Projects/climasite/docs/plans/18-project-completion.md
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/angular.json
- **Why it matters:** Future agents/devs reading the audit doc will re-fix solved issues and miss the real ones (dead caching, rate limiter). 'Complete' status discourages finishing PERF-100/104 which are genuinely outstanding.
- **Recommended fix:** Refresh performance-audit.md against current code (or mark superseded), change CLAUDE.md Performance row to 'Partial — PERF-100/104 outstanding', and note the budget raise rationale. (Effort: Small)
- **Acceptance criteria:** Docs match code on budgets, fetchpriority, lazy-loading coverage; status tables align with plan 18 open items.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 15. Wishlist mutation lock dictionary grows unbounded and is single-instance only
- **Finding:** WishlistApplicationService keeps a static per-user SemaphoreSlim dictionary that is never evicted and provides no protection across multiple API instances; the read path also lacks AsNoTracking.
- **Category:** backend-scalability
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs:10 `static ConcurrentDictionary<Guid, SemaphoreSlim> UserMutationLocks` — entries are never removed (GetOrAdd at line 24, no eviction), so one SemaphoreSlim per ever-active user accumulates for process lifetime. Being in-process, it also provides no protection if the API ever scales beyond one instance (Railway horizontal scaling). Read path GetWishlistDtoByUserIdAsync (lines 65-118) tracks entities (no AsNoTracking) for a pure DTO mapping.
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Application/Features/Wishlist/Services/WishlistApplicationService.cs
- **Why it matters:** Memory impact is small per entry but unbounded; the bigger issue is the implicit single-instance assumption — fine today, silent correctness/perf trap on scale-out. Otherwise wishlist hydration is well done: one wishlist+items query plus one batched product query (no N+1).
- **Recommended fix:** Add AsNoTracking to the read paths; either accept and document the single-instance lock (comment + ADR) or replace with a DB-level guard (the unique (WishlistId, ProductId) index at WishlistConfiguration.cs:107 already makes concurrent adds safe — catch the unique violation instead of locking). (Effort: Small)
- **Acceptance criteria:** Read-path queries run with AsNoTracking (EF logs); concurrent add test still passes using constraint-based handling or documented lock rationale.
- **Dependencies or follow-up:** None
- **Confidence:** verified

### 16. No router preloading strategy — first navigation to each route pays a chunk-fetch round trip
- **Finding:** `provideRouter(routes)` is configured without any preloading strategy, so every lazy route chunk downloads only on first click; the translation preload hint covers only en.json regardless of the user's persisted language.
- **Category:** frontend-bundle
- **Severity/Priority:** P3 — verification: unverified (P2/P3)
- **Evidence:** app.config.ts uses `provideRouter(routes)` with no `withPreloading(...)`; grep for PreloadAllModules/withPreloading over src returns nothing. All routes are loadComponent/loadChildren (app.routes.ts:4-139), so /products, /cart etc. chunks download only on click. Translation preload hint covers only en.json (index.html:11) regardless of the user's persisted language (BG users fetch the 67KB bg.json without a hint).
- **Affected files/areas:**
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/app/app.config.ts
  - /Users/sarkisharalampiev/Projects/climasite/src/ClimaSite.Web/src/index.html
- **Why it matters:** Cheap latency win: home -> products is the dominant funnel transition; preloading likely-next chunks during idle removes 100-300ms perceived delay on slow connections without hurting initial load (initial payload is only ~73KB gzip, leaving headroom).
- **Recommended fix:** Add `provideRouter(routes, withPreloading(PreloadAllModules))` or a quicklink-style selective strategy; consider preloading the persisted language's JSON via a small inline script or loader warm-up. (Effort: Small)
- **Acceptance criteria:** DevTools network shows products/cart chunks fetched during idle after home load; no regression in Lighthouse TBT/LCP on /.
- **Dependencies or follow-up:** None
- **Confidence:** verified

## Dimension data

### Hot-endpoint risk table

| Endpoint (anonymous, hot) | Handler | Per-request DB cost | Cache (actual) | Risk |
|---|---|---|---|---|
| GET /api/products | GetProductsQueryHandler.cs | 1 count + 1 page query with 3 Includes (full entities) | accidental 5-min output cache only | Medium — projection missing, pageSize unclamped |
| GET /api/products/search | SearchProductsQuery.cs | seq-scan multi-ILIKE over products+translations, recursive category N+1 | none effective (ICacheableQuery dead) | High at scale |
| GET /api/products/filters (facets) | GetFilterOptionsQuery.cs:57 | **full table hydration of all active products** | none effective (30-min key dead) | High |
| GET /api/products/{slug} | GetProductBySlugQuery.cs | 1 query, filtered Includes — good | accidental 5-min output cache | Low |
| POST/GET recommendations (home v3) | GetRecommendationsQueryHandler.cs:35-39 | all in-stock products + variants hydrated + scored in C# | none | Medium (homepage hot path, low-cardinality inputs begging for cache) |
| GET /api/categories/tree | GetCategoryTreeQuery.cs | 1 query + in-memory tree — good | 1-h key dead; 5-min output cache | Low |
| GET /api/wishlist (auth) | WishlistApplicationService.cs:65-118 | 2 batched queries, no N+1 — good | n/a (auth excluded from output cache) | Low (missing AsNoTracking only) |
| GET /api/orders (auth) | GetUserOrdersQuery.cs:88-103 | server-side projection — best-in-repo pattern | n/a | Low |
| GET admin dashboard KPIs | GetDashboardKpisQuery.cs:30-73 | 13 sequential aggregates | none | Low (admin-only, indexed) |
| **All of the above through nginx** | Program.cs:208-217 | — | — | **P0: shared 100 req/min bucket (RemoteIpAddress = proxy IP)** |

### DB index coverage map (verified in Configurations + InitialCreate migration)

| Hot path | Index | Status |
|---|---|---|
| products.slug | unique b-tree (ProductConfiguration.cs:154) | OK |
| products tags / specifications (jsonb) | GIN (ProductConfiguration.cs:161-162) | exists but **unused by current LIKE search** |
| orders.user_id / order_number / created_at / payment_intent_id | OrderConfiguration.cs:144-149 | OK |
| wishlists.user_id (unique), share_token (unique) | WishlistConfiguration.cs:46-47; present in 20260111071552_InitialCreate.cs:932-934 — no pending-migration gap for the new wishlist work | OK |
| carts.user_id / session_id / expires_at | CartConfiguration.cs:45-47 | OK |
| product_translations (product_id, language_code) unique | ProductTranslationConfiguration.cs:67 | OK |
| FTS (tsvector/pg_trgm) | **absent** — no migration, no EF.Functions usage | Gap |
| variants.attributes jsonb | GIN (ProductVariantConfiguration.cs:88) | OK |

Only 2 migrations exist (InitialCreate 2026-01-11, AddNotifications); model snapshot matches working-tree entity changes (wishlist share fields were already in InitialCreate).

### Caching reality check (claim vs actual)

| Layer | Configured | Actually running |
|---|---|---|
| Redis IDistributedCache | AddStackExchangeRedisCache (Infrastructure/DependencyInjection.cs:59) | **Never used** — CachingBehavior unregistered, no direct ICacheService consumers; Redis only serves health checks (Program.cs:188-190), meaning Redis outage fails /health and restarts an app that doesn't need it |
| MediatR ICacheableQuery (14 queries) | keys + TTLs defined | **Dead code** |
| OutputCache named policies "Products"/"Categories" + tags | Program.cs:248-249 | **Never referenced by any endpoint** |
| OutputCache base policy 5 min | Program.cs:247 | **Active for ALL anonymous GETs**, per-instance memory, zero eviction (no EvictByTagAsync anywhere) |
| ResponseCaching middleware | Program.cs:193/301 | Inert (no controller sets Cache-Control via [ResponseCache]) — dead middleware |
| nginx static assets | 1y immutable for js/css/img/fonts (nginx.conf.template) | OK (hashed filenames); i18n *.json correctly excluded from immutable |
| CDN | none configured (railway.toml is bare dockerfile+healthcheck) | MinIO/origin serves all images |

### Verified frontend facts

- Bundle (dist rebuilt 2026-06-11 20:31, after latest source change): initial = main 34.0KB + polyfills 34.6KB + styles 55.8KB + 10 modulepreload chunks = **259KB raw / ~73KB gzip** — comfortably inside 650KB warning budget. Largest lazy chunks: 153KB, 141KB, 93KB, 90KB (likely admin/product features — acceptable as lazy).
- All 20+ routes lazy (app.routes.ts); @defer(on timer) used in home-v3.component.html:5,21,31 and main-layout.component.ts:17,60.
- Not zoneless: zone.js + provideZoneChangeDetection(eventCoalescing).
- prerendered-routes.json = `{"routes": {}}` → no prerender; no @angular/ssr dependency.
- Fonts: Google Fonts with preconnect + print-media swap (index.html:14-20); critical CSS inlined (beasties), stylesheet async-swapped.
- i18n: runtime HTTP loader (app.config.ts CustomHttpLoader), en.json preloaded via link hint; bg.json 67KB / de.json 51KB / en.json 47KB raw.
- Animations: flying-cart animates `transform` with `will-change` (flying-cart.service.ts:116,168 — GPU-friendly); confetti is canvas-based; room-preview canvas paints on-demand (not a continuous loop). The perf issue is zone interaction, not layout thrash.
- `@for ... track` used consistently (46 files); zero untracked `*ngFor`.
- `loading="lazy"`: 20 usages in 18 components — the "Needs fix" list in docs/performance/performance-audit.md is resolved; that doc is stale.

### Docs-claim disposition

| Claim | Source | Verdict |
|---|---|---|
| Lighthouse mobile 0.97 / desktop 1.00 on `/` | plan 18 HOME-012 (docs/plans/18-project-completion.md:156-160) | Plausible & consistent with current config (73KB gzip initial, deferred below-fold). Not re-measured; only `/` was ever measured. |
| Lighthouse >= 90 on 5 routes | plan 18 PERF/A11Y-104:327 | Open — not done |
| "Performance Optimizations: Complete" | CLAUDE.md status table | Overstated — PERF-100 (OnPush >=70%, actual ~12%) and 104 open |
| "Full-text search" | CLAUDE.md status table | **Not implemented** — naive ILIKE |
| Budgets 500kB/8kB | docs/performance/performance-audit.md | Stale — angular.json now 650kB/18kB |
| "No fetchpriority anywhere" | performance-audit.md §2.3 | Stale — 2 usages exist now |

### Quick wins (ordered by value/effort)

1. `UseForwardedHeaders` + trusted-proxy config before the rate limiter (P0 fix, ~10 lines).
2. `cfg.AddOpenBehavior(typeof(CachingBehavior<,>))` — activates 14 prepared caches instantly (then add invalidation on product/category writes).
3. Remove blanket OutputCache base-policy expiry; apply named policies explicitly + EvictByTagAsync on mutations.
4. Clamp pageNumber/pageSize in PaginatedList.CreateAsync (backstop) + validators.
5. Projection in GetFilterOptionsQuery (`.Select` of 4 columns) — one-line-ish change removing full-table hydration.
6. `withPreloading(PreloadAllModules)` in provideRouter.
7. NgZone.runOutsideAngular around AnimationService/confetti/flying-cart rAF loops.
8. Batch the recursive category-descendant lookup (shared helper, one query).
9. Refresh performance-audit.md + CLAUDE.md status rows.

### Open questions / cannot verify from repo

- Railway runtime topology: number of API replicas (affects in-memory OutputCache coherence and the wishlist lock assumption) and whether Railway edge adds its own X-Forwarded-For hop (affects KnownProxies config for the P0 fix).
- Actual production catalog size — several "at scale" findings (search, facets, recommendations) are currently masked by a small seeded catalog.
- Whether the 650kB/18kB budget raise (vs the documented 500kB/8kB) was a deliberate decision or drift; no ADR found.
- Real-user metrics: no RUM (web-vitals package not installed), so claimed lab scores are the only performance signal.

## Refuted during verification

None.
