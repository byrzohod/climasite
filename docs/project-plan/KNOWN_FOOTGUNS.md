# Known footguns — climasite

Recurring traps that have each cost a debugging cycle more than once. Committed here (not just in an
agent's memory) so any session/agent has them in-context at gate time. Terse by design; add to it when a
new footgun bites twice. Last updated: 2026-07-02.

## Formatting / CRLF

- **The C# format gate is `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes` at DEFAULT severity.**
  Run it via **`scripts/ci/format-check.sh`** (CI and local use the identical command). **NEVER run
  `dotnet format --severity info`** — it reformats the whole ~280-file repo; recover with
  `git checkout -- '*.cs'`.
- **CRLF is a false alarm, not a gate.** `git diff --check` flags CRLF as trailing-whitespace, but
  `.editorconfig` leaves `end_of_line` unset, so the real gate (`dotnet format --verify-no-changes`)
  passes. Don't "fix" CRLF to satisfy `git diff --check`.
- **The Edit tool normalizes a mixed-ending file to its dominant EOL**, which can balloon a diff. For a
  surgical change to a CRLF file: `perl -0777 -i -pe 's/\r\n/\n/g' <file>` → edit → re-CRLF only if HEAD
  was CRLF, or use a `\r?\n`-aware `perl` replace directly.
- An **EF-generated migration** `.cs`/`.Designer.cs` can carry a UTF-8 BOM that the format gate flags —
  strip it. The EF model **snapshot** legitimately keeps its BOM (auto-generated); leave it.

## Dependency audit

- Suppressions live in **`security/dependency-audit-allowlist.txt`** (`id | pkg | reason | tracking |
  expiry`), read by `scripts/ci/dependency-audit.sh`. The gate fails on any un-allowlisted vulnerable
  package **and** on any past-expiry allowlist entry. A **`NuGetAuditSuppress` in a `.csproj` does NOT
  affect this gate** — `dotnet list --vulnerable` ignores it, so the allowlist file is the only lever.

## /acceptance on the running app

- **`/acceptance` on the REAL app is mandatory** for behavior/source changes — it has caught fatals that
  every unit test missed (e.g. an `NG0200` circular DI that broke all runtime SEO while 1764 specs stayed
  green).
- **Output-cache staleness:** `Program.cs` sets a global 5-min `AddOutputCache` base policy, so a public
  GET can serve a **stale** body right after a mutation. Symptom: the plain GET is stale but the same URL
  with `?_cb=<rand>` is fresh. During `/acceptance`, **append `?_cb=<rand>`** to a cached GET to read
  post-mutation. For an endpoint whose data a user expects to change immediately after their own write,
  add `[OutputCache(NoStore = true)]` (or tag + `EvictByTagAsync`).

## PR checklist gate

- **The `PR Checklist` gate reads the trigger event-payload body, not the live PR.** Editing the PR body
  and re-running the failed job replays the **stale** payload. Include `## Summary` + `## Testing`
  up front; to fix after the fact, **push a commit** (`git commit --allow-empty` works) to fire a fresh
  `synchronize` event.
- The auth-path sub-check (requires a `/security-review` mention when auth/payment/GDPR/Stripe paths
  change) fires on **test-only** additions under `src/app/auth/**` too — mention it in the body up front.

## E2E

- **NEVER reintroduce `LoadState.NetworkIdle`** in E2E — the app lazy-`@defer`s header/footer ~3.2s
  post-nav, so NetworkIdle's quiet window never settles deterministically. Use `BasePage.SettleAsync`
  (locator auto-wait) + web-first `Expect`.
- **Any price/number display change breaks the .NET E2E price parsers** (`CartPage`/`ProductPage`/
  `CheckoutPage` `decimal.Parse` UI text). Grep `tests/ClimaSite.E2E` for `Parse` on UI text and update
  the parsers when you change how money renders.

## Reusable council findings (backend correctness)

- **`UseAuthentication()` must precede `UseRateLimiter()`** — otherwise `context.User` is empty and a
  per-user rate-limit partition **silently no-ops**.
- With `EnableRetryOnFailure` on, **make the read + state-transition decision OUTSIDE the retrying
  `strategy.ExecuteAsync`** — a commit-unknown retry that re-reads committed state can run the OPPOSITE
  transition (e.g. vote→un-vote). Inside: one from-state-gated conditional op whose rows-affected gates an
  atomic **`ExecuteUpdate` counter delta** — never a tracked load-increment-save (lost update + double-apply).

## Local run

- API `:5029`, Angular `:4200`, shared-infra Postgres `:5432` (NOT the appsettings `:5433`). Override
  `ConnectionStrings__DefaultConnection` directly (not `DATABASE_URL`, which forces `SSL Mode=Require` and
  fails against local non-SSL Postgres). **Canonical shared-infra creds live in `~/Projects/shared-infra/.env`.**
