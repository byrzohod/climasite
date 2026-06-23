---
name: threat-model
description: Design-time STRIDE + agentic-AI threat modeling, run inside /design-doc before any code. Use this whenever a system is being designed and touches AI/LLM agents, tools, MCP, RAG, or the network; when the user mentions threat model, STRIDE, attack surface, prompt injection, jailbreak, tool abuse, data exfiltration, excessive agency, untrusted input, or trust boundaries; or before ratifying an architecture for anything that processes external/untrusted data. Methodology-as-prompt only -- zero dependencies, no scanners, no tooling. Produces a threat table + mitigations that become Knowledge/<Project>/Risks/ nodes and gate the DoR.
---

# /threat-model - Design-time STRIDE + agentic-AI threat modeling

Reason about how the *designed* system could be attacked, before a line of it exists. Pure judgment exercise -- no code, no scanners, no dependencies. Output is a threat table whose surviving rows become `Risks/R-NNN-*` nodes (seeding the `plan-critic`) and a DoR checkbox. Runtime/code-level checks are `/security-review`'s job; this is the *design* shift-left.

## When to use

Invoke this skill:
- **Inside `/design-doc`, at Gate 1** -- alongside `/data-classify`, after architecture candidates exist and before the design is ratified
- **At the start of each major phase** -- if the phase adds a new trust boundary (a new external integration, a new agent/tool, a new network surface)
- **When the architecture changes shape** -- a new data flow crossing a trust boundary always re-opens the model

## When NOT to use

- **Code/runtime audit** -- that is `/security-review` (OWASP, deps, secrets, auth flows). This skill never reads implementation; it reasons about the design.
- **Data sensitivity classification** -- that is `/data-classify` (`DATA.md`). They run together; this one models *attacks*, that one labels *data*. Cross-reference, don't duplicate.
- **Pure-local hobby tier with no AI and no network** -- off by default (see Tiering). A static local script touching only the user's own files has no meaningful adversary.
- **After the fact** -- if code already exists, you missed the window; run it anyway for the next phase and note the gap.

## Tiering (Blueprint §C)

| Tier | Default |
|------|---------|
| **hobby** | OFF -- unless the system touches AI/LLM **or** the network (then ON) |
| **MVP** | ON if AI **or** network; else optional |
| **production** | ON, always |

The trigger is a trust boundary: **does untrusted input reach a component that can act, decide, or expose data?** Anything with an LLM agent, tool calls, MCP, RAG, a public endpoint, or fetched/third-party content crosses it. If yes → run this. Quality bar is uniform across tiers; only the *on/off* toggle differs.

## Process

### Step 1: Read the graph + the design context first

