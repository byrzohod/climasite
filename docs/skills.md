# Required Skills & Team Competencies

## Overview

This document outlines the skills and competencies needed to successfully build and maintain the ClimaSite HVAC e-commerce platform based on the implementation plans in `docs/plans/`.

---

## Backend Development

### Core Skills

- **C# and .NET 10**
  - Async/await patterns and Task-based programming
  - LINQ and expression trees
  - Dependency injection (built-in DI container)
  - Generic types and constraints
  - Record types and pattern matching
  - Nullable reference types

- **ASP.NET Core**
  - Web API development (Controllers and Minimal APIs)
  - Middleware pipeline configuration
  - Authentication/Authorization (JWT, ASP.NET Core Identity)
  - Model binding and validation
  - Configuration and options pattern
  - Health checks and diagnostics
  - ProblemDetails for error responses

- **Entity Framework Core**
  - Code-first migrations
  - Fluent API configuration
  - Query optimization and tracking behavior
  - Change tracking
  - Raw SQL and stored procedures
  - JSONB column mapping (PostgreSQL)
  - Owned entities and value objects

### Architecture Knowledge

- Clean Architecture / Onion Architecture
- CQRS pattern with MediatR
- Repository and Unit of Work patterns
- Domain-Driven Design basics
- SOLID principles
- Event-driven architecture (domain events)

### Backend Libraries to Know

| Library | Purpose | Plan Reference |
|---------|---------|----------------|
| MediatR | CQRS implementation | All backend plans |
| FluentValidation | Input validation | AUTH, CAT, CHK |
| Argon2 (Isopoh.Cryptography.Argon2) | Password hashing | AUTH plan |
| Stripe.net | Payment processing | CHK plan |
| Serilog | Structured logging | All plans |
| Hangfire or Quartz.NET | Background jobs | INV, NOT plans |

---

## Frontend Development

### Core Skills

- **TypeScript**
  - Strong typing and interfaces
  - Generics
  - Decorators
  - ES modules
  - Utility types

- **Angular 19+**
  - Standalone components (no NgModules)
  - Angular Signals for reactive state
  - Reactive Forms with validation
  - RxJS for HTTP and async operations
  - Routing with guards and resolvers
  - HTTP interceptors (auth, error handling)
  - Lazy loading feature routes
  - Dependency injection

- **HTML/CSS**
  - Semantic HTML5
  - CSS Grid and Flexbox
  - CSS custom properties (variables)
  - Responsive design (mobile-first)
  - Accessibility (WCAG 2.1 AA)
  - Tailwind CSS utility classes

### UI/UX Skills

- Component design patterns
- State management with Angular Signals
- Performance optimization (OnPush, trackBy)
- Cross-browser compatibility
- Mobile-first development
- Form UX patterns

### Frontend Libraries to Know

| Library | Purpose | Plan Reference |
|---------|---------|----------------|
| @ngx-translate/core | Runtime i18n | I18N plan |
| @ngx-translate/http-loader | Translation loading | I18N plan |
| @stripe/stripe-js | Stripe Payment Element | CHK plan |
| ngx-stripe | Angular Stripe integration | CHK plan |
| ng2-charts | Dashboard charts | ADM plan |
| Angular CDK | UI primitives | DST plan |

---

## Testing (CRITICAL)

### Backend Testing

- **xUnit**
  - Test organization (Facts, Theories)
  - Test fixtures and collections
  - Async test methods
  - Test output and logging

- **Moq**
  - Mock setup and verification
  - Argument matchers
  - Callback handling

- **FluentAssertions**
  - Assertion syntax
  - Collection assertions
  - Exception assertions

- **Integration Testing**
  - WebApplicationFactory
  - TestContainers for PostgreSQL
  - In-memory database (for fast tests)
  - Test authentication

### Frontend Testing

- **Jasmine/Karma**
  - Unit test structure (describe, it)
  - Spies and mocks
  - Async testing
  - Component testing with TestBed

### E2E Testing (Playwright) - CRITICAL

**E2E tests are MANDATORY for every feature. Must know:**

- **Playwright Fundamentals**
  - Page object model
  - Locators and selectors (`data-testid` attributes)
  - Assertions and expectations
  - Waiting strategies
  - Browser contexts

- **Test Data Management (NO MOCKING)**
  - Creating real test data via API
  - TestDataFactory pattern
  - Database cleanup strategies
  - Test isolation with correlation IDs

- **Advanced Playwright**
  - Network interception (for Stripe test mode)
  - File uploads
  - iFrame handling (Stripe Payment Element)
  - Visual regression testing
  - Cross-browser testing

### Playwright Test Example Pattern

```typescript
import { test, expect } from '@playwright/test';
import { TestDataFactory } from '../fixtures/test-data-factory';

test.describe('Feature Name', () => {
  let factory: TestDataFactory;

  test.beforeEach(async ({ request }) => {
    factory = new TestDataFactory(request);
  });

  test('user can perform action', async ({ page }) => {
    // Arrange - Create REAL data (NO MOCKING)
    const user = await factory.createUser();
    const product = await factory.createProduct();

    // Act - Perform UI actions
    await page.goto('/login');
    await page.fill('[data-testid="email"]', user.email);
    await page.fill('[data-testid="password"]', 'TestPass123!');
    await page.click('[data-testid="login-button"]');

    // Assert - Verify results
    await expect(page).toHaveURL('/dashboard');
  });
});
```

---

## Database

### PostgreSQL 16+

- SQL query writing and optimization
- Index design and analysis (B-tree, GIN, GiST)
- **JSONB operations** (critical for product specs)
- **Full-text search** (tsvector, tsquery, GIN indexes)
- Database normalization
- Transaction management
- Stored procedures and triggers
- Database naming: **snake_case convention**

