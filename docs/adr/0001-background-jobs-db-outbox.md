# ADR 0001 — Background jobs via `BackgroundService` + a DB email outbox

- **Status:** Accepted
- **Date:** 2026-06-16
- **Deciders:** Project owner (decision O-1), engineering
- **Task:** ARCH-05 (Milestone M2 foundation)

## Context

Several M2 features need work to happen reliably *outside* the request that triggers it:

- **GAP-03 — transactional emails** (order confirmation, welcome, password reset, shipped).
- **GAP-09 — notifications** and **wishlist price-drop / back-in-stock alerts**.
- **GAP-05 — contact endpoint** acknowledgements.

Today email is sent **inline and best-effort** inside command handlers (see
`ForgotPasswordCommandHandler`, `DeleteUserDataCommandHandler`): a wrapped `try/catch` swallows
failures. That means a transient SMTP outage silently drops the email, and a slow SMTP server adds
latency to the user's request. There is no retry, no audit trail, and no delivery guarantee.

We need a reliable mechanism that:

1. Does not lose messages across process crashes or SMTP outages.
2. Can be written **atomically with the business state change** (e.g. the order *and* its
   confirmation email commit together, or neither does).
3. Adds no request latency.
4. Introduces **no new infrastructure** — nothing is deployed yet (decision OPS-08) and the team
   does not want to operate a broker for v1.

## Decision

Adopt the **transactional outbox pattern** backed by PostgreSQL, drained by an in-process
`BackgroundService`.

- A new `outbox_messages` table stores each email as a durable row: semantic `type`, recipient,
  a JSON `payload`, `status` (Pending → Processing → Sent / Failed), `attempt_count`,
  `next_attempt_at`, `processed_at`, and `last_error`.
- Producers enqueue via `IEmailOutbox`:
  - `Add(message)` stages the row on the **shared `DbContext`** so it commits in the *same
    transaction* as the business change (the transactional guarantee).
  - `QueueAsync(message)` persists immediately for callers with no surrounding transaction.
- `IOutboxProcessor.ProcessPendingAsync()` claims a batch of due, pending messages
  (`status = Pending AND next_attempt_at <= now`, oldest first), attempts delivery through the
  existing `IEmailService` (dispatched by `type`), and on failure applies **exponential backoff**
  (`Base * 2^(attempt-1)`) until `MaxAttempts`, after which the message is marked `Failed`.
- `EmailOutboxBackgroundService` (in the API host) is a thin polling loop that resolves
  `IOutboxProcessor` from a fresh DI scope every `PollIntervalSeconds`. It is disabled via
  `Outbox:Enabled = false` (integration tests drive the processor directly for determinism).

The processing logic lives in the **Application layer** (depends only on `IApplicationDbContext`
and `IEmailService`), so it is unit-testable with the in-memory `MockDbContext`. Only the hosting
shell lives in the API project, where the ASP.NET hosting abstractions are already referenced.

### Configuration (`Outbox` section)

| Key | Default | Meaning |
|-----|---------|---------|
| `Enabled` | `true` | Run the polling loop. |
| `PollIntervalSeconds` | `15` | Sleep between drains. |
| `BatchSize` | `25` | Max messages claimed per drain. |
| `MaxAttempts` | `5` | Attempts before permanent failure. |
| `BaseRetryDelaySeconds` | `30` | Backoff base. |

## Consequences

**Positive**

- Emails survive crashes and SMTP outages; failures retry with backoff instead of vanishing.
- Atomic enqueue with business transactions (no "order saved but email lost" or vice-versa).
- Zero new infrastructure — reuses the existing Postgres and `IEmailService`.
- A queryable audit trail (`outbox_messages` rows with status/error) for support and ops.
- The same table generalizes to notifications and price-drop alerts (GAP-09) later.

**Negative / trade-offs**

- Polling adds a small, bounded delay (≤ `PollIntervalSeconds`) before delivery — acceptable for
  email.
- The current claim is **single-instance-safe** (one host). Running multiple API instances would
  let two workers grab the same batch. Before horizontal scaling we must add a DB-level claim
  (`UPDATE ... SET status = Processing ... RETURNING`, `FOR UPDATE SKIP LOCKED`, or a per-row lease).
  Tracked as a follow-up; not needed for v1 (nothing deployed, OPS-08).
- A separate `Sent` retention/cleanup job will eventually be needed so the table does not grow
  unbounded.

## Alternatives considered

- **Hangfire / Quartz** — capable, but adds a dependency and its own storage/dashboard for what is
  currently a single email use case. Rejected for v1.
- **RabbitMQ / a real broker** — operational overhead and new infrastructure; violates the
  "no new infra" constraint. Rejected.
- **Keep inline best-effort send** — the status quo; no delivery guarantee, adds request latency,
  no retry. Rejected (this is the problem we are solving).
