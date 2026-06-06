---
name: agent-workflow
description: Load the master Agent Workflow document for any project work. Use at the start of a session, when planning a new feature, when uncertain about a procedure, or any time the user references "the workflow", "the workflow guide", or "agent workflow".
---

# /agent-workflow

The user maintains a master AI Agent Software Development Workflow at:

`/Users/sarkisharalampiev/Projects/vault/vault/AI/Agent Workflow.md`

It is the single source of truth for how the agent should approach any software project: project init, batch questions, layered planning, ADRs, branching, testing (with strict UI/E2E rules), code quality, code review, security, UI/UX, observability, deployment, rollback, runbooks, performance, releases, compliance, and ~50 more sections.

## When to invoke

- **Start of any new project** — read sections 1, 2, 3, 4
- **Planning a new feature** — read sections 2, 4, 5, 6
- **Before opening a PR** — read sections 6, 7 (use `/code-review` and `/security-review`)
- **Before deploying** — read section 17 (use `/deploy-checklist`)
- **Cutting a release** — read section 25 (use `/release`)
- **Writing a database migration** — read section 13 (use `/db-migrate`)
- **Working with the user's vault / Projects Catalog** — section 9.3
- Any time you are about to make an architectural decision and want to check the workflow's stance

## How to load

If access has not been granted in this session, request the AI directory:
```
mcp__cowork__request_cowork_directory({ path: "/Users/sarkisharalampiev/Projects/vault/vault/AI" })
```

Then read the workflow file in chunks (it is ~21k tokens — never read all at once):
```
Read /sessions/.../mnt/AI/Agent Workflow.md offset=<N> limit=<M>
```

## The non-negotiables (memorize, do not re-read)

1. **Batch questions** — 5–15 per round, max 2–3 rounds, never drip-feed
2. **User decides the stack** — never silently pick framework / DB / auth / hosting
3. **Tests with the feature** — never deferred, never placeholder
4. **No mocking in UI/E2E** — real DB (test containers), real backend, real services, human-like UI interaction
5. **Conventional commits + feature branches** — never push to main
6. **Visual verification** — Playwright screenshots before declaring UI done
7. **Lean CLAUDE.md** — ~200–300 lines, principles only, procedures live in skills
8. **ADR for every non-obvious decision** — `docs/adr/`
9. **Update memory + skills + CLAUDE.md after every iteration**
10. **Update Obsidian Projects Catalog** — `/Users/sarkisharalampiev/Projects/vault/vault/AI/Projects Catalog.md`

## Related skills in this project

- `/code-review` — multi-dimensional review, optionally with external Codex CLI second opinion
- `/security-review` — OWASP-driven security pass
- `/deploy-checklist` — pre/post deploy verification
- `/release` — semver tagging + changelog + release notes
- `/db-migrate` — safe expand-contract EF Core migrations
