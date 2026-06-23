---
name: cost-check
description: FinOps cost guardrails for the APP being built (never the agent's own reasoning model) -- an infrastructure cost diff on every PR via Infracost (pinned action SHA), an LLM token-budget ceiling per AI feature plus a per-run eval cost cap, and cloud cost guardrails (budgets, alerts, tagging). Use this whenever a PR adds or changes infrastructure (Terraform/Pulumi/CDK/Bicep, a new managed service, a bigger instance, storage, egress) or an AI/LLM feature (a model call, embedding job, RAG index, eval suite), or when the user mentions cloud cost, cost diff, Infracost, FinOps, token budget, cost cap, cost ceiling, runaway spend, cost guardrail, budget alert, or "how much will this cost". Runs design-time and on the PR stage; methodology-as-prompt plus one pinned CI action.
---

# /cost-check - FinOps cost guardrails for the app

Put a price tag on a change *before* it merges, so cost is a reviewed number on the PR -- not a surprise on next month's bill. This skill governs the **cost of the application being built**: its cloud infrastructure, and the per-call / per-run spend of any AI feature it ships. It does **not** govern the agent's own model -- the agent always runs at maximum reasoning, by policy; that is a fixed input, not a variable to optimize here. Output is three guardrails: an **Infracost diff on the PR**, an **LLM token + eval cost budget** per AI feature, and **cloud cost guardrails** (budget + alerts + tagging) that catch what the PR-time checks can't.

## When to use

Invoke this skill:
- **On the PR stage, when the diff touches infrastructure** -- any change to IaC (Terraform/Pulumi/CDK/Bicep), a new managed service, a larger instance class, added storage, a new queue/cache, or anything that moves egress.
- **On the PR stage, when the diff touches an AI/LLM feature** -- a new or changed model call, an embedding/ingestion job, a RAG/vector index, an agent loop, or an eval suite.
- **Design-time, inside `/design-doc`** -- to set the per-feature token ceiling and eval cost cap *before* the architecture commits to an expensive shape (a chatty agent, an oversized model, a per-request embedding).
- **When the user asks "how much will this cost"** -- for a planned feature or an architecture fork.

This is a **tiered toggle** (Blueprint §C). It is ON from MVP upward, because that is when real infrastructure and real per-call spend appear. A pre-MVP local-only hobby project with no cloud resources and no metered model calls has nothing to meter -- skip it until the first paid resource or paid model call lands.

## When NOT to use

- **The agent's own reasoning cost.** Out of scope by policy -- the agent runs max reasoning everywhere; do not "optimize" it here. The agent's *research/planning* loops in `/research-loop` and `/plan-tree` are bounded by call-count / round / concurrency (Blueprint §G/§H), **not** by any dollar/spend ceiling -- cost is never a constraint on the agent. This skill is exclusively about the **shipped app**.
- **Pure-local hobby tier with zero cloud + zero metered model calls** -- nothing is billed, so there is no budget to guard. Record "no metered cost" in DESIGN.md so the next phase re-checks when the first paid resource appears.
- **Performance/latency budgets** -- that is `/perf-budget` (LCP/INP/p95/bundle). Cost and latency often trade against each other (a smaller instance is cheaper but slower; a bigger model is better but pricier); the two skills are siblings that constrain the *same* architecture decision from different axes. Cross-reference, don't merge.
- **Runtime security of the spend path** (a leaked key racking up usage, an unauthenticated endpoint that anyone can call to burn tokens) -- that is `/security-review` + `/threat-model` (the DoS / excessive-agency rows). A cost cap is a *mitigation* those skills will call for; this skill implements it.

## Tiering (Blueprint §C)

| Tier | Default |
|------|---------|
| **hobby** | OFF -- unless the app provisions a paid cloud resource **or** makes a metered LLM/embedding call (then the relevant guardrail turns ON for that piece) |
| **MVP** | ON -- Infracost diff on infra PRs; token ceiling + eval cap on AI features; a cloud budget + alert |
| **production** | ON, always -- all three guardrails, plus cost allocation by tag/label and a cost-anomaly alert |

The quality bar is uniform across tiers (Blueprint LOCKED: no tiering of *rigor*) -- only which guardrails *apply* differs, driven by what the app actually provisions and calls. If a tier has no cloud and no metered calls, the toggle is simply moot, not "lower quality".

## Process

### Step 1: Read prior cost decisions first

Before pricing anything, read (do not re-derive):
- `Knowledge/<Project>/` -- prior cost ADRs (a chosen instance class, a "Sonnet for volume / Opus for quality-sensitive" decision, a self-host-vs-managed call), open `Questions/` (cost unknowns seed the clarifying batch), and `Risks/` (existing runaway-spend or DoS-of-wallet risks).
- The ratified/draft `DESIGN.md` -- the architecture whose cost you're guarding, and any `DATA.md` mask-before-model rules (masking changes token volume).
- The repo's existing budget/alert config and IaC, so you extend rather than duplicate.

### Step 2: Wire the Infracost PR diff (infrastructure guardrail)

Add a **FAST-stage** PR job that runs Infracost on the IaC diff and **posts the monthly cost delta as a PR comment**, so reviewers approve a number, not a guess.

- **Pin the action to a full commit SHA** (the house supply-chain rule -- never a floating tag/`@master`; mirror the `# pin to full commit SHA for supply-chain safety` convention already used for trivy/gitleaks/codeql in `templates/ci/*.yml`):

  ```yaml
  cost-diff:
    name: "FAST · Infra Cost Diff (infracost)"
    if: github.event_name == 'pull_request'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pull-requests: write   # to post/update the cost comment
    steps:
      - uses: actions/checkout@v4
      - uses: infracost/actions/setup@<FULL_COMMIT_SHA>  # pin to full commit SHA for supply-chain safety
        with:
          api-key: ${{ secrets.INFRACOST_API_KEY }}
      - run: infracost breakdown --path=. --format=json --out-file=/tmp/infracost.json
      - run: infracost comment github --path=/tmp/infracost.json
               --repo=$GITHUB_REPOSITORY --pull-request=${{ github.event.pull_request.number }}
               --github-token=${{ secrets.GITHUB_TOKEN }} --behavior=update
  ```

- **Path-filter it** so it only runs when IaC changed (don't tax every PR): scope the job to `**/*.tf`, `**/*.bicep`, `cdk.out/**`, `pulumi/**`, etc., matching the project's IaC.
- **Threshold, not just a comment.** Set a diff ceiling in `infracost.yml` (e.g. fail if the PR adds more than `$X/month` without a label) so a large cost increase **blocks the merge button** until either it's reduced or the PR carries an explicit `cost-approved` label + a one-line justification in the description. A silent comment nobody gates on is decoration, not a guardrail.
- **`INFRACOST_API_KEY` is a secret** (free self-serve key) -- never commit it; it goes in repo/org secrets like every other key (per `/security-review` secrets rules).

This is the PR-time half: it catches the cost of *provisioning*. Steps 3-4 catch the cost of *running*.

### Step 3: Set the per-feature LLM token ceiling + per-run eval cost cap (AI guardrail)

For every AI/LLM feature, decide two numbers **at design time** and enforce them in code + CI -- this is the app's per-call/per-run spend, distinct from the agent's own reasoning.

1. **Per-feature token budget (the runtime ceiling).** For each model-calling feature, set a **max tokens per request** (input + output) and a **max calls per user action** (kills chatty agent loops). Enforce in the app: pass `max_tokens`, cap tool-call loop iterations, and **measure actual usage** (log token counts per feature). Pick the model per use case at this point -- quality-sensitive vs high-volume vs cheap-batch -- and record the choice as an ADR (this is the app's model tiering; the agent's own model is unrelated and fixed at max). Apply the `DATA.md` mask-before-model rule first, since masking changes token volume.

2. **Per-run eval cost cap (the CI ceiling for AI features).** An eval/grader suite calls models in a loop and can quietly cost more than the feature it grades. For any AI feature with an eval suite (`/ai-eval`), set a **hard cost cap per eval run** and **abort on exceed** -- the same abort-on-exceed shape the agent's own Blueprint §G/§H loops use for their call-count / round bounds (those are not dollar caps; this one is, because it meters the *app's* eval spend). Concretely: estimate `tokens/case x cases x price` before running, cap the per-run dollar figure, run graders on the cheaper-but-equal-quality tier where available, and cache where the input is stable. On the PR stage, run a **bounded sample** of the eval (a few cases under a small cap) for fast signal; run the **full** eval suite **nightly** (non-blocking), where a larger cap is acceptable -- mirroring the FAST-vs-NIGHTLY split the CI templates already use for deep scans.

Write both numbers per feature so they're reviewable and testable -- a feature with no token ceiling and no eval cap is NOT done (it's an open-ended spend).

