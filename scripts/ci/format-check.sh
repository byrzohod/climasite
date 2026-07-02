#!/usr/bin/env bash
# format-check.sh — the SINGLE canonical C# format check.
#
# Runs the EXACT command the CI "Lint & Format" job runs, so local and CI are byte-identical:
#     dotnet format ClimaSite.NoE2E.slnf --verify-no-changes
#
# WHY THIS EXISTS: never run `dotnet format --severity info` — it reformats the whole
# ~280-file repo (recover with `git checkout -- '*.cs'`). The gate is DEFAULT severity on the
# no-E2E solution filter. Use this script instead of retyping (and mis-typing) the flags.
#
# Usage:
#   scripts/ci/format-check.sh            # local: restores + verifies formatting
#   scripts/ci/format-check.sh --no-restore   # CI: skip restore (a prior step already restored)
# Any extra args are passed straight through to `dotnet format`.
#
# Exit code is `dotnet format`'s: 0 = clean, non-zero = formatting drift (the file list is printed).
set -euo pipefail

# Resolve repo root from this script's location so it works from any CWD.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

exec dotnet format ClimaSite.NoE2E.slnf --verify-no-changes "$@"
