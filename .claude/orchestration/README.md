# Orchestration Cookbook

Reusable multi-agent patterns for the research and planning loops. These are **Dynamic-Workflow script templates** — copy one, adapt the constants/prompts to the task, and pass it to the `Workflow` tool. Each has a **sequential fallback** (plain `Task`-tool subagents) for when Dynamic Workflows aren't available.

The skills that drive these: `/research-loop` (uses `parallel-fanout` + `judge-panel` + `loop-until-dry`), `/plan-tree` and `/design-doc` (use `parallel-fanout` + `judge-panel`), `/review-orchestrate` (uses `parallel-fanout`), `pipeline` for per-item review→verify chains.

## The patterns

| File | Shape | Use for |
|------|-------|---------|
| `parallel-fanout.js` | N blind, diverse agents → consolidate ALL (with a disagreements register) | research streams, review angles, competing proposals |
| `judge-panel.js` | N proposers → M judges score by rubric → pick winner, **preserve dissent**, tie-break to human | architecture choice, plan selection |
| `loop-until-dry.js` | repeat rounds until K consecutive add nothing new (or budget/round cap) | unknown-size discovery; the research "second sweep" |
| `pipeline.js` | each item flows through all stages independently (no barrier) | review→verify per finding; per-unit plan→test-plan |
| `council.sh` | cross-vendor council: the **read-only Codex (OpenAI) leg** for `/council` (Claude leg runs native, in-session) | cross-vendor second opinion on high-stakes design / review / research / planning / tests |

## Hard rules (from the workflow blueprint §G/§H)

- **Evidence-based adjudication, never debate / majority-vote.** A single confident-wrong agent drops group accuracy 10–40%. Judges score against a rubric and cite evidence; they do not "discuss."
- **Blind generation.** Proposers/researchers do NOT see each other's output during generation — diversity is the point.
- **Consolidate ALL.** Conflicts are surfaced in a disagreements register, never silently merged.
- **Adversarial verification.** Material claims/findings get an independent verify pass (default-to-refute); survive only on ≥ majority.
- **Explicit STOP + budget.** Every loop has: a saturation threshold (< ~15% new verified items → stop), a round cap (≤2 research sweeps / ≤2 plan rounds), a per-loop agent-call ceiling, and a no-progress HALT (2 consecutive dry rounds → escalate to human).
- **Concurrency ceiling.** ≤16 concurrent (Workflow cap); a per-loop agent-call ceiling; every agent — proposers, judges, verifiers — runs at maximum reasoning effort (no downgrade). One-level supervisor→worker nesting only.

## Sequential fallback

When the `Workflow` tool isn't available, each template's header comment shows the equivalent using parallel `Task` subagents from the main session (4–6 at a time, same prompts, same STOP logic enforced by the orchestrator agent reading results between rounds). The `orchestrator` agent owns this fallback.

## Budgets

Scale fan-out to the turn's token target if one was set (the `+500k`-style directive surfaces as `budget` in a Workflow script). With no target, use the static defaults: research = 6 agents × 3 verify votes × ≤2 sweeps; planning = 3 proposers × ≤2 rounds.
