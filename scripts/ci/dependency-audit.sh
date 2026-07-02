#!/usr/bin/env bash
# dependency-audit.sh — fail the build on any vulnerable .NET package, honoring a tracked,
# expiry-dated allowlist (security/dependency-audit-allowlist.txt).
#
# Behaviour (the security bar is UNCHANGED from the old inline check — only strengthened):
#   1. Every allowlist entry is validated (strict advisory-id format + a REAL calendar date for
#      <expiry>); an entry that is malformed OR past-expiry FAILS the build, so a stale suppression
#      surfaces instead of silently persisting.
#   2. `dotnet list --vulnerable --format json` is parsed STRUCTURALLY (not by console columns), and
#      a vulnerability is suppressed only when its advisory id EXACTLY matches an active allowlist id.
#      The build FAILS on any remaining vulnerable package (top-level or transitive, any severity).
#
# Parsing JSON (not grep on columns) fixes two real gaps: top-level rows carry an extra
# Requested/Resolved column that a column-position regex misses, and substring id-matching could
# both over-suppress (an id appearing outside the advisory field) and be spoofed by a malformed id.
#
# Usage:
#   scripts/ci/dependency-audit.sh                          # full check (needs dotnet + restore done)
#   scripts/ci/dependency-audit.sh --check-allowlist-only        # validate/expiry-check the allowlist only (no dotnet)
#   ALLOWLIST=path SOLUTION=x.slnf scripts/ci/dependency-audit.sh # overrides (for tests)
#   VULN_JSON=path scripts/ci/dependency-audit.sh                # parse a pre-captured json (for tests, no dotnet)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

ALLOWLIST="${ALLOWLIST:-security/dependency-audit-allowlist.txt}"
SOLUTION="${SOLUTION:-ClimaSite.NoE2E.slnf}"
MODE="full"
[ "${1:-}" = "--check-allowlist-only" ] && MODE="check"

# One Python program does allowlist validation and (in full mode) vuln matching — real date parsing,
# strict id regex, and exact advisory-id matching are all far safer here than in shell.
# argv: <allowlist-path> <mode: check|VULN_JSON_PATH>
audit_py() {
  python3 - "$ALLOWLIST" "$1" <<'PY'
import sys, os, re, json, datetime
allow_path, mode = sys.argv[1], sys.argv[2]
today = datetime.date.today()
id_re = re.compile(r'^(GHSA-[0-9a-z]{4}-[0-9a-z]{4}-[0-9a-z]{4}|CVE-\d{4}-\d{4,})$', re.I)

active, problems = set(), []
if os.path.exists(allow_path):
    with open(allow_path, encoding='utf-8') as fh:
        for raw in fh:
            line = raw.rstrip('\r\n'); s = line.strip()
            if not s or s.startswith('#'):
                continue
            parts = [p.strip() for p in line.split('|')]
            if len(parts) != 5:
                problems.append(f"malformed (need exactly 5 pipe-fields '<id>|<pkg>|<reason>|<tracking>|<expiry>'): {line}")
                continue
            aid, pkg, _reason, _tracking, expiry = parts
            if not id_re.match(aid):
                problems.append(f"bad advisory id '{aid}' (want GHSA-xxxx-xxxx-xxxx or CVE-YYYY-NNNN)")
                continue
            try:
                exp = datetime.date.fromisoformat(expiry)   # rejects 2026-99-99 etc.
            except ValueError:
                problems.append(f"entry '{aid}' has invalid expiry '{expiry}' (want a real YYYY-MM-DD)")
                continue
            if exp < today:
                problems.append(f"entry '{aid}' ({pkg}) EXPIRED on {expiry} — re-verify the advisory then bump the expiry, or remove the line")
                continue
            active.add(aid.upper())
            print(f"Active allowlist: {aid} ({pkg}) — expires {expiry}", file=sys.stderr)
else:
    print(f"No allowlist file at {allow_path} (nothing suppressed).", file=sys.stderr)

for p in problems:
    print(f"::error::dependency-audit allowlist: {p}", file=sys.stderr)
if problems:
    sys.exit(1)

if mode == "check":
    print(f"Allowlist OK ({len(active)} active entr{'y' if len(active)==1 else 'ies'}).", file=sys.stderr)
    sys.exit(0)

with open(mode, encoding='utf-8') as fh:
    data = json.load(fh)

# Pin the schema: `dotnet list --format json` defaults can move to a newer output version. We request
# --output-version 1; refuse to pass on anything else (fail closed) rather than silently find zero vulns.
if data.get("version") != 1 or not isinstance(data.get("projects"), list):
    print("::error::Unexpected `dotnet list --format json` schema (expected version:1 with a projects[] array) — refusing to pass; pin/update the parser.", file=sys.stderr)
    sys.exit(1)

def advisory_id(url):
    return (url or "").rstrip('/').split('/')[-1].upper()

findings = []
for proj in data.get("projects", []) or []:
    for fw in proj.get("frameworks", []) or []:
        for bucket in ("topLevelPackages", "transitivePackages"):
            for pkg in fw.get(bucket, []) or []:
                for v in pkg.get("vulnerabilities", []) or []:
                    aid = advisory_id(v.get("advisoryurl") or v.get("advisoryUrl"))
                    if aid in active:
                        continue
                    findings.append(f"{pkg.get('id')} {pkg.get('resolvedVersion','?')} [{v.get('severity','?')}] {aid or '(no advisory id)'}")

if findings:
    print("::error::Vulnerable .NET packages not covered by an active allowlist entry:", file=sys.stderr)
    for f in sorted(set(findings)):
        print(f"  - {f}", file=sys.stderr)
    sys.exit(1)
print(f"No vulnerable .NET packages (excluding {len(active)} active allowlist entr{'y' if len(active)==1 else 'ies'}).", file=sys.stderr)
PY
}

if [ "$MODE" = "check" ]; then
  audit_py "check"
  exit 0
fi

# Full mode: capture the vulnerability report as JSON, echo it for the log, then parse structurally.
json="${VULN_JSON:-}"
if [ -z "$json" ]; then
  json="$(mktemp)"
  # `dotnet list package` needs a prior restore (the CI job restores first); it exits 0 even when
  # vulnerabilities are found, so structural parsing (below) is what fails the build. --output-version 1
  # pins the JSON schema the parser expects.
  dotnet list "$SOLUTION" package --vulnerable --include-transitive --format json --output-version 1 > "$json"
fi
echo "----- dotnet list --vulnerable (json) -----"
cat "$json"
echo "-------------------------------------------"
audit_py "$json"
