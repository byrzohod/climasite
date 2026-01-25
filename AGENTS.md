# CLIMASITE KNOWLEDGE BASE

**Generated:** 2026-01-25  
**Commit:** 69b7e8b  
**Branch:** main

## OVERVIEW

HVAC e-commerce platform. ASP.NET Core 10 API + Angular 19 SPA + PostgreSQL + Redis. Multi-language (EN/BG/DE), dual-theme (light/dark), Stripe payments.

## STRUCTURE

```
climasite/
├── src/
│   ├── ClimaSite.Api/           # REST API, controllers, middleware
│   ├── ClimaSite.Application/   # CQRS handlers (MediatR), validators
│   ├── ClimaSite.Core/          # Domain entities, interfaces
│   ├── ClimaSite.Infrastructure/# EF Core, repositories, external services
│   └── ClimaSite.Web/           # Angular 19 SPA (standalone components)
├── tests/
│   ├── ClimaSite.Api.Tests/     # Integration tests (Testcontainers)
│   ├── ClimaSite.Application.Tests/
│   ├── ClimaSite.Core.Tests/
│   └── ClimaSite.E2E/           # Playwright (NO MOCKING - real data)
└── docs/plans/                  # Implementation plans
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Add API endpoint | `src/ClimaSite.Api/Controllers/` | Use MediatR dispatch |
| Add business logic | `src/ClimaSite.Application/Features/{Feature}/` | CQRS: Commands/ or Queries/ |
| Add domain entity | `src/ClimaSite.Core/Entities/` | Inherit BaseEntity, rich domain model |
| Add Angular component | `src/ClimaSite.Web/src/app/features/{feature}/` | Standalone, Signal-based |
| Add shared component | `src/ClimaSite.Web/src/app/shared/components/` | Reusable across features |
| Add Angular service | `src/ClimaSite.Web/src/app/core/services/` | Singleton, Signal state |
| Colors/theming | `src/ClimaSite.Web/src/styles/_colors.scss` | ONLY source of truth |
| Translations | `src/ClimaSite.Web/src/assets/i18n/{lang}.json` | EN, BG, DE |
| E2E tests | `tests/ClimaSite.E2E/Tests/` | Real API, TestDataFactory |

## CRITICAL CONVENTIONS

### Backend (.NET)
- **CQRS**: All operations via MediatR handlers in `Application/Features/`
- **Validation**: FluentValidation for all commands/queries
- **Entities**: Rich domain models with private setters, `SetUpdatedAt()` pattern
- **Naming**: snake_case for PostgreSQL columns

### Frontend (Angular 19)
- **Standalone only**: NO NgModules anywhere
- **Signals for state**: NOT RxJS BehaviorSubjects (RxJS only for HTTP)
- **inject() function**: NOT constructor injection
- **New control flow**: `@if`, `@for`, `@switch` (NOT `*ngIf`, `*ngFor`)
- **data-testid**: Required on ALL interactive elements

### Theming (CRITICAL)
```scss
// CORRECT
.button { background: var(--color-primary); }

// WRONG - NEVER hardcode colors
.button { background: #3b82f6; }
```

### i18n (CRITICAL)
- ALL user-facing text must use translation keys
- `{{ 'key' | translate }}` in templates
- Supported: EN (default), BG, DE

## ANTI-PATTERNS (FORBIDDEN)

| Pattern | Why |
|---------|-----|
| `as any`, `@ts-ignore` | Type safety violation |
| Hardcoded colors | Breaks theming |
| Hardcoded text | Breaks i18n |
| RxJS for UI state | Use Signals instead |
| Constructor injection | Use `inject()` |
| NgModules | Use standalone components |
| Mocking in E2E | Use real API + TestDataFactory |
| `*ngIf`, `*ngFor` | Use `@if`, `@for` |

## KNOWN ISSUES (TODOs)

| ID | Issue | Location |
|----|-------|----------|
| API-008,9,10 | N+1 queries in admin endpoints | AdminCategories, AdminQuestions, AdminReviews |
| API-014 | Shipping costs hardcoded | CreateOrderCommand.cs:179 |
| SVC-001 | Error messages not i18n | cart.service.ts |
| AUTH-015 | No session expiration warning | auth.service.ts |

## COMMANDS

```bash
# Backend
dotnet build
dotnet test
dotnet run --project src/ClimaSite.Api  # localhost:5000

# Frontend
cd src/ClimaSite.Web
ng serve                                  # localhost:4200
ng test --watch=false --browsers=ChromeHeadless

# E2E
cd tests/ClimaSite.E2E
npx playwright test

# Full test suite
dotnet test && cd src/ClimaSite.Web && ng test --watch=false --browsers=ChromeHeadless
```

## CHILD AGENTS.md

- `src/ClimaSite.Web/src/app/core/services/AGENTS.md` - Angular services
- `src/ClimaSite.Core/Entities/AGENTS.md` - Domain entities
- `src/ClimaSite.Application/Features/AGENTS.md` - CQRS handlers
- `src/ClimaSite.Web/src/app/shared/AGENTS.md` - Shared components
- `tests/ClimaSite.E2E/AGENTS.md` - E2E testing
