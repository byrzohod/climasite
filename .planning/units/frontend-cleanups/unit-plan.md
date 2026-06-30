---
unit: frontend-cleanups
title: Frontend follow-up cleanups (no-image asset, dead orphan components, load-race guard)
status: approved
severity: Low (×3 follow-ups)
source: B-002 + B-018/B-020 council/verifier follow-ups
date: 2026-06-30
---

# Unit plan — frontend follow-up cleanups

## FOUND-B002-noimage
Admin product list + related-products-manager referenced `assets/images/no-image.svg` (missing) → broken
thumbnails. A canonical placeholder exists at `assets/images/fallbacks/no-product-image.svg`. Fix: repoint the
3 refs to the existing asset.

## FOUND-B002-orphans
`frequently-bought` + `product-variants` shared components use the OPPOSITE sale convention and are confirmed
DEAD (zero `<app-*>` usages, zero imports, no routes/barrels). Fix: delete both + specs.

## FOUND-loaderr-race
`loadOrders()`/`loadCart()` had no latest-request guard. Fix: a monotonic `loadSeq`; only the most recent load
writes state. Tested with out-of-order resolution (cart via HttpTestingController.match, orders via Subjects).
