using System.Text;
using System.Threading.RateLimiting;
using ClimaSite.Api.BackgroundServices;
using ClimaSite.Api.Configuration;
using ClimaSite.Api.Middleware;
using ClimaSite.Api.RateLimiting;
using ClimaSite.Api.Services;
using ClimaSite.Application;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Infrastructure;
using ClimaSite.Infrastructure.Data;
using ClimaSite.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

var app = builder.Build();

// Seed the database
await SeedDatabaseAsync(app);

// Configure the HTTP request pipeline
ConfigurePipeline(app);

// Flush buffered logs on graceful shutdown (OPS-05).
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
{
    // SEC-07: in Production, fail fast at startup if Stripe is missing/placeholder (no dummy keys are
    // committed any more, so a by-the-book deploy that forgets the real keys must not boot silently).
    StripeConfiguration.ValidateProductionConfiguration(configuration, environment);

    // Application layer services (MediatR, FluentValidation, Mapster)
    services.AddApplicationServices();

    // Infrastructure layer services (EF Core, Identity, Redis, etc.)
    services.AddInfrastructureServices(configuration);

    // Email outbox background worker (ARCH-05). Self-guards on Outbox:Enabled, so it is safe to
    // register unconditionally; integration tests set Enabled=false and drive the processor directly.
    services.AddHostedService<EmailOutboxBackgroundService>();

    // JWT Authentication (SEC-05 / B-011) — resolve + validate the signing secret ONCE here, then bind a
    // single JwtOptions used by BOTH the bearer validation below AND TokenService issuance, so the two
    // provably share one secret/issuer/audience. ResolveSecret fail-fasts in every non-Development/Testing
    // environment without a real JWT_SECRET and rejects the committed placeholder in all environments.
    var jwtSettings = configuration.GetSection("JwtSettings");
    var jwtOptions = new JwtOptions
    {
        Secret = JwtConfiguration.ResolveSecret(configuration, environment),
        Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? jwtSettings["Issuer"]
            ?? "https://localhost:5001",
        Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? jwtSettings["Audience"]
            ?? "https://localhost:4200",
        AccessTokenExpirationMinutes = int.TryParse(jwtSettings["AccessTokenExpirationMinutes"], out var accessTokenMinutes)
            ? accessTokenMinutes
            : 15
    };

    // Single source of truth for issuance: TokenService consumes IOptions<JwtOptions>.
    services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtOptions));

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
    });

    // Current user service
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Guest session (INV-01 Wave A0, shipped DARK): the signed guest cookie is minted + validated + published
    // on IGuestSessionAccessor, but is NOT yet authoritative for cart-keying (that flip lands in Wave A).
    // Derive the cookie signing key from the SAME resolved JWT secret so it inherits that secret's production
    // fail-fast guarantee (JwtConfiguration.ResolveSecret) without introducing a new required secret. The
    // accessor is exposed both as its concrete type (the middleware sets it) and as the read-only interface
    // (Wave A consumes it), backed by ONE scoped instance per request.
    services.AddSingleton<IGuestSessionTokenService>(new GuestSessionTokenService(jwtOptions.Secret));
    services.AddSingleton<IGuestSessionMintLimiter, GuestSessionMintLimiter>();
    services.AddScoped<GuestSessionAccessor>();
    services.AddScoped<IGuestSessionAccessor>(sp => sp.GetRequiredService<GuestSessionAccessor>());
    // A1: resolves the trusted guest id (migrating a returning guest's legacy cart onto the cookie id) for the
    // cart, payments and orders controllers.
    services.AddScoped<IGuestCartIdentity, GuestCartIdentity>();

    var guestSessionOptions = new GuestSessionOptions();
    configuration.GetSection(GuestSessionOptions.SectionName).Bind(guestSessionOptions);
    services.AddSingleton(guestSessionOptions);

    // CORS
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy.WithOrigins(
                    configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // Controllers
    // TODO: API-012 - Consider implementing RFC 7807 ProblemDetails for standardized error responses.
    // Currently, error responses use { message: "..." } format which is consistent but not standards-compliant.
    // Migration to ProblemDetails would improve API documentation and client error handling.
    // See: https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // API Documentation
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ClimaSite API",
            Version = "v1",
            Description = "HVAC E-Commerce Platform API"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    });

    // Health checks - use same env vars as infrastructure with URL conversion
    // Local functions for URL conversion
    string ConvertPostgresUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var isInternal = host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase);
        var sslMode = isInternal ? "Disable" : "Require";
        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    string ConvertRedisUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var password = userInfo.Length > 1 ? userInfo[1] : (userInfo.Length > 0 ? userInfo[0] : string.Empty);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;
        // For Railway internal connections, don't use SSL
        var useSsl = !host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(password))
            return useSsl ? $"{host}:{port},ssl=true,abortConnect=false" : $"{host}:{port},abortConnect=false";
        return useSsl ? $"{host}:{port},password={password},ssl=true,abortConnect=false" : $"{host}:{port},password={password},abortConnect=false";
    }

    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    var dbHealthConnection = dbUrl != null
        ? ConvertPostgresUrl(dbUrl)
        : configuration.GetConnectionString("DefaultConnection")
          ?? throw new InvalidOperationException("Database connection not configured for health checks.");
    var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
    var redisHealthConnection = redisUrl != null
        ? ConvertRedisUrl(redisUrl)
        : configuration.GetConnectionString("Redis")
          ?? "localhost:6379";
    services.AddHealthChecks()
        .AddNpgSql(dbHealthConnection)
        .AddRedis(redisHealthConnection);

    // Response caching
    services.AddResponseCaching();

    // In-memory cache — backs the host-independent sitemap enumeration (B-044). Idempotent.
    services.AddMemoryCache();

    // Forwarded headers — populate the real client IP/scheme from the nginx reverse proxy.
    // This MUST be applied (in the pipeline) before rate limiting and HTTPS redirection so the
    // rate limiter partitions per real client rather than per proxy. (SEC-03 / SR-06)
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // The reverse proxy runs at a dynamic address on the PaaS, so the immediate proxy
        // cannot be pinned by IP. Clearing both lists lets the middleware honor the
        // proxy-supplied headers (nginx appends the real client to X-Forwarded-For and the
        // default ForwardLimit of 1 reads that last hop). When the deploy topology is known,
        // restrict these to the proxy network to remove the spoofing surface (OPS-08 follow-up).
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Rate limiting
    services.AddRateLimiter(options =>
    {
        // Return 429 Too Many Requests with proper message
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            var response = new { message = "Too many requests. Please try again later." };
            await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        };

        // Global rate limiter: 100 requests per minute per IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Auth policy: 10 requests per minute per IP (prevent brute force)
        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Strict policy for sensitive ANONYMOUS operations (contact form, installation lead, guest
        // create-payment-intent): 5 requests per minute per IP. Kept IP-partitioned on purpose — these
        // endpoints accept anonymous callers, so a user key here would let anyone widen their budget by
        // presenting any bearer token / farming accounts.
        options.AddPolicy("strict", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Strict policy for AUTHENTICATED sensitive operations (Q&A votes): 5 requests per minute,
        // partitioned per user (falling back to client IP for the anonymous requests that [Authorize] then
        // rejects) so signed-in users behind one NAT/CGNAT IP each get an independent bucket instead of
        // sharing (and exhausting) one. Only apply this to [Authorize] endpoints. (B-039 follow-up)
        options.AddPolicy("strict-user", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: RateLimitPartitioning.UserOrIpKey(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));
    });

    // Output caching
    services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
        options.AddPolicy("Products", builder => builder.Expire(TimeSpan.FromMinutes(10)).Tag("products"));
        options.AddPolicy("Categories", builder => builder.Expire(TimeSpan.FromHours(1)).Tag("categories"));
    });

    // Compression
    services.AddResponseCompression();
}

