# Technology Stack

## Overview

This document outlines the technology choices for ClimaSite, an HVAC e-commerce platform. The stack prioritizes open-source solutions, maintainability, and scalability.

---

## Backend

### Framework: ASP.NET Core (.NET 10)

- **Why**: Enterprise-grade performance, cross-platform, excellent tooling
- **Features Used**:
  - Minimal APIs for simple endpoints
  - Controllers for complex operations
  - Built-in dependency injection
  - Health checks and diagnostics

### ORM: Entity Framework Core 10

- **Why**: First-class .NET integration, migration support, LINQ queries
- **Configuration**: Code-first approach with Fluent API

### Authentication: ASP.NET Core Identity + JWT

- **Why**: Built-in security, customizable, industry standard
- **Features**:
  - JWT tokens for API authentication
  - Refresh token rotation
  - Role-based authorization
  - OAuth2 support for social logins (future)

---

## Frontend

### Framework: Angular 19+ (Latest LTS)

- **Why**: TypeScript-first, comprehensive framework, excellent for large applications
- **Key Libraries**:
  - Angular Router for navigation
  - Angular Forms (Reactive)
  - Angular HttpClient for API calls
  - Angular CDK for UI primitives

### UI Components: Angular Material or PrimeNG

- **Options**:
  - **Angular Material**: Google's Material Design implementation
  - **PrimeNG**: Rich component library, more e-commerce focused
- **Decision**: TBD based on design requirements

### State Management: NgRx or Angular Signals

- **NgRx**: For complex state with time-travel debugging
- **Signals**: For simpler reactive state (built-in Angular 17+)

### CSS Framework: Tailwind CSS

- **Why**: Utility-first, highly customizable, small production bundle
- **Integration**: PostCSS with Angular CLI

---

## Database

### Primary Database: PostgreSQL 16+

- **Why**:
  - Robust ACID compliance
  - Excellent JSON support (for flexible product attributes)
  - Full-text search capabilities
  - Strong open-source community
  - Free and enterprise-ready

### Database Features to Utilize

- **JSONB columns**: For product specifications, variants
- **Full-text search**: For product search functionality
- **Array types**: For tags, categories
- **Materialized views**: For reporting dashboards

---

## Caching

### Distributed Cache: Redis

- **Why**: High performance, versatile data structures, pub/sub support
- **Use Cases**:
  - Session storage
  - Shopping cart (temporary)
  - Product catalog caching
  - Rate limiting

### In-Memory Cache: IMemoryCache

- **Why**: Built into ASP.NET Core, zero configuration
- **Use Cases**: Short-lived, single-instance caching

---

## Search

### Option 1: PostgreSQL Full-Text Search

- **Pros**: No additional infrastructure, good enough for most cases
- **Cons**: Limited advanced features

### Option 2: Elasticsearch / OpenSearch

- **Pros**: Advanced search, faceted navigation, typo tolerance
- **Cons**: Additional infrastructure complexity
- **Decision**: Start with PostgreSQL, migrate if needed

---

## File Storage

### Development: Local File System

### Production Options:

- **MinIO**: S3-compatible, self-hosted, open-source
- **Cloud Storage**: Azure Blob, AWS S3 (if cloud-hosted)

**Use Cases**: Product images, documents, invoices

---

## Message Queue (Future)

### RabbitMQ or Redis Streams

- **Use Cases**:
  - Order processing pipeline
  - Email notifications
  - Inventory sync
  - Analytics events

---

## Monitoring & Logging

### Logging: Serilog

- **Why**: Structured logging, multiple sinks, .NET integration
- **Sinks**: Console, File, Seq (development), ELK stack (production)

### Metrics: OpenTelemetry

- **Why**: Vendor-neutral, comprehensive observability
- **Exporters**: Prometheus, Jaeger

### Health Monitoring: ASP.NET Core Health Checks

- **Endpoints**: `/health`, `/health/ready`, `/health/live`

---

## Development Tools

### API Documentation: Swagger / OpenAPI

- **Library**: Swashbuckle or NSwag
- **Features**: Interactive docs, client generation

### Database Tools

- **Migrations**: EF Core Migrations
- **Admin**: pgAdmin, DBeaver

### Testing

- **Backend**: xUnit, Moq, FluentAssertions
- **Frontend**: Jasmine, Karma, Cypress (E2E)
- **API**: REST Client, Postman

---

## DevOps & Infrastructure

### Containerization: Docker

- **Backend**: .NET runtime image
- **Frontend**: Nginx serving static files
- **Database**: Official PostgreSQL image

### Orchestration (Production)

- **Options**: Docker Compose (simple), Kubernetes (scale)

### CI/CD

- **Options**: GitHub Actions, GitLab CI, Azure DevOps
- **Pipeline**: Build → Test → Analyze → Deploy

### Reverse Proxy: Nginx or YARP

- **Features**: SSL termination, load balancing, static file serving

---

## Security

### HTTPS: Let's Encrypt

- **Why**: Free, automated SSL certificates

### Secrets Management

- **Development**: User Secrets, .env files
- **Production**: Azure Key Vault, HashiCorp Vault, or environment variables

### Security Headers: ASP.NET Core Middleware

- CSP, HSTS, X-Frame-Options, etc.

---

## Summary Table

| Layer | Technology | License |
|-------|------------|---------|
| Backend Framework | ASP.NET Core .NET 10 | MIT |
| Frontend Framework | Angular | MIT |
| Database | PostgreSQL | PostgreSQL License |
| ORM | Entity Framework Core | MIT |
| Cache | Redis | BSD |
| Search | PostgreSQL FTS / OpenSearch | PostgreSQL / Apache 2.0 |
| File Storage | MinIO | AGPL / Commercial |
| Message Queue | RabbitMQ | MPL 2.0 |
| Logging | Serilog | Apache 2.0 |
| Containerization | Docker | Apache 2.0 |
| CSS | Tailwind CSS | MIT |

All core technologies are open-source with permissive licenses suitable for commercial use.
