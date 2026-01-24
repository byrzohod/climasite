# Platform & Infrastructure - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Application Startup (API)** - ASP.NET Core Web API initialization, middleware pipeline, service registration
- **Application Startup (Web)** - Angular SPA, nginx configuration, runtime environment injection
- **Environment Configuration** - appsettings.json, environment variables, development vs production settings
- **Secrets Management** - JWT secrets, database credentials, Stripe keys, Redis passwords
- **Docker/Compose Services** - PostgreSQL, Redis, MinIO, pgAdmin containerization
- **Health Checks** - Database and Redis connectivity health endpoints
- **Database Migrations & Seeding** - EF Core migrations, DataSeeder initialization
- **Background Jobs** - Hosted services, scheduled tasks (if any)

### Infrastructure Philosophy
- **Environment variable precedence** - Production uses env vars (Railway), development uses appsettings
- **URL conversion** - PostgreSQL and Redis URLs converted to connection strings automatically
- **Health-first deployment** - Health checks enable container orchestration and load balancing
- **Idempotent seeding** - Database seeding checks for existing data before inserting

---

## 2. Code Path Map

### API Application Startup

| File | Path | Purpose |
|------|------|---------|
| **Program.cs** | `src/ClimaSite.Api/Program.cs` | Main entry point, service configuration, middleware pipeline |
| **ExceptionHandlingMiddleware.cs** | `src/ClimaSite.Api/Middleware/ExceptionHandlingMiddleware.cs` | Global exception handling, error responses |

### Infrastructure Layer

| File | Path | Purpose |
|------|------|---------|
| **DependencyInjection.cs** | `src/ClimaSite.Infrastructure/DependencyInjection.cs` | Infrastructure service registration (EF, Identity, Redis, Storage) |
| **ApplicationDbContext.cs** | `src/ClimaSite.Infrastructure/Data/ApplicationDbContext.cs` | EF Core database context |
| **DataSeeder.cs** | `src/ClimaSite.Infrastructure/Data/DataSeeder.cs` | Database initialization and seed data |

### Configuration Files

| File | Path | Purpose |
|------|------|---------|
| **appsettings.json** | `src/ClimaSite.Api/appsettings.json` | Base configuration (connection strings, JWT, Stripe, logging) |
| **appsettings.Development.json** | `src/ClimaSite.Api/appsettings.Development.json` | Development-specific overrides |

### Frontend Infrastructure

| File | Path | Purpose |
|------|------|---------|
| **environment.ts** | `src/ClimaSite.Web/src/environments/environment.ts` | Development environment (localhost:5029) |
| **environment.prod.ts** | `src/ClimaSite.Web/src/environments/environment.prod.ts` | Production environment (relative /api) |
| **nginx.conf.template** | `src/ClimaSite.Web/nginx.conf.template` | Nginx configuration with API proxy |
| **docker-entrypoint.sh** | `src/ClimaSite.Web/docker-entrypoint.sh` | Runtime environment injection |

### Docker Configuration

| File | Path | Purpose |
|------|------|---------|
| **docker-compose.yml** | `docker-compose.yml` | Local development services (Postgres, Redis, MinIO, pgAdmin) |
| **Dockerfile.api** | `Dockerfile.api` | API container build (multi-stage .NET 10) |
| **Dockerfile.web** | `Dockerfile.web` | Web container build (Node 22 + nginx) |

### Database Migrations

| File | Path | Purpose |
|------|------|---------|
| **InitialCreate** | `src/ClimaSite.Infrastructure/Migrations/20260111071552_InitialCreate.cs` | Initial schema |
| **AddNotifications** | `src/ClimaSite.Infrastructure/Migrations/20260111094355_AddNotifications.cs` | Notifications table |
| **AddProductTranslations** | `src/ClimaSite.Infrastructure/Data/Migrations/20260111164205_AddProductTranslations.cs` | Product translations |
| **AddOrderEvents** | `src/ClimaSite.Infrastructure/Data/Migrations/20260111222309_AddOrderEvents.cs` | Order event tracking |
| **AddProductQA** | `src/ClimaSite.Infrastructure/Data/Migrations/20260113185306_AddProductQuestionsAndAnswers.cs` | Q&A feature |
| **AddInstallationRequests** | `src/ClimaSite.Infrastructure/Data/Migrations/20260113190643_AddInstallationRequests.cs` | Installation requests |
| **AddProductPriceHistory** | `src/ClimaSite.Infrastructure/Data/Migrations/20260113191619_AddProductPriceHistory.cs` | Price history tracking |