When a feature **lands over its token ceiling**, `/autoresearch` is the optimizer for clawing it back: minimize the feature's tokens-per-call *subject to the quality golden set still passing* (a cheaper prompt that fails the eval is not a win -- it's a regression). This optimizes the **app's** metered spend, never the agent's own reasoning (fixed at max by policy -- see "When NOT to use"). The metric is `tokens` with `DIRECTION: lower`. Make the quality gate an explicit `CONSTRAINT_CMD` (the `/ai-eval` golden-set run -- a kept change must keep it green, the anti-gaming guard against cutting `max_tokens` / truncating output to "win" on cost), and add a `HOLDOUT_CMD` quality check on **unseen** prompts validated on every kept candidate (so a token cut that overfits the tune set is caught). Keep two numbers **separate**: `MIN_IMPROVEMENT_TO_KEEP` is the small per-run noise floor (a handful of tokens is not a "keep"), while the **token ceiling is the *target*** -- a "done" condition (tokens back under the ceiling), NOT the keep threshold; conflating them makes the loop discard the small real savings that compound under the ceiling:

```markdown
# .autoresearch/program.md
## What we're optimizing
Bring <feature> back under its per-call token ceiling WITHOUT dropping quality.
## Files
- EDIT_FILE: prompts/<feature>.txt          # the prompt/template the loop trims
- RUNNER_CMD: ./scripts/cost-eval.sh <feature>   # prints tokens for the app's per-call spend
- CONSTRAINT_CMD: ./scripts/eval.sh <feature> --split=tune   # /ai-eval golden set must stay green every kept run (exit 0) -- a cheaper prompt that fails quality is a discard
- HOLDOUT_CMD: ./scripts/eval.sh <feature> --split=holdout   # quality on UNSEEN prompts -- validated on every kept candidate so a token cut isn't overfit to the tune set
- LOG_FILE: run.log
## Metric
- METRIC_NAME: tokens                        # app's tokens per call -- NOT the agent's
- METRIC_PATTERN: ^tokens:
- DIRECTION: lower
- BUDGET_PER_RUN_SECONDS: 300
## Constraints
- DO NOT modify: evals/<feature>/golden.jsonl   # the quality bar is fixed; only the prompt may change
- Cost cap per loop: <hard $ cap on the eval calls; abort on exceed>
## Stop conditions
- MAX_RUNS: 50
- MIN_IMPROVEMENT_TO_KEEP: <small noise floor, e.g. 20 tokens>   # smaller deltas are noise, discard
- TARGET: <the token ceiling, e.g. tokens < 1500>   # the under-ceiling goal -- a stop/done condition, distinct from the keep threshold above
- WALL_CLOCK_LIMIT: 4h
## Notes for the agent
A keep requires ALL THREE: tokens down by >= MIN_IMPROVEMENT_TO_KEEP, CONSTRAINT_CMD green
(golden set still passes), AND HOLDOUT_CMD validates quality holds on unseen prompts. A token
cut that fails the golden set or drops holdout quality is a DISCARD, never a keep. Holdout is
checked on every kept candidate, with a final holdout summary on the shipped prompt.
```

