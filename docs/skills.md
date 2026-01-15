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
  - Source generators (for code generation)

- **ASP.NET Core**
  - Web API development (Controllers and Minimal APIs)
  - Middleware pipeline configuration
  - Authentication/Authorization (JWT, ASP.NET Core Identity)
  - Model binding and validation
  - Configuration and options pattern
  - Health checks and diagnostics
  - ProblemDetails for error responses
  - Rate limiting and throttling
  - Response caching

- **Entity Framework Core**
  - Code-first migrations
  - Fluent API configuration
  - Query optimization and tracking behavior
  - Change tracking and state management
  - Raw SQL and stored procedures
  - JSONB column mapping (PostgreSQL)
  - Owned entities and value objects
  - Global query filters (soft deletes)
  - Split queries for performance

### Architecture Knowledge

- Clean Architecture / Onion Architecture
- CQRS pattern with MediatR
- Repository and Unit of Work patterns
- Domain-Driven Design basics
- SOLID principles
- Event-driven architecture (domain events)
- Vertical slice architecture

### Backend Libraries to Know

| Library | Purpose | Plan Reference |
|---------|---------|----------------|
| MediatR | CQRS implementation | All backend plans |
| FluentValidation | Input validation | AUTH, CAT, CHK |
| Argon2 (Isopoh.Cryptography.Argon2) | Password hashing | AUTH plan |
| Stripe.net | Payment processing | CHK plan |
| Serilog | Structured logging | All plans |
| Hangfire or Quartz.NET | Background jobs | INV, NOT plans |
| AutoMapper | Object mapping | All plans |
| Polly | Resilience patterns | External API calls |

---

## Frontend Development

### Core Skills

- **TypeScript**
  - Strong typing and interfaces
  - Generics and utility types
  - Decorators and metadata
  - ES modules and dynamic imports
  - Strict null checks
  - Type guards and narrowing

- **Angular 19+ (CRITICAL - Must use latest patterns)**
  - **Standalone components (NO NgModules)**
  - **Angular Signals for reactive state**
  - Signal inputs (`input()`, `input.required()`)
  - Signal outputs (`output()`)
  - Computed signals (`computed()`)
  - Effects (`effect()`)
  - Reactive Forms with validation
  - RxJS for HTTP and async operations (minimize usage)
  - Routing with guards and resolvers
  - HTTP interceptors (auth, error handling)
  - Lazy loading feature routes
  - Dependency injection with `inject()`
  - New control flow (`@if`, `@for`, `@switch`)

- **HTML/CSS**
  - Semantic HTML5
  - CSS Grid and Flexbox
  - CSS custom properties (variables)
  - Responsive design (mobile-first)
  - Accessibility (WCAG 2.1 AA)
  - SCSS/Sass preprocessing
  - BEM or similar naming convention

### UI/UX Design Skills

- Component design patterns
- State management with Angular Signals
- Performance optimization (OnPush, trackBy, defer)
- Cross-browser compatibility
- Mobile-first responsive development
- Form UX patterns (validation, error states)
- Loading states and skeleton screens
- Empty states and error handling UI
- Toast notifications and feedback
- Modal and dialog patterns
- Data table patterns (sorting, filtering, pagination)
- Infinite scroll and virtual scrolling
- Drag and drop interactions
- Animation and micro-interactions

### Frontend Libraries to Know

| Library | Purpose | Plan Reference |
|---------|---------|----------------|
| @ngx-translate/core | Runtime i18n | I18N plan |
| @ngx-translate/http-loader | Translation loading | I18N plan |
| @stripe/stripe-js | Stripe Payment Element | CHK plan |
| ngx-stripe | Angular Stripe integration | CHK plan |
| ng2-charts | Dashboard charts | ADM plan |
| Angular CDK | UI primitives, a11y | DST plan |

---

## UI/UX Architecture

### Design System Knowledge

- **Color Systems**
  - CSS custom properties for theming
  - Light/dark theme implementation
  - Color contrast ratios (WCAG)
  - Semantic color naming (primary, secondary, error)

- **Typography**
  - Font scale and hierarchy
  - Responsive typography
  - Line height and spacing

