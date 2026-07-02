using ClimaSite.Api.Services;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.PostgreSql;

namespace ClimaSite.Api.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // SEC-05/B-011: a deterministic ≥32-char non-placeholder JWT signing secret for integration tests.
    private const string TestJwtSecret = "integration-test-jwt-signing-secret-32+chars-xyz";

    static TestWebApplicationFactory()
    {
        // appsettings.json no longer ships a JWT secret, and every WithWebHostBuilder sub-factory
        // (incl. the UseEnvironment("Staging") one in TestControllerTests) is NOT exempt from the
        // require-a-secret startup gate. In the minimal-hosting model JwtConfiguration.ResolveSecret
        // runs while the app is still building (before the test ConfigureAppConfiguration overrides
        // merge in), so an in-memory JwtSettings:Secret is invisible to it — the reliable lever is the
        // JWT_SECRET environment variable, which the resolver reads directly. Setting it process-wide
        // (once) makes the Testing default AND every sub-factory boot deterministically. Only set it
        // when unset so an ambient/CI-provided secret still wins.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_SECRET")))
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", TestJwtSecret);
        }
    }

    /// <summary>
    /// Controllable fake payment service so integration tests exercise the money
    /// path without calling Stripe. Registered as a singleton below.
    /// </summary>
    public FakePaymentService PaymentService { get; } = new();

    /// <summary>
    /// Deterministic fake Google token validator so integration tests exercise the Google sign-in
    /// flow without contacting Google. Registered as a singleton below.
    /// </summary>
    public FakeGoogleTokenValidator GoogleTokenValidator { get; } = new();

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("climasite_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    private readonly IContainer _redisContainer = new ContainerBuilder()
        .WithImage("redis:7-alpine")
        .WithPortBinding(6379, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "ping"))
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();
    public string RedisConnectionString => $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Disable the email outbox polling loop in integration tests; tests that exercise the
        // outbox drive IOutboxProcessor directly for determinism. Force placeholder email mode so
        // delivery never reaches a real SMTP server.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:Enabled"] = "false",
                ["Email:UsePlaceholder"] = "true",
                // SEC-05/B-011: mirrors the static-ctor JWT_SECRET env var so any runtime read of
                // JwtSettings:Secret is also covered. The startup gate (JwtConfiguration.ResolveSecret)
                // is satisfied by the env var, not this entry — see the static constructor above.
                ["JwtSettings:Secret"] = TestJwtSecret
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Remove any DbContext factory
            var factoryDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));

            if (factoryDescriptor != null)
                services.Remove(factoryDescriptor);

            // Add test database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(ConnectionString));

            // Replace the real Stripe payment service with a controllable fake so
            // integration tests never call out to Stripe.
            var paymentDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPaymentService));
            if (paymentDescriptor != null)
                services.Remove(paymentDescriptor);

            services.AddSingleton<IPaymentService>(PaymentService);

            // Replace the real Google token validator with a deterministic fake so integration tests
            // never call Google's JWKS endpoint.
            var googleDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IGoogleTokenValidator));
            if (googleDescriptor != null)
                services.Remove(googleDescriptor);

            services.AddSingleton<IGoogleTokenValidator>(GoogleTokenValidator);

            // INV-01 A2: every in-process test "guest" shares one loopback IP, so the per-IP guest-cookie mint
            // limiter (20/min) would exhaust across the suite and starve later guests of the signed cookie that
            // A2 checkout now requires — flaking guest create-intent/order tests. Replace it with an always-allow
            // limiter (each test guest is a distinct real client). Config can't do this: GuestSessionOptions is
            // bound while the host is still building, before the test config merges (same as the JWT secret).
            var mintLimiterDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IGuestSessionMintLimiter));
            if (mintLimiterDescriptor != null)
                services.Remove(mintLimiterDescriptor);

            services.AddSingleton<IGuestSessionMintLimiter>(new AlwaysAllowMintLimiter());

            // Replace production health checks so integration tests don't depend on local services.
            services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
            services.AddHealthChecks()
                .AddNpgSql(ConnectionString)
                .AddRedis(RedisConnectionString);
        });

        // Set environment to Testing to disable rate limiting
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _redisContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}

/// <summary>Always-allow guest-cookie mint limiter for integration tests (see the registration comment): the
/// per-IP production cap would otherwise flake guest checkout across the shared-loopback-IP suite.</summary>
internal sealed class AlwaysAllowMintLimiter : IGuestSessionMintLimiter
{
    public bool TryReserveMint(string clientIp) => true;
}