### Step 4: Set cloud cost guardrails (the backstop for what PR-time misses)

PR-time checks can't catch usage-driven cost (traffic spikes, a runaway loop in prod, egress from a hot path, a forgotten always-on resource). Set runtime guardrails on the cloud account:

- **A budget with alert thresholds** -- a monthly budget per environment (dev/staging/prod) with alerts at, e.g., 50% / 80% / 100% to the owner. Provision it **in IaC** (AWS Budgets / GCP Budget / Azure Cost Management), so it's versioned and reviewed like everything else, not clicked in a console.
- **A cost-anomaly alert** (production) -- the provider's native anomaly detection (AWS Cost Anomaly Detection / GCP recommender / Azure anomaly alerts) to catch a sudden spike the static budget would only notice at month-end.
- **Cost allocation by tag/label** -- tag every resource with `project` + `env` (+ `feature` where it helps), so spend is attributable and the budget is enforceable per slice. Untagged resources are unattributable cost; require the tag in IaC.
- **Kill switches for the metered-spend paths** -- the per-feature token ceiling (Step 3) is the in-app cap; back it with a provider-side spend limit / rate cap where the model vendor offers one, and an egress allow-list (a threat-model mitigation) so a "DoS of wallet" can't run unbounded. Wire the budget-exceeded alert to a documented response (throttle, disable the feature flag, page the owner).