### Test Infrastructure

| File | Path | Purpose |
|------|------|---------|
| **TestWebApplicationFactory.cs** | `tests/ClimaSite.Api.Tests/Infrastructure/TestWebApplicationFactory.cs` | Test server with Testcontainers PostgreSQL |
| **IntegrationTestBase.cs** | `tests/ClimaSite.Api.Tests/Infrastructure/IntegrationTestBase.cs` | Base class for integration tests |

---

## 3. Test Coverage Audit

### Current Test Coverage

| Area | Test Type | Files | Status |
|------|-----------|-------|--------|
| **API Startup** | Integration | `TestWebApplicationFactory.cs` | Partial - Tests app builds |
| **Health Checks** | None | - | **MISSING** |
| **Database Migrations** | Integration | Via `InitializeAsync()` | Implicit |
| **Database Seeding** | None | - | **MISSING** |
| **Docker Builds** | None | - | **MISSING** |
| **Environment Config** | None | - | **MISSING** |
| **Exception Middleware** | None | - | **MISSING** |

### Test Infrastructure Details

```csharp
// TestWebApplicationFactory uses Testcontainers for real PostgreSQL
private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("climasite_test")
    .WithUsername("test_user")
    .WithPassword("test_password")
    .Build();
```

### Missing Test Coverage

| Gap | Risk Level | Description |
|-----|------------|-------------|
| Health check tests | High | `/health` endpoint not tested |
| Startup failure tests | High | Missing config scenarios not tested |
| Migration rollback tests | Medium | No tests for migration down paths |
| Seeder idempotency tests | Medium | Data seeding edge cases not tested |
| Docker build tests | Low | Container builds not CI-verified |
| Environment fallback tests | Medium | Config precedence not unit tested |

---

## 4. Manual Verification Steps

### API Startup Verification

```bash
# 1. Start infrastructure services
cd /Users/sarkisharalampiev/Projects/climasite
docker-compose up -d postgres redis

# 2. Run API (should apply migrations and seed)
cd src/ClimaSite.Api
dotnet run

# 3. Verify health check
curl http://localhost:5000/health
# Expected: Healthy

# 4. Verify Swagger
open http://localhost:5000/swagger
```

### Frontend Startup Verification

```bash
# 1. Start Angular dev server
cd src/ClimaSite.Web
npm install
ng serve

# 2. Verify app loads
open http://localhost:4200

# 3. Check API connectivity (requires backend running)
# Network tab should show successful API calls
```

### Docker Build Verification

```bash
# 1. Build API container
docker build -f Dockerfile.api -t climasite-api .

# 2. Build Web container
docker build -f Dockerfile.web -t climasite-web .

# 3. Run full stack with compose
docker-compose up -d
docker-compose logs -f

# 4. Verify health checks
docker inspect climasite_postgres --format='{{.State.Health.Status}}'
docker inspect climasite_redis --format='{{.State.Health.Status}}'
```

### Database Migration Verification

```bash
# 1. Check pending migrations
cd src/ClimaSite.Infrastructure
dotnet ef migrations list --startup-project ../ClimaSite.Api

# 2. Apply migrations
dotnet ef database update --startup-project ../ClimaSite.Api

# 3. Verify schema in pgAdmin
open http://localhost:5050
# Login: admin@climasite.local / admin
```

### Environment Configuration Verification

```bash
# 1. Verify env var precedence (should use DATABASE_URL if set)
export DATABASE_URL="postgresql://test:test@localhost:5432/test"
dotnet run --project src/ClimaSite.Api
# Check logs for connection string used

# 2. Verify JWT config
export JWT_SECRET="TestSecretKeyAtLeast32CharactersLong"
dotnet run --project src/ClimaSite.Api
# Should start without error
```

---

## 5. Gaps & Risks

### Critical Gaps

| Gap | Impact | Risk Level |
|-----|--------|------------|
| **No health check tests** | Deployment issues go undetected | Critical |
| **No startup failure tests** | Missing config crashes production | Critical |
| **Secrets in appsettings.json** | Development secrets in repo (acceptable for dev-only) | Medium |

### High-Priority Gaps

| Gap | Impact | Risk Level |
|-----|--------|------------|
| No background job infrastructure | Cannot run scheduled tasks | High |
| No readiness vs liveness probes | K8s deployments less reliable | High |
| No circuit breaker for Redis | Redis failures cascade to API | High |
| No graceful shutdown handling | In-flight requests may fail | Medium |

