---
unit: B-044-seo
type: NORMAL
status: approved
created: 2026-06-30
plan_status: approved
design: ../../design/DESIGN.md
couples: [B-048, B-033]
council: codex gpt-5.5@xhigh — DESIGN: R1 APPROVE-WITH-CHANGES (7H+8M) → R2 (6 more) → R3 (XForwardedHost host-injection resolved by NOT enabling it) — converged. WAVE-A DIFF: R1 REWORK (H1 detail out-of-order→loadSeq; H2 list leak/race→takeUntil+fetchSeq; M1 breadcrumb-emitted-nowhere→wire into pages; M2 no-variant InStock→variant-authoritative; L1 FAQ leak; L2 weak tests) → R2 REWORK (category-resolution loadSeq) → CLEAN. **/ACCEPTANCE caught a fatal NG0200 circular-DI (Router→TitleStrategy→Router) that all 1764 unit tests missed → removed Router from SeoService/SeoTitleStrategy (canonical from document.location.pathname; NavigationStart JSON-LD reset moved to an app-initializer) → councilled → re-acceptance PASS (see .planning/acceptance/B-044-seo-waveA.md)**
---

# Unit plan — B-044: SEO foundation (per-route meta, JSON-LD wiring, robots.txt + dynamic sitemap.xml)

## Context / Definition of Ready
ClimaSite is a **pure CSR** Angular 19 storefront (no SSR/prerender — that is the separate, owner-gated
**DEC-SSR** decision) served in prod by **nginx** (`src/ClimaSite.Web/nginx.conf.template`: `/api/` →
API, `location /` SPA fallback `try_files $uri $uri/ /index.html`). i18n is **single-URL, runtime-only**
(ngx-translate EN/BG/DE from localStorage; **no `/en//bg//de/` URL segments**; one URL/page serves all
languages; API takes `?lang=`). Slugs are language-agnostic English.

Today: `index.html` title is brand-inconsistent (`CDL - Climate & Water Solutions`); `provideRouter` has
**no `TitleStrategy`**; **no** per-route meta description / canonical / Open Graph / Twitter tags; the
fully-built `StructuredDataService` is **wired into zero components**; the breadcrumb component emits its
**own** competing BreadcrumbList JSON-LD via a template `<script [innerHTML]>` (**B-048**);
`LanguageService.initializeTranslations()` never sets `document.documentElement.lang` so a returning
BG/DE user's first paint stays `lang="en"` (**B-033**); there is **no robots.txt / sitemap.xml**; and no
production domain is decided (**OPS-08** — nothing deployed). Entities `Product/Category/Brand` gate on
`IsActive` (no soft-delete) with `UpdatedAt` (BaseEntity); `Promotion` exposes `IsCurrentlyActive`
(`IsActive && StartDate<=now<=EndDate`). ~2260 active products.

This unit is the **"ship per-route meta NOW" partial win** — it does NOT add SSR. Its job: give the
storefront a credible SEO foundation that is correct for a CSR app, and document the residual CSR
limitation honestly.

## Known CSR limitation (documented, NOT fixed here)
Meta/OG/Twitter tags and JSON-LD injected at runtime are seen by **Googlebot** (it renders JS) but **not**
by non-JS social scrapers (Slack/Facebook/LinkedIn) or on first byte — those see only the **static
`index.html` defaults** (which we improve). Full fidelity + `hreflang` (per-language URLs) require
**DEC-SSR**. `hreflang` is intentionally omitted: single-URL runtime i18n has no per-language URL to point
alternates at. A true server/edge **404** for unknown SPA routes also needs DEC-SSR; we mitigate with a
`noindex` not-found route and a sitemap that lists only genuinely-active URLs.

---

## Scope / approach — two waves (each: council the diff → /acceptance → PR → 6 green CI checks → squash)

