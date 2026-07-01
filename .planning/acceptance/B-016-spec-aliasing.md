---
unit: B-016-spec-aliasing
surface: api + ui (real running API on :5029 + ng serve :4200 against shared Postgres :5432)
result: PASS
date: 2026-07-01
commit: feature/b-016-hvac-spec-resolver tip (validated against the working diff; final squash tip on merge)
driver: live-API recommendation drive (curl/jq across area/zone), PDP DTO inspection, and a headless Playwright
  UI drive of the real home wizard + a PDP. Backend behaviour is also covered by Application 965 + Api 470
  integration tests (Testcontainers), incl. a display-only-keys JSONB round-trip and the public-DTO strip.
---

# Acceptance — B-016 canonical HVAC spec resolver (+ realistic BTU recalibration)

The recommendation engine (and HVAC filter facets) read canonical camelCase spec keys the seeded/admin catalog
never emits, so fit-scoring silently collapsed to a flat fallback. This unit adds a shared alias-aware
`HvacSpecResolver`, recalibrates the sizing model to realistic BTU/m², and hides machine-only keys from the PDP.

## Setup
Real API on :5029 (Development) against the shared-infra Postgres (2260-product dev catalog). The 5 hand-authored
AC/heat-pump seed products were patched in the DB to the exact shape the new `DataSeeder` writes (display keys +
the added machine fields `isInverter`/`minTemp`/`recommendedRoomTypes`) so the machine-field behaviours + PDP
strip could be exercised live; the resolver already reads the display `"BTU"`/`"Noise Level"` keys from **all**
2260 products unchanged.

## What was verified (live)

### Recommendations are now size-matched (the core fix — was flat fallback)
- **`area=48 living B`** (required 48×250 = 12000): top-3 are all ~12200–12700 BTU inverter units, `score 1.0`,
  `matchReason=perfectFit`, with `btuCapacity` / `isInverter` / `noiseLevel` all populated (resolved from display
  + canonical keys). Before B-016 every product scored the same fallback and ranking was noise.
- **`area=12 bedroom B`** (required 3000): top products are ~2600 BTU units, `matchReason=efficient` — small rooms
  get small units.
- **`area=75 living C`** (required 24000): top-3 are ~22000–23400 BTU **inverter** units, `perfectFit` — large
  cold-zone rooms get large units.
- **Heat-pump alias precedence**: EcoHeat (`"BTU Cooling":24000`, no plain `"BTU"`) resolves `btu=24000` (cooling
  wins over `"BTU Heating"`) and is recommendable in zone C.

### PDP hides machine-only keys, keeps display specs
- `GET /api/products/dualzone-pro-12000` → `specifications` keys = `["BTU","Noise Level","Refrigerant","Room
  Size","SEER Rating"]` — the customer-facing rows; `isInverter`/`minTemp`/`recommendedRoomTypes` **stripped**.
- `GET /api/products/ecoheat-plus-24` → `["BTU Cooling","BTU Heating","HSPF","Operating Range","SEER Rating"]` —
  both BTU display rows kept, machine keys stripped.
- Confirmed again in the browser: the PDP spec table renders no "Is Inverter" / "Min Temp" / "Recommended Room
  Types" row.

### Frontend (headless Playwright drive of the real app)
- Home hero + sizing wizard + recommendation cards render; **zero console errors** (guards the NG0200-class
  runtime break that bit B-044 — every unit test was green then too).
- Area slider default = **35**; the required-BTU preview reflects the recalibrated **250 BTU/m²** (35 × 250 =
  **8750**), not the old 2640/3850 — FE and BE multipliers are in sync.

### Validation still holds
- `area=2` (below the 5–500 min) → `ValidationException` thrown (input rejected). Returns **400 in Testing/
  Production** (confirmed by the `GetRecommendations_InvalidArea_ReturnsBadRequest` integration test); the local
  Development run shows a 500 developer-exception page for the same failure — a **Dev-only middleware artifact**,
  not a B-016 regression (validation + the production 400 mapping are unchanged).

## Screenshots
`scratchpad/b016-home-light.png` (home wizard + size-matched recommendations), `scratchpad/b016-pdp.png` (PDP
spec table with no machine-key rows).

## Result
**PASS** — zero blocker / zero major. The one observation (Dev-only 500 on invalid input) is a pre-existing
environment artifact tracked as not-a-B-016-bug (production returns 400, proven by an integration test). Backend
Core 428 / Application 965 / Api 470 + FE 1788 green; `dotnet format` clean; break-probe proven; design +
diff councils clean (re-council: no findings).
