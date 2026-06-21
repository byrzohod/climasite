---
name: perf-budget
description: Define and enforce performance budgets in CI -- LCP < 2.5s, INP < 200ms, CLS < 0.1, API p95 latency, bundle size per route. Use this whenever the user mentions performance budgets, slow page load, Web Vitals, Lighthouse CI, bundle size limits, latency targets, p50/p95/p99 thresholds, perf regression in CI, or wants to prevent performance degradation from sneaking into main.
---

# /perf-budget - Define and enforce performance budgets

## When to use

Invoke this skill:
- **Before launching the project to real users** -- set the initial budget
- **Before every release** -- verify nothing has regressed
- **When adding a major feature** -- update the budget if the feature legitimately changes the baseline
- **When users report "the app feels slow"** -- measure, don't guess

This skill is the **prevention** counterpart to `/autoresearch` (which **optimizes**). Set budgets here; iterate against them in autoresearch when something is over budget.

## Why budgets

Performance regressions are paid in compounding interest. A 50ms slowdown today plus a 30ms slowdown next month plus an extra 200KB of JS the month after equals an unhappy user a quarter from now -- and at that point, isolating the cause is expensive. Budgets catch each regression at the moment it lands.

## Budget Categories

### Frontend / Web Vitals

Set explicit thresholds for Core Web Vitals -- they directly impact UX and SEO ranking:

| Metric | Default Budget | Critical |
|--------|----------------|----------|
| **LCP** (Largest Contentful Paint) | < 2.5s | < 4.0s |
| **INP** (Interaction to Next Paint) | < 200ms | < 500ms |
| **CLS** (Cumulative Layout Shift) | < 0.1 | < 0.25 |
| **TTFB** (Time to First Byte) | < 600ms | < 1.5s |
| **Total Blocking Time** | < 200ms | < 600ms |

Adjust per page type if necessary -- a marketing landing page has tighter budgets than an authenticated dashboard.

### JavaScript Bundle

| Metric | Default Budget |
|--------|----------------|
| **Initial JS** (gzipped, route entry) | < 170KB |
| **Per-route lazy chunk** (gzipped) | < 100KB |
| **CSS** (gzipped, critical) | < 30KB |
| **Largest single image** (above the fold) | < 250KB |
| **Total page weight** (above the fold) | < 600KB |

These numbers are starting points for a typical SPA. SSR/SSG sites can usually go smaller; data-heavy dashboards may legitimately need more (set the budget honestly, then watch it).

### API / Backend

| Metric | Default Budget |
|--------|----------------|
| **p50 endpoint latency** (read endpoints) | < 100ms |
| **p95 endpoint latency** (read endpoints) | < 300ms |
| **p99 endpoint latency** (read endpoints) | < 800ms |
| **p95 endpoint latency** (write endpoints) | < 500ms |
| **DB query time** (per query, p95) | < 50ms |
| **Background job runtime** (per job, p95) | < 30s |

Set per-endpoint budgets where one default doesn't fit (a search endpoint with full-text scoring has different expectations than `GET /me`).

### Build / CI

| Metric | Default Budget |
|--------|----------------|
| **Cold install** (`npm ci`, `dotnet restore`, etc.) | < 90s |
| **Production build** | < 3 min |
| **Full test suite** | < 5 min |
| **CI pipeline end-to-end** | < 10 min |

Slow CI punishes every PR. If any of these exceed the budget, treat as tech debt.

## Setup

### Step 1: Decide what matters for this project

Don't enforce every metric -- pick the ones that matter for this product:

- A consumer landing page: LCP, CLS, JS bundle, total page weight (highest)
- A mobile-heavy app: INP, mobile-specific perf, image weights
- An API service: endpoint p95/p99, DB query time, throughput
- A SaaS dashboard: time-to-interactive, route-chunk size, data-fetching latency

For each chosen metric, write down: name, threshold, measurement method, who's responsible.

### Step 2: Capture the current baseline

```bash
# Frontend (Lighthouse via lhci, run multiple times for stability)
npx --yes @lhci/cli@latest collect --numberOfRuns=3 --url=http://localhost:3000

# Bundle size
npx --yes @next/bundle-analyzer
# or for vanilla webpack/vite:
npx --yes vite-bundle-visualizer
# or for any project:
du -sh dist/

# API latency (synthetic load)
npx --yes autocannon -c 10 -d 30 http://localhost:3000/api/health
# or k6:
k6 run scripts/load-test.js
```