### Medium-Priority Gaps

| Gap | Impact | Risk Level |
|-----|--------|------------|
| No structured logging to file/sink | Production debugging harder | Medium |
| No request rate limiting | API vulnerable to abuse | Medium |
| No correlation ID propagation | Request tracing incomplete | Medium |
| No database connection pooling config | May hit connection limits | Medium |

### Low-Priority Gaps

| Gap | Impact | Risk Level |
|-----|--------|------------|
| No APM integration | Performance monitoring missing | Low |
| No distributed tracing | Cross-service debugging harder | Low |
| No blue-green deployment support | Zero-downtime deploys harder | Low |

### Security Considerations

| Item | Current State | Risk |
|------|---------------|------|
| JWT Secret | Env var with fallback | OK for Railway |
| Database Password | Env var with fallback | OK for Railway |
| Stripe Keys | appsettings.json (test keys) | OK - test keys only |
| Admin Password | Hardcoded in seeder | **CHANGE IN PROD** |
| CORS | Localhost only in config | OK - Railway overrides |

---

## 6. Recommended Fixes & Tests

### Critical Priority (P0)

| Issue | Recommendation |
|-------|----------------|
| No health check tests | Create `tests/ClimaSite.Api.Tests/Health/HealthCheckTests.cs` testing `/health` endpoint |
| No startup tests | Create `tests/ClimaSite.Api.Tests/Startup/StartupTests.cs` verifying DI container builds |
| No migration tests | Add test ensuring all migrations apply cleanly to fresh database |

### High Priority (P1)

| Issue | Recommendation |
|-------|----------------|
| No background jobs | Add `BackgroundService` infrastructure for email queues, cleanup tasks |
| No readiness probe | Add `/ready` endpoint checking warm dependencies |
| No Redis circuit breaker | Add Polly resilience to `CacheService` |
| Hardcoded admin password | Use environment variable for production admin password |

### Medium Priority (P2)

| Issue | Recommendation |
|-------|----------------|
| No rate limiting | Add `AspNetCoreRateLimit` package |
| No correlation IDs | Add `CorrelationId` middleware |
| No structured sink | Add Seq or Application Insights sink for Serilog |
| No connection pool config | Add `Maximum Pool Size` to connection string |

### Recommended Test Suite

```csharp
// tests/ClimaSite.Api.Tests/Infrastructure/HealthCheckTests.cs
public class HealthCheckTests : IntegrationTestBase
{
    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}

// tests/ClimaSite.Api.Tests/Infrastructure/StartupTests.cs
public class StartupTests : IntegrationTestBase
{
    [Fact]
    public void ApiStarts_WithValidConfiguration()
    {
        // Factory.CreateClient() throws if startup fails
        using var client = Factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public async Task SwaggerEndpoint_IsAccessible()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
    }
}

// tests/ClimaSite.Api.Tests/Infrastructure/SeederTests.cs
public class SeederTests : IntegrationTestBase
{
    [Fact]
    public async Task Seeder_CreatesRoles()
    {
        var roles = await DbContext.Roles.ToListAsync();
        Assert.Contains(roles, r => r.Name == "Admin");
        Assert.Contains(roles, r => r.Name == "Customer");
    }

    [Fact]
    public async Task Seeder_CreatesAdminUser()
    {
        var admin = await DbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@climasite.local");
        Assert.NotNull(admin);
    }

    [Fact]
    public async Task Seeder_IsIdempotent()
    {
        // Run seeder twice, should not throw
        var seeder = Scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        
        // Should still have one admin
        var adminCount = await DbContext.Users
            .CountAsync(u => u.Email == "admin@climasite.local");
        Assert.Equal(1, adminCount);
    }
}
```

---

## 7. Evidence & Notes

### Application Startup Flow (API)

```
Program.cs
├── Create WebApplicationBuilder
├── Configure Serilog (from appsettings)
├── ConfigureServices()
│   ├── AddApplicationServices() - MediatR, FluentValidation, Mapster
│   ├── AddInfrastructureServices() - EF Core, Identity, Redis, Storage
│   ├── JWT Authentication (env var > config)
│   ├── Authorization Policies
│   ├── CORS Configuration
│   ├── Controllers + JSON options
│   ├── Swagger/OpenAPI
│   ├── Health Checks (Postgres + Redis)
│   ├── Response Caching
│   └── Output Caching
├── Build app
├── SeedDatabaseAsync() - Migrations + seed data
└── ConfigurePipeline()
    ├── ExceptionHandling middleware
    ├── Developer exception page (dev only)
    ├── HSTS (prod only)
    ├── Swagger UI
    ├── Serilog request logging
    ├── HTTPS redirection
    ├── Response compression
    ├── Static files
    ├── CORS
    ├── Response/Output caching
    ├── Authentication
    ├── Authorization
    ├── Health checks (/health)
    └── Controllers
```

