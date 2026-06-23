#!/usr/bin/env bash
# council.sh — Cross-vendor council: the Codex (OpenAI) leg.
#
# Runs Codex non-interactively, READ-ONLY, at a pinned model + reasoning effort, and
# writes ONLY the final message to <out>/codex.md for the /council synthesis step.
# The Claude leg runs natively as a blind, report-only subagent inside the /council
# skill — this script is just the Codex member (the leg that needs careful scripting).
#
# Usage:
#   council.sh --out <dir> --prompt-file <f> [--model <m>] [--effort <e>]
#   echo "<prompt>" | council.sh --out <dir>            # stdin also accepted (piped only)
#
# Defaults: model=gpt-5.5, effort=xhigh (strongest; enum: none|minimal|low|medium|high|xhigh).
# Always passed explicitly — `codex exec` honors -c even though the TUI has an effort-reset bug.
#
# READ-ONLY guarantee: `-s read-only` blocks the model's shell from writing the repo or
# reaching the network (verified: writes fail even inside the workspace; read-only git
# diff/log is still allowed and is fine). `approval_policy=never` fails CLOSED (auto-deny
# escalations, never auto-approve). Never use --dangerously-bypass-approvals-and-sandbox.
# The only files THIS SCRIPT writes are the council's own audit artifacts under <out>.
#
# Egress: Codex sends the prompt + any repo code it reads to OpenAI. Auth = ChatGPT
# sign-in (~/.codex/auth.json). Keep <out> (audit artifacts) gitignored — never committed.
set -euo pipefail

MODEL="gpt-5.5"; EFFORT="xhigh"; OUT=""; PROMPT_FILE=""

need_val() { [ "$2" -ge 2 ] || { echo "council.sh: $1 needs a value" >&2; exit 2; }; }
while [ $# -gt 0 ]; do case "$1" in
  --out)         need_val "$1" "$#"; OUT="$2"; shift 2;;
  --prompt-file) need_val "$1" "$#"; PROMPT_FILE="$2"; shift 2;;
  --model)       need_val "$1" "$#"; MODEL="$2"; shift 2;;
  --effort)      need_val "$1" "$#"; EFFORT="$2"; shift 2;;
  *) echo "council.sh: unknown arg: $1" >&2; exit 2;;
esac; done

# Validate effort against the Codex enum (a bad value would 400 mid-run).
case "$EFFORT" in
  none|minimal|low|medium|high|xhigh) ;;
  *) echo "council.sh: invalid --effort '$EFFORT' (none|minimal|low|medium|high|xhigh)" >&2; exit 2;;
esac

# --- Recursion guard: normalize to digits + fail closed; a council can never spawn a council. ---
DEPTH="${VAULT_COUNCIL_DEPTH:-0}"; case "$DEPTH" in *[!0-9]*) DEPTH=1;; esac
if [ "$DEPTH" -ge 1 ]; then
  echo "council.sh: already inside a council (depth=$DEPTH); refusing to nest" >&2
  exit 0
fi
export VAULT_COUNCIL_DEPTH=$(( DEPTH + 1 ))

[ -n "$OUT" ] || { echo "council.sh: --out <dir> is required" >&2; exit 2; }
mkdir -p -- "$OUT"

# --- Codex present? Degrade gracefully to a Claude-only council ---
if ! command -v codex >/dev/null 2>&1; then
  echo "council.sh: codex CLI not found — skipping Codex leg (council runs Claude-only)" >&2
  printf '_Codex CLI not installed — Codex leg skipped; council is Claude-only._\n' > "$OUT/codex.md"
  exit 0
fi

# --- Prompt: from --prompt-file, or piped stdin (never a TTY — that would hang) ---
if [ -n "$PROMPT_FILE" ]; then
  [ -r "$PROMPT_FILE" ] || { echo "council.sh: cannot read --prompt-file '$PROMPT_FILE'" >&2; exit 2; }
  PROMPT="$(cat -- "$PROMPT_FILE")"
elif [ ! -t 0 ]; then
  PROMPT="$(cat)"
else
  echo "council.sh: provide --prompt-file <f> or pipe a prompt on stdin" >&2; exit 2
fi
[ -n "$PROMPT" ] || { echo "council.sh: empty prompt" >&2; exit 2; }

# --- HARD RULE: Codex is a REPORT-ONLY advisor. Claude is the sole orchestrator/executor. ---
# The read-only sandbox is the real enforcement; this preamble is defense-in-depth.
REPORT_ONLY='You are an ADVISORY, REPORT-ONLY member of a cross-vendor review council. You run in a READ-ONLY sandbox: you CANNOT and MUST NOT modify files, write anything, commit, push, install, deploy, or synchronize anything (read-only inspection such as `git diff`/`git log` is fine). Respond with findings, analysis, risks, and recommendations as TEXT ONLY. A separate orchestrator (Claude) is the only agent that decides on and applies changes — your role is to advise it, not to act.

---

'
PROMPT="${REPORT_ONLY}${PROMPT}"

echo "council.sh: Codex leg — model=$MODEL effort=$EFFORT (READ-ONLY advisor, egress to OpenAI)" >&2

# Clear any STALE output so a failed run can never be mistaken for a fresh result.
rm -f -- "$OUT/codex.md" "$OUT/codex.jsonl" "$OUT/codex.err"

# Prompt AS A POSITIONAL ARG + </dev/null defuses the non-TTY pipe hang/crash
# (codex #20919/#19945). --skip-git-repo-check so it also works in fresh clones / CI
# (safe: the sandbox is read-only). Capture the REAL exit code (no `|| true` masking).
rc=0
codex exec "$PROMPT" \
  -m "$MODEL" \
  -c model_reasoning_effort="$EFFORT" \
  -s read-only \
  -c approval_policy="never" \
  --skip-git-repo-check \
  -o "$OUT/codex.md" \
  --json </dev/null >"$OUT/codex.jsonl" 2>"$OUT/codex.err" || rc=$?

if [ "$rc" -eq 0 ] && [ -s "$OUT/codex.md" ]; then
  echo "council.sh: Codex leg done -> $OUT/codex.md" >&2
else
  echo "council.sh: Codex leg FAILED (rc=$rc) — see $OUT/codex.err and codex.jsonl. Council degrades to Claude-only." >&2
  printf '_Codex leg failed (rc=%s); see codex.err. Council degrades to Claude-only._\n' "$rc" > "$OUT/codex.md"
fi
