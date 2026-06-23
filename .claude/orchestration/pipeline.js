// TEMPLATE — per-item multi-stage pipeline (NO barrier between stages).
// Each item flows review → adversarial-verify independently; wall-clock = slowest single chain.
// Adapt: meta, ITEMS source, each stage, the verdict rule.
// Use for: review-finding → verify; per-unit plan → test-plan; any "do X then check X per item".
// Sequential fallback: orchestrator runs each item's chain with Task subagents.

export const meta = {
  name: 'pipeline-verify',
  description: 'Run each item through stage1 → adversarial verify independently; keep only majority-confirmed survivors',
  phases: [{ title: 'Stage1' }, { title: 'Verify' }],
}

const VERDICT_SCHEMA = {
  type: 'object', additionalProperties: false, required: ['refuted', 'why'],
  properties: { refuted: { type: 'boolean' }, why: { type: 'string' } },
}
const ITEMS = (typeof args !== 'undefined' && args && args.items) || []   // ADAPT: the work-list
const VOTES = 3                                                            // adversarial verifiers per item

const results = await pipeline(
  ITEMS,
  // Stage 1 — ADAPT (e.g. produce a finding / draft a sub-plan).
  (item, _orig, i) => agent(
    `Stage 1 on item ${i}:\n${JSON.stringify(item)}`,
    { label: `stage1:${i}`, phase: 'Stage1', effort: 'max' }
  ),
  // Stage 2 — adversarial verification (default-to-refute), majority confirms.
  (s1, item, i) => parallel(Array.from({ length: VOTES }, (_, v) => () =>
    agent(
      `Adversarially verify the following. Try to REFUTE it; default refuted=true if uncertain.\n\n${s1}`,
      { label: `verify:${i}:${v}`, phase: 'Verify', schema: VERDICT_SCHEMA, effort: 'max' }
    )
  )).then(votes => {
    const refuted = votes.filter(Boolean).filter(v => v.refuted).length
    return { item, stage1: s1, confirmed: refuted < Math.ceil(VOTES / 2), refuted, votes: VOTES }
  })
)

return results.filter(Boolean).filter(r => r.confirmed)
