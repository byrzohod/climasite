---
unit: B-039-qa-vote-ledger
type: NORMAL
status: approved
created: 2026-07-01
plan_status: approved
design: ../../design/DESIGN.md
council: codex gpt-5.5@xhigh — design R1 REWORK (4H+6M+4L folded) → R2 STILL-HAS-ISSUES (3 concurrency residuals → resolved via conditional rows-affected-gated SQL + ON CONFLICT DO NOTHING, §9) → R3 CONVERGED (council: "the described rows-affected-gated, ON CONFLICT DO NOTHING, ExecuteUpdate-only mechanism WOULD close the prior residuals" — design validated; it then noted src isn't yet that mechanism, which is the implementation's job). **The diff-council on the real code is the concurrency gate — the implementation MUST be exactly §9 (ON CONFLICT / ExecuteDelete / ExecuteUpdate / rows-affected gate), never the tracked Add/Remove methods.**
---

# Unit plan — B-039: per-voter Q&A vote ledger (stop inflatable "helpful" counts)

## Context / Definition of Ready
Product Q&A "helpful" voting is freely inflatable: `VoteQuestionCommand` just calls
`ProductQuestion.AddHelpfulVote()`, `VoteAnswerCommand` just increments `HelpfulCount`/`UnhelpfulCount` —
NO voter identity, NO ledger, NO dup check, and both endpoints (`POST /api/questions/{id}/vote`,
`POST /api/questions/answers/{id}/vote`) are **anonymous** (no auth attr). A test even asserts a user can
vote twice (1→2). Reviews already solved this with a per-voter `ReviewVote` ledger; this unit mirrors it
for Q&A and hardens the gaps a design council found (auth identity, concurrency, approval-gating, drift,
GDPR, status codes).

**Verified:** `ProductQuestion` = `Guid? UserId`, `Status` (QuestionStatus: Pending/**Approved**/…),
`HelpfulCount`, `AddHelpfulVote()` (no Remove). `ProductAnswer` = `Guid? UserId`, `Status`
(AnswerStatus.**Approved**), `HelpfulCount`+`UnhelpfulCount`, `AddHelpfulVote()`/`AddUnhelpfulVote()` (no
Remove). Public read (`GetProductQuestionsQuery`) only exposes `Status==Approved` (answers too).
`QuestionsController` captures NO user identity anywhere. `ReviewVote`/`VoteReviewCommand`/
`ReviewVoteConfiguration`/`AddReviewVotes` migration + `DeleteUserDataCommand` hard-delete = the template.

## Decisions (council-adjudicated)
- **Authenticated-only** voting (mirror ReviewVote) — anon-token voting rejected (spoofing/privacy/support
  cost). This is a **user-facing behavior change**: Q&A voting now requires login.
- **Toggle-off on repeat** (click same vote again → un-vote) — accepted deviation from ReviewVote's
  no-op-on-same, VALID because the UI shows an authoritative active/pressed state. A toggle-off only ever
  decrements the vote **the ledger created** (never the legacy baseline).
- **Count drift**: keep the existing denormalised counts as a **legacy anonymous baseline** (do NOT zero
  them in the migration); the ledger governs post-cutover per-user state. count = legacy_baseline + Σ(ledger).

## Scope / approach (mirror ReviewVote; ships as ONE PR — backend + FE + migration)

### Core (entities)
1. **`ProductQuestionVote(questionId, userId)`** (BaseEntity) — presence = one "helpful" vote (questions are
   helpful-only). **`ProductAnswerVote(answerId, userId, isHelpful)` + `ChangeVote(bool)`** (mirror ReviewVote).
   (Council L: `ProductQuestionVote` not generic `QuestionVote`.)
2. Add **`RemoveHelpfulVote()`** to `ProductQuestion` and **`RemoveHelpfulVote()`/`RemoveUnhelpfulVote()`** to
   `ProductAnswer` (floor at 0) for domain completeness/other callers — but the **vote handler does NOT use
   these tracked methods** (they load-increment-save = the lost-update bug); the vote path mutates the count
   via atomic SQL only (see §9). Keep them for the count-floor invariant + admin paths.
