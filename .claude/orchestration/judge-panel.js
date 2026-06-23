// TEMPLATE — N blind proposers → evidence-based judge panel → winner + preserved dissent.
// Adapt: meta, N, the proposer angles, RUBRIC, the judge prompt.
// RULE: judges score against the rubric WITH evidence; they do NOT debate or vote.
// If the top two totals are within 10% → escalate to a human tie-break (do not auto-pick).
// Sequential fallback: orchestrator spawns N proposer Tasks, then one judge Task per proposal.

export const meta = {
  name: 'judge-panel',
  description: 'Generate N independent proposals, score each with an evidence-based judge, pick a winner and preserve dissent',
  phases: [{ title: 'Propose' }, { title: 'Judge' }],
}

const N = 3                                  // ADAPT: planning=3, design=3
const RUBRIC = [                             // ADAPT: the scoring criteria
  'fit to requirements',
  'simplicity / KISS',
  'scalability headroom (10x)',
  'risk / failure surface',
  'reversibility',
]
const ANGLES = ['simplest thing that works', 'risk-first', 'long-horizon / extensibility']  // ADAPT
const TASK = (typeof args !== 'undefined' && args && args.task) || 'ADAPT: what to propose (architecture / plan / approach)'

const SCORE_SCHEMA = {
  type: 'object', additionalProperties: false, required: ['scores', 'total', 'verdict'],
  properties: {
    scores: { type: 'array', items: {
      type: 'object', additionalProperties: false, required: ['criterion', 'score', 'why'],
      properties: { criterion: { type: 'string' }, score: { type: 'number' }, why: { type: 'string' } } } },
    total: { type: 'number' },
    verdict: { type: 'string' },
  },
}

phase('Propose')
const proposals = (await parallel(Array.from({ length: N }, (_, i) => () =>
  agent(
    `Proposal #${i + 1}. Independently propose: ${TASK}\n` +
    `Angle: ${ANGLES[i % ANGLES.length]}. You cannot see other proposals. ` +
    `Justify your choice against: ${RUBRIC.join(', ')}. Read Knowledge/<Project>/ for prior decisions/risks first.`,
    { label: `propose:${i + 1}`, phase: 'Propose', effort: 'max' }
  )
))).filter(Boolean)

phase('Judge')
// Each proposal scored independently against the rubric — adversarial, evidence-cited, no debate.
const judged = (await parallel(proposals.map((p, i) => () =>
  agent(
    `Score this proposal against the rubric [${RUBRIC.join(', ')}], 0-10 each with a cited reason. ` +
    `Be adversarial about weaknesses. Sum to "total".\n\nPROPOSAL #${i + 1}:\n${p}`,
    { label: `judge:${i + 1}`, phase: 'Judge', schema: SCORE_SCHEMA, effort: 'max' }
  ).then(v => v && ({ i, proposal: p, ...v }))
))).filter(Boolean)

const ranked = judged.sort((a, b) => b.total - a.total)
const tie = ranked.length >= 2 && (ranked[0].total - ranked[1].total) / Math.max(ranked[0].total, 1) < 0.10

return {
  winner: ranked[0],
  runnersUp: ranked.slice(1),                                   // graft their best ideas in synthesis
  needsHumanTiebreak: tie,                                      // within 10% → human decides
  dissent: ranked.map(r => ({ proposal: r.i + 1, total: r.total, verdict: r.verdict })),
}