### Wave A — Frontend SEO meta + JSON-LD wiring (frontend only; ships independently)
1. **`SeoService`** (`core/services/seo.service.ts`, `providedIn:'root'`, `isPlatformBrowser`-guarded;
   inject `Title`, `Meta`, `DOCUMENT`, `Router`, `TranslateService`). API: `setMeta({titleKey|title,
   descriptionKey|description, image?, type?, robots?, canonicalPath?})`. It sets idempotently:
   `<title>` (brand suffix applied **once** — see §brand-suffix), `<meta name=description>`,
   `<link rel=canonical>`, `og:title/description/type/url/image/site_name/locale`,
   `twitter:card(summary_large_image)/title/description/image`, `<meta name=robots>`.
   - **Canonical/og:url base = `window.location.origin`** (CSR always has `window`; self-correcting; no
     drift). **Do NOT add `environment.siteBaseUrl`** (council #12 — avoids build-time/runtime drift).
   - **Canonical normalization** (council #14): `origin` + path, **strip query + fragment**, **strip
     trailing slash except root**. (Filtered/paginated/search states are `noindex` — see item 8 — so
     collapsing them to the clean canonical is correct.)
   - i18n: titles/descriptions are **translation keys** resolved via `TranslateService` and **re-applied
     on `onLangChange`** (CLAUDE.md: all user-facing text translatable; `check-i18n.mjs` enforces en/bg/de
     parity). Dynamic page copy (product/brand/promo names) comes from already-translated API data.
2. **`SeoTitleStrategy`** (`core/services/seo-title.strategy.ts`) registered in `app.config.ts`
   (`{provide: TitleStrategy, useClass: SeoTitleStrategy}`). On each `NavigationEnd` it: resolves the
   deepest route's `data.seo` (`{titleKey, descriptionKey, robots?, image?}`), **applies sane DEFAULT
   meta + canonical via `SeoService`** (so a previous route's meta/JSON-LD never leaks — council #4/#9),
   then sets the title. Routes without `data.seo` get a brand-only default title + site description +
   `index,follow`. **Timing (council R2 #4)**: route-owned **JSON-LD is cleared on `NavigationStart`**;
   **default title/meta/canonical are applied on `NavigationEnd`**; component data-driven overrides run
   after their fetch resolves.
3. **Route `data.seo`**: add `seo:{titleKey,descriptionKey,robots?}` to every route in `app.routes.ts`,
   `legal.routes.ts`, `account.routes`, `admin.routes`, `promotions.routes`, `brands.routes`. **Remove
   the hardcoded branded `title:` strings** (`'… - ClimaSite'`) so the strategy is the single brand
   authority (council #15 — no double-branding). Private/semi-private routes get `robots:'noindex,follow'`:
   `login, register, forgot-password, reset-password, cart, wishlist, wishlist/shared/:token, checkout,
   checkout/confirmation/:orderId, account/**, admin/**`, **and the `**` not-found route** (council #5/#6).
4. **Wire `StructuredDataService`** (pass `baseUrl = window.location.origin`):
   - home → `setOrganizationData` + `setWebsiteData` (SearchAction → `/products?search={term}` — the
     **real** param is `search`, NOT `q`; council R2 #2 / product-list.component.ts:1227);
   - product detail → `setProductData` (id `'product'`) + breadcrumb via the component (item 5);
   - product-list / category → `setProductListData` (id `'product-list'`);
   - FAQ → `setFaqData`; brand detail / promotion detail → appropriate blocks.
   - **Stable ids per schema type** + clear route-owned JSON-LD on **`NavigationStart`** (NOT
     `NavigationEnd` — council R2 #4: clearing on End races and can erase the JSON-LD a freshly-activated
     component just emitted synchronously). Give `setProductData` id `'product'` and `setProductListData`
     id `'product-list'` (today both use `'default'`).
   - **Accurate product JSON-LD** (council #10): absolutize `image` URLs against base; `availability`
     from real stock (InStock/OutOfStock via `TotalStock`/variants); **effective/current price** via the
     same pricing source as B-002 (`ProductPricing.GetSalePrice`-equivalent), not raw `basePrice`.
5. **B-048 — single breadcrumb JSON-LD emitter**: the **breadcrumb component** becomes the sole emitter —
   in an `effect()` it calls `StructuredDataService.setBreadcrumbData(breadcrumbs())` and clears it on
   destroy; **delete the template `<script [innerHTML]="structuredData()">`** and the now-unused
   `structuredData` computed. Keep the visible `<nav>` (drop the inline microdata `itemscope/itemprop/meta
   position` to avoid dual breadcrumb markup — JSON-LD is the single machine-readable source). Pages that
   want product/category names in the trail pass `[items]` (already supported).
6. **`index.html`** (council known-limit fallback): fix `<title>` to the ClimaSite brand (e.g.
   `ClimaSite — HVAC, Heating & Climate Solutions`); add **static default** `og:title/og:description/
   og:type/og:site_name/og:image` + `twitter:card` so non-JS scrapers get brand-level cards.
7. **B-033**: in `LanguageService.initializeTranslations()` set `document.documentElement.lang = initialLang`
   (browser-guarded) so first paint matches the stored language.
8. **List `noindex` for thin/dup states** (council #13 + R2 #2): product-list / search set
   `robots:'noindex,follow'` when the URL carries **specific** thin-state params — `search`, `page`, or a
   filter param (category/brand/price/sort) — **not** on arbitrary marketing params (`utm_*`, `gclid`,
   `fbclid` must NOT trigger noindex). The clean `/products` (no thin params) stays `index,follow`;
   canonical → clean URL (query stripped). Deep products are discovered via the sitemap, not index pages.
9. **Detail-component param correctness** (council #7 + R2 #3, #5): product / brand / promotion detail must
   react to **route param changes** (not a one-shot `snapshot`) so same-route navigation refreshes content
   + head. **Avoid the existing double-fetch race**: product-detail already reloads on language via an
   `effect()` + `currentSlug` (product-detail.component.ts:860) — **unify** slug + language into a **single**
   reactive load trigger keyed on `(slug, lang)` (e.g. a slug **signal** from `toSignal(paramMap)` combined
   with the language signal in ONE effect, deduped against the last-loaded key) — do NOT add a second
   `switchMap` subscription alongside the effect. Set meta only for the **current** URL. **H5**: when a
   detail load returns 404 / not-found, the component **sets `robots:'noindex,follow'`** for that error
   state (the `**` route's noindex does not cover a matched `/products/:slug` whose slug 404s in-component).
   **(Higher-risk change — dedicated spec asserting one fetch per (slug,lang) change; verify in acceptance.)**
10. **Product curated-meta passthrough (graceful)**: add optional `metaTitle?`/`metaDescription?` to the
    **frontend `Product` model**; product-detail uses `metaTitle ?? name` / `metaDescription ??
    shortDescription`. Harmless while the backend doesn't send them yet (undefined → fallback); **lights up
    automatically when Wave B maps them** (council #8). No cross-wave blocking.

### Wave B — Backend crawler files + reliable noindex + nginx (backend + ops)
11. **`SeoController`** (`Controllers/SeoController.cs`, `[ApiController]`, `[AllowAnonymous]`):
    `[HttpGet("~/robots.txt")] → text/plain`, `[HttpGet("~/sitemap.xml")] → application/xml`.
12. **Base-URL resolution** (council #2/#3 + R2 #1 + **R3 host-injection fix**): resolved **inside
    `SeoController`** — do **NOT** touch the global `ForwardedHeaders` middleware. **Do NOT enable
    `XForwardedHost`** (enabling it globally with `KnownProxies/KnownIPNetworks` cleared is an app-wide
    host-injection surface — council R3 blocker). Resolution order:
    - `Seo:SiteBaseUrl` if set — **validated as an absolute `https://` (or `http://` in non-prod) URI**;
      reject + log otherwise. Used in ALL environments.
    - else, **only in `Development`/`Testing`** (both non-public — local/CI): scheme from the
      `X-Forwarded-Proto` header (fallback `Request.Scheme`) + host from `Request.Host`. This is safe
      because the new nginx SEO `location`s set the **real `Host $host` header** (item 17), so
      `Request.Host` is the public host WITHOUT any global forwarded-host trust; direct-to-Kestrel dev
      yields `localhost` (fine).
    - else (**Staging/Production with empty `Seo:SiteBaseUrl`**): **fail-closed** — `GET /sitemap.xml`
      returns **503** and `robots.txt` omits the `Sitemap:` line (logged). Never emit canonical URLs from
      an untrusted host.
    deploy.md gains a `Seo__SiteBaseUrl` **required / acceptance-blocking** env row (all non-dev/test
    deploys) + a single-canonical-host note; Knowledge gets a node tying it to OPS-08.
13. **No cache poisoning** (council #3): cache only the **host-independent** `(path, lastmod)` enumeration
    (IMemoryCache/Redis ~1h); **render absolute `<loc>` per-request** from the resolved base. robots.txt is
    rendered per-request (trivial; no DB).
14. **sitemap.xml contents**: static public pages (`/`, `/products`, `/categories`, `/brands`,
    `/promotions`, `/contact`, `/about`, `/resources`, `/faq`, `/terms`, `/privacy`, `/cookies`,
    `/returns`, `/shipping`, `/impressum`) + **active** product slugs (`/products/{slug}`) + **active**
    category slugs as **`/products/category/{slug}`** (council #4 — NOT `/categories/{slug}`, which
    soft-404s) + active brand slugs (`/brands/{slug}`) + **`IsCurrentlyActive`** promotion slugs
    (`/promotions/{slug}`) (council #11). `<lastmod>` from `UpdatedAt` (W3C date). One file (≪50k URLs).
    **Exclude** private routes + any query/filter URLs. Trailing-slash consistent with the frontend
    canonical (no trailing slash except root).
15. **robots.txt**: `User-agent: *`, `Allow: /`, **`Disallow:`** the private prefixes (`/admin/`,
    `/account/`, `/checkout`, `/cart`, `/login`, `/register`, `/forgot-password`, `/reset-password`,
    `/wishlist`) — **NOT `/api`** (council #1: Googlebot needs public API XHRs to render), plus an
    **absolute** `Sitemap: {base}/sitemap.xml` line (the reason robots is dynamic).
16. **Reliable noindex** (council #1/#6): (a) `X-Robots-Tag: noindex` response header on **`/api/*`**
    responses (small middleware/filter) so any stray JSON isn't indexed while staying crawlable for
    rendering; (b) **nginx `X-Robots-Tag: noindex` `location` blocks** for the private SPA prefixes (the
    JS-independent guard the council wants), layered over Wave A's route-meta noindex.
17. **nginx.conf.template**: add `location = /robots.txt` and `location = /sitemap.xml` (before the SPA
    catch-all) that `proxy_pass` to the API **setting the real `proxy_set_header Host $host;` (so
    `Request.Host` = the public host without any global forwarded-host trust — R3 fix) AND
    `proxy_set_header X-Forwarded-Proto $scheme;`** (without the proto header the fallback emits `http://`
    locs; mirror the existing `/api/` block otherwise). Add the private-prefix
    `X-Robots-Tag` `location` blocks (item 16b) — **and inside each such block RE-DECLARE the server-level
    security headers** (`X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`), because nginx
    `add_header` does NOT inherit once a block declares its own (council R2 #6). The /acceptance nginx
    smoke must assert those headers are still present on a private prefix. Document the (prod)
    single-canonical-host + trailing-slash redirects as ops follow-ups.

---

## Acceptance criteria
- [ ] **Wave A**: every public route sets a unique, translated `<title>` (single ` | ClimaSite` brand
      suffix, no doubles), a meta description, a self-referential canonical (origin + clean path), and
      OG/Twitter tags; titles/descriptions update on EN↔BG↔DE switch. Private + not-found + filtered-list
      routes emit `<meta name=robots content="noindex,follow">`. JSON-LD is present and correct on
      home/product/list/FAQ (one block per type; none leak across navigations). Breadcrumb JSON-LD is
      emitted **once** by the head service (template `<script>` gone). `index.html` shows the ClimaSite
      brand + default OG. Returning BG/DE user gets `<html lang>` matching their language on first paint.
- [ ] **Wave B**: `GET /sitemap.xml` → valid `application/xml`, **contains** active product/category
      (`/products/category/{slug}`)/brand/current-promotion + static URLs with absolute `<loc>` + `<lastmod>`,
      and **excludes** inactive/expired ones and all private/filter URLs. `GET /robots.txt` → `text/plain`,
      Disallows private prefixes, does **not** Disallow `/api`, and emits an absolute `Sitemap:` line.
      Both resolve the public host correctly behind a proxy (forwarded-host honored / `Seo:SiteBaseUrl`
      wins). `/api/*` responses carry `X-Robots-Tag: noindex`; nginx adds it on private SPA prefixes.
- [ ] Works in light AND dark themes and EN/BG/DE (meta is theme-agnostic; titles/descriptions translate).
- [ ] No console/network errors introduced; no Lighthouse-SEO regressions; existing E2E green.

## Test / verification plan
- **Wave A (Jasmine)**: `seo.service.spec.ts` (DOCUMENT-injection pattern from
  `structured-data.service.spec.ts`): title brand-suffix idempotency, canonical normalization
  (query/fragment/trailing-slash stripped), og/twitter/robots tags set, `onLangChange` re-application,
  noindex path. `seo-title.strategy.spec.ts`: deepest-route `data.seo` resolution + default-meta-on-nav
  reset. Component specs: product-detail sets `setProductData` with **effective price/availability/absolute
  image**, sets `noindex` on a 404 slug (H5), and **fetches exactly once per `(slug,lang)` change**
  (R2 #3 no-double-fetch); breadcrumb emits via head service + cleans up (template `<script>` gone);
  product-list sets `noindex` for `search`/`page`/filter params but **NOT** for `utm_*`/`gclid` (R2 #2).
  **Break-the-code probe**: temporarily double-append the brand suffix → the idempotency test must fail.
- **Wave B (ClimaSite.Api.Tests, Testcontainers)**: seed active+inactive products/categories/brands and an
  in-window + expired promotion → assert sitemap **includes** active, **excludes** inactive/expired, uses
  `/products/category/{slug}`, valid XML, `<lastmod>` present, absolute `<loc>`. Assert robots content-type,
  Disallow set, **no `/api`** Disallow, absolute `Sitemap:`. **Forwarded-header simulation** (needs
  `XForwardedHost` enabled): send `X-Forwarded-Host`/`-Proto` → assert emitted base host+scheme match
  (council #2/R2 #1); set `Seo:SiteBaseUrl` → assert it wins. **Prod fail-closed**: Environment=Production +
  empty `Seo:SiteBaseUrl` → `/sitemap.xml` returns 503 and robots omits the `Sitemap:` line (council R2).
  Assert `/api/*` responses carry `X-Robots-Tag: noindex`. **Config validation**: a non-absolute / non-https
  `Seo:SiteBaseUrl` is rejected+logged (and treated as unset). **Smoke**: every static `<loc>` path maps to
  a real registered route (no soft-404 feed).
- **/acceptance (REAL running stack, both waves)**: drive the live app — view-source/`<head>` on home,
  a product, a category, FAQ, a legal page (title/desc/canonical/OG/JSON-LD correct, no leak across SPA
  nav); switch language and re-check title; confirm private routes show `noindex`. `curl /robots.txt` and
  `/sitemap.xml` (incl. via the nginx-shaped path with forwarded headers) and validate the XML. Commit a
  PASS report at `.planning/acceptance/B-044-seo.md` whose `commit:` matches the merged tip, per wave.
- **CI** (six required checks) is the evidence of record.

## Out of scope (explicit — tracked elsewhere / follow-ups)
- **SSR / prerender** and therefore full social-scraper OG fidelity, server-side `X-Robots-Tag` for *every*
  SPA route, and a true edge **404** for unknown product URLs → **DEC-SSR** (owner-gated).
- **hreflang / per-language URLs** → needs an i18n-URL decision (couples with DEC-SSR).
- nginx **single-canonical-host redirect** (apex↔www) + **trailing-slash redirect** → documented ops
  follow-ups (canonical normalization in-app covers the SEO signal now).
- Image sitemaps / news sitemaps / sitemap-index sharding (not needed under 50k URLs).
