---
unit: B-044-seo (Wave A — frontend SEO meta + JSON-LD wiring)
surface: ui (real running app — Angular CSR on :4200 against the live API on :5029 + shared Postgres)
result: PASS
date: 2026-06-30
commit: feature/b-044-seo-frontend tip (validated against the working diff; final squash tip on merge)
driver: Playwright (project Chromium) drove the REAL running app and inspected the JS-injected <head>
  (title / canonical / robots / og+twitter / JSON-LD) across 9 routes + a back-nav leak check + an
  EN→DE language switch. Plus full frontend suite + build + lint + i18n parity.
---

# Acceptance — B-044 Wave A (SEO foundation: per-route meta + structured-data wiring)

## Why this gate mattered
Unit tests (1764) and three diff-council rounds were GREEN, yet the first runtime acceptance pass
**FAILED**: every route rendered the static `index.html` title, `canonical:null`, `jsonld:[]`, and the
console was flooded with **`NG0200: Circular dependency in DI detected for _Router`**. The Angular Router
constructs the `TitleStrategy` during its own init, so `SeoTitleStrategy`/`SeoService` injecting `Router`
was circular → the strategy threw on every navigation → no SEO ever applied. Fixed by removing `Router`
from both (default canonical path from `document.location.pathname`) and moving the NavigationStart
JSON-LD reset into an `app-initializer`. The re-run below is the post-fix evidence.

## Scenarios (real running app, post-fix)
| # | Route | Title | Canonical | robots | JSON-LD | Result |
|---|---|---|---|---|---|---|
| 1 | `/` (home) | `HVAC, Heating & Climate Solutions \| ClimaSite` | `…/` | index,follow | Organization + WebSite | ✅ |
| 2 | `/products` | `All Products \| ClimaSite` | `…/products` | index,follow | ItemList(12) + BreadcrumbList(2) | ✅ |
| 3 | `/products?brand=Daikin&page=2` | (list) | **stripped → `…/products`** | **noindex,follow** | ItemList + Breadcrumb | ✅ |
| 4 | `/products/b-002-acceptance-on-sale-ac` | `B-002 Acceptance On-Sale AC \| ClimaSite` | full slug URL | index,follow | Product (**price 499.99, InStock**) + BreadcrumbList(3) | ✅ |
| 5 | `/faq` | `Frequently Asked Questions \| ClimaSite` | `…/faq` | index,follow | FAQPage | ✅ |
| 6 | `/terms` | `Terms of Service \| ClimaSite` | `…/terms` | index,follow | — | ✅ |
| 7 | `/login` | `Sign In \| ClimaSite` | `…/login` | **noindex,follow** | — | ✅ |
| 8 | `/cart` | `Shopping Cart \| ClimaSite` | `…/cart` | **noindex,follow** | — | ✅ |
| 9 | `/this-route-does-not-exist-zzz` (404) | `Page Not Found \| ClimaSite` | (path) | **noindex,follow** | — | ✅ |

## Cross-cutting checks
- **No meta/JSON-LD leak across SPA navigation**: product → back to home leaves home with ONLY
  Organization + WebSite JSON-LD (no stale Product/BreadcrumbList), title back to home, canonical `…/`. ✅
- **Brand suffix idempotent**: every title carries exactly one ` | ClimaSite`. ✅
- **Canonical normalization**: query string + fragment stripped; trailing slash removed except root. ✅
- **Effective price**: product JSON-LD emits 499.99 (the current selling price), not the 599.99
  struck-through original. ✅
- **Language switch EN→DE**: home title becomes `HLK, Heizung & Klimalösungen | ClimaSite` and
  `<html lang>` becomes `de` (B-033). ✅
- **og:image** is the 1200×630 raster `og-default.png` (social scrapers ignore SVG); og:url is the
  self-referential canonical. ✅
- **Console**: **zero errors** on every route (the NG0200 flood is gone). ✅

## Automated evidence
- `ng build --configuration=development` → 0 errors. `ng lint` → 0 errors (6 pre-existing warnings in
  untouched specs). `npm run test:i18n` → PASS (926 keys, en/bg/de parity). `ng test` → **1764 SUCCESS**
  (incl. brand-suffix idempotency break-the-code probe; out-of-order `loadSeq`/`fetchSeq`/category-`loadSeq`
  race guards; marketing-param-not-noindex; availability-from-stock; NavigationStart-reset helper + negative).
- Cross-vendor Codex council (gpt-5.5@xhigh) on the diff: design 3 rounds → CONVERGED; Wave-A diff
  REWORK (out-of-order races, breadcrumb-emitted-nowhere, no-variant InStock, FAQ leak) → fixed →
  category-race High → fixed → CLEAN; NG0200 DI-fix councilled (see PR).

## Limitations (documented, out of scope — DEC-SSR)
Runtime-injected OG/Twitter/JSON-LD are seen by Googlebot (renders JS) but NOT non-JS social scrapers
(Slack/Facebook) — those get the static `index.html` brand defaults. hreflang omitted (single-URL runtime
i18n has no per-language URLs). robots.txt + dynamic sitemap.xml are **Wave B** (backend).

## Verdict
**PASS** — zero blocker, zero major; all scenarios green on the REAL running app in EN + DE. Frontend
only; no backend change; no migration.
