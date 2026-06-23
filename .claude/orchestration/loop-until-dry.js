// TEMPLATE — repeat discovery rounds until consecutive rounds add nothing new (or caps hit).
// Adapt: meta, the round agent + ITEM_SCHEMA, key(), and the caps.
// This is the research "second sweep until dry" with explicit STOP + no-progress HALT.
// Sequential fallback: orchestrator loops, spawning one discovery Task per round and
// diffing against `seen` itself.

export const meta = {
  name: 'loop-until-dry',
  description: 'Keep discovering until rounds stop adding new items, bounded by the round cap and per-loop agent-call ceiling with a no-progress HALT',
  phases: [{ title: 'Discover' }],
}

const ITEM_SCHEMA = {
  type: 'object', additionalProperties: false, required: ['items'],
  properties: { items: { type: 'array', items: {
    type: 'object', additionalProperties: false, required: ['id', 'desc'],
    properties: { id: { type: 'string' }, desc: { type: 'string' } } } } },
}
const key = it => String(it.id || it.desc || '').toLowerCase().trim()

const MAX_ROUNDS = 2          // ADAPT: ≤2 research sweeps
const MAX_AGENT_CALLS = 50    // per-loop agent-call ceiling (count, not dollars; distinct from the ≤16 *concurrency* cap)
const DRY_STOP = 2            // stop after this many CONSECUTIVE dry rounds (matches README's "2 consecutive dry")
const SATURATION = 0.15       // a round adding < 15% new is treated as "dry"
const QUESTION = (typeof args !== 'undefined' && args && args.question) || 'ADAPT: what to discover'

phase('Discover')
const seen = new Set(), all = []
let dry = 0, round = 0, agentCalls = 0, wentDry = false
while (round < MAX_ROUNDS) {
  if (agentCalls >= MAX_AGENT_CALLS) { log('agent-call ceiling — stopping'); break }
  round++
  agentCalls++
  const r = await agent(
    `Discovery round ${round} for:\n${QUESTION}\n\nFind items NOT already known:\n` +
    `${[...seen].slice(0, 50).join('; ') || '(none yet)'}`,
    { label: `discover:r${round}`, phase: 'Discover', schema: ITEM_SCHEMA, effort: 'max' }
  )
  const items = (r && r.items) || []
  const fresh = items.filter(it => !seen.has(key(it)))
  fresh.forEach(it => { seen.add(key(it)); all.push(it) })
  const ratio = items.length ? fresh.length / items.length : 0
  log(`round ${round}: +${fresh.length} new (saturation ${Math.round(ratio * 100)}%)`)
  if (fresh.length === 0 || ratio < SATURATION) dry++; else dry = 0
  if (dry >= DRY_STOP) { wentDry = true; break }   // stop on DRY_STOP consecutive dry rounds
}
// No-progress HALT: two consecutive rounds added nothing new (the loop went dry rather than
// finishing under a cap), or nothing was ever found — escalate to a human either way.
if (wentDry || all.length === 0) log('HALT: no progress (consecutive dry rounds) — escalate to human')
return { items: all, rounds: round }
