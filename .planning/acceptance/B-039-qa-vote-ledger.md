---
unit: B-039-qa-vote-ledger
surface: api + ui (real running API on :5029 + ng serve :4200 against shared Postgres :5432; migration applied on API startup)
result: PASS
date: 2026-07-01
commit: feature/b-039-qa-vote-ledger tip (validated against the working diff; final squash tip on merge)
driver: live-API vote flow (register/login → ask → admin-approve → vote) via curl/python, DB ledger inspection,
  and a Playwright anon-state check of the real Q&A vote buttons. Backend behaviour is also covered by 468
  Api integration tests (Testcontainers) incl. a real concurrent double-POST.
---

# Acceptance — B-039 per-voter Q&A vote ledger

Replaces the inflatable anonymous Q&A "helpful" voting with a per-voter ledger (mirrors `ReviewVote`).

## Setup (created live via the API)
Logged in as the seeded dev admin, registered a fresh voter, asked an **anonymous** question (votable) + a
question **owned by the voter** (self-vote target) + an answer, and admin-approved all three (the migration
`AddQaVoteLedgers` applied on API startup — both `product_question_votes` + `product_answer_votes` tables exist,
each with a UNIQUE index).

## Scenarios (real running API)
| # | Action | Result |
|---|---|---|
| 1 | `GET /questions/product/{id}` (anon) | ✅ each question carries `hasVotedHelpful:false`; answers carry `userVoteHelpful` (absent = no vote) |
| 2 | `POST /questions/{qA}/vote` (voter) | ✅ `{ helpfulCount:1, hasVotedHelpful:true }` |
| 3 | `POST /questions/{qA}/vote` again (voter) | ✅ **toggle-off** → `{ helpfulCount:0, hasVotedHelpful:false }` — no inflation |
| 4 | `POST /questions/{qA}/vote` (anonymous) | ✅ **401** |
| 5 | `POST /questions/{qB}/vote` (voter votes own question) | ✅ **400** (self-vote rejected) |
| 6 | `POST /questions/{random}/vote` (voter) | ✅ **404** (missing/non-approved) |
| 7 | `POST /answers/{a}/vote {isHelpful:true}` | ✅ `{ helpful:1, unhelpful:0, userVoteHelpful:true }` |
| 8 | `POST /answers/{a}/vote {isHelpful:false}` | ✅ **flip** → `{ helpful:0, unhelpful:1, userVoteHelpful:false }` |
| 9 | `POST /answers/{a}/vote {isHelpful:false}` again | ✅ **toggle-off** → `{ helpful:0, unhelpful:0 }` (no vote) |
| 10 | 6th vote within a minute | ✅ **429** — `[EnableRateLimiting("strict")]` (5/min) is live-proven |

## DB ledger integrity
- After the question toggle-off: **0 rows** in `product_question_votes` for that (question, voter) — the ledger
  reflects the un-vote; the count decremented via the atomic floored `ExecuteUpdate`.
- **UNIQUE(question_id, user_id)** and **UNIQUE(answer_id, user_id)** indexes present → one vote per voter per
  target enforced at the DB (also proven by the Api concurrent-double-POST integration test: one row, delta
  applied once, no 500).

## Frontend (live, anonymous — Playwright)
- The Q&A vote buttons render; **disabled for anonymous** users (`question-vote-btn` + `answer-helpful-btn`),
  `aria-busy` present, and a **login prompt** is shown. **No `climasite_voted_questions/_answers` localStorage
  keys** (the client-only tracking was removed). Zero console errors.

## Automated evidence
- `dotnet build` 0/0; `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes` clean (fixed a mid-fluent-chain
  comment + an EF-generated BOM on the migration). Core **428** · Application **912** (incl. 5 retry-idempotency
  tests + break-probe) · Api **468** (incl. concurrent double-POST, 401/404/400, GDPR delete) · frontend **1772**
  (incl. per-id pending-guard, active-state, toggle, 401, no-localStorage) · i18n 926 keys en/bg/de.
- Cross-vendor Codex council (gpt-5.5@xhigh): design converged over 3 rounds; backend diff REWORK
  (retry-idempotency under `EnableRetryOnFailure`) → fixed (decide-outside + ON CONFLICT + rows-affected-gated)
  → CLEAN; combined diff APPROVE-WITH-CHANGES → the frontend overlapping-request Low fixed (per-id guard).

## Notes / documented
- Voting now **requires authentication** (design-approved anti-inflation change; anon voting removed).
- GDPR: account deletion hard-deletes the user's Q&A votes; the denormalised counts remain as anonymized
  aggregates (mirrors `ReviewVote`); Q&A votes are delete-only in `ExportUserData` (as `ReviewVote` is).
- `"strict"` rate-limit is IP-partitioned (5/min); a per-user partition is a tracked follow-up.

## Verdict
**PASS** — the per-voter ledger stops count inflation (toggle-off, one row per voter, unique constraint),
enforces auth + approved-gating + self-vote rejection with correct status codes, is race-safe under retry and
concurrency, and the UI reflects authoritative server vote-state (no localStorage). Backend + frontend; one
expand migration; no data loss.
