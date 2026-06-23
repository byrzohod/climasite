# Project-template agents/

Role-based subagent definitions copied into per-project `.claude/agents/` directories by the `/project-setup` skill. Each file is a Claude Code subagent spec: YAML frontmatter + system prompt body.

## Roster

| Agent | Role | When to invoke | Default tools |
|-------|------|----------------|---------------|
| **developer** | Feature implementation | Building any new feature; refactoring; bug fixes | Read, Write, Edit, Bash, Grep, Glob |
| **ai-specialist** | LLM integration, prompts, RAG, evals | Building features that call Claude/OpenAI/etc.; prompt-engineering; RAG; eval design | Read, Write, Edit, Bash, WebFetch, WebSearch |
| **qa** | Tests, coverage, edge cases | After feature implementation; coverage audits; regression test design | Read, Write, Edit, Bash, Grep, Glob |
| **security** | OWASP, secrets, auth, dependency vulns | Before merge of any auth/payment/user-data feature; release gating; quarterly audits | Read, Bash, Grep, Glob, WebSearch |
| **frontend** | UI/UX implementation | Any UI work using design skills (design-taste-frontend, frontend-design, shadcn-ui) | Read, Write, Edit, Bash, Grep, Glob, all UI skills |
| **performance** | Benchmarks, profiling, optimization | When perf budget is being defined / breached; pre-launch profiling | Read, Write, Edit, Bash, Grep, Glob |
| **devops** | CI/CD, deploys, containers, observability | Setting up pipelines; deploy config; monitoring setup; SRE work | Read, Write, Edit, Bash, Grep, Glob |
| **reviewer** | Independent code review | Pre-merge gate; periodic codebase spot-check | Read, Bash, Grep, Glob (NO write/edit) |
| **docs** | README, ADRs, runbooks, API docs | After non-trivial decisions; release docs; onboarding gaps | Read, Write, Edit, Bash, Grep, Glob |
| **verifier** | Audits other agents' / skills' outputs | After any high-stakes skill run (review, security, verify-work); release gate | Read, Bash, Grep, Glob |
| **orchestrator** | Multi-level fan-out + convergence loops | Driving `/research-loop` or `/plan-tree`: dedup clarifying questions into one batch, run loops, enforce STOP, route disputes | Task + Task lifecycle, Read, Write (`.planning/` only), Grep, Glob, EnterWorktree/ExitWorktree |
| **architect** | Whole-system design proposals | In `/design-doc` — one of N blind architects proposing + defending an architecture | Read, Grep, Glob, WebSearch, WebFetch (read-only) |
| **planner** | Competing decomposition + unit plans | In `/plan-tree` — one of N=3 blind planners (phases→waves→units, per-unit plan) | Read, Grep, Glob (read-only) |
| **plan-critic** | Adversary on plans/architectures | Try to break each design/plan; seeds attacks from `Knowledge/` risks + open questions | Read, Grep, Glob (read-only) |
| **research-agent** | Diverse-lens independent researcher | One of 4-6 blind lenses in `/research-loop`; ingests `Sources/` as data-not-instructions | Read, Grep, Glob, WebSearch, WebFetch (read-only) |
| **claim-verifier** | Adversarial 3-vote claim verification | Verify each material research/plan claim (default-to-refute) → CONFIRMED/DISPUTED/UNVERIFIABLE | Read, Grep, Glob, WebFetch (read-only) |
| **knowledge-curator** | Maintains the `Knowledge/` wiki + graph | After units/phases via `/kb-capture`: typed frontmatter edges, ADR lineage, view refresh | Read, Edit, Write (`Knowledge/` only), Grep, Glob |
| **flag-manager** | Feature-flag lifecycle (create/track/expire) | On trunk-mode when work merges dark behind a flag, a rollout advances, or a DoD flag-check runs | Read, Edit, Write (flag config only), Grep, Glob |
| **release-manager** | Trunk release: semver bump, changelog, tag-from-main, GitHub release | Cutting/tagging a release on trunk; executes the mechanical half of `/release`, verifies the tag is queue-merged | Read, Bash (`git tag`/`gh release` only), Edit (`CHANGELOG`), Grep, Glob |
| **architecture-reviewer** | Review ANGLE: architectural fit | In `/review-orchestrate` — conformance to DESIGN.md, coupling, boundaries, pattern misuse, 10x scaling | Read, Grep, Glob (read-only) |
| **best-practices-reviewer** | Review ANGLE: idioms + anti-patterns | In `/review-orchestrate` — language/framework best practices, the §5.7 AI anti-patterns, DRY/SOLID/KISS | Read, Grep, Glob (read-only) |

