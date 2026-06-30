---
unit: B-044-seo (Wave B — backend robots.txt + dynamic sitemap.xml + reliable noindex + nginx)
surface: api (real running ASP.NET Core API on :5029 against shared Postgres :5432, seeded ~2260 products)
result: PASS
date: 2026-06-30
commit: feature/b-044-seo-backend tip (validated against the working diff; final squash tip on merge)
driver: curl against the live API (robots.txt / sitemap.xml / /api X-Robots-Tag / product-detail M8) +
  XML well-formedness validation + DB cross-checks. The nginx proxy layer (Host/header re-declaration) is
  covered by the integration tests' forwarded-host simulation + a documented owner-run `nginx -t` smoke
  (placeholders need envsubst); fail-closed / config-validation are covered by the integration tests.
---

# Acceptance — B-044 Wave B (backend crawler files)

## Scenarios (real running API)
| # | Check | Result |
|---|---|---|
| 1 | `GET /robots.txt` → `200 text/plain` | ✅ |
| 1a | Disallows `/admin/ /account/ /checkout /cart /login /register /forgot-password /reset-password /wishlist` | ✅ |
| 1b | Does **NOT** disallow `/api` (Googlebot needs it to render the CSR app — council #1) | ✅ |
| 1c | Absolute `Sitemap: http://localhost:5029/sitemap.xml` line (dev base from request host) | ✅ |
| 1d | No `X-Robots-Tag` header on robots.txt (root file, not `/api`) | ✅ |
| 2 | `GET /sitemap.xml` → `200 application/xml`, **well-formed XML** | ✅ |
| 2a | **2285** `<loc>` entries (15 static + ~2256 products + categories + 14 brands + 3 promotions) | ✅ |
| 2b | Categories emitted as **`/products/category/{slug}`**, not `/categories/{slug}` (council #4 — no soft-404) | ✅ (`/products/category/accessories`, `…/ventilation`; zero `/categories/{slug}`) |
| 2c | Static pages present (`/`, `/products`, `/faq`, …); fixture product `b-002-acceptance-on-sale-ac` present | ✅ |
| 2d | `<lastmod>` (W3C `yyyy-MM-dd`) on 2270 entity URLs; absent on the 15 static pages (null UpdatedAt) | ✅ |
| 2e | No `X-Robots-Tag` header on sitemap.xml | ✅ |
| 3 | `GET /api/products?…` response carries `X-Robots-Tag: noindex` | ✅ |
| 4 | M8 — product-detail `ProductDto` exposes `metaTitle`/`metaDescription` (null-omitted by `WhenWritingNull`; no seeded product has curated meta — live-null is correct; passthrough proven by the +2 integration tests) | ✅ |

## Covered by tests (not separately reproducible live)
- **Prod fail-closed**: `Environment=Staging` (same non-dev branch; a Production factory won't boot without
  Stripe/admin secrets) + empty `Seo:SiteBaseUrl` → `/sitemap.xml` 503, robots omits `Sitemap:`. (`SeoControllerTests`)
- **Config validation**: a non-absolute / non-https `Seo:SiteBaseUrl` is rejected + treated as unset. (`SeoBaseUrlResolverTests`)
- **Behind-proxy base**: request `Host` + `X-Forwarded-Proto: https` → emitted base host+scheme match (nginx sets
  real `Host $host`; `XForwardedHost` deliberately NOT enabled — council R3). (`SeoControllerTests`)
- **Active-only + window**: inactive products/categories/brands and an expired promotion are excluded; an
  in-window promotion is included. (`SeoControllerTests`)
- **Slug URL-encoding**: a slug with reserved characters (`a/b?c`) is `Uri.EscapeDataString`-encoded into the
  `<loc>` (`/products/a%2Fb%3Fc`), the raw value does not leak, and the XML stays well-formed. (`SeoControllerTests`)

## Automated evidence
- `dotnet build ClimaSite.sln` → 0 errors / 0 new warnings.
- `dotnet test` (per-project): **Core 424**, **Application 893** (incl. SeoBaseUrlResolver 11, SeoDocumentBuilder
  8, M8 passthrough 2), **Api 456** (incl. 10 new SEO integration; Testcontainers postgres:16/redis:7) — all green.
- A test caught a real **cross-host output-cache poisoning** risk: the app's global path-keyed `AddBasePolicy`
  was caching the SEO responses → fixed with `[OutputCache(NoStore=true)]` on the controller (the
  host-independent `(path,lastmod)` enumeration is the only thing cached, in `IMemoryCache`; council #3).
- Cross-vendor Codex council on the diff (see PR).

## Limitations / follow-ups (documented)
- nginx `-t` smoke (Host `$host`, `X-Forwarded-Proto`, private-prefix `X-Robots-Tag` + re-declared security
  headers) needs `envsubst` of the template placeholders → owner-run at deploy. The header re-declaration
  (council R2) is in the template; assert it in the deploy smoke.
- `Seo__SiteBaseUrl` MUST be set for any non-dev/test deploy (acceptance-blocking; deploy.md row added) — until
  then a Staging/Prod sitemap fails closed (503) by design.

## Verdict
**PASS** — robots.txt + a 2285-URL dynamic sitemap serve correctly on the real API; private areas disallowed
(but `/api` crawlable); category URLs are the real route; `/api` is `noindex` while the root crawler files are
not; M8 curated meta flows through. Backend + nginx + docs only; no migration.