3. **Identity capture (council H1)**: `AskQuestionCommand`/`AnswerQuestionCommand` must persist the
   authenticated author's `UserId` (from `ICurrentUserService`) so `parent.UserId == voter` self-vote
   prevention is meaningful. Controller `AskQuestion`/`AnswerQuestion` set the command's `UserId` when
   authenticated (still `[AllowAnonymous]`-capable → UserId null for anon authors).

### Infrastructure
4. EF configs mirroring `ReviewVoteConfiguration`: `product_question_votes` UNIQUE(question_id,user_id);
   `product_answer_votes` UNIQUE(answer_id,user_id); single indexes on each FK; FK CASCADE to the parent +
   users; `ValueGeneratedNever` id; snake_case; `NOW()` timestamp defaults.
5. One **expand migration** `AddQaVoteLedgers` creating both tables (mirror `AddReviewVotes` DDL). **No
   backfill, no zeroing of existing counts** (council M — legacy baseline preserved).
6. `DbSet<ProductQuestionVote>` + `DbSet<ProductAnswerVote>` on `IApplicationDbContext` + `ApplicationDbContext`.

### Application
7. **`VoteQuestionCommand`** → `Result<VoteQuestionResult{HelpfulCount, HasVotedHelpful}>`. Voter from
   `ICurrentUserService.UserId` (fail → `Result.Failure`); **question exists AND `Status==Approved`** (council
   H3); **self-vote reject** when `question.UserId == voter` (only when non-null); **toggle** via the
   race-safe transitions in §9.
8. **`VoteAnswerCommand`** → `Result<VoteAnswerResult{HelpfulCount, UnhelpfulCount, UserVoteHelpful (bool?)}>`
   (**typed nullable bool**, not a loose string — council L). Answer exists AND `Status==Approved` AND its
   **parent question `Status==Approved`** (council H3); self-vote reject; toggle/flip via §9.
9. **Concurrency — conditional, rows-affected-gated, one mechanism (council H2 + re-council R2)**: all in ONE
   **execution-strategy transaction**, using conditional SQL whose **rows-affected gates the atomic count
   delta** (never the tracked domain methods, which double-apply against `ExecuteUpdate` and lose concurrent
   updates):
   - **First vote**: `INSERT … ON CONFLICT (…, user_id) DO NOTHING` (NO exception → the txn never aborts, so
     no re-read-in-aborted-txn hazard). If **1 row inserted** → `ExecuteUpdate(SetProperty(HelpfulCount,
     h => h + 1))`; if 0 (someone else already voted) → no count change.
   - **Toggle-off**: conditional `ExecuteDelete()` on `(target, user)`. If **1 row deleted** →
     `ExecuteUpdate(count => count - 1)` (floored ≥ 0); if 0 → no change.
   - **Answer flip**: conditional `ExecuteUpdate` on the ledger row `WHERE is_helpful = @old` setting
     `@new`. If **1 row changed** → apply BOTH atomic count deltas (old−1, new+1); if 0 → re-read + return
     current state.
   Counts are changed ONLY via `ExecuteUpdate` (atomic at the DB); the tracked `Add/RemoveHelpfulVote()`
   methods are NOT called in the vote path (§2). No `23505` catch/re-read inside an aborted transaction
   (the `ON CONFLICT` avoids the exception entirely). Wrap in the `DeleteUserDataCommand` execution-strategy
   pattern.
10. **Read side (council H3 + M8)**: `QuestionDto += hasVotedHelpful: bool`; `AnswerDto += userVoteHelpful:
    bool?`. `GetProductQuestionsQuery` uses `ICurrentUserService`; after paging approved questions+answers,
    **collect the page's question-ids + answer-ids and batch-load** the current user's vote rows into
    HashSet/Dictionary (NO per-row join); anonymous → false/null.
11. **GDPR (council M)**: `DeleteUserDataCommand` hard-deletes the user's `ProductQuestionVotes` +
    `ProductAnswerVotes` (RemoveRange by UserId, same transaction/execution-strategy) — but **does NOT
    decrement the denormalised counts** (they become anonymized historical aggregates, mirroring ReviewVote).
    `ExportUserDataQuery`: mirror how it treats `ReviewVote` — export the Q&A votes if it exports review votes,
    else document delete-only. (Verify at implementation.)