### Step 5: Feed the loop

Close the loop so the next design/research pass inherits these decisions:
- **Each cost decision -> an ADR** in `Knowledge/<Project>/` via `/kb-capture` (the chosen instance class, the app's model-per-use-case choice, the token ceiling and eval cap per feature, the budget figures). Read `_schema.md`; `type: decision`.
- **Each unaddressed runaway-spend concern -> a `Risks/R-NNN` node** -- these join the `plan-critic`'s attack list in `/plan-tree` and the cost lens of `/research-loop`. A "DoS of wallet" on a public model endpoint is both a `/threat-model` row (DoS / excessive agency) and a cost risk; cross-link, don't duplicate.
- **Each cost unknown -> an open `Question`** (e.g. "expected req/day at launch?" -- you can't size a token budget without it).
- **DoR / DoD wiring:** a unit touching infra or an AI feature is not READY without its cost noted (the per-feature token ceiling + eval cap, or the expected infra delta), shifted left next to the perf and a11y budgets (Blueprint §2.6 DoR); and not DONE until the Infracost diff was reviewed (or N/A) and the budget/alerts exist.

## Iteration & STOP

- This is a **bounded check per PR and per design gate**, not a loop. Run the Infracost diff on the infra PR; set/confirm the token ceiling + eval cap on the AI PR; confirm the cloud budget + alerts exist. Then stop.
- **STOP** when: every infra-touching PR carries a reviewed cost delta (or the diff is provably $0/N/A); every AI feature has a token ceiling **and** an eval cost cap recorded and enforced; and a budget + alert exists for each paid environment. A change that provisions a paid resource with no budget, or ships a model call with no token ceiling, means NOT done.
- **Re-open** when a phase adds a new paid resource, a new model call, a new eval suite, or materially changes traffic assumptions -- price only the *new* piece; read prior cost ADRs, don't re-litigate decided ones.
- Do **not** gold-plate: a single small managed DB does not need per-feature tag-level cost allocation; a one-shot model call behind a flag does not need a cost-anomaly detector. Depth scales to the size of the spend, not to effort -- the quality bar is "no un-budgeted metered cost", not "maximum tooling".

## See also

- `/perf-budget` -- the sibling guardrail; constrains the *same* architecture decision on the latency/size axis (cost and latency trade off -- decide them together).
- `/design-doc` -- the design-time host; the token ceiling, eval cap, and instance/model choices are set here before code commits.
- `/ai-eval` -- the eval suite this skill caps; the per-run cost cap lives where the eval runs.
- `/autoresearch` -- the optimizer when a feature lands over its token ceiling: minimize the app's tokens subject to the `/ai-eval` golden set still passing (the app's spend, never the agent's).
- `/threat-model` -- the DoS / excessive-agency rows that demand a cost cap as their mitigation (DoS-of-wallet); `/data-classify` -- the mask-before-model rule that changes token volume.
- `/security-review` -- secrets handling for `INFRACOST_API_KEY` and the model keys; runtime audit of the spend path.
- `/kb-capture` -- persists cost decisions as ADRs and runaway-spend risks into `Knowledge/<Project>/`.
- `/research-loop`, `/plan-tree` -- the agent's research/planning loops, bounded by call-count / round / concurrency (Blueprint §G/§H), **not** by spend -- a different axis entirely, not to be confused with the app's metered cost this skill governs.
- [[../../Knowledge/_schema|Knowledge/_schema.md]] -- the `decision` / `risk` node + edge contract.
- [[../Agent Workflow]] -- §0 (tiering), §2.6 (DoR: cost noted alongside perf/a11y), §12 (app model-per-use-case tiering -- distinct from the agent's fixed max-reasoning model), §24 (Cloud Cost Management), and the §J two-tier CI model the Infracost job slots into.
