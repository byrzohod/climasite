---
unit: PLAN19-B3-specs
type: NORMAL
status: approved
created: 2026-06-28
plan_status: approved
design: ../../design/DESIGN.md
---

# Unit plan — Plan-19 B3: spec coverage for untested frontend files

## Context / Definition of Ready
Plan-19 B2 added specs for the highest-value untested components. B3 closes the remaining gap: **28
frontend files with NO colocated `*.spec.ts`** (verified 2026-06-28): 21 components + 6 directives + 1 pipe.
(The scoping's "~27 placeholder `should create` specs" turned out NOT to exist — existing specs are
substantive; the real gap is the 28 entirely-untested files.) Frontend coverage gate is ≥70% (enforced).

## Scope / approach
Write a meaningful colocated `*.spec.ts` for each of the 28 files, matching the project's conventions
(template: `src/app/shared/components/button/button.component.spec.ts`): standalone `TestBed.configureTesting
Module({ imports: [Cmp] })`; signal inputs via `fixture.componentRef.setInput(...)`; `@Output` via
`spyOn(cmp.out,'emit')`; services via `jasmine.createSpyObj` + signal stubs; `TranslateModule.forRoot` with a
fake loader (or `provideMock`) where the template uses `| translate`; `data-testid`/`By.css` queries;
`fakeAsync`/`tick` for async; `HttpTestingController` for services. NO placeholder-only specs — each asserts
real behavior (rendering, inputs→DOM, outputs, conditional logic, signal reactivity, public methods).
Directives: assert the host-element effect (class/style/observer wiring) with a `TestHostComponent`; respect
`prefers-reduced-motion` / SSR guards. Animation directives (count-up/reveal/scroll/etc.) — test the
non-animation logic (inputs, easing/format helpers, reduced-motion fallback, lifecycle cleanup) without
flaky real timers.

Files (28), batched for parallel authoring:
- **Auth/account:** forgot-password, reset-password, account-dashboard, settings · **about**
- **Directives:** count-up, animate-on-scroll, magnetic-hover, reveal, scroll-progress, split-text
- **Pipe:** spec-key
- **Shared:** glass-card, mini-cart-item, category-header, trust-badge-strip, warranty-card,
  product-consumables, similar-products, skeleton-product-card, skeleton-text
- **Feature pages:** brand-detail, brands-list, category-list, promotion-detail, promotions-list, resources,
  not-found

## Acceptance criteria
- [ ] All 28 files have a colocated `*.spec.ts` with real assertions (no `expect(component).toBeTruthy()`-only).
- [ ] `npm test -- --watch=false --browsers=ChromeHeadless` GREEN (full suite, incl. the i18n key-check).
- [ ] `ng lint` clean. Frontend coverage stays ≥70% (gate) — net up.
- [ ] No mocking violations / no `fdescribe`/`fit`; specs are deterministic (no real-timer flake).

## Test / verification plan
This unit IS tests. Verify by running the full frontend suite + lint; the new specs must pass and meaningfully
cover the targeted files (spot-check a few for real assertions, not placeholders). Test-only change → no
`/acceptance` runtime gate (no behavior change); a lighter review suffices (no cross-vendor council required
for test-only).

## Out of scope
Component/behavior changes (this is tests only); E2E; backend specs; the already-covered B2 components.

## DoD gates
Green CI (6 checks) · `ng lint` · coverage ≥70% · update CHANGELOG/BACKLOG (B3 progress). Test-only → no
council / no `/acceptance`.
