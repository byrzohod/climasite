# Implementation plans — index

Feature-level implementation plans for ClimaSite. Some are **active**, most are **archived or
superseded**. For *current* status and the live work queue, do not read these plans — go to
`.planning/STATE.md` and `docs/project-plan/` (see [`docs/README.md`](../README.md) for the map).

> **Plan-number collisions (audit note).** Plan numbering drifted over time. There are **two plans
> numbered 19** (`19-test-and-kg-hardening.md` active; `19-ui-ux-redesign-masterplan.md` superseded —
> plus a third 19-artifact in `archive/19-home-page-redesign.md`), **two numbered 20**, and **several
> numbered 21** (`21-...` ×3 at the top level, the `21B`–`21J` sub-plans, and `archive/21A-...`). The
> numbers are not unique; rely on the filename + status note below, not the number alone.

## Active

| Plan | Title | Status note |
|---|---|---|
| [18-project-completion.md](18-project-completion.md) | Project Completion & Production Readiness | **The live master plan.** Its task IDs are absorbed by `docs/project-plan/`; the roadmap there is now the planning entry point. |
| [19-test-and-kg-hardening.md](19-test-and-kg-hardening.md) | Test hardening (E2E + UI) + Knowledge-graph enrichment | **Active / mostly done** (2026-06-24). The current quality-hardening plan; B2/B3 component-spec work remains. |
| [21F-animation-interaction-audit.md](21F-animation-interaction-audit.md) | Animation & Interaction Audit | **Active tail** — Phases 1–4/6 done; Phase 5 (full Lighthouse/device profiling) deferred (needs a live stack). Codified in [`../animation-style-guide.md`](../animation-style-guide.md). Archive once ANIM-* closes. |

## Superseded / stale (kept for history)

These predate later work and no longer drive execution. Some carry their own `SUPERSEDED` banner.

| Plan | Title | Why superseded |
|---|---|---|
| [00-master-overview.md](00-master-overview.md) | Master Implementation Plan | **Bannered.** Original inception plan; "100% complete" status index is stale. |
| [12-notifications-system.md](12-notifications-system.md) | Notifications System Plan | Largely implemented (outbox GAP-03, in-app GAP-09); checkbox state never reconciled. Input to Plan 18 Phase 2. |
| [17-future-enhancements.md](17-future-enhancements.md) | Future Enhancements | Complete per its own status table; belongs in `archive/`. |
| [19-ui-ux-redesign-masterplan.md](19-ui-ux-redesign-masterplan.md) | UI/UX Redesign Masterplan | Home content replaced by Plan 18 / ADR 001; its over-animation direction was explicitly reversed by 21F. Number collides with `archive/19-home-page-redesign.md`. |
| [20-issue-registry.md](20-issue-registry.md) | Issue Registry | **Bannered.** Frozen 2026-01-24 audit registry; counts false today. |
| [20-quality-audit-masterplan.md](20-quality-audit-masterplan.md) | Quality Audit & Improvement Masterplan | The Jan-2026 audit program; not running. Historical. |
| [21-cdl-data-import-and-ux-improvements.md](21-cdl-data-import-and-ux-improvements.md) | CDL.bg Data Import & UX Improvements | 0/13 boxes; execution unclear. Awaiting disposition or fold into Plan 18. |
| [21-ui-improvement-plan.md](21-ui-improvement-plan.md) | UI Improvement Plan | Self-declared COMPLETE (v2.0, 2026-01-24). |
| [21-ui-redesign-master-plan.md](21-ui-redesign-master-plan.md) | UI/UX Redesign Master Plan | Parent of the 21B–21J sub-plans; one of three competing "masters", partly superseded by Plan 18. |
| [21B-product-experience-redesign.md](21B-product-experience-redesign.md) | Product Experience Redesign | Partially executed; surviving tasks folded into Plan 18. |
| [21C-navigation-header-redesign.md](21C-navigation-header-redesign.md) | Navigation & Header Redesign | Partially executed; design direction partly superseded. |
| [21D-cart-checkout-optimization.md](21D-cart-checkout-optimization.md) | Cart & Checkout Optimization | Partially executed; surviving tasks folded into Plan 18. |
| [21E-component-library-modernization.md](21E-component-library-modernization.md) | Component Library Modernization | Partially executed; surviving tasks folded into Plan 18. |
| [21G-trust-credibility-system.md](21G-trust-credibility-system.md) | Trust & Credibility System | Large open-task count; design direction partly superseded. |
| [21H-visual-assets-imagery.md](21H-visual-assets-imagery.md) | Visual Assets & Imagery | Declared complete per status-of-record; checkboxes not maintained. |
| [21I-mobile-experience-optimization.md](21I-mobile-experience-optimization.md) | Mobile Experience Optimization | Declared complete per status-of-record; checkboxes not maintained. |
| [21J-dark-mode-refinement.md](21J-dark-mode-refinement.md) | Dark Mode Refinement | Remaining tasks overlap Plan 18 Phase 3. |
| [22-validation-master-plan.md](22-validation-master-plan.md) | Validation Master Plan | "Draft — Area Catalog"; its output (`docs/validation/`) was already produced (and is itself now a frozen snapshot). |

## Archived (`archive/`) — complete

Plans whose features shipped. Numbers `01`–`11` are the original feature build-out; later additions
follow the same archive pattern.

| Plan | Feature |
|---|---|
| [archive/01-design-system-theming.md](archive/01-design-system-theming.md) | Design System & Theming |
| [archive/02-internationalization-i18n.md](archive/02-internationalization-i18n.md) | Internationalization (EN/BG/DE) |
| [archive/03-authentication-user-management.md](archive/03-authentication-user-management.md) | Authentication & User Management |
| [archive/04-product-catalog.md](archive/04-product-catalog.md) | Product Catalog |
| [archive/05-shopping-cart.md](archive/05-shopping-cart.md) | Shopping Cart |
| [archive/06-checkout-orders.md](archive/06-checkout-orders.md) | Checkout & Orders |
| [archive/07-admin-panel.md](archive/07-admin-panel.md) | Admin Panel |
| [archive/08-testing-infrastructure.md](archive/08-testing-infrastructure.md) | Testing Infrastructure |
| [archive/09-inventory-management.md](archive/09-inventory-management.md) | Inventory Management |
| [archive/10-search-navigation.md](archive/10-search-navigation.md) | Search & Navigation |
| [archive/11-reviews-ratings.md](archive/11-reviews-ratings.md) | Reviews & Ratings |
| [archive/13-wishlist.md](archive/13-wishlist.md) | Wishlist (sharing, guest merge) |
| [archive/19-home-page-redesign.md](archive/19-home-page-redesign.md) | Home page redesign (early; superseded by Plan 18 / ADR 001) |
| [archive/21A-home-page-redesign.md](archive/21A-home-page-redesign.md) | Home page redesign (21-series; superseded by Plan 18 / ADR 001) |