- **Component Architecture**
  - Atomic design principles
  - Presentational vs. container components
  - Composition patterns
  - Prop drilling vs. service injection

### Responsive Design

- Mobile-first approach
- Breakpoint system (sm, md, lg, xl)
- Flexible grids and layouts
- Touch-friendly interactions
- Viewport considerations

### Accessibility (WCAG 2.1 AA)

- Semantic HTML usage
- ARIA attributes and roles
- Keyboard navigation
- Focus management
- Screen reader support
- Color contrast (4.5:1 minimum)
- Form accessibility
- Skip links and landmarks

---

## Testing (CRITICAL)

### Backend Testing

- **xUnit**
  - Test organization (Facts, Theories)
  - Test fixtures and collections
  - Async test methods
  - Test output and logging
  - Test categories and traits

- **Moq**
  - Mock setup and verification
  - Argument matchers
  - Callback handling
  - Mock sequences

- **FluentAssertions**
  - Assertion syntax
  - Collection assertions
  - Exception assertions
  - Object graph comparison

- **Integration Testing**
  - WebApplicationFactory
  - TestContainers for PostgreSQL
  - In-memory database (for fast tests)
  - Test authentication/authorization
  - API contract testing

### Frontend Testing

- **Jasmine/Karma**
  - Unit test structure (describe, it, beforeEach)
  - Spies and mocks
  - Async testing (fakeAsync, tick, waitForAsync)
  - Component testing with TestBed
  - Service testing with HttpTestingController
  - Signal testing patterns

- **Component Testing**
  - Input/Output testing
  - Template testing
  - Change detection testing
  - Router testing
  - Form testing

### E2E Testing (Playwright) - CRITICAL

**E2E tests are MANDATORY for every feature. Must know:**

- **Playwright Fundamentals**
  - Page object model
  - Locators and selectors (`data-testid` attributes)
  - Assertions and expectations
  - Waiting strategies (auto-waiting, explicit waits)
  - Browser contexts and isolation

- **Test Data Management (NO MOCKING)**
  - Creating real test data via API
  - TestDataFactory pattern
  - Database cleanup strategies
  - Test isolation with correlation IDs
  - Unique data generation per test

- **Advanced Playwright**
  - Network interception (for Stripe test mode)
  - File uploads and downloads
  - iFrame handling (Stripe Payment Element)
  - Visual regression testing
  - Cross-browser testing (Chromium, Firefox, WebKit)
  - Mobile viewport testing
  - Screenshot comparison
  - Video recording for failures
  - Parallel test execution
  - Test retries and flaky test handling

### Playwright Test Pattern

```typescript
import { test, expect } from '@playwright/test';
import { TestDataFactory } from '../fixtures/test-data-factory';
import { loginAs } from '../helpers/auth-helper';

test.describe('Feature Name', () => {
  let factory: TestDataFactory;

  test.beforeEach(async ({ request }) => {
    factory = new TestDataFactory(request);
  });

  test('user can perform action', async ({ page }) => {
    // Arrange - Create REAL data (NO MOCKING)
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'Test Product',
      price: 599.99
    });

    // Act - Perform UI actions
    await loginAs(page, user.email, 'TestPass123!');
    await page.goto('/products');
    await page.click('[data-testid="product-card"]');
    await page.click('[data-testid="add-to-cart"]');

    // Assert - Verify results
    await expect(page.locator('[data-testid="cart-count"]')).toHaveText('1');
  });
});
```

---

## Database

### PostgreSQL 16+

- SQL query writing and optimization
- Index design and analysis (B-tree, GIN, GiST)
- **JSONB operations** (critical for product specs)
  - jsonb_extract_path
  - jsonb_agg
  - jsonb_build_object
  - JSONB indexing with GIN
- **Full-text search** (tsvector, tsquery, GIN indexes)
- Database normalization (3NF)
- Transaction management and isolation levels
- Stored procedures and triggers
- Window functions
- CTEs (Common Table Expressions)
- Database naming: **snake_case convention**
- EXPLAIN ANALYZE for query optimization

### Data Modeling Skills

