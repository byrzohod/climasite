# Architecture Decision Records

Every non-obvious architectural or technical decision in ClimaSite lives here as a numbered ADR. The format is lightweight but load-bearing — the goal is to make the *reasoning* survive the decision so future contributors (including future Claude sessions) can judge whether a decision is still valid.

## Format

- File name: `NNNN-kebab-case-title.md` where `NNNN` is a four-digit zero-padded sequence number (e.g. `0003-...`). This is the go-forward convention (it matches the most recent ADR `0001-...` and the SDLC pipeline's verify-plan gate). The legacy three-digit files (`001-`, `002-`) are left as-is — do not renumber them; continue from `0003` onward.
- Status: `Proposed` → `Accepted` → (optionally) `Superseded by ADR NNN` or `Deprecated`.
- Never edit the body of an accepted ADR to reverse its decision. Write a new ADR that supersedes it.

See [`000-template.md`](./000-template.md) for the skeleton.

## Index

| # | Title | Status | Date |
|---|---|---|---|
| [001](./001-home-page-concept.md) | Home page v3 concept (Configurator-First) | Accepted | 2026-04-08 |
| [002](./002-home-v3-stack-and-assets.md) | Home v3 stack, assets, and build order | Superseded by ADR 0003 (renderer) | 2026-04-08 |
| [0001](./0001-background-jobs-db-outbox.md) | Background jobs via `BackgroundService` + a DB email outbox | Accepted | 2026-06-16 |
| [0003](./0003-home-v3-rendering-canvas2d.md) | Home v3 configurator preview renders with Canvas 2D | Accepted | 2026-06-25 |
| [0004](./0004-gdpr-order-pii-retention.md) | GDPR erasure anonymizes Order PII but retains the invoice record | Accepted | 2026-06-27 |

## When to write an ADR

Write an ADR when the decision:

- Picks one option from several and the others are non-obvious losers (library choice, data model shape, protocol, build order, test strategy).
- Introduces a cross-cutting convention (naming, error handling, auth flow).
- Has consequences that will be painful to reverse (schema, public API shape, dependency adoption).
- Overrides or amends an earlier decision.

Do *not* write an ADR for: code style nits, single-file refactors, or decisions the framework/stack already makes for you.

## When to read ADRs

Before starting any feature whose subsystem has an ADR index entry, read the ADR. Before proposing a change that would contradict an ADR, read the ADR and then either: (a) honor the decision, or (b) write a new ADR that supersedes it with fresh rationale.
