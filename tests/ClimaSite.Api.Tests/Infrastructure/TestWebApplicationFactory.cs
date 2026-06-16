using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Infrastructure.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace ClimaSite.Api.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Controllable fake payment service so integration tests exercise the money
    /// path without calling Stripe. Registered as a singleton below.
    /// </summary>
    public FakePaymentService PaymentService { get; } = new();

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
                ["Email:UsePlaceholder"] = "true"
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
