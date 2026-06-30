# ClimaSite — Deploy Runbook (Railway)

**Status:** Readiness artifacts prepared (OPS-08). The deploy itself is **owner-gated** — it needs the
owner's Railway account, secrets, and managed add-ons. Work through the
[Owner-action checklist](#5-owner-action-checklist--the-5-ops-08-confirmation-questions) in one pass.

**Platform:** Railway (owner-decided).
**Last reviewed:** 2026-06-27 (config + code review; nothing deployed yet — see
`MEMORY` / OPS-08 / SEC-01: as of the last check no live Railway deploy exists, so the well-known
dev admin is *latent*, not active).

> This runbook is config-review + procedure. It does **not** deploy anything and changes no application
> code. Where it recommends a change to a deploy artifact, it says so explicitly and cross-references the
> tracked backlog item (OPS-03/04/05/07, SEC-06/07/08) rather than making the change here.

---

## 0. Canonical artifact map (READ FIRST — there are duplicates)

The repo currently contains **two parallel sets** of Dockerfiles and **three** `railway.toml` files.
Only one set is wired into Railway. The canonical mapping (derived from the `railway*.toml` that
actually carry `dockerfilePath`) is:

| Service | Railway config (root) | `dockerfilePath` it points at | Canonical Dockerfile |
|---|---|---|---|
| **Web** (Angular + nginx) | `railway.toml` | `Dockerfile.web` | **`/Dockerfile.web`** (root) |
| **API** (.NET 10) | `railway.api.toml` | `Dockerfile.api` | **`/Dockerfile.api`** (root) |

**Duplicates that are NOT used by these configs (do not edit them expecting an effect — they are dead):**

- `src/ClimaSite.Api/Dockerfile` — byte-for-byte identical to `Dockerfile.api`. **Dead duplicate.**
- `src/ClimaSite.Web/Dockerfile` — near-identical to `Dockerfile.web` but uses a **different build
  context** (`COPY package*.json` / `COPY . .` — i.e. it expects the build context to be
  `src/ClimaSite.Web/`, whereas `Dockerfile.web` expects the **repo root** and copies
  `src/ClimaSite.Web/...`). **Dead duplicate**, and a foot-gun if someone points Railway at it.
- `src/ClimaSite.Api/railway.toml` — a **third** Railway config, `dockerfilePath = "Dockerfile"` +
  `watchPatterns = ["src/**"]`. Only takes effect if a Railway service has its **root directory set to
  `src/ClimaSite.Api`**. **Ambiguous / dead** under the root-config topology above.
- `src/ClimaSite.Web/nginx.conf` — a **stale** standalone server block: `listen 80` (hardcoded, ignores
  Railway's `$PORT`) and **no `/api/` proxy**. The build actually uses `nginx.conf.template`
  (see §2). **Stale copy — never copied into the image.**

> **✅ DONE (OPS-03, 2026-06-27):** the four dead/stale files above were **deleted** — the root
> `Dockerfile.api` / `Dockerfile.web` + `railway.toml` / `railway.api.toml` are now the **single canonical
> set** (one Dockerfile + one Railway config per service). The `.github/workflows/deploy.yml` CD workflow
> is still TODO under OPS-03 (owner-gated — needs the Railway project + a `RAILWAY_TOKEN` secret).

### Config-review findings (flag list)

| # | Severity | File(s) | Finding | Recommendation / tracking |
|---|---|---|---|---|
| F1 | Medium | `src/ClimaSite.Api/Dockerfile`, `src/ClimaSite.Web/Dockerfile`, `src/ClimaSite.Api/railway.toml` | **Duplicate/ambiguous deploy artifacts.** Editing the wrong copy silently does nothing. | Delete duplicates; one-per-service (OPS-03). |
| F2 | Medium | `src/ClimaSite.Web/nginx.conf` | **Stale nginx config** (`listen 80`, no `/api/` proxy). Not used by the image but invites confusion. | Delete; `nginx.conf.template` is canonical (OPS-03). |
| F3 | Medium | `Dockerfile.api` | **API does not honor Railway's `$PORT`.** It hardcodes `ASPNETCORE_URLS=http://+:8080` and `EXPOSE 8080`. Railway injects `$PORT`; the app ignores it. | Works **only if** the Railway API service's target port is set to **8080** (owner action, §5). Long-term: make the entrypoint bind `$PORT` (OPS-03/07). |
| F4 | Medium | `Dockerfile.api`, `Dockerfile.web` | **Containers run as root** — no `USER` directive (API runs `dotnet` as root; nginx master as root). | Add non-root `USER` (tracked OPS-07). |
| F5 | Medium | `src/ClimaSite.Web/docker-entrypoint.sh` | **Web entrypoint defaults `API_URL` to `http://localhost:8080`.** A web service deployed without `API_URL` passes its `/health` check while every `/api` call 502s. | Fail hard on unset `API_URL` in prod (OPS-07); always set `API_URL` (§3). |
| F6 | High (config hygiene) | `src/ClimaSite.Api/appsettings.json` | **Non-empty dummy secrets committed** (Stripe `sk_test_…`/`pk_test_…`/`whsec_…`, a JWT `Secret`, a dev DB password). They are baked into the published image. They are not *real* prod secrets, but the non-empty Stripe dummies **defeat fail-fast**: a by-the-book deploy can silently run with dummy payment keys. | Remove dummy keys + add Production startup validation (SEC-07). For now: **the env vars in §3 override all of these at runtime** — set them all. |
| F7 | Medium | `Program.cs` (Swagger) | **Swagger UI is served unconditionally** (no environment gate) — it will be live in prod at `/swagger`. | Gate out of Production (SEC-06). |
| F8 | Low | `Program.cs` (ForwardedHeaders) | `KnownProxies`/`KnownNetworks` cleared (honors any proxy's `X-Forwarded-For`) — acceptable until topology is pinned; small spoofing surface. | Restrict to the proxy network once topology is known (SEC-03 follow-up, OPS-08). |
| F9 | Low | `Dockerfile.api` | Installs `curl` into the runtime image purely for `HEALTHCHECK`. Railway uses its own `healthcheckPath` (`/health`), so the in-image `HEALTHCHECK` + `curl` are redundant on Railway and add attack surface. | Optional: drop the Docker `HEALTHCHECK` for Railway (OPS-07). |

**Naming-convention mismatch (important, SEC-07):** the code reads **mixed** conventions —
flat env vars for some keys, ASP.NET config-section keys for others. The CLAUDE.md "Environment
Variables" table documents flat names that **do not all reach the code**. The §3 matrix below uses the
names the code **actually reads** and flags the mismatches. Trust §3 over the CLAUDE.md table until
SEC-07 reconciles them.

---

## 1. Topology

Two Railway services + three managed/external backing stores. The browser talks only to the Web
service; the Web nginx reverse-proxies `/api/*` to the API service over Railway's private network.

```
                       ┌──────────────────────────────────────────────┐
                       │                  Railway project                │
                       │                                                 │
  Browser ── HTTPS ──► │  Web service  (Dockerfile.web)                  │
  (custom domain)      │   nginx + Angular static bundle                 │
                       │   listens on $PORT                              │
                       │   proxies /api/* ──► API_URL ──┐                │
                       │                                 ▼               │
                       │  API service  (Dockerfile.api)                  │
                       │   .NET 10 / ASP.NET Core (:8080)                │
                       │   /health /health/ready /health/live           │
                       │      │            │            │                │
                       └──────┼────────────┼────────────┼────────────────┘
                              ▼            ▼            ▼
                       Postgres 16    Redis 7     Object storage
                       (managed)     (managed)    (MinIO svc / external S3)
```

- **Web service** — built from `Dockerfile.web`; serves the compiled Angular SPA via nginx, listens on
  Railway's `$PORT` (templated into `nginx.conf.template` by `docker-entrypoint.sh`), and proxies
  `/api/` to the API via `$API_URL`. SPA routing falls back to `index.html`.
- **API service** — built from `Dockerfile.api`; ASP.NET Core on `:8080` (see F3 — set Railway target
  port to 8080). Reads connection info from env vars; runs DB migrations + admin bootstrap on startup
  (see §4); exposes three health endpoints.
- **Postgres 16** — Railway-managed plugin or external. The API reads it via `DATABASE_URL`
  (`postgresql://…`; the app converts URL→Npgsql and disables SSL only for `*.railway.internal`).
- **Redis 7** — Railway-managed plugin or external, via `REDIS_URL` (used for distributed cache +
  output cache).
- **Object storage** — MinIO (Railway service) **or** external S3-compatible store, for product images.
  Configured via `Minio:*` keys (see §3). Defaults are localhost — **must be set** for prod.

`docker-compose.yml` at the repo root is **local-dev only** (Postgres/Redis/MinIO/pgAdmin on local
ports) and per CLAUDE.md the project normally uses the **shared external dev stack**. It is **NOT** the
deploy topology and is never deployed to Railway.

---

## 2. How `$PORT` is handled

| Service | Reads Railway `$PORT`? | Mechanism |
|---|---|---|
| **Web** | ✅ Yes | `docker-entrypoint.sh` exports `PORT` (default 80) and `envsubst` writes `listen ${PORT};` into `nginx.conf.template` → `default.conf`, then `nginx -t` + `exec nginx`. |
| **API** | ❌ No (see **F3**) | Dockerfile hardcodes `ASPNETCORE_URLS=http://+:8080` + `EXPOSE 8080`. The owner must set the Railway API service's **target/exposed port to 8080** so Railway routes to it. (Long-term fix: bind `$PORT` — OPS-03/07.) |

---

## 3. Environment-variable matrix

"Service": **API** = the .NET service, **Web** = the nginx/Angular service.
"Set in": Railway **service variable** (Settings → Variables) for that service. Managed plugins
(Postgres/Redis) expose `DATABASE_URL` / `REDIS_URL` you reference; everything else is a secret you
paste in. **Nested keys** (`Stripe:SecretKey`, `Minio:Endpoint`, …) are set on Railway using the
ASP.NET double-underscore form: **`Stripe__SecretKey`**, **`Minio__Endpoint`**, etc.

### Required for a working production deploy

| Variable (as the code reads it) | Service | Required? | Secret? | Notes / where set | Code ref |
|---|---|---|---|---|---|
| `DATABASE_URL` | API | **Required** | secret | `postgresql://user:pass@host:port/db`. App converts URL→Npgsql; SSL `Require` unless host ends `.railway.internal`. | `DependencyInjection.cs:22`, `Program.cs:187` |
| `REDIS_URL` | API | **Required** | secret | `redis://[:pass@]host:port`. SSL on unless `.railway.internal`. | `DependencyInjection.cs:55`, `Program.cs:192` |
| `JWT_SECRET` | API | **Required** | secret | ≥32 chars. **Production throws at startup if unset** (`JwtConfiguration:21`). | `JwtConfiguration.cs`, `TokenService.cs:24`, login/refresh handlers |
| `JWT_ISSUER` | API | **Required** (prod) | config | e.g. `https://api.climasite.<domain>`. Falls back to localhost otherwise. | `Program.cs:66`, `TokenService.cs:27` |
| `JWT_AUDIENCE` | API | **Required** (prod) | config | e.g. `https://climasite.<domain>` (the SPA origin). | `Program.cs:69`, `TokenService.cs:30` |
| `Stripe__SecretKey` | API | **Required** (payments) | secret | Real `sk_live_…` (or `sk_test_…` for a test deploy). **`StripePaymentService` throws at startup if empty** — but the committed dummy is non-empty, so it won't throw on the dummy (F6/SEC-07). | `StripePaymentService.cs:18` |
| `Stripe__PublishableKey` | API | **Required** (payments) | config (public) | `pk_live_…`. The **API serves this to the SPA** via `GET /api/payments/config` — the **frontend does NOT bake a Stripe key at build time**. | `PaymentsController.cs:45` |
| `Stripe__WebhookSecret` | API | **Required** (payments) | secret | `whsec_…` from the Stripe webhook endpoint you create. Webhook 500s/ignores if missing. | `WebhooksController.cs:51` |
| `ADMIN_EMAIL` | API | **Required** (first deploy) | config | Bootstraps the **first** admin in Production. **Production startup THROWS if no admin exists and this is unset** (`DataSeeder.cs:127`). | `DataSeeder.cs:109` |
| `ADMIN_INITIAL_PASSWORD` | API | **Required** (first deploy) | secret | Initial admin password (Identity rules: ≥8, upper/lower/digit/non-alnum). Rotate after first login. Can be removed once an admin exists. | `DataSeeder.cs:110` |
| `API_URL` | **Web** | **Required** | config | Internal URL of the API service, e.g. `http://climasite-api.railway.internal:8080` (or the public API URL). nginx proxies `/api/` here. **Defaults to `http://localhost:8080` if unset (F5) → silent 502s.** | `docker-entrypoint.sh:8` |
| `PORT` | Web (+API) | Injected by Railway | — | Railway sets it. Web honors it; API does not (F3). Do not set manually. | `docker-entrypoint.sh:5` |
| `ASPNETCORE_ENVIRONMENT` | API | **Required** = `Production` | config | Set in `Dockerfile.api` already; keep `Production`. Gates seeding/admin-bootstrap/JWT-fail-fast behavior. | `DataSeeder.cs:45/127`, `Program.cs:299` |
| `Seo__SiteBaseUrl` | API | **Required / acceptance-blocking** (any non-dev/test deploy) | config | The **single canonical public origin** (one host — pick apex **or** www and redirect the other), e.g. `https://www.climasite.com`. Used to build absolute `sitemap.xml` `<loc>` entries + the `robots.txt` `Sitemap:` line. **Must be an absolute `https://` URL.** When empty/invalid in Staging/Production the API **fails closed**: `GET /sitemap.xml` → **503** and `robots.txt` omits the `Sitemap:` line (logged), so the deploy never publishes canonical URLs from an untrusted request host. | `SeoController.cs`, `SeoBaseUrlResolver.cs` |

> **SEO routing (B-044):** nginx proxies the crawler files to the API via two exact-match locations —
> `location = /robots.txt` and `location = /sitemap.xml` — that set the real `Host $host` (so the API sees
> the public canonical host) plus `X-Forwarded-Proto $scheme` (so the emitted `<loc>` URLs are `https`).
> Private SPA prefixes (`/admin`, `/account`, `/checkout`, `/cart`, `/login`, `/register`,
> `/forgot-password`, `/reset-password`, `/wishlist`) get an `X-Robots-Tag: noindex` nginx `location` block
> (which re-declares the server security headers, since nginx `add_header` stops inheriting once a block
> sets its own). That block is declared **before** the static-asset regex so an asset-like private path is
> covered too — the nginx smoke should assert both a private page (`curl -I /admin` → `X-Robots-Tag` +
> `X-Frame-Options`/`X-Content-Type-Options`/`X-XSS-Protection`) **and** an asset-like private path
> (`curl -I /admin/x.js` → still carries `X-Robots-Tag`, not the cache-asset headers). `/api/*` responses
> carry `X-Robots-Tag: noindex` from the API. The apex↔www canonical redirect and trailing-slash redirect
> remain ops follow-ups (the in-app canonical covers the SEO signal).

### Object storage (product images) — required if storage is used; defaults are localhost

| Variable | Service | Required? | Secret? | Notes | Code ref |
|---|---|---|---|---|---|
| `Minio__Endpoint` | API | Required (if storage used) | config | Host:port of MinIO/S3, e.g. `bucket.s3.<region>.amazonaws.com` or the Railway MinIO host. | `DependencyInjection.cs:91` |
| `Minio__AccessKey` | API | Required (if storage used) | secret | Storage access key. | `:92` |
| `Minio__SecretKey` | API | Required (if storage used) | secret | Storage secret key. | `:93` |
| `Minio__UseSSL` | API | Recommended `true` (prod) | config | `true`/`false`. Default `false`. Use `true` for external S3. | `:94` |
| `Minio__PublicUrl` | API | Required (if storage used) | config | Public base URL used to build image links shown to shoppers. | `:95` |

### SMTP / email (required only when sending real email)

Email defaults to **placeholder mode** in Development; for real mail, set `Email__UsePlaceholder=false`
plus the SMTP block. CLAUDE.md documents flat `SMTP_HOST`/`SMTP_PORT`/`SMTP_USER`/`SMTP_PASSWORD`, but
the code reads **`Email:*` config keys** — set the `Email__*` forms (SEC-07 mismatch).

| Variable (code-actual) | CLAUDE.md name (does NOT reach code) | Service | Required? | Secret? | Notes |
|---|---|---|---|---|---|
| `Email__UsePlaceholder` | — | API | set `false` for real mail | config | `true` logs instead of sending. |
| `Email__SmtpHost` | `SMTP_HOST` | API | Required (real mail) | config | SMTP server host. |
| `Email__SmtpPort` | `SMTP_PORT` | API | Required (real mail) | config | e.g. `587`. |
| `Email__Username` | `SMTP_USER` | API | Required (real mail) | secret | SMTP username. |
| `Email__Password` | `SMTP_PASSWORD` | API | Required (real mail) | secret | SMTP password. |
| `Email__From` / `Email__FromName` / `Email__EnableSsl` | — | API | Optional | config | Sender identity + TLS. |

### Other config the code reads (set as needed, sensible defaults exist)

| Variable | Service | Required? | Notes | Code ref |
|---|---|---|---|---|
| `AllowedOrigins__0`, `AllowedOrigins__1`, … | API | **Required** (prod) | CORS allowlist — set to the **real SPA origin(s)** (the custom domain). Default is localhost only, so prod CORS will block the SPA if unset. | `Program.cs:108` |
| `AllowedHosts` | API | Recommended | Currently `*`; tighten to the API host (SEC-08). | `appsettings.json:67` |
| `Outbox__Enabled` / `Outbox__PollIntervalSeconds` / … | API | Optional | Email outbox worker tuning. | `appsettings.json:16` |
| `Contact__RecipientEmail` | API | Optional | Where contact-form mail goes. | `appsettings.json:23` |
| `BankTransfer__Iban` / `__AccountName` / `__BankName` | API | Optional | Shown to shoppers choosing bank transfer. | `appsettings.json:26` |

> **Secrets summary (never in git; set as Railway service variables):** `DATABASE_URL`, `REDIS_URL`,
> `JWT_SECRET`, `Stripe__SecretKey`, `Stripe__WebhookSecret`, `ADMIN_INITIAL_PASSWORD`,
> `Minio__SecretKey`, `Email__Password`. The repo tracks **no** `.env`/secret files (verified) and
> `.dockerignore` excludes `.env*` from images — keep it that way.

---

## 4. Deploy procedure

Today this is **Railway auto-deploy on push to `main`** once the owner connects the repo (no CD
workflow exists yet — OPS-03 will add `.github/workflows/deploy.yml`). Per deploy:

1. **Build.** Railway builds each service from its Dockerfile (`Dockerfile.api`, `Dockerfile.web`).
   Multi-stage builds are correct: API = .NET 10 SDK → `aspnet:10.0` runtime; Web = `node:22-alpine`
   build → `nginx:alpine`. Build context is the **repo root** for both (so the root Dockerfiles, not the
   `src/**` duplicates).
2. **Run migrations.** **Migrations apply automatically on API startup** — `DataSeeder.SeedAsync()`
   calls `_context.Database.MigrateAsync()` on boot (`DataSeeder.cs:52`), with EF Core retry-on-failure
   (×3) and EF's built-in migration lock. **Caveats (OPS-04):** a bad migration crash-loops the API ×3
   against prod data with **no automated rollback target**; with >1 replica the boots are serialized by
   the migration lock but still risky. Recommended target state: gate `MigrateAsync` to
   Dev/Testing and run a dedicated **pre-deploy** migration step (`scripts/migrate.sh` / EF bundle)
   before flipping traffic — tracked as OPS-04. Until then, **deploy a single replica** and treat every
   migration as expand-contract (see §Rollback).
3. **Admin bootstrap (first deploy only).** On an empty prod DB the API creates the first admin from
   `ADMIN_EMAIL` + `ADMIN_INITIAL_PASSWORD`; it **throws on startup** if neither those are set nor an
   admin already exists (so the deploy fails loudly rather than running admin-less). The well-known
   `admin@climasite.local` / `Admin123!` is **never** seeded outside Dev/Testing.
4. **Health-check gate.** Railway waits on `healthcheckPath` (`/health`, `healthcheckTimeout = 300s`)
   before routing traffic. Endpoints:
   - `GET /health` — overall (DB + Redis).
   - `GET /health/ready` — readiness, runs **all** checks (DB + Redis) — use this for the smoke gate.
   - `GET /health/live` — liveness, **no** checks (process up).
   - Web service: nginx returns `200 healthy` at `/health`.
5. **Smoke test (post-deploy, before declaring success).** Hit the live URLs:
   ```
   curl -fsS https://<api-host>/health/ready          # expect 200 Healthy
   curl -fsS https://<api-host>/api/products?lang=en   # expect 200 + items JSON (public catalog)
   curl -fsS https://<api-host>/api/categories?lang=en # expect 200 + categories
   curl -fsS https://<api-host>/api/payments/config     # expect 200 + publishableKey (proves Stripe wired)
   curl -fsS https://<web-host>/                        # expect 200 + SPA index.html
   curl -fsS https://<web-host>/api/products?lang=en    # expect 200 (proves nginx→API proxy + API_URL)
   ```
   If `/api/products` works on the API host but **not** the web host, `API_URL` is wrong (F5).
6. **Done.** Tag the release (no git tags exist yet — OPS-09) so there is a rollback target and the
   deployed artifact is correlatable to source.

---

## 5. Owner-action checklist — the 5 OPS-08 confirmation questions

These are the parts **only the owner can do** (Railway account, secrets, managed add-ons). They are
framed as the **5 OPS-08 confirmation questions** from `PRIORITIZED_BACKLOG.md` (OPS-08) /
`_review/devops.md` §6 so you can answer them in one pass. Record the answers in
`docs/project-plan/DECISIONS.md` and update the OPS-08 row.

**Q1 — Is Railway auto-deploy connected, and which services/config files?**
- [ ] Create the Railway **project** and **two services**: `climasite-web` (root config `railway.toml`
      → `Dockerfile.web`) and `climasite-api` (root config `railway.api.toml` → `Dockerfile.api`),
      build context = repo root.
- [ ] Connect the **GitHub repo** for auto-deploy on `main` (or wait for the OPS-03 CD workflow).
- [ ] Set the **API service target port = 8080** (because the API does not honor `$PORT` — F3).
- [ ] Set every API env var from §3 (all required + payments + storage as applicable). Set `API_URL`
      on the **web** service to the API's internal URL (F5). Set `AllowedOrigins__0` to the SPA origin.
- [ ] **Answer:** is auto-deploy now live, and which config file backs each service? → record it.

**Q2 — Has a production DB ever been provisioned/seeded? (If yes → rotate the seeded admin NOW.)**
- [ ] Confirm whether any prod/staging DB was ever booted by this app.
- [ ] **If yes:** the well-known `admin@climasite.local` / `Admin123!` (Dev/Testing default) and/or a
      committed dummy JWT secret may be live → **rotate immediately**: delete/disable that admin user,
      rotate `JWT_SECRET`, rotate any real Stripe/SMTP keys that were ever set, and re-deploy. (As of
      the last OPS-08 check nothing was deployed, so this is *latent* — confirm it stayed that way.)
- [ ] **Answer:** ever seeded? yes/no + remediation done → record it.

**Q3 — What is production object storage?**
- [ ] Decide: Railway **MinIO** service vs **external S3-compatible** store vs **none yet**.
- [ ] Provision it; set `Minio__Endpoint/AccessKey/SecretKey/UseSSL/PublicUrl` on the API (§3). Defaults
      are localhost and will not work in prod.
- [ ] **Answer:** which storage + is it set? → record it.

**Q4 — Are Postgres backups enabled, and has a restore been tested?**
- [ ] Provision **Postgres 16** + **Redis 7** (managed plugins or external); reference `DATABASE_URL` /
      `REDIS_URL` on the API.
- [ ] **Enable automated Postgres backups/snapshots** and record the RPO/RTO.
- [ ] **Test a restore** into a scratch DB at least once (a backup never restored is not a backup).
- [ ] Enable **TLS + a custom domain** on the web service (and API host if public); set
      `JWT_ISSUER`/`JWT_AUDIENCE`/`AllowedOrigins` to the real domains.
- [ ] **Answer:** backups on? restore tested (date)? → record it.

**Q5 — Replica count / develop-branch + CI triggers?**
- [ ] Decide the **replica count** for each service. **Keep the API at 1 replica until OPS-04** (startup
      migrations + non-evicting in-memory locks like BUG-09 + OutputCache coherence are single-instance
      assumptions).
- [ ] Decide whether CI's `develop` trigger should be removed (no `develop` branch exists) — OPS-06.
- [ ] **Answer:** replicas per service + develop-branch decision → record it.

---

## 6. Rollback

**App rollback (fast, < 5 min):**
- Railway → service → **Deployments** → pick the previous successful deploy → **Redeploy / Rollback**.
  (This is why **OPS-09 git tags** matter — tag each release so the deployed image maps to a known
  commit and there is an explicit rollback target.)
- Because the API runs migrations on boot, a rollback **re-runs migrations for the older image** — only
  safe if migrations are **expand-contract** (next item).

**Database rollback caution (expand-contract — non-negotiable):**
- **Never** ship a destructive schema change (drop column/table, narrow a type, rename) in the same
  deploy that removes the code using it. EF Core migrations have **no down-migration applied
  automatically**, and there are no down-tests — a forward-only auto-migrate means a bad migration is
  not cleanly reversible by an app rollback.
- Pattern: **(1) expand** — additive migration deploys first (new nullable column / new table), old +
  new code both work. **(2) migrate data + ship code** that uses the new shape. **(3) contract** — only
  after the new code is fully live and proven, a later deploy drops the old shape.
- If a deploy fails on a bad migration: the API crash-loops (×3 retry) and Railway keeps the previous
  deploy serving (health gate). **Roll back the app**, fix the migration forward (or restore from the
  §5-Q4 backup if data was mutated), and redeploy. Do **not** hand-edit the prod schema.
- Target state (OPS-04): move migrations to a **pre-deploy** step so a bad migration is caught **before**
  any new traffic is routed and the previous version keeps serving untouched.

---

## 7. Known gaps blocking / shadowing go-live (cross-reference)

These are tracked elsewhere; listed here so the deploy owner sees them in one place. None are fixed by
this runbook.

| Gap | What's left for go-live | Tracked |
|---|---|---|
| **Frontend CSP + CORS allowlist + AllowedHosts** | The API's CSP is strict (it serves JSON only). The **web** service that serves the SPA needs a **Stripe-compatible CSP**; CORS `AllowAnyHeader()` → explicit allowlist; `AllowedHosts` `*` → the real host. All depend on the final topology/domain. | **SEC-08** (headers done; these 3 remain) |
| **Swagger in prod** | `/swagger` is served unconditionally — gate it out of Production (or auth-protect it). | **SEC-06** |
| **Secret-name convention + dummy keys** | Mixed flat/section env-var names; non-empty dummy Stripe keys in `appsettings.json` defeat fail-fast. Standardize names, remove dummies, add Production startup validation. | **SEC-07** (F6) |
| **Observability** | Correlation IDs + structured logs + graceful flush are in; still missing: error tracker (Sentry — vendor choice O-4), OpenTelemetry, JSON-console-in-Production, health-check uptime alerting. | **OPS-05**, **PROD-103** |
| **CD pipeline + one-config-per-service** | No `deploy.yml`; delete the duplicate Dockerfiles/railway.toml/nginx.conf (F1/F2). | **OPS-03** |
| **Startup migrations** | Move `MigrateAsync` off the boot path into a gated pre-deploy step. | **OPS-04** |
| **Non-root containers + entrypoint hardening** | Add `USER`; fail hard on unset `API_URL`. | **OPS-07** (F3/F4/F5) |
| **Forwarded-headers / rate-limit bucket** | `KnownProxies` cleared until topology pinned — once known, restrict to the proxy network. | **SEC-03** (F8) |
| **Release tags** | No git tags → no rollback target / artifact↔source mapping. | **OPS-09** |
| **Branch protection on `main`** | Must be on before Railway auto-deploy from `main`, or unreviewed commits ship. | **OPS-02** |

---

## See also

- `docs/project-plan/PRIORITIZED_BACKLOG.md` — OPS-02..OPS-09, SEC-01/03/06/07/08 (the tracked work).
- `docs/project-plan/_review/devops.md` — the full devops review + the 5 owner questions.
- `docs/project-plan/PRODUCTION_READINESS_CHECKLIST.md` — launch-blocker table.
- `skills/deploy-checklist.md` / `skills/release.md` — the deploy + release skills this runbook follows.
- Root deploy artifacts: `railway.toml`, `railway.api.toml`, `Dockerfile.web`, `Dockerfile.api`,
  `src/ClimaSite.Web/nginx.conf.template`, `src/ClimaSite.Web/docker-entrypoint.sh`.
