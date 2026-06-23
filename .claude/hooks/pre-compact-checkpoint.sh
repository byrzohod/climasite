#!/usr/bin/env bash
# PreCompact hook — fires before (auto|manual) compaction.
#
# A shell hook can't serialize the agent's working memory, so it can't fully write STATE.md
# itself. Instead it drops a breadcrumb: after compaction the SessionStart hook re-injects
# STATE.md, and this note tells the next context to re-verify before continuing. The PRIMARY
# mechanism is the agent keeping STATE.md current via /checkpoint at unit boundaries; this is
# the safety net.
set -euo pipefail

STATE=".planning/STATE.md"
[ -f "$STATE" ] || exit 0

TS="$(date '+%Y-%m-%d %H:%M')"
printf '\n> NOTE: context compaction occurred at %s. Re-verify **Next action** + the active unit-plan against git before continuing; run /checkpoint if this file is stale.\n' "$TS" >> "$STATE"
exit 0