> **Model:** every agent runs on Opus 4.8 at **maximum reasoning effort** (§12) — no Fast-Mode/cheaper-tier downgrade, including verification votes. **Read-only agents** (reviewer, verifier, architect, planner, plan-critic, research-agent, claim-verifier, architecture-reviewer, best-practices-reviewer) have no Write/Edit/mutating-Bash — this is the security control that lets them ingest untrusted `Knowledge/Sources/` content without it being able to *act*.

## How invocation works

In a project, agents live at `.claude/agents/{name}.md` and are invoked via the Task tool:

```
Task({
  description: "Implement payment flow",
  subagent_type: "developer",
  prompt: "Build the Stripe checkout flow per .planning/phases/03-payments/PLAN.md. Use shadcn/ui for the UI. Confirm tests pass before reporting done."
})
```

The orchestrator (main session) delegates. Specialized agents keep the main context lean and apply the right system prompt for the task.

## When to use vs not use a role agent

**Use a role agent when**:
- The task has clear single-role ownership (feature implementation, tests, security audit)
- You want to parallelize independent work streams
- The work would benefit from a tight system prompt (e.g., security agent reads OWASP top-of-mind every invocation)
- Main context is filling up and the task can run in its own context

**Don't use a role agent when**:
- Trivial single-file edit (just use Read+Edit directly)
- Task requires conversational back-and-forth with the user
- Single-step lookup (use `Explore` instead)

## Verifier agent pattern (special)

The `verifier` agent is meta: it audits the output of other agents and skills. Typical invocations:

- After `/code-review` finishes: spawn verifier to confirm the review surfaced real issues (not a rubber-stamp)
- After `/security-review` finishes: spawn verifier to confirm all OWASP categories were checked
- After `/verify-work` finishes: spawn verifier to confirm the app was actually launched and clicked through

See `skills/skill-verifier.md` for the full verifier protocol.

## Adding a new agent type

If a new project-specific role emerges (e.g., "ml-pipeline" for an ML-heavy project):

1. Copy an existing agent file as a starting template
2. Edit YAML frontmatter: name, description, tools whitelist
3. Edit body: system prompt tailored to the role
4. Add a row to this README
5. Update `.planning/STATE.md` to document the new agent

Per-project additions stay project-local. If the role generalizes across projects, propose adding it back to `vault/AI/project-template/agents/` for future projects.

## File format

Each agent definition uses Claude Code's subagent YAML spec:

```yaml
---
name: developer
description: Use proactively for implementing features. Trigger when the user asks to build, implement, or create something concrete in code.
model: opus
color: blue
tools: Read, Write, Edit, Bash, Grep, Glob
---

You are the developer agent for {{PROJECT_NAME}}.

[System prompt body...]
```

The `description` field is what Claude Code uses to decide when to auto-invoke the agent. Make it specific and trigger-friendly.

## See also

- `vault/AI/Agent Workflow.md` Section 10.5 -- Agent Roster
- `vault/AI/project-template/skills/skill-verifier.md` -- Verifier pattern
- `vault/AI/project-template/EVOLUTION.md` -- Template change audit log