Before modeling, read (do not re-derive):
- `Knowledge/<Project>/Risks/` -- existing open/accepted risks (extend, don't duplicate them)
- `Knowledge/<Project>/Questions/` open security/trust questions
- The candidate architecture(s) from `/design-doc` and the `DATA.md` from `/data-classify` (sensitivity labels tell you what an attacker wants)

If `/data-classify` has not run yet, run it first or in parallel -- you cannot rank exfiltration impact without knowing what data exists.

### Step 2: Draw the trust boundaries (in prose)

Sketch the data-flow in words: actors → entry points → components → data stores → external calls. Mark each **trust boundary** -- every place untrusted data crosses into a more-trusted zone:
- user input → app
- fetched/RAG/third-party content → LLM context
- LLM output → a tool/executor/shell/DB/HTTP call
- one service → another with different privilege
- the network edge (any inbound/outbound)

Boundaries are where threats live. List them explicitly; the threat table works boundary by boundary.

### Step 3: STRIDE pass per boundary

For each trust boundary, walk the six STRIDE categories and ask "how could this be violated here?":

| STRIDE | Question | Typical mitigation lever |
|--------|----------|--------------------------|
| **S**poofing | Can an attacker pretend to be a user/service/component? | authn, signed requests, mTLS, webhook signature verification |
| **T**ampering | Can data/messages/state be modified in transit or at rest? | integrity checks, signing, parameterization, immutable logs |
| **R**epudiation | Can an actor deny an action with no trace? | tamper-evident audit log, request IDs, no anonymous privileged actions |
| **I**nformation disclosure | Can data leak across the boundary? | least-privilege, encryption, output filtering, scoped tokens, no secrets in context |
| **D**enial of service | Can the component be exhausted or wedged? | rate limits, quotas, timeouts, cost caps, circuit breakers |
| **E**levation of privilege | Can an actor gain rights they shouldn't? | default-deny, scoped capabilities, no client-side-only authz |

One threat = one row. Be concrete ("a malicious RAG document instructs the agent to call `delete_user`"), not generic ("injection is possible").

### Step 4: Agentic-AI threat pass (run whenever an LLM/agent/tool exists)

STRIDE undersells AI systems. Add a second pass over these four agentic threat classes -- they are the reason this skill is on-by-default for AI:

- **Prompt injection** (direct + indirect): untrusted content (user message, fetched page, RAG doc, tool result, file, email) carries instructions the model obeys. *Indirect* injection -- payloads riding inbound data the model later reads -- is the dominant real-world vector. Mitigations: **treat all external content as data, never instructions** (delimit/quote it, system-prompt the boundary), keep ingesting agents **read-only** (the §F/§D control: a successful injection can lie but cannot act), human-in-the-loop on irreversible actions, output allow-listing.
- **Tool / function abuse**: the model invokes a real-world tool (shell, HTTP, DB write, payment, file delete) with attacker-influenced arguments, or chains tools to escalate. Mitigations: minimal tool whitelist per agent, parameter validation/allow-lists on tool args, no shell/eval/arbitrary-HTTP tools where a narrower one suffices, confirm-before-act on destructive tools, per-tool rate/cost limits.
- **Data exfiltration**: secrets, PII, or other context (from `DATA.md`) is steered out -- echoed into a response, embedded in a URL/image the model is tricked into fetching, written to an attacker-readable store, or leaked via a vector index. Mitigations: **mask before model** (per `/data-classify`), no secrets in the prompt/context, egress allow-list (no auto-followed links, no arbitrary outbound fetch), output scanning for sensitive patterns, PII rule on RAG/vector stores.
- **Excessive agency**: the agent has more permission, autonomy, or blast radius than the task needs -- broad scopes, write access it never uses, no human gate on high-impact actions, unbounded loops/spend. Mitigations: **least privilege + least autonomy**, scoped short-lived credentials, human approval gates on irreversible/high-value actions, explicit STOP + cost caps + no-progress HALT on any agent loop (the §G/§H ceiling), one-level nesting.

Map each agentic threat to its source boundary so it joins the same table.

### Step 5: Rate and decide each threat

For every row, assign **Likelihood** (Low/Med/High) and **Impact** (Low/Med/High) given the data sensitivity from `DATA.md`, then a **Severity** (L×I). Then pick a disposition:
- **Mitigate** -- name the concrete design control and where it lands in the architecture (this becomes a design requirement, and often an ADR).
- **Accept** -- only with an explicit one-line rationale; accepted threats still become tracked risks.
- **Transfer / Defer** -- push to a provider, a later phase, or behind a flag; record why and when it's revisited.

No row is left undecided. "Eliminate by design" (remove the dangerous capability entirely) beats "mitigate" -- prefer it when the capability isn't load-bearing.

### Step 6: Produce the threat table

Emit the table into the design doc (`.planning/design/DESIGN.md`, "Threats" section):

| ID | Boundary | STRIDE / Agentic class | Threat (concrete) | Likelihood | Impact | Severity | Disposition | Mitigation / control | → Risk node |
|----|----------|------------------------|-------------------|-----------|--------|----------|-------------|----------------------|-------------|
| T-01 | RAG doc → LLM context | Prompt injection (indirect) | Poisoned doc tells agent to call `send_email` to attacker | High | High | High | Mitigate | ingesting agent read-only; no `send_email` tool in that agent's whitelist; content delimited as data | R-014 |
| T-02 | LLM output → shell tool | Tool abuse / EoP | Model emits `rm -rf` via attacker-shaped arg | Med | High | High | Mitigate | replace shell tool with narrow scoped command; arg allow-list; confirm-before-act | R-015 |

Keep "Threat" concrete and "Mitigation" actionable. Every **Mitigate/Accept** row of Med+ severity gets a Risk node ID in the last column.

### Step 7: Feed mitigations into Risks/ and the DoR

Close the loop -- this is the whole point of running at design time:
- **Each surviving Med+ threat → a `Risks/R-NNN-<slug>` node** via `/kb-capture` (read `_schema.md`; `type: risk`, `status: open|mitigated|accepted`, `raised_by: "[[Phases/Phase-N]]"`, `affects:` the component(s) it threatens). These risks **seed the `plan-critic`'s attack list** and the next `/research-loop` failure-modes lens.
- **Each Mitigate control becomes a design requirement** in `DESIGN.md` -- and an ADR when it's an architecture choice (e.g., "agents that ingest external content are read-only").
- **DoR gate:** a unit is not READY until "threat checked" is satisfied -- meaning every threat that touches this unit's components has a disposition and its mitigations are reflected in the unit's plan. This is the design-time half; `/security-review` confirms the runtime half later.

## Iteration & STOP

- This is a **bounded single pass per design/phase**, not a loop. One STRIDE pass per boundary + one agentic pass -- then rate, decide, table. Done.
- **STOP** when every trust boundary has been walked through all six STRIDE categories, the agentic pass is complete (or N/A with reason), and every row has a disposition. Coverage of boundaries is the stop signal, not a count of threats.
- **Re-open** only when the architecture changes shape (new boundary) or a new phase adds a surface -- then run an incremental pass over the *new* boundary only, extending the existing table and Risks (don't restart).
- Do **not** gold-plate: a hobby-tier CLI does not need an EoP analysis of a privilege model it doesn't have. Mark inapplicable categories "N/A -- no such boundary" and move on. Depth scales to attack surface, not to effort.

## See also
- `/data-classify` -- runs alongside at Gate 1; provides the sensitivity labels (`DATA.md`) that set exfiltration impact
- `/design-doc` -- the host skill; the threat table lands in its `DESIGN.md`
- `/security-review` -- the runtime/code counterpart (OWASP, deps, secrets); confirms what this design-time pass required
- `/kb-capture` -- writes surviving threats into `Knowledge/<Project>/Risks/` (Step 7)
- `agents/plan-critic.md` -- consumes the Risks as its attack list; `agents/research-agent.md` -- failure-modes lens
- [[../../Knowledge/_schema|Knowledge/_schema.md]] -- the `risk` node + edge contract
- [[../Agent Workflow]] -- §2.3 (DoR), §7 (design-time threat modeling); reference/domain-concerns.md §34 (AI Agent Security: read-only ingest, treat-as-data, cost caps)