- E-commerce data models
- Product variants and attributes (JSONB)
- Inventory tracking patterns
- Order state machines
- Soft deletes and audit trails
- Hierarchical data (categories with tree structure)
- Many-to-many relationships with extra columns
- Temporal data (price history, stock history)

### Redis

- Caching strategies (cache-aside, write-through)
- Session storage
- Rate limiting implementation
- Pub/sub for real-time features
- Data expiration and TTL
- Redis data structures (strings, hashes, lists, sets)

---

## DevOps & Infrastructure

### Essential

- **Docker**
  - Dockerfile creation (multi-stage builds)
  - Docker Compose for local development
  - Container networking
  - Volume management
  - Environment variables
  - Health checks

- **Git**
  - Branching strategies (feature branches)
  - Merge vs rebase
  - Conflict resolution
  - Conventional commits
  - Git hooks (pre-commit, pre-push)
  - Interactive rebase
  - Cherry-picking

- **CI/CD (GitHub Actions)**
  - Workflow configuration (YAML)
  - Service containers (PostgreSQL, Redis)
  - Test automation
  - Artifact management
  - Deployment pipelines
  - Matrix builds
  - Secrets management
  - Cache optimization

### Beneficial

- Kubernetes basics (deployments, services, ingress)
- Cloud platforms (Azure preferred)
- Nginx configuration and reverse proxy
- Linux server administration
- Monitoring (Prometheus, Grafana)
- Log aggregation (ELK stack)
- SSL/TLS certificate management

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
- Secure session management
- Content Security Policy (CSP)
- HTTP security headers

### Infrastructure Security

- HTTPS/TLS configuration
- Secrets management (User Secrets, Azure Key Vault)
- JWT token security (short expiry, refresh rotation)
- PCI compliance awareness (Stripe handles most)
- GDPR compliance (data deletion, export)
- Rate limiting
- API key management

---

## Performance Optimization

### Backend Performance

- Database query optimization
- N+1 query prevention
- Eager vs. lazy loading decisions
- Response caching
- Output caching
- Memory management
- Async/await best practices
- Connection pooling

### Frontend Performance

- Bundle size optimization
- Lazy loading routes
- Image optimization (WebP, lazy loading)
- Code splitting
- Tree shaking
- Virtual scrolling for large lists
- Memoization (computed signals)
- Change detection optimization (OnPush)
- Defer blocks for non-critical content
- Prefetching strategies

### Metrics to Monitor

- Time to First Byte (TTFB)
- Largest Contentful Paint (LCP)
- First Input Delay (FID)
- Cumulative Layout Shift (CLS)
- API response times
- Database query times

---

## E-Commerce Domain Knowledge

### Business Concepts

- Product catalog management
- Pricing strategies (discounts, bundles, dynamic pricing)
- Inventory management and stock reservations
- Order lifecycle and state machines
- Payment processing flows (Stripe)
- Shipping and fulfillment
- Customer service workflows
- Return/refund processes
- Promotions and coupons
- Wishlist and favorites
- Product recommendations

### HVAC Industry Knowledge (Important)

- **Product Categories**
  - Air Conditioners (split, multi-split, cassette, ducted, VRF/VRV)
  - Heating Systems (heat pumps, convectors, infrared, underfloor)
  - Ventilation (exhaust fans, recovery ventilators, air curtains)
  - Water Purification (filters, reverse osmosis, UV)
  - Accessories (installation kits, refrigerants, remote controls)

- **Technical Specifications**
  - **BTU (British Thermal Units)** - Cooling/heating capacity
  - **SEER** - Seasonal Energy Efficiency Ratio (cooling)
  - **SCOP** - Seasonal Coefficient of Performance (heating)
  - **Energy Class** (A+++, A++, A+, A, B, C, D)
  - **Noise levels** (dB indoor/outdoor)
  - **Refrigerant type** (R32, R410A)
  - **Voltage requirements**
  - **Room size coverage** (m² or ft²)
  - **Inverter vs. non-inverter technology**
  - **Wi-Fi/smart home compatibility**

- **Installation Considerations**
  - Professional installation requirements
  - Pipe length limitations
  - Electrical requirements
  - Mounting options (wall, floor, ceiling)

