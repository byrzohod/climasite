using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using ClimaSite.Core.Interfaces;
using ClimaSite.Infrastructure.Data;
using ClimaSite.Infrastructure.Repositories;
using ClimaSite.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - prefer DATABASE_URL env var (Railway), fallback to config
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var connectionString = databaseUrl != null
            ? ConvertPostgresUrlToConnectionString(databaseUrl)
            : configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string not found. Set DATABASE_URL or ConnectionStrings:DefaultConnection.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(3);
            }));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Redis Caching - prefer REDIS_URL env var (Railway), fallback to config
        var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
        var redisConnection = redisUrl != null
            ? ConvertRedisUrlToConnectionString(redisUrl)
            : configuration.GetConnectionString("Redis")
              ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "ClimaSite_";
        });

        // Services
        services.AddTransient<IDateTime, DateTimeService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPaymentService, StripePaymentService>();

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Data Seeder
        services.AddScoped<DataSeeder>();

        return services;
    }

    /// <summary>
    /// Converts a PostgreSQL URL (postgresql://user:password@host:port/database) to Npgsql connection string
    /// </summary>
    private static string ConvertPostgresUrlToConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        // For Railway internal connections (.railway.internal), SSL is handled automatically
        var isInternal = host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase);
        var sslMode = isInternal ? "Disable" : "Require";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true";
    }

    /// <summary>
    /// Converts a Redis URL (redis://user:password@host:port) to StackExchange.Redis connection string
    /// </summary>
    private static string ConvertRedisUrlToConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var password = userInfo.Length > 1 ? userInfo[1] : (userInfo.Length > 0 ? userInfo[0] : string.Empty);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;

        // For Railway internal connections (.railway.internal), don't use SSL
        var useSsl = !host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(password))
        {
            return useSsl
                ? $"{host}:{port},ssl=true,abortConnect=false"
                : $"{host}:{port},abortConnect=false";
        }

        return useSsl
            ? $"{host}:{port},password={password},ssl=true,abortConnect=false"
            : $"{host}:{port},password={password},abortConnect=false";
    }
}
