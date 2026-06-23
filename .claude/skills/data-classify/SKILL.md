---
name: data-classify
description: Build DATA.md -- classify every data element the system stores, processes, or sends to a model (public/internal/confidential/PII/PHI/biometric), with retention, the mask-before-model rule, the RAG/vector-store PII rule, and the DPIA trigger. Use this whenever the project handles personal data, user accounts, emails, names, locations, faces/voices/fingerprints, health or payment data, anything sent to an LLM/embedding model, anything stored in a vector store, or when the user mentions GDPR, PII, PHI, biometric, DPIA, data retention, data classification, privacy by design, or data governance. Runs design-time inside /design-doc; feeds the risks register and the security review.
---

# /data-classify - Classify data + write DATA.md (data governance)

Produces the project's single source of truth for what data exists, how sensitive it is, how long it lives, and what is allowed to leave the box (into a model, a vector store, or a log). Design-time artifact -- it constrains the architecture before code, not after.

## When to use

Invoke this skill:
- **Inside `/design-doc`** -- runs alongside `/threat-model` at the design gate (§H Gate 1), once per project and at the start of each major phase that adds new data.
- **Whenever a new data element appears** -- a new field, a new external feed, a new model call, a new vector index, a new log line carrying user data.
- **Before any feature that sends data to an LLM or embedding model** -- the mask-before-model rule must be decided first.
- **Before standing up a RAG / vector store** -- the vector-PII rule must be decided first.

This is a **tiered toggle**: ON when the system touches personal data, OFF (skip) for a system that provably handles none. When in doubt, run it -- a wrong "no personal data" call is the expensive one.

## When NOT to use

- The system handles **no** personal data of any kind and never sends user content to a model (e.g., a stateless math library, a pure build tool). Skip -- but record the "no personal data" determination in DESIGN.md so the next phase re-checks.
- You need the *threat* surface (prompt-injection, tool-abuse, exfil paths) -- that is `/threat-model`. This skill classifies the data; the threat model attacks the flows. They run together and feed each other.
- You need the implementation-time audit (no secrets in code, queries parameterized, logs scrubbed) -- that is `/security-review`. DATA.md is its input, not its replacement.

## Classification scheme

Every data element gets exactly one **sensitivity class** (highest wins on a tie):

| Class | Meaning | Examples |
|---|---|---|
| `public` | Already public; no harm if disclosed | published prices, docs, OSS code |
| `internal` | Non-personal but not for disclosure | infra config, aggregate metrics, internal IDs |
| `confidential` | Business-sensitive, non-personal | API keys, trade secrets, unreleased plans |
| `PII` | Identifies a natural person | name, email, phone, address, IP, device ID, precise location |
| `PHI` | Health data about a person | diagnoses, medications, vitals, mental-health notes |
| `biometric` | Special-category identifier of a body | face embeddings, voiceprints, fingerprints, gait, iris |

Treat **PII / PHI / biometric as special-category personal data**. Biometric and PHI are the highest-risk classes: they trigger the strictest retention, the hardest mask-before-model rule, and almost always the DPIA.

## Process

1. **Read prior knowledge first.** Read `Knowledge/<Project>/` -- prior ADRs (existing retention/privacy decisions), open `Questions/` (seed the clarifying batch), and `Risks/` (existing privacy risks). Decided questions are excluded from re-asking; open ones go to the user batch. Read the ratified/draft `DESIGN.md` for the data model and data flows.

2. **Enumerate every data element.** Walk the data model, every external feed, every model/embedding call, every log/telemetry sink, every persisted store (DB, cache, queue, blob, vector index), and every third-party destination. One row per element. Missing an element is the failure mode -- be exhaustive over *flows*, not just tables.

3. **Classify each element** into one class from the scheme above (highest wins). For each, record: where it is collected, where it is stored, who/what it is sent to, and the lawful basis if personal (consent / contract / legitimate-interest / legal-obligation).

4. **Assign retention** per element: a concrete duration or rule (e.g. "30 days then hard-delete", "indefinite, rotates on retrain", "session-only, never persisted"). No element may be "retained forever" by omission -- forever is a decision that must be written and justified. Special-category data gets the shortest defensible retention. Wire deletion to the user-data-rights path (export + erasure).

5. **Apply the mask-before-model rule.** For every element that can reach an LLM or embedding model: decide redact / pseudonymize / tokenize / drop **before** the call. Default: **PII/PHI/biometric raw values never go into a prompt or an embedding input** unless there is a written lawful basis and it is local-only. Record the exact transform per element (e.g. "names -> `[PERSON_n]` placeholder", "face crop -> never sent to cloud VLM"). Prompts and tool outputs are model inputs too -- include them.

