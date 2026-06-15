# Changelog

All notable changes to ClimaSite are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Plan 18 — Project Completion master plan (`docs/plans/18-project-completion.md`) consolidating the final 9-phase path to production readiness.
- ADR 001 — Home page v3 concept decision (Configurator-First selected).
- ADR 002 — Home v3 stack, assets, and build order (Three.js latest, procedural geometry, rules-based scoring, backend-first).
- ADR index and template at `docs/adr/README.md` and `docs/adr/000-template.md`.
- Home v3 concept proposals and interactive HTML mock at `docs/concepts/home-v3/`.
- Gap audit report at `docs/audit/2026-04-08-gap-report.md`.
- Home v3 unit and E2E coverage for recommendations, translations, reduced motion, responsive viewports, keyboard navigation, and route behavior. (HOME-008, HOME-009, HOME-011)

### Changed

- Home page replaced by Home v3 with a configurator-first wizard, live room preview, real product recommendations, translated trust/category sections, and deferred below-fold content. (HOME-004..HOME-012)
- Production frontend API configuration now uses relative `/api` calls so deployed frontend traffic goes through the backend proxy instead of a cross-origin API URL.
- Authentication restore now retries `/me` after refresh when a persisted access token is expired, preventing valid refresh sessions from being dropped.
- Main layout now renders a lightweight global navigation shell immediately, while the heavier header/footer chunks load after the initial paint.
- `main` is now branch-protected (OPS-02): pull requests are required, all six CI checks (Unit, Integration, Frontend, Build, E2E, Test Summary) must pass, force-pushes and deletions are blocked, and conversation resolution is required. CLAUDE.md's post-implementation workflow now mandates the feature-branch → PR → green-CI → merge flow instead of pushing directly to `main`.

### Removed

- Legacy `features/home/` and its scroll-driven v2 implementation; superseded by `features/home-v3/` per ADR 001.

### Fixed

- Admin and root route empty-path redirects now use explicit `pathMatch: 'full'`.
- Home recommendations and product browsing E2E tests now wait on real API responses instead of brittle DOM timing.
- Missing Home v3 translation keys added across EN/BG/DE.

### Security

- DataSeeder is now environment-gated (SEC-01): the well-known default admin (`admin@climasite.local` / `Admin123!`) and the demo catalog are seeded **only** in Development/Testing. In Production/Staging the first admin is bootstrapped from the `ADMIN_EMAIL` / `ADMIN_INITIAL_PASSWORD` environment variables, and startup fails fast if no admin exists and no bootstrap credentials are provided.
- Production startup now requires `JWT_SECRET` instead of falling back to the committed development JWT secret.
- Test-only admin setup secret defaults are restricted to Development; Testing must configure `TestSettings:AdminSecret`.

---

## Guidance for future entries

- Group changes under: Added, Changed, Deprecated, Removed, Fixed, Security.
- One bullet per user-visible or developer-visible change. No marketing copy.
- Link to the plan task ID in parens when relevant, e.g. `(HOME-005)`.
- Roll `[Unreleased]` into a dated release section on every tagged release; `/release` skill handles the mechanics.
