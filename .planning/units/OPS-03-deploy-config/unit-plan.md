---
unit: OPS-03-deploy-config
type: NORMAL
status: approved
plan_status: approved
created: 2026-06-27
design: ../../design/DESIGN.md
---

# Unit plan — OPS-03 (cleanup): delete the dead duplicate deploy config

## Context (verified — from the OPS-08 deploy-readiness review)
Railway uses the **root** `railway.toml`→`Dockerfile.web` (web) and `railway.api.toml`→`Dockerfile.api`
(api) — confirmed by their `dockerfilePath`. Four files under `src/**` are dead/stale duplicates and pure
edit-traps (verified: nothing in code/CI/compose references them — only docs that describe the problem):
- `src/ClimaSite.Api/Dockerfile` — **byte-identical** to root `Dockerfile.api`.
- `src/ClimaSite.Web/Dockerfile` — different build context (expects context = `src/ClimaSite.Web/`) → a foot-gun.
- `src/ClimaSite.Api/railway.toml` — a third Railway config (`dockerfilePath="Dockerfile"`), ambiguous.
- `src/ClimaSite.Web/nginx.conf` — stale (`listen 80`, no `/api` proxy); the image build uses
  `nginx.conf.template` (via the entrypoint `envsubst`), never this file.

CI builds via `dotnet`/`ng` directly (not these Dockerfiles), so deletion does not affect any CI job.

## Scope
1. `git rm` the four dead files above.
2. Update docs: OPS-03 backlog row → deletions DONE; `docs/runbooks/deploy.md` "delete under OPS-03 — not
   deleted here" → "deleted"; tick the duplicate-config item in `PRODUCTION_READINESS_CHECKLIST.md`.

## Out of scope (deferred — owner-gated)
The `.github/workflows/deploy.yml` CD workflow (build→migrate→Railway deploy) + the service→config mapping
doc need the owner's Railway project + a `RAILWAY_TOKEN` secret (auto-deploy), so they wait for the
OPS-08 owner setup. OPS-03 here is the **dead-file cleanup** half.

## Acceptance criteria
- [ ] The four dead files are gone; `git grep` finds no code/CI reference to them.
- [ ] Root `Dockerfile.api`/`Dockerfile.web` + `railway.toml`/`railway.api.toml` remain (canonical, untouched).
- [ ] Build + non-E2E test suites stay green (deletions don't touch the CI build path); E2E unaffected
      (uses `ng serve`, not the web Dockerfile).

## Verification
- `dotnet build ClimaSite.NoE2E.slnf` + the non-E2E suites green; confirm the canonical root files intact.
- `/acceptance`: N/A — no runtime/app behavior change (dead build-config deletion).
- Cross-vendor council on the change (per the standing cadence).
