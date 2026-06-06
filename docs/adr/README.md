# Architecture Decision Records

Every non-obvious architectural or technical decision in ClimaSite lives here as a numbered ADR. The format is lightweight but load-bearing — the goal is to make the *reasoning* survive the decision so future contributors (including future Claude sessions) can judge whether a decision is still valid.

## Format

- File name: `NNN-kebab-case-title.md` where `NNN` is a zero-padded sequence number.
- Status: `Proposed` → `Accepted` → (optionally) `Superseded by ADR NNN` or `Deprecated`.
- Never edit the body of an accepted ADR to reverse its decision. Write a new ADR that supersedes it.

See [`000-template.md`](./000-template.md) for the skeleton.

## Index

| # | Title | Status | Date |
|---|---|---|---|
| [001](./001-home-page-concept.md) | Home page v3 concept (Configurator-First) | Accepted | 2026-04-08 |
| [002](./002-home-v3-stack-and-assets.md) | Home v3 stack, assets, and build order | Accepted | 2026-04-08 |

## When to write an ADR

Write an ADR when the decision:

- Picks one option from several and the others are non-obvious losers (library choice, data model shape, protocol, build order, test strategy).
- Introduces a cross-cutting convention (naming, error handling, auth flow).
- Has consequences that will be painful to reverse (schema, public API shape, dependency adoption).
- Overrides or amends an earlier decision.

Do *not* write an ADR for: code style nits, single-file refactors, or decisions the framework/stack already makes for you.

## When to read ADRs

Before starting any feature whose subsystem has an ADR index entry, read the ADR. Before proposing a change that would contradict an ADR, read the ADR and then either: (a) honor the decision, or (b) write a new ADR that supersedes it with fresh rationale.