### Environment Variable Precedence

The system prioritizes environment variables for production deployment:

```csharp
// JWT Configuration
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? jwtSettings["Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

// Database Configuration
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = databaseUrl != null
    ? ConvertPostgresUrlToConnectionString(databaseUrl)
    : configuration.GetConnectionString("DefaultConnection");

// Redis Configuration
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
var redisConnection = redisUrl != null
    ? ConvertRedisUrlToConnectionString(redisUrl)
    : configuration.GetConnectionString("Redis");
```

### URL Conversion for Railway

PostgreSQL and Redis URLs are converted to connection string format:

```csharp
// PostgreSQL: postgresql://user:pass@host:port/db
// Converts to: Host=host;Port=port;Database=db;Username=user;Password=pass;SSL Mode=Require

// Redis: redis://user:pass@host:port
// Converts to: host:port,password=pass,ssl=true,abortConnect=false

// Railway internal connections disable SSL automatically:
var isInternal = host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase);
var sslMode = isInternal ? "Disable" : "Require";
```

### Docker Configuration Details

**API Dockerfile (Dockerfile.api)**:
- Multi-stage build (SDK -> Runtime)
- .NET 10 SDK for build, aspnet:10.0 for runtime
- Installs curl for health checks
- Exposes port 8080
- Health check: `curl -f http://localhost:8080/health`

**Web Dockerfile (Dockerfile.web)**:
- Multi-stage build (Node 22 -> nginx:alpine)
- Builds Angular production bundle
- Uses nginx with custom configuration template
- Runtime environment injection via entrypoint script

**docker-compose.yml Services**:
- **postgres**: PostgreSQL 16 Alpine on port 5433
- **redis**: Redis 7 Alpine on port 6379
- **minio**: MinIO for object storage on ports 9000/9001
- **pgadmin**: Optional database admin tool on port 5050

### Database Seeding Order

The seeder runs in a specific order to handle dependencies:

```
1. MigrateAsync() - Apply pending migrations
2. SeedRolesAsync() - Create Admin, Customer roles
3. SeedAdminUserAsync() - Create admin@climasite.local
4. SeedBrandsAsync() - Create 14 HVAC brands
5. SeedCategoriesAsync() - Create 4 parent + 8 child categories
6. SeedProductsAsync() - Create 15 products with variants/images
7. SeedPromotionsAsync() - Create 5 promotions linked to products
8. SeedProductRelationsAsync() - Create similar/accessory relations
```

### Health Check Implementation

```csharp
services.AddHealthChecks()
    .AddNpgSql(dbHealthConnection)    // Checks PostgreSQL connectivity
    .AddRedis(redisHealthConnection); // Checks Redis connectivity

app.MapHealthChecks("/health");       // Exposes at /health endpoint
```

### Test Infrastructure with Testcontainers

Integration tests use Testcontainers for isolated PostgreSQL:

```csharp
private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("climasite_test")
    .WithUsername("test_user")
    .WithPassword("test_password")
    .Build();
```

This ensures tests run against a real PostgreSQL instance without affecting development or production databases.

### Current Background Job Status

**No background job infrastructure is currently implemented.** Potential use cases:

- Email queue processing
- Order status update notifications
- Abandoned cart reminders
- Cache warm-up
- Database cleanup tasks
- Search index updates

Recommendation: Add `IHostedService` or Hangfire for background processing.

---

## Summary

The platform infrastructure is **production-ready for Railway deployment** with:

- Environment variable-based configuration
- Automatic URL conversion for managed services
- Health checks for container orchestration
- Multi-stage Docker builds
- Comprehensive database seeding

**Critical gaps to address:**

1. Add health check integration tests
2. Add startup failure scenario tests
3. Implement background job infrastructure
4. Add readiness probe for warm dependencies
5. Add Redis circuit breaker for resilience

**Test coverage priority:**

| Priority | Tests Needed |
|----------|--------------|
| P0 | Health check endpoint tests |
| P0 | Startup configuration tests |
| P1 | Seeder idempotency tests |
| P1 | Migration apply/rollback tests |
| P2 | Docker build CI verification |
