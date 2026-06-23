---
name: council
description: Cross-vendor council — get a second, independent opinion from Codex (OpenAI gpt-5.5) alongside Claude on a high-stakes question, then synthesize to consensus. Use for whole-system design, high-stakes code review (auth/payments/migrations), contested research, implementation planning (phases/waves/units), and adversarial test-planning — whenever vendor-family blind spots matter. Codex is a READ-ONLY advisor; Claude orchestrates and is the only agent that applies changes. Trigger on "council", "second opinion", "cross-check with codex", "what would another model say", or before committing to an irreversible high-stakes decision.
---

# /council — Cross-vendor council (vendor diversity axis)

A third diversity axis on top of the panel you already run: **channel** (`/code-review`) → **angle** (`/review-orchestrate`) → **vendor** (`/council`). It runs the same question past **two model lineages** — Claude (native) and Codex/OpenAI (`gpt-5.5 @ xhigh`) — **blind and in parallel**, then synthesizes them with the §10.6 evidence-based rule. The payoff is **vendor-family-correlated blind spots**: failure modes one lineage systematically misses, the other catches.

## The one hard rule

**Codex is a READ-ONLY ADVISOR. Claude is the sole orchestrator and the only agent that runs commands or applies changes.**

- The Codex leg runs in a `read-only` sandbox (`orchestration/council.sh`): it cannot write files, commit, push, install, deploy, or synchronize anything (read-only inspection like `git diff` / `git log` is fine) — enforced by the sandbox, reinforced by a report-only preamble.
- **Both council members are advisory.** The Claude leg also runs **report-only** — spawn it without `Write`/`Edit` tools; it returns findings, it does not change files. Only the orchestrator (this parent session, *outside* the council) applies anything.
- A member's output is **advice only**. The council **never auto-applies** a suggestion. Claude (this session) evaluates it, and — if accepted — implements it through the normal workflow (plan → code → tests → review).
- Never escalate the Codex sandbox (`workspace-write` / `danger-full-access` / `--dangerously-bypass-…` are forbidden in the council).

## When to use

- **Whole-system design** (`/design-doc`) — Codex as one more blind architect.
- **High-stakes code review** (auth, payments, migrations, irreversible changes) — the cross-vendor review mode of `/code-review` / `/review-orchestrate`.
- **Contested research** (`/research-loop`) — cross-vendor fact-check on conflicting claims.
- **Implementation planning** (`/plan-tree`) — on high-stakes phases/waves/units, Codex as one more blind plan proposer.
- **Adversarial test-planning** (`/test-plan`) — Codex surfaces failure modes the Claude lineage misses.

## When NOT to use

- Routine / trivial work (typo, doc, one-liner, sub-~15-min units) — negative ROI; use self-review + one `/review`.
- Per-merge by default — the council is **opt-in per high-stakes unit**, never on every merge.
- Anything that must not leave the box where the owner has *not* accepted egress — **Codex egresses the prompt + any repo code it reads to OpenAI** (this owner accepts that everywhere; for a client/repo that doesn't, run Claude-only, below).

## Process

### 1. Frame the question
Write the task/question to a prompt file `(.planning/council/<topic>/<ts>/prompt.md)` — for review, include or instruct how to get the diff ("review the changes on this branch vs `main`; run `git diff main...HEAD`"). Keep it self-contained; both legs see the same prompt.

### 2. Run both legs BLIND, in parallel
- **Claude leg (native, preferred):** spawn a blind **report-only** subagent (Task / `orchestrator` / a `parallel-fanout` member) at `effort: 'max'` with the same prompt — **no `Write`/`Edit` tools** (it advises, it doesn't change files). It does not see the Codex output. Capture its answer.
- **Codex leg (read-only advisor):**
  ```bash
  AI/project-template/orchestration/council.sh \
    --out .planning/council/<topic>/<ts> \
    --prompt-file .planning/council/<topic>/<ts>/prompt.md
  # → writes <out>/codex.md (final message) + codex.jsonl (full log)
  ```
  Defaults to `gpt-5.5 @ xhigh`, `read-only`. Degrades gracefully (skips the leg) if `codex` isn't installed.

Run them concurrently — wall-clock ≈ the slower leg, not the sum.

### 3. Synthesize (Claude, max effort) — the 4-section output
Read both answers and produce, **preserving dissent, no debate, no majority-vote** (§10.6):
1. **Consensus** — claims both lineages reached independently (highest confidence).
2. **Disagreements register** — every conflict + an **evidence-based adjudication** (cite file:line / source). Never silently merged, never averaged.
3. **Unique findings** — caught by exactly one lineage (the cross-vendor payoff).
4. **Recommendation** — synthesized; flag "vendors materially disagree → human decides" where unresolved.

For high-stakes units, route each disagreement through an adversarial `claim-verifier` (the `pipeline.js` default-to-refute pass) so a confident-wrong vendor can't force a needless change.

### 4. Rounds + STOP (1 round is the architecture)
- **1 round** (both blind → synthesis) is the design. Agreement / no unique critical findings → **done**.
- **Hard cap ≤2 rounds.** Round 2 only for a *resolvable, evidence-shaped* disagreement (e.g. "Codex cites a CVE Claude missed → re-query both on that one point"); both legs stay blind. Otherwise → **escalate to a human**. The cap is the termination guarantee — "no consensus" can't stall forever.

### 5. Act (Claude only)
Claude decides what to adopt and applies it through the normal flow. Record a material decision as an ADR via `/kb-capture` (note which lineage surfaced it). Keep the run dir (`prompt.md`, `codex.md`, `codex.jsonl`, `synthesis.md`) as the audit trail.

## Guardrails

- **Recursion:** `council.sh` sets `VAULT_COUNCIL_DEPTH` — a council can never spawn a council. The Claude leg, if run as a process, uses `--bare` (no hooks). **Never launch the council from a Stop hook** (it can burn a whole session); the Stop hook only *suggests* it.
- **Egress (owner: allowed everywhere):** Codex sends the prompt + read code to OpenAI; auth = ChatGPT sign-in (`~/.codex/auth.json`). For a repo/unit where egress is unacceptable, run **Claude-only multi-run** (several blind Claude subagents = cross-run diversity, zero egress) — same synthesis, skip `council.sh`. Reconfirm per project — a client/private repo may mandate Claude-only.
- **Audit artifacts:** the run dir (`.planning/council/…`) holds prompts, code excerpts, and logs — keep `.planning/council/` **gitignored**; never commit or sync it.
- **Cost/latency:** ~2× a single-vendor pass; **minutes** at `xhigh`. Gate to high-stakes units; run detached for long passes (don't block on it).
- **Trusted-source-only:** both legs are first-party CLIs (Anthropic, OpenAI). No third-party orchestration frameworks, no unvetted MCP wrappers.

## See also
- `orchestration/council.sh` — the read-only Codex leg.
- `skills/review-orchestrate.md` (angle layer) · `skills/code-review.md` (channel layer) — `/council` is the vendor layer.
- `skills/design-doc.md`, `research-loop.md`, `plan-tree.md`, `test-plan.md` — stages that invoke the council.
- `Agent Workflow.md` §6 (Code Review), §10.6 (evidence-based panels, not debate), §12 (max-reasoning model policy).