### Data Modeling Skills

- E-commerce data models
- Product variants and attributes (JSONB)
- Inventory tracking patterns
- Order state machines
- Soft deletes and audit trails

---

## DevOps & Infrastructure

### Essential

- **Docker**
  - Dockerfile creation (multi-stage builds)
  - Docker Compose for local development
  - Container networking
  - Volume management

- **Git**
  - Branching strategies (feature branches)
  - Merge vs rebase
  - Conflict resolution
  - Conventional commits

- **CI/CD (GitHub Actions)**
  - Workflow configuration
  - Service containers (PostgreSQL, Redis)
  - Test automation
  - Artifact management
  - Deployment pipelines

### Beneficial

- Kubernetes basics
- Cloud platforms (Azure preferred)
- Nginx configuration
- Linux server administration
- Monitoring (Prometheus, Grafana)

---

## Security

### Application Security

- OWASP Top 10 awareness
- Input validation and sanitization
- SQL injection prevention (EF Core parameterization)
- XSS prevention (Angular sanitization)
- CSRF protection
- Secure authentication flows
- **Argon2 password hashing** (not bcrypt)

### Infrastructure Security

- HTTPS/TLS configuration
- Secrets management (User Secrets, Azure Key Vault)
- JWT token security (short expiry, refresh rotation)
- PCI compliance awareness (Stripe handles most)
- GDPR compliance (data deletion, export)

---

## E-Commerce Domain Knowledge

### Business Concepts

- Product catalog management
- Pricing strategies (discounts, bundles)
- Inventory management and stock reservations
- Order lifecycle and state machines
- Payment processing flows (Stripe)
- Shipping and fulfillment
- Customer service workflows
- Return/refund processes

### HVAC Industry (Beneficial)

- Product categories (AC, heating, ventilation)
- Technical specifications (BTU, SEER, AFUE)
- Installation requirements
- Warranty handling
- Seasonal demand patterns

---

## Skill Matrix

| Skill Area | Junior | Mid | Senior |
|------------|--------|-----|--------|
| C#/.NET 10 | Basic syntax, CRUD | Async, DI, CQRS | Architecture, optimization |
| Angular 19+ | Components, templates | Signals, RxJS, state | Performance, testing |
| PostgreSQL | Basic SQL | Indexes, JSONB | Full-text search, optimization |
| Playwright | Basic tests | Page objects, factories | Visual regression, CI/CD |
| Docker | Run containers | Write Dockerfiles | Orchestration |
| Security | Awareness | Implementation | Architecture |
| E-commerce | Basic concepts | Full workflows | Complex scenarios |

---

## Training Resources

### Backend

- Microsoft Learn (.NET 10 paths)
- Pluralsight .NET courses
- "Clean Architecture" by Robert C. Martin
- MediatR documentation

### Frontend

- Angular official documentation (angular.dev)
- Angular Signals guide
- "ng-book" for Angular
- Tailwind CSS documentation

### Testing

- **Playwright documentation** (playwright.dev) - CRITICAL
- xUnit documentation
- "The Art of Unit Testing" book
- TestContainers documentation

### Database

- PostgreSQL official documentation
- "The Art of PostgreSQL" book
- Use The Index, Luke (indexing)
- PostgreSQL JSONB guide

### DevOps

- Docker official tutorials
- GitHub Actions documentation
- "The DevOps Handbook"

### E-Commerce

- Stripe documentation (stripe.com/docs)
- E-commerce architecture patterns
- Industry case studies

---

## Skills by Implementation Plan

| Plan | Primary Skills Required |
|------|------------------------|
| 01-design-system-theming | CSS custom properties, Tailwind, Angular signals |
| 02-internationalization | ngx-translate, Angular pipes, JSON |
| 03-authentication | ASP.NET Identity, JWT, Argon2, Angular guards |
| 04-product-catalog | EF Core, JSONB, PostgreSQL full-text search |
| 05-shopping-cart | Redis, session management, Angular state |
| 06-checkout-orders | Stripe API, state machines, transactions |
| 07-admin-panel | Angular routing, ng2-charts, CRUD patterns |
| 08-testing | **Playwright, xUnit, TestContainers** |
| 09-inventory | Database transactions, background jobs |
| 10-search-navigation | PostgreSQL FTS, faceted search, Angular |
| 11-reviews-ratings | EF Core, moderation workflows |
| 12-notifications | Email templates, background jobs, signals |
| 13-wishlist | Angular state, sharing/tokens |

---

## Team Composition Suggestion

### Minimum Viable Team

- 1 Full-stack Developer (Backend focus) - Must know .NET 10, EF Core, Playwright
- 1 Full-stack Developer (Frontend focus) - Must know Angular 19+, Playwright
- 1 Part-time DevOps / Infrastructure

### Recommended Team

- 2 Backend Developers (.NET 10, PostgreSQL)
- 2 Frontend Developers (Angular 19+, Tailwind)
- 1 Full-stack Developer
- 1 DevOps Engineer
- 1 QA Engineer (Playwright specialist)
- 1 UI/UX Designer
- 1 Product Owner / Business Analyst

---

## Critical Skills Summary

**These skills are NON-NEGOTIABLE for this project:**

1. **Playwright E2E Testing** - Every feature needs E2E tests with real data
2. **Angular Signals** - Primary state management approach
3. **PostgreSQL JSONB** - Product specifications storage
4. **MediatR/CQRS** - All backend operations use this pattern
5. **Stripe Integration** - Payment processing
6. **ngx-translate** - Internationalization
7. **CSS Custom Properties** - Theming system
8. **Docker** - Local development environment
