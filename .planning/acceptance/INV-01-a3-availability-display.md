---
unit: INV-01-checkout-reservations (Wave A3)
gate: acceptance
result: PASS
date: 2026-07-02
branch: feature/inv-01-a3-availability-display
commit: <squash-merge tip of PR feature/inv-01-a3-availability-display>
env: Development, real API on :5029 against shared-infra Postgres :5432 + Redis :6379
---

# /acceptance — INV-01 Wave A3: reservation-aware availability DISPLAY (PDP + cart only)

**Scope:** A3 makes the product page and cart show honest `available = max(stock − reserved, 0)` (so shoppers don't
see/add units another checkout already holds), while browse/list/search/recommendation/admin deliberately stay on
raw `StockQuantity` (owner decision #3 — short holds would just flicker there). Display-only; the authoritative
charge-before-stock gate is A2.

## Scenarios driven against the REAL running app

| # | Scenario | Expected | Result |
|---|---|---|---|
| 1 | `stock=10, reserved=0` — GET the PDP + browse list | PDP `available=10`; browse `InStock=true` | **PASS** — PDP available=10; browse InStock=true |
| 2 | Insert an Active hold (`reserved=3`, another cart's checkout), re-GET the PDP | PDP `available` drops to **7** — and **fresh, no cache-expiry wait** (the [H] cache fix) | **PASS** — PDP `available=7` **immediately** (both cache layers bypassed: `ICacheableQuery` dropped + `[OutputCache(NoStore=true)]`) |
| 3 | Same held state — GET browse list | browse still `InStock=true` (raw stock, scope guard) | **PASS** — browse `InStock=true` (unchanged) |

Scenario 2 double-proves the cache fix: before A3 the PDP query was cached ~10 min (MediatR/Redis) **and** by a global
5-min HTTP output-cache base policy; the reserved-adjusted `availableQuantity` reflected the new hold with zero wait,
confirming both layers are bypassed. Cleanup: the test hold deleted, the variant reset to `stock=50 reserved=0`.

## Automated evidence (this branch)
- `dotnet build ClimaSite.sln`: 0 errors (1 pre-existing GDPR CS8602 warning).
- Core **430** / Application **1030** / Api integration **514** / Frontend **1801** — all green. `dotnet format ClimaSite.NoE2E.slnf --verify-no-changes`: clean.
- Key tests: PDP `AvailableQuantity == stock − reserved` + fresh-after-reserve (`PdpAvailability_IsFresh…` — a live break-probe that caught the output-cache layer) + `GetProductBySlugQuery_IsNotCacheable` (deterministic guard); cart `AvailableStock`/`MaxQuantity` reservation-adjusted with the cart's OWN hold added back (decrease-then-re-increase succeeds); `AddToCart` picks the first AVAILABLE active variant; scope-guard integration test — a fully-reserved variant (available 0) is still `InStock=true` on `/api/products`, `/search`, and `/recommendations`.

## Mechanism
`GetProductBySlugQuery`/`GetCartQuery`: `AvailableQuantity/AvailableStock = max(stock − reserved, 0)` (in-memory on
the materialized rows). Cart's own advisory ceiling adds back this cart's Active hold
(`CartReservationAvailability.GetOwnActiveHoldsAsync`) so a mid-checkout cart isn't blocked from re-growing to what it
holds. PDP display uses the DEFAULT variant (first-available-active, mirroring add-to-cart) not the sum. Cache: PDP
query out of both the MediatR cache (`ICacheableQuery` dropped) and the HTTP output cache (`[OutputCache(NoStore=true)]`).

## Council history
Diff R1 REWORK (cache staleness [H] + own-hold [M] + PDP-sum [M] + test breadth [L]) → R2 (cache CLOSED — the fix
found + bypassed BOTH cache layers; 2 [M] residual) → R3 (own-hold guard + PDP default-variant) → R3-recouncil found 2
more [M] (own-hold not yet in the cart MUTATION-response DTOs; PDP/add-to-cart variant selection order-dependent) →
fixed: a shared `CartReservationAvailability.LineAvailable` applied to ALL cart response mappers (GetCart + AddToCart +
UpdateCart + Merge), and deterministic `OrderBy(SortOrder).ThenBy(Id)` variant ordering in both the PDP DTO and
add-to-cart default selection. All verified green. (A trivial non-nullable `VariantId` compile slip from an agent pass
was fixed inline.) Display-only wave; the money-path concurrency gate is A2 (merged).

## Verdict
**PASS** — zero blocker, zero major. The PDP + cart show honest, FRESH reservation-adjusted availability; browse/
search/recommendations/admin stay on raw stock per the owner decision; a cart's own held units count toward its own
ceiling. Ready for PR → CI → squash-merge. **Wave B** (bank-transfer hold-with-expiry) is the last INV-01 slice.