6. **Apply the RAG / vector-store PII rule.** For every vector index / RAG corpus: decide what is allowed in the embedded text and the metadata. Default: **no raw PII/PHI/biometric in embedded chunks or vector metadata.** If personal data must be retrievable, scope it (per-tenant index isolation, row-level access on retrieval, encrypted metadata) and record it. Embeddings of biometric/PHI are themselves special-category -- classify the *index* too. Decide vector-store retention + deletion (deleting a source row must delete its vectors).

7. **Evaluate the DPIA trigger.** A DPIA (Data Protection Impact Assessment) is **required** if ANY hold:
   - biometric or PHI / special-category data is processed;
   - systematic monitoring of a public/semi-public area (e.g. cameras over a street, behavioral tracking);
   - large-scale processing of personal data, or profiling with legal/significant effect;
   - vulnerable data subjects (children, patients), or novel/high-risk tech (re-identification, automated cross-session linking).
   If triggered: record `DPIA: REQUIRED` plus the prerequisites (LIA, masking, signage/notice, jurisdiction consult) and **gate the dependent feature behind them** (build the core; do not ship the triggering data flow until the DPIA + prerequisites exist). Emit a high-severity risk for the risks register.

8. **Write `DATA.md`** to `.planning/design/DATA.md` (next to DESIGN.md), using the template below.

9. **Feed the loop.** Emit each unaddressed privacy concern as a **risk** (these seed the `plan-critic`'s attack list in `/plan-tree`) and pass DATA.md as a named input to `/security-review`. Push new decisions (retention, mask rule, vector rule, DPIA verdict) into `Knowledge/<Project>/` as ADRs via `/kb-capture`, and any new unknowns into `Questions/`.

## DATA.md template

```markdown
---
tags: [knowledge, <project>, data-governance]
created: YYYY-MM-DD
type: reference
status: draft            # draft | ratified
---
# Data Classification -- <Project>

DPIA: REQUIRED | NOT-REQUIRED   <!-- + one-line justification -->
Personal data present: yes | no
Lawful basis (overall): consent | contract | legitimate-interest | legal-obligation

## Data inventory
| Element | Class | Collected at | Stored where | Sent to | Retention | Lawful basis |
|---|---|---|---|---|---|---|
| email | PII | signup | users tbl | -- | account-life + 30d | contract |
| face embedding | biometric | camera | pgvector | local VLM only | rotates on retrain | consent/LIA |

## Mask-before-model rule (per element reaching an LLM/embedding model)
| Element | Reaches model? | Transform before call | Local-only? |
|---|---|---|---|
| name | yes (prompt) | -> [PERSON_n] placeholder | n/a |
| face crop | no | dropped -- never sent to cloud | -- |

## RAG / vector-store rule
- Index: <name> | embedded text contains: <classes allowed> | metadata contains: <fields>
- PII/PHI/biometric in chunks/metadata: forbidden | scoped (how) | n/a
- Isolation: <per-tenant / RLS-on-retrieval / none> | Vector deletion wired to source delete: yes/no

## DPIA
- Trigger(s): <which condition(s) fired, or "none">
- Prerequisites before the triggering flow ships: <LIA · masking · signage/notice · jurisdiction consult>
- Gated feature(s): <name(s)> -- not shipped until prerequisites met

## Open privacy risks (-> risks register / plan-critic)
- R-NNN: <risk> -- severity -- mitigation
```

## Iteration & STOP

- Run **once per design gate**; re-run when a phase introduces new data elements, a new model call, a new vector index, or a new external destination. Do not re-litigate decided classifications -- read them from `Knowledge/<Project>/` and only classify the *new* elements.
- **STOP** when every enumerated element has a class + retention + (if model-bound) a mask rule, the vector rule is decided for every index, and the DPIA verdict is recorded with justification. An element with no retention or no class means NOT done.
- If the DPIA trigger fires and prerequisites are unmet, **do not block the whole project** -- ratify DATA.md, gate only the triggering data flow, and proceed with the rest. Escalate the gate to the user (it is a lawfulness decision, not an implementation detail).
- If a class or lawful basis is genuinely unknowable at design time, log it as an open `Question` and assume the **stricter** class until resolved -- never the looser one.

## See also

- `design-doc` -- the gate this runs inside; DATA.md sits beside DESIGN.md and constrains the architecture.
- `threat-model` -- the sibling design-time control; it attacks the flows DATA.md classifies (exfil, prompt-injection of personal data).
- `security-review` -- consumes DATA.md as input for the Data Privacy section (data minimization, inventory, user-data rights, breach procedure).
- `plan-tree` -- reads the emitted risks; they seed the `plan-critic`'s attack list.
- `kb-capture` -- persists retention / mask / vector / DPIA decisions as ADRs in `Knowledge/<Project>/`.
