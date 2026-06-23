// TEMPLATE — parallel fan-out → consolidate ALL (with a disagreements register).
// Adapt: meta, LENSES, the per-lens prompt, FINDINGS_SCHEMA, the consolidation prompt.
// Pass the adapted script to the Workflow tool.
// Sequential fallback (no Dynamic Workflows): the orchestrator agent spawns the same
// LENSES as parallel Task subagents (4-6 at a time), collects their structured returns,
// then runs one consolidation Task — same blind-generation + consolidate-ALL rules.

export const meta = {
  name: 'fanout-consolidate',
  description: 'Fan out N blind, diverse agents over one question, then consolidate ALL findings with a disagreements register',
  phases: [{ title: 'Fan-out' }, { title: 'Consolidate' }],
}

const FINDINGS_SCHEMA = {
  type: 'object', additionalProperties: false, required: ['findings'],
  properties: {
    findings: { type: 'array', items: {
      type: 'object', additionalProperties: false,
      required: ['claim', 'soWhat', 'confidence'],
      properties: {
        claim: { type: 'string' },
        soWhat: { type: 'string' },
        evidence: { type: 'string' },
        confidence: { type: 'string', enum: ['low', 'medium', 'high'] },
      },
    } },
  },
}

// ADAPT: one entry per DISTINCT lens — diversity is the point, no overlap.
const LENSES = [
  { key: 'prior-art',     ask: 'existing solutions, libraries, and proven patterns' },
  { key: 'failure-modes', ask: 'how this typically fails and the edge cases' },
  { key: 'security',      ask: 'the security / abuse / privacy angle' },
  { key: 'cost',          ask: 'cost, latency, and operational burden' },
  { key: 'alternatives',  ask: 'fundamentally different approaches' },
]
const QUESTION = (typeof args !== 'undefined' && args && args.question) || 'ADAPT: the research/decision question'

phase('Fan-out')
const reports = (await parallel(LENSES.map(l => () =>
  agent(
    `You are the "${l.key}" lens. Investigate ${l.ask} for:\n${QUESTION}\n\n` +
    `Work independently — you cannot see the other agents; that independence is the point. ` +
    `Ground every claim (cite a source URL or file:line). Read Knowledge/<Project>/ first if it exists ` +
    `(prior decisions, open questions, risks are inputs).`,
    { label: `fanout:${l.key}`, phase: 'Fan-out', schema: FINDINGS_SCHEMA, effort: 'max' }
  )
))).filter(Boolean)

phase('Consolidate')
// An agent consolidates so conflicts are REASONED about, not code-merged.
const consolidated = await agent(
  `Consolidate ALL of these independent findings into one report. Dedupe overlaps. ` +
  `Emit a DISAGREEMENTS REGISTER: every claim where lenses conflicted, plus the adjudication ` +
  `(evidence-based, not majority-vote). Never silently merge a conflict.\n\n` +
  `FINDINGS (JSON):\n${JSON.stringify(reports, null, 2)}`,
  { label: 'consolidate', phase: 'Consolidate', effort: 'max' }
)

return { reports, consolidated }