### API
12. `[Authorize]` on `VoteQuestion` + `VoteAnswer`; add a **stricter rate-limit** partitioned by user where
    possible (council M — the vote endpoints are cheap to spam). Handlers read the voter from
    `ICurrentUserService` (controller just enforces auth). **Map `Result` failures (council H4)**: not-found →
    **404**, self-vote / invalid-state → **400** (`[Authorize]` already yields 401 for no token); success →
    `Ok` with the result DTO. Mirror `ReviewsController`'s Result handling but with proper status codes.

### Frontend
13. `questions.service.ts`: `Question += hasVotedHelpful: boolean`; `Answer += userVoteHelpful: boolean | null`;
    the vote methods return the authoritative counts + state. **Replace the localStorage vote tracking**
    (`climasite_voted_questions/_answers`) with the server state; buttons show the user's own vote as an
    **active/pressed** state (NOT disabled-after-vote) + support toggle; **`[disabled]="!isAuthenticated()"` +
    a login prompt** for anon (mirror `product-reviews`); handle a `401` gracefully; reconcile the optimistic
    update with the returned authoritative counts. Add `data-testid`s to the vote controls (council L). New
    i18n keys (en/bg/de) for the voted/active states.

## Acceptance criteria
- [ ] A logged-in user voting the same question/answer twice does **not** inflate the count (2nd = toggle-off);
      the DB has at most one ledger row per (voter, target). Under **concurrent** same-user requests — double
      first-vote, double toggle-off, and answer helpful↔unhelpful flip — the count delta is applied **exactly
      once** (rows-affected-gated), with no lost update and no 500 (the `ON CONFLICT`/conditional ops never
      abort the transaction).
- [ ] Voting is rejected (401) for anonymous requests; (404) for a missing/**non-approved** question/answer;
      (400) for voting on your own question/answer.
- [ ] The public question list returns `hasVotedHelpful`/`userVoteHelpful` reflecting the CURRENT user's own
      votes (false/null when anonymous), via a batched load (no N+1).
- [ ] Account deletion hard-deletes the user's Q&A vote rows (counts left as anonymized aggregates).
- [ ] FE: the vote button shows the user's own vote as active, toggles on repeat, is disabled+login-prompted
      when logged out, and shows the authoritative count returned by the API. Works light+dark, EN/BG/DE.
- [ ] The pre-existing `VoteQuestionCommandHandlerTests` "votes twice → 1 then 2" is **inverted** to assert
      one-vote-per-user + toggle.

## Test / verification plan
- **Application unit** (mirror `VoteReviewCommandHandlerTests`): first vote → +1 + ledger row + returned
  state; repeat → toggle-off (−1, row gone); answer flip helpful↔unhelpful moves the tally; self-vote → 400;
  unauth → failure; not-found/non-approved → 404-mapped failure; break-the-code probe: remove the toggle
  guard → the "no inflation" test fails.
- **Api integration (Testcontainers)**: real unique-constraint (concurrent double-POST → one row, current
  state, no 500); `[Authorize]` 401; approved-gating 404; self-vote 400; `GetProductQuestions` populates
  `hasVoted*` for the authed caller; GDPR delete removes the rows; rate-limit 429 after the budget.
- **Migration**: applies cleanly; the two tables + unique indexes exist; existing counts untouched.
- **Frontend (Jasmine)**: button active-state from `hasVotedHelpful`/`userVoteHelpful`; toggle; anon disabled;
  401 handling; optimistic-count reconciliation.
- **/acceptance** on the REAL running stack: log in, vote a question + an answer, re-vote (toggle off), try
  to double-vote via rapid clicks, vote your own Q&A (blocked), log out (button disabled), reload (state
  persists from the server not localStorage). Committed PASS at `.planning/acceptance/B-039-qa-vote-ledger.md`.
- **CI** (six required checks) is the evidence of record. Cross-vendor council on the diff (migration+auth = hard bar).

## Out of scope
- A moderation/anti-abuse system beyond the per-user ledger + rate-limit. Backfilling legacy anonymous
  counts into the ledger (they stay as an anonymized baseline). Changing the ask/answer moderation flow
  (only the minimal authed-author `UserId` capture needed for self-vote prevention).
