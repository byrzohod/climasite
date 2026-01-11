using System.Text;
using ClimaSite.Application;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Infrastructure;
using ClimaSite.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ClimaSite.Api.Middleware;
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
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Seed the database
await SeedDatabaseAsync(app);

// Configure the HTTP request pipeline
ConfigurePipeline(app);

app.Run();

async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Application layer services (MediatR, FluentValidation, Mapster)
    services.AddApplicationServices();

    // Infrastructure layer services (EF Core, Identity, Redis, etc.)
    services.AddInfrastructureServices(configuration);

    // JWT Authentication - prefer env vars (Railway), fallback to config
    var jwtSettings = configuration.GetSection("JwtSettings");
    var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? jwtSettings["Secret"]
        ?? throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET or JwtSettings:Secret.");
    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
        ?? jwtSettings["Issuer"]
        ?? "https://localhost:5001";
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? jwtSettings["Audience"]
        ?? "https://localhost:4200";

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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClimaSite API V1");
        options.RoutePrefix = "swagger";
    });

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

    // Caching
    app.UseResponseCaching();
    app.UseOutputCache();

    // Auth
    app.UseAuthentication();
    app.UseAuthorization();

    // Health checks
    app.MapHealthChecks("/health");

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