void ConfigurePipeline(WebApplication app)
{
    // Forwarded headers FIRST so the real client IP/scheme (from the nginx reverse proxy) is
    // used by request logging, HTTPS redirection, and especially the rate limiter below.
    // Without this, every request behind the proxy shares one IP and one rate-limit bucket. (SEC-03)
    app.UseForwardedHeaders();

    // Defensive response security headers on every response (SEC-08). Early + unconditional so they
    // apply to error responses too and in all environments.
    app.UseSecurityHeaders();

    // Correlation IDs (OPS-05): assign/echo X-Correlation-Id + push it to the Serilog LogContext.
    // Before the exception handler + request logging so both carry the id.
    app.UseCorrelationId();

    // Custom exception handling middleware (handles NotFoundException, ValidationException, etc.)
    app.UseExceptionHandling();

    // Built-in exception handling
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    // Swagger — Development only (SEC-06). The API schema + UI must not be exposed in Production
    // (or Staging/Testing); it's a local dev tool. The Testing integration env is non-Development,
    // so /swagger 404s there — which is what the SwaggerGatingTests assert.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClimaSite API V1");
            options.RoutePrefix = "swagger";
        });
    }

    // Serilog request logging
    app.UseSerilogRequestLogging();

    // Security
    app.UseHttpsRedirection();

    // Compression
    app.UseResponseCompression();

    // Static files (if any)
    app.UseStaticFiles();

    // CORS
    app.UseCors("AllowAngular");

    // Guest session (INV-01 Wave A0, shipped DARK): validate/mint the signed httpOnly cs_guest cookie and
    // publish the verified id on IGuestSessionAccessor for Wave A to consume — it is NOT read for cart-keying
    // yet. After CORS so the credentialed cross-origin response is negotiated, and before authentication since
    // the guest identity is independent of the bearer principal.
    app.UseGuestSession();

    // Authentication runs BEFORE the rate limiter so the per-user "strict-user" policy can read the
    // authenticated principal when partitioning; authorization stays AFTER the limiter so unauthenticated
    // requests are still rate-limited (by IP) before being rejected. (B-039 follow-up)
    app.UseAuthentication();

    // Rate limiting. In the deployed envs (Production/Staging) it is ALWAYS on and cannot be disabled by
    // config (fail-closed). In local/dev/test it defaults on except the Testing integration env (so tests
    // aren't throttled) and is config-overridable, so a targeted integration test can re-enable it
    // (RateLimiting:Enabled=true) to drive the real limiter pipeline. (B-039 follow-up)
    //
    // Accepted tradeoff: UseAuthentication runs before UseRateLimiter (above) so the per-user "strict-user"
    // policy can key on the authenticated principal. A bogus-token flood therefore incurs a cheap JWT
    // signature check before the global IP limiter sheds it — but the global 100/min/IP limiter still runs
    // before the controllers, so the expensive downstream (handlers/DB) stays shielded; only the microsecond
    // HMAC verify is unbounded. Documented rather than split into a separate pre-auth IP shield.
    var isDeployedEnv = app.Environment.IsProduction() || app.Environment.IsStaging();
    var rateLimitingEnabled = isDeployedEnv
        || app.Configuration.GetValue("RateLimiting:Enabled", !app.Environment.IsEnvironment("Testing"));
    if (rateLimitingEnabled)
    {
        app.UseRateLimiter();
    }

    // Caching
    app.UseResponseCaching();
    app.UseOutputCache();

    // Authorization
    app.UseAuthorization();

    // Health checks
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => true // Run all checks for readiness
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // No checks for liveness - just confirms app is running
    });

    // Controllers
    app.MapControllers();
}

// CurrentUserService implementation
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userId != null && Guid.TryParse(userId, out var guid) ? guid : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}