Record the baseline alongside each metric. The budget is set RELATIVE to the baseline -- usually current value + 10-20% headroom for legitimate growth.

### Step 3: Write the budget file

Create `.performance-budget.json` (or your project's equivalent) at the repo root:

```json
{
  "frontend": {
    "lcp": { "max_ms": 2500, "critical_ms": 4000 },
    "inp": { "max_ms": 200, "critical_ms": 500 },
    "cls": { "max": 0.1, "critical": 0.25 },
    "ttfb_ms": { "max_ms": 600 },
    "bundle": {
      "initial_js_gzip_kb": 170,
      "per_route_chunk_gzip_kb": 100,
      "total_above_fold_kb": 600
    }
  },
  "api": {
    "endpoints": {
      "GET /api/users/:id": { "p95_ms": 100, "p99_ms": 300 },
      "POST /api/orders": { "p95_ms": 500, "p99_ms": 1500 },
      "*": { "p95_ms": 300, "p99_ms": 800 }
    }
  },
  "ci": {
    "install_seconds": 90,
    "build_seconds": 180,
    "test_seconds": 300,
    "pipeline_seconds": 600
  }
}
```

This file is the source of truth. CI reads it and fails the build if any metric exceeds its budget.

### Step 4: Wire enforcement into CI

The CI templates (`templates/ci/*.yml`) already include a `performance-budget` job that runs Lighthouse CI on PRs. Make sure your `lighthouserc.json` matches the frontend budgets:

```json
{
  "ci": {
    "collect": {
      "numberOfRuns": 3,
      "startServerCommand": "npm start",
      "url": ["http://localhost:3000/"]
    },
    "assert": {
      "assertions": {
        "categories:performance": ["error", { "minScore": 0.9 }],
        "categories:accessibility": ["error", { "minScore": 0.95 }],
        "largest-contentful-paint": ["error", { "maxNumericValue": 2500 }],
        "cumulative-layout-shift": ["error", { "maxNumericValue": 0.1 }],
        "interactive": ["warn", { "maxNumericValue": 3500 }],
        "resource-summary:script:size": ["error", { "maxNumericValue": 174000 }]
      }
    }
  }
}
```

For API budgets, run the load test in CI on PRs touching the affected endpoints (or nightly for full coverage), and fail if p95 exceeds the budget.

For bundle size, use `size-limit` (Node) or your stack's equivalent in CI.

### Step 5: Make budget violations LOUD

A failing budget check is not a warning -- it's a CI fail. The PR cannot merge until either:

- The regression is fixed
- The budget is **deliberately** updated (with a justification in the PR description and a one-line entry in CHANGELOG)

Sneaking past a budget by lowering it without explanation is a debt move. Make it visible.

## Maintenance

### Weekly: Review budget headroom

If a metric is consistently 20% under its budget, tighten it -- you're leaving free quality on the table. If a metric is consistently bumping the budget, either invest in optimization (with `/autoresearch`) or honestly raise the budget with a recorded reason.

### After every regression: Postmortem-lite

When CI fails on a budget check:

- Record what change caused the regression (in `docs/perf-history.md` or equivalent)
- Identify the specific commit, file, or dependency responsible
- Decide: fix forward, revert, or accept (and update the budget with reasoning)

Over time this history reveals which kinds of changes blow budgets -- often dependency upgrades, image-heavy features, or accidentally bundling a large library client-side.

### Quarterly: Re-baseline

Hardware, browsers, and user expectations change. Quarterly, re-measure baselines on a representative device, compare to the budgets, and update.

## When budgets are wrong

Budgets are not gospel. They can be wrong:

- Too tight -- legitimate features are blocked from shipping
- Too loose -- regressions slip through
- Wrong metric -- you're measuring something that doesn't correlate with user pain

If you find yourself routinely overriding the budget, that's a signal to recalibrate, not to disable enforcement.

## When NOT to use this skill

- Pre-MVP / pre-real-users: budgets are noise without traffic to compare against. Wait until the product is live.
- One-off scripts and CLIs: budgets target user-facing latency. A script run once a quarter doesn't need them.
- Internal tools where users are tolerant of slowness: spend the effort somewhere else.

## Integration with other skills

- **`/autoresearch`** -- when a budget is violated and the fix isn't obvious, autoresearch optimizes against the metric
- **`/verify-work`** -- a feature is not done if it pushes a metric over budget
- **`/code-review`** -- the performance dimension explicitly checks for known anti-patterns (N+1 queries, unoptimized images, etc.)
- **`/release`** -- pre-release verification includes a budget check