- **Warranty Handling**
  - Manufacturer warranty periods
  - Extended warranty options
  - Installation warranty requirements

- **Seasonal Demand Patterns**
  - Peak AC season (spring/summer)
  - Heating season (fall/winter)
  - Promotional timing

---

## Debugging & Troubleshooting

### Backend Debugging

- Visual Studio/Rider debugging
- Remote debugging
- Log analysis with Serilog
- Exception tracking
- Performance profiling
- Memory leak detection
- SQL query profiling

### Frontend Debugging

- Chrome/Firefox DevTools
- Angular DevTools extension
- Network request inspection
- Console debugging
- Source map debugging
- Performance profiling
- Memory profiling
- Redux DevTools (if using NgRx)

### E2E Test Debugging

- Playwright Inspector (`--debug`)
- Screenshot capture
- Video recording
- Trace viewer
- Network HAR files
- Console log capture

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
| UI/UX | Basic styling | Responsive design | Design systems |
| Accessibility | Awareness | WCAG AA compliance | Expert patterns |

---

## Training Resources

### Backend

- Microsoft Learn (.NET 10 paths)
- Pluralsight .NET courses
- "Clean Architecture" by Robert C. Martin
- MediatR documentation
- Entity Framework Core documentation

### Frontend

- Angular official documentation (angular.dev)
- Angular Signals guide
- "ng-book" for Angular
- CSS Tricks for advanced CSS
- web.dev for performance

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

### UI/UX

- Nielsen Norman Group articles
- Material Design guidelines
- WCAG 2.1 guidelines

---

## Skills by Implementation Plan

| Plan | Primary Skills Required |
|------|------------------------|
| 01-design-system-theming | CSS custom properties, SCSS, Angular signals |
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
| 17-future-enhancements | All skills combined |
| **18-bug-fixes** | **Debugging, CSS, Angular routing, Auth** |

---

## Team Composition Suggestion

### Minimum Viable Team

- 1 Full-stack Developer (Backend focus) - Must know .NET 10, EF Core, Playwright
- 1 Full-stack Developer (Frontend focus) - Must know Angular 19+, Playwright
- 1 Part-time DevOps / Infrastructure

### Recommended Team

- 2 Backend Developers (.NET 10, PostgreSQL)
- 2 Frontend Developers (Angular 19+, UI/UX)
- 1 Full-stack Developer
- 1 DevOps Engineer
- 1 QA Engineer (Playwright specialist)
- 1 UI/UX Designer
- 1 Product Owner / Business Analyst

---

## Critical Skills Summary

**These skills are NON-NEGOTIABLE for this project:**

1. **Playwright E2E Testing** - Every feature needs E2E tests with real data
2. **Angular Signals** - Primary state management approach (NOT RxJS BehaviorSubjects)
3. **Standalone Components** - NO NgModules, use `inject()` function
4. **PostgreSQL JSONB** - Product specifications storage
5. **MediatR/CQRS** - All backend operations use this pattern
6. **Stripe Integration** - Payment processing
7. **ngx-translate** - Internationalization (EN, BG, DE)
8. **CSS Custom Properties** - Theming system (light/dark)
9. **Docker** - Local development environment
10. **Accessibility (WCAG 2.1 AA)** - All UI must be accessible
11. **HVAC Domain Knowledge** - Understanding product specifications (BTU, SEER, etc.)
12. **Responsive Design** - Mobile-first approach

---

## Current Project Focus (Plan 18)

Based on `docs/plans/18-bug-fixes-and-enhancements.md`, the following skills are immediately needed:

| Task | Required Skills |
|------|----------------|
| NAV-001: Category Navigation | Angular routing, query params, signals |
| NAV-002: Wishlist Feature | Angular services, localStorage, API integration |
| AUTH-001: Auth Bug Fix | Angular guards, async initialization, signals |
| I18N-001/002: Translations | ngx-translate, JSON, Angular pipes |
| THEME-001: Color Contrast | CSS custom properties, WCAG contrast |
| HOME-001: Home Redesign | UI/UX design, Angular components, CSS |
| FILTER-001: Product Filters | Angular forms, URL params, API filtering |
