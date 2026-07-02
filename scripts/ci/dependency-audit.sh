#!/usr/bin/env bash
# dependency-audit.sh — fail the build on any vulnerable .NET package, honoring a tracked,
# expiry-dated allowlist (security/dependency-audit-allowlist.txt).
#
# Behaviour (the security bar is UNCHANGED from the old inline check — only strengthened):
#   1. Every allowlist entry is validated; an entry whose <expiry> is in the past (or that is
#      malformed) FAILS the build, so a stale suppression surfaces instead of silently persisting.
#   2. `dotnet list --vulnerable` runs; lines for each ACTIVE (non-expired) allowlisted advisory
#      id are dropped, then the build FAILS on ANY remaining vulnerable package line.
#
# Usage:
#   scripts/ci/dependency-audit.sh                     # full check (needs dotnet + restore done)
#   scripts/ci/dependency-audit.sh --check-allowlist-only   # only validate/expiry-check the allowlist (no dotnet)
#   ALLOWLIST=path scripts/ci/dependency-audit.sh      # override the allowlist path (for tests)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

ALLOWLIST="${ALLOWLIST:-security/dependency-audit-allowlist.txt}"
SOLUTION="${SOLUTION:-ClimaSite.NoE2E.slnf}"
CHECK_ONLY=0
[ "${1:-}" = "--check-allowlist-only" ] && CHECK_ONLY=1

today="$(date -u +%Y-%m-%d)"   # ISO dates sort lexicographically = chronologically (portable, no `date -d`)
active_ids=()
problems=0

if [ -f "$ALLOWLIST" ]; then
  # `|| [ -n "$line" ]` processes a final line with no trailing newline.
  while IFS= read -r line || [ -n "$line" ]; do
    # strip a trailing CR (CRLF-safe) and skip comments / blanks
    line="${line%$'\r'}"
    case "$(printf '%s' "$line" | tr -d '[:space:]')" in ''|'#'*) continue;; esac

    IFS='|' read -r id pkg reason tracking expiry _rest <<< "$line"
    id="$(printf '%s' "${id:-}" | xargs)"
    expiry="$(printf '%s' "${expiry:-}" | xargs)"
    pkg="$(printf '%s' "${pkg:-}" | xargs)"

    if [ -z "$id" ] || [ -z "$expiry" ]; then
      echo "::error::Malformed allowlist entry (need '<id> | <pkg> | <reason> | <tracking> | <expiry>'): $line"
      problems=1; continue
    fi
    if ! printf '%s' "$expiry" | grep -qE '^[0-9]{4}-[0-9]{2}-[0-9]{2}$'; then
      echo "::error::Allowlist entry '$id' has a non-ISO expiry '$expiry' (want YYYY-MM-DD)."
      problems=1; continue
    fi
    if [[ "$expiry" < "$today" ]]; then
      echo "::error::Allowlist entry '$id' ($pkg) EXPIRED on $expiry — re-verify the advisory then bump the expiry, or remove the line."
      problems=1; continue
    fi
    active_ids+=("$id")
    echo "Active allowlist: $id ($pkg) — expires $expiry"
  done < "$ALLOWLIST"
else
  echo "No allowlist file at $ALLOWLIST (nothing suppressed)."
fi

if [ "$problems" -ne 0 ]; then
  echo "::error::Dependency-audit allowlist has expired/malformed entries — fix $ALLOWLIST before merge."
  exit 1
fi

if [ "$CHECK_ONLY" -eq 1 ]; then
  echo "Allowlist OK (${#active_ids[@]} active entr$([ "${#active_ids[@]}" -eq 1 ] && echo y || echo ies))."
  exit 0
fi

out="$(dotnet list "$SOLUTION" package --vulnerable --include-transitive 2>&1)"
echo "$out"

filtered="$out"
for id in "${active_ids[@]:-}"; do
  [ -n "$id" ] && filtered="$(printf '%s' "$filtered" | grep -Fv "$id" || true)"
done

# `dotnet list --vulnerable` prints vulnerable packages as:  `   > <pkg> <resolved> <severity> <url>`
if printf '%s' "$filtered" | grep -qiE '^[[:space:]]*>[[:space:]]+\S+[[:space:]]+\S+[[:space:]]+(Critical|High|Moderate|Low)\b'; then
  echo "::error::Vulnerable .NET packages found that are not covered by an active allowlist entry — update them before merge."
  exit 1
fi
echo "No vulnerable .NET packages (excluding ${#active_ids[@]} active allowlist entr$([ "${#active_ids[@]}" -eq 1 ] && echo y || echo ies))."
