<!--
PROC-01 PR template. The CI "PR Checklist" gate (scripts/ci/pr-checklist.sh) requires the Summary and
Testing sections to be filled in (not left as placeholders). Delete the HTML comments as you fill it in.
-->

## Summary

<!-- What does this PR change and why? 1–3 sentences. Link the feature folder / backlog ID if any (e.g. docs/features/GAP-10/, PROC-01). -->

## Changes

<!-- Bullet the notable changes. -->
-

## Testing

<!-- REQUIRED. How is this verified? Name the tests added/updated and how they run. CI is the evidence of record. -->
- Tests added/updated:
- CI: all required checks green (this PR)

## Checklist

- [ ] Tests added/updated for the change (or `[no-tests]` justified in the commit body for non-code changes)
- [ ] `dotnet format` + `ng lint` clean (the Lint & Format gate)
- [ ] Coverage stays ≥ 80% backend / ≥ 70% frontend (the Coverage Gate)
- [ ] Docs updated per `DEV_WORKFLOW.md` §5 (CHANGELOG `[Unreleased]`, status tables, ADR if a decision was made)
- [ ] Works in light **and** dark themes (UI changes)
- [ ] Works in EN / BG / DE (user-facing text via i18n keys)

## UI changes — screenshots (delete if none)

<!-- Attach light + dark screenshots (and mobile if layout-affecting). Visual review is ephemeral per the SDLC plan. -->

## Migrations / env / config (delete if none)

<!-- Expand-contract migration steps; new env vars (with the name the code actually reads); feature flags. -->

## Reviews

- [ ] `/code-review` run; findings triaged
- [ ] `/security-review` run (REQUIRED for auth / payment / GDPR / config-touching changes)

🤖 PRs are merged on green CI; the review gate is the read-only `reviewer`/`verifier` agents + this template + the CI PR-checklist (no required human approval — see DEV_WORKFLOW.md).
