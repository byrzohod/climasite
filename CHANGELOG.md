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

### Changed

- (pending) Home page replaced by Home v3 (Configurator-First wizard + recommendation slab + procedural 3D preview).

### Removed

- (pending) Legacy `features/home/` and its scroll-driven v2 implementation; superseded by `features/home-v3/` per ADR 001.

### Security

- (pending) JWT secret, Stripe keys, SMTP password rotated out of `appsettings.json` and into user-secrets / environment variables per Phase 5.

---

## Guidance for future entries

- Group changes under: Added, Changed, Deprecated, Removed, Fixed, Security.
- One bullet per user-visible or developer-visible change. No marketing copy.
- Link to the plan task ID in parens when relevant, e.g. `(HOME-005)`.
- Roll `[Unreleased]` into a dated release section on every tagged release; `/release` skill handles the mechanics.
