---
unit: B-038-B-039-qa-hardening
title: Q&A answered-state correctness (B-038) + per-user strict rate-limit partition (B-039 follow-up)
status: approved
owner: agent
created: 2026-07-01
severity: B-038 Medium (data integrity) · B-039-followup Low (availability hardening)
---

# Unit plan — Q&A answered-state correctness (B-038) + per-user strict rate-limit partition (B-039 follow-up)

## Context / Definition of Ready

Two small, backend-only hardening items surfaced as follow-ups of shipped Q&A work. Bundled because both
live in the Questions feature slice and neither warrants its own PR. Frontend-cleanup siblings
(FOUND-B002-noimage/orphans/loaderr-race) are **already merged** (#91) — this unit is the residual.

### B-038 — a still-`Pending` answer flips the question to "answered" (data-integrity bug)
`AnswerQuestionCommand` (`…/Features/Questions/Commands/AnswerQuestionCommand.cs:74-77`) calls
`question.MarkAsAnswered()` whenever `AnsweredAt` is null — but the just-created `ProductAnswer` defaults to
`Status = Pending` (it has NOT been moderated). Meanwhile the read side only ever counts/shows **approved**
answers:
- `GetProductQuestionsQuery.cs:41,45` filter/count by `q.AnsweredAt.HasValue`;
- `GetProductQuestionsQuery.cs:36` includes only `Answers.Where(a => a.Status == Approved)`, so
  `AnswerCount = q.Answers.Count` (line 93) = **approved** count.

Result: submit one un-moderated answer → the question is flagged answered (drops out of the admin
"unanswered" queue via `AnsweredQuestions`/the `IncludeUnanswered=false` filter) **while it renders 0
answers to the shopper**. Inconsistent, and it hides genuinely-unanswered questions from moderators.

The **approval** path already does the right thing: `ModerateAnswerCommand.cs:47-49` sets `AnsweredAt` on the
first *approved* answer. So the fix is to make answered-state derive **only** from approved-answer existence,
at the one boundary where that existence changes (answer moderation), and stop the write-on-submission.

There is also a **latent sibling bug**: `ProductQuestion.SetStatus` (line 58) sets `AnsweredAt` when the
question is approved and `Answers.Any()` — **any** status, not approved. And there is **no self-heal**: if the
only approved answer is later rejected/un-approved, `AnsweredAt` stays set (the same inconsistency, reversed).

### B-039 follow-up — the `strict` rate-limit policy is IP-partitioned, not user-partitioned
`Program.cs:276-285` partitions `"strict"` purely by `context.Connection.RemoteIpAddress`. The B-039 Q&A vote
endpoints are `[Authorize] + [EnableRateLimiting("strict")]`, so **every signed-in user behind one
NAT/CGNAT/corporate egress IP shares a single 5-req/min bucket** — one user's votes throttle everyone else's.

## Scope / approach

Backend only. No migration (no schema change — `AnsweredAt` already exists; we only change *who writes it*).
Deliver in ONE PR: council the design → implement + tests → `dotnet format` + build + unit/integration tests
green → council the diff → `/acceptance` on the real app → PR → 6 green CI checks → squash-merge.

### B-038 — reconcile answered-state at the approval boundary (single source of truth)

1. **Domain** (`ProductQuestion.cs`):
   - Add `void RefreshAnsweredState()` — idempotent reconciliation from the **loaded** `Answers` collection:
     - set `AnsweredAt = DateTime.UtcNow` iff `Answers.Any(a => a.Status == Approved)` **and** `AnsweredAt`
       is currently null (first approved answer);
     - clear `AnsweredAt = null` iff **no** approved answer exists **and** `AnsweredAt` is set (last approved
       answer removed/un-approved);
     - otherwise no-op (stable timestamp — never overwrites an existing `AnsweredAt`). Calls `SetUpdatedAt()`
       only when it actually changes state.
   - **Remove `MarkAsAnswered()`** — the unconditional setter is the footgun that caused B-038; force all
     answered-state through the reconciler.
   - **Remove the `AnsweredAt` side-effect from `SetStatus`** — a question status transition must not guess
     answered-state (single responsibility). `SetStatus` becomes `Status = status; SetUpdatedAt();`.

2. **Write boundary**:
   - `AnswerQuestionCommand.cs` — delete the `if (!question.AnsweredAt.HasValue) MarkAsAnswered();` block.
     Submitting an answer (always Pending) no longer touches answered-state.
   - `ModerateAnswerCommand.cs` — load the sibling answers into the tracked graph
     (`.Include(a => a.Question).ThenInclude(q => q.Answers)`) and, after `answer.SetStatus(request.NewStatus)`,
     replace the conditional `MarkAsAnswered()` with `answer.Question.RefreshAnsweredState()`. Because EF
     identity-resolves the mutated `answer` into `question.Answers`, the reconciler sees the new status —
     purely in-memory, **no post-mutation DB round-trip** (avoids the unsaved-change-vs-DB-WHERE divergence
     hazard). This handles first-approval (set) AND last-un-approval (clear).
   - `ModerateQuestionCommand.cs` — **unchanged**. `AnswerStatus.Approved` is only ever written by
     `ModerateAnswerCommand`, which now always reconciles; question approval cannot change approved-answer
     existence, so it needs no reconciliation. (Confirmed: grep shows no other writer of `AnswerStatus.Approved`
     outside seeding.)

3. **Read side** (`GetProductQuestionsQuery`) — **defensive derivation** (design-council Medium): derive
   answered-state from **approved-answer existence**, never the denormalized `AnsweredAt` column:
   - `IncludeUnanswered=false` filter and the `AnsweredQuestions` count use
     `q.Answers.Any(a => a.Status == Approved)` (SQL `EXISTS`) instead of `q.AnsweredAt.HasValue`;
   - the DTO exposes `AnsweredAt` only when an approved answer exists (else null), and `AnswerCount` /
     `Answers` explicitly filter to approved. This **heals existing dirty production data at read time (no
     backfill migration)** and makes any `AnsweredAt` drift user-invisible.

### B-039 follow-up — user-partitioned strict for authenticated votes (council-revised)

The design council caught three Highs that reshaped this (all confirmed against the code):
- **Middleware order** — `UseRateLimiter()` ran BEFORE `UseAuthentication()`, so `context.User` was empty at
  keying time (the naive fix was a silent no-op). **Fix:** move `UseAuthentication()` before
  `UseRateLimiter()`; keep `UseAuthorization()` after (unauthenticated votes are still IP-limited, then 401).
- **`strict` also guards ANONYMOUS endpoints** — `PaymentsController.CreatePaymentIntent` (`[AllowAnonymous]`),
  Contact, Installation. Blanket user-keying would let anyone widen their budget with any bearer token /
  farmed accounts. **Fix:** two policies, not one.

4. New `public static class RateLimitPartitioning` (`src/ClimaSite.Api/RateLimiting/RateLimitPartitioning.cs`):
   `IpKey` → `"ip:" + RemoteIpAddress ?? "unknown"`; `UserOrIpKey` → `"user:" + <NameIdentifier>` when present
   else `IpKey`. Mirrors `CurrentUserService`/`TokenService` (`ClaimTypes.NameIdentifier`); `user:`/`ip:`
   prefixes keep key spaces disjoint.
5. `Program.cs` — **`strict` stays IP-only, byte-identical** (anonymous Payments/Contact/Installation). New
   **`strict-user`** policy partitions by `UserOrIpKey`, applied ONLY to the two `[Authorize]` Q&A vote
   endpoints (`QuestionsController` VoteQuestion + VoteAnswer). `global`/`auth` stay IP-only (pre-auth).
   No change to `PermitLimit`/`Window` (5/min).

## Known residual (accepted, documented — design-council High #3)
Under **simultaneous** moderation of two answers of the **same** question by two admins, each with a stale
tracked graph, the persisted `AnsweredAt` column can transiently drift (e.g. both reject, neither clears).
This is a human-paced admin path, and the read side no longer trusts `AnsweredAt` for correctness (§3 above),
so the drift is **user-invisible** (at most a missing "answered on" timestamp) and **self-corrects** on the
next moderation of that question. Deliberately NOT worth an aggregate optimistic-concurrency token + migration
(unlike the high-frequency B-039 vote counts). Tracked as a note, not a blocker.

## Accepted tradeoff (diff-council Medium, round 2)
Moving `UseAuthentication()` before `UseRateLimiter()` (required so `strict-user` can key on the principal)
means a bogus-`Authorization`-header flood incurs a JWT signature check before the global IP limiter sheds it.
Accepted + documented rather than split into a separate pre-auth shield: the check is a microsecond
symmetric-key HMAC verify with no DB hit, and the global **100/min/IP** limiter still runs before the
controllers, so the expensive downstream (handlers/DB) stays shielded. The limiter is also now **fail-closed
in Production/Staging** (config can never disable it there; the `RateLimiting:Enabled` override only applies
in local/dev/test) — diff-council round-2 Low.

## Security / integrity notes
- User-partitioning does not weaken vote integrity: the B-039 ledger's `UNIQUE(target,user)` makes votes
  idempotent per target (re-vote = toggle), so 5/min/user is a secondary guard; account-farming for extra
  buckets is bounded by that uniqueness. Anonymous `strict` endpoints (contact, installation lead) keep IP
  limiting via the fallback — no regression.
- No PII in partition keys beyond the already-logged user id / client IP; keys live in memory only.

## Acceptance criteria
- [ ] Submitting an answer (Pending) leaves the question in the **unanswered** queue; `AnsweredAt` stays null.
- [ ] Approving the **first** answer sets `AnsweredAt`; the question leaves the unanswered queue and
      `AnswerCount ≥ 1` — no more "answered with 0 answers".
- [ ] Approving a **second** answer keeps the original `AnsweredAt` (stable, not overwritten).
- [ ] Rejecting/un-approving the **last** approved answer clears `AnsweredAt` (question returns to unanswered).
- [ ] Rejecting a pending answer never sets `AnsweredAt`.
- [ ] Two distinct authenticated users behind the same client IP resolve to **distinct** `strict` partition
      keys; anonymous requests resolve to an IP key; the same user resolves to a stable key.
- [ ] `dotnet format` clean; Core + Application + Api test projects green; FE unaffected.

## Test / verification plan
- **Core unit** (`ProductQuestionTests`): `RefreshAnsweredState` — approved→sets; only-pending→stays null;
  set-then-last-approved-removed→clears; idempotent (twice keeps timestamp); rewrite the two `SetStatus`
  answered-state tests (SetStatus no longer touches `AnsweredAt`); drop the `MarkAsAnswered` test (method gone).
  **Break-probe:** revert `Answers.Any(a => Approved)` → `Answers.Any()` and the only-pending test fails.
- **Application unit** (`AnswerQuestionCommandHandlerTests`): rework `…AndMarksQuestionAnswered` →
  `…DoesNotMarkAnswered_ForPendingAnswer` asserting `AnsweredAt` null (the B-038 behavioral break-probe);
  fix the second-answer test to seed answered-state via an approved answer + `RefreshAnsweredState`.
  (`ModerateAnswerCommandHandlerTests`): update `SeedAnswer` to also add the answer to `question.Answers`
  (mock does not run Include joins); keep first-approval-marks-answered; add un-approve-clears-answered +
  second-approval-keeps-timestamp; keep reject-does-not-mark. (`GetProductQuestionsQueryHandlerTests`):
  swap the seed's `MarkAsAnswered()` for `RefreshAnsweredState()` (approved answer already seeded).
- **Api unit** (`RateLimitPartitioningTests`, new): user set→`user:<id>`; two users→distinct; same user→stable;
  anonymous→`ip:<addr>`; no IP→`ip:unknown`; user present with IP→user precedence. **Break-probe:** revert
  `strict` to IP-only and the two-users-distinct test fails (both collapse to the same IP key).
- **`/acceptance`** on the running stack: create a question (API), approve it; submit an answer → assert the
  question still shows unanswered / 0 answers and remains in the admin unanswered queue; approve the answer →
  assert it flips to answered with the answer visible; reject it → assert it returns to unanswered. Commit the
  PASS report at `.planning/acceptance/B-038-B-039-qa-hardening.md` matching the merged tip.

## Out of scope (explicit)
- Global/auth rate-limit key format (unchanged, pre-auth by design).
- Full end-to-end limiter-exhaustion integration test (fixed-window timing is flaky and shares global state;
  the partition-key unit test + framework-guaranteed per-key partitioning is the evidence of record).
- Notifications `summary` recentCount (acknowledged intentional Low).
