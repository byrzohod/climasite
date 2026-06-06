---
name: climasite-release
description: Use when cutting a ClimaSite release, version bump, changelog, release notes, tag, or release PR.
---

# ClimaSite Release

Preflight:
- Work from up-to-date `main` or a release branch.
- CI green and local checks complete.
- Code review and security review clean.
- Migrations and rollback plan verified.
- No release-blocking TODOs introduced.

Versioning:
- `feat!:` or `BREAKING CHANGE` -> major.
- `feat:` -> minor.
- fixes/docs/chore only -> patch.

Produce changelog, user-facing release notes in `docs/releases/`, update version-bearing files, commit with `chore(release): vX.Y.Z`, tag, push through PR/branch protection, and smoke test staging before prod.

