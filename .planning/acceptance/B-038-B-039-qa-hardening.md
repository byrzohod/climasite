---
unit: B-038-B-039-qa-hardening
surface: api (real running API on :5029 against shared Postgres :5432; Development env, rate limiter live)
result: PASS
date: 2026-07-01
commit: feature/b-038-b-039-qa-answered-state-ratelimit tip (validated against the working diff; final squash tip on merge)
driver: live-API Q&A moderation flow (ask → admin-approve question → submit answer → admin approve/reject) via
  curl/jq with cache-busting reads, DB inspection (docker exec psql), and a live vote-burst 429 check. Backend
  behaviour is also covered by 479 Api integration tests (Testcontainers) incl. a rate-limit-enabled two-user
  partition test, plus 968 Application + 430 Core unit tests. All break-probe-verified.
---

# Acceptance — B-038 Q&A answered-state correctness + B-039 follow-up (per-user strict rate limit)

## B-038 — a still-Pending answer must not flag a question "answered"

Setup (created live via the API): logged in as the seeded dev admin, asked a question, admin-approved it.
All reads use a random `?_cb=` query param to defeat the pre-existing 5-min output cache on the public
questions endpoint (see Observation below).

| # | Action | Result |
|---|---|---|
| 1 | approve question, `GET /questions/product/{id}` | ✅ question present, **unanswered** — `answeredAt:null`, `answerCount:0`, 0 answers shown |
| 2 | `POST /questions/{id}/answers` (a Pending answer) | ✅ question **STILL unanswered** — `answeredQuestions` unchanged, `answeredAt:null`, `answerCount:0`; the answer appears in the admin `pending` moderation queue (status Pending) |
| 3 | admin `POST /answers/{aid}/approve` | ✅ now **ANSWERED** — `answeredQuestions` +1, `answeredAt` set, `answerCount:1`, answer visible |
| 4 | submit a 2nd Pending answer while approved | ✅ `answerCount` stays **1**, `answeredAt` **unchanged** (pending answers are invisible / don't inflate) |
| 5 | admin `POST /answers/{aid}/reject` (last approved) | ✅ **self-heals** back to unanswered — `answeredQuestions` −1, `answeredAt:null`, `answerCount:0` (even with a Pending answer still present) |

DB cross-check confirmed the writes (`product_answers.status` transitions Pending→Approved→Rejected while
`product_questions.answered_at` tracks approved-answer existence). Re-verified end-to-end on the final code
(after the diff-council fixes) — each transition moves `answeredQuestions` by exactly 1.

**Observation (pre-existing, NOT a regression):** the public `GET /api/questions/product/{id}` is output-cached
by the global 5-min base policy (`Age` header present), so a freshly-approved answer isn't visible on the PDP
for up to 5 minutes. This predates B-038 (I changed no caching) and affects all Q&A moderation equally; noted
as an existing UX staleness item, out of scope for this unit.

## B-039 follow-up — the `strict-user` per-user rate-limit partition is live

| # | Action | Result |
|---|---|---|
| 6 | authenticated user votes on an approved answer 7× (5/min budget) | ✅ votes 1–5 → **200**, votes 6–7 → **429** — the `strict-user` policy is active on the vote endpoint (auth runs before the limiter; auth otherwise fully functional — admin login + every admin op worked) |

Per-user partitioning itself (two authenticated users behind one host get independent buckets) is proven by the
new `QaVoteRateLimitPartitionTests` integration test (rate-limit-enabled variant: user A throttled at 5/min,
user B on the same host still served) — **break-probe-verified**: reverting the middleware order (auth after the
limiter) makes user B get 429 and the test fails.

## Verdict
**PASS** — zero blockers. B-038 is fixed and self-healing (write-side + read-side derivation heals legacy
dirty data); B-039's per-user partition is live and regression-guarded. The only observation (output-cache
staleness) is pre-existing and out of scope.
