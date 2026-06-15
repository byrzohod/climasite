using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace ClimaSite.Api.Tests;

/// <summary>
/// SEC-01 regression tests: the <see cref="DataSeeder"/> must never seed the well-known default
/// admin (<c>admin@climasite.local</c> / <c>Admin123!</c>) or the demo catalog outside
/// Development/Testing, and must bootstrap the production admin only from environment variables.
/// </summary>
public class DataSeederTests
{
    private const string DefaultAdminEmail = "admin@climasite.local";
    private const string DefaultAdminPassword = "Admin123!";

    private static PostgreSqlContainer NewPostgres() =>
        new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    private static ServiceProvider BuildProvider(
        string connectionString, string environment, IDictionary<string, string?> config)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddInMemoryCollection(config).Build());
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment { EnvironmentName = environment });
        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<DataSeeder>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Production_skips_demo_catalog_and_bootstraps_admin_from_environment()
    {
        await using var postgres = NewPostgres();
        await postgres.StartAsync();

        var config = new Dictionary<string, string?>
        {
            ["ADMIN_EMAIL"] = "owner@example.com",
            ["ADMIN_INITIAL_PASSWORD"] = "Boot$trapPass123"
        };

        await using var provider = BuildProvider(postgres.GetConnectionString(), "Production", config);
        using var scope = provider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        await seeder.SeedAsync();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // No demo catalog in production.
        (await db.Products.CountAsync()).Should().Be(0);
        (await db.Categories.CountAsync()).Should().Be(0);
        (await db.Brands.CountAsync()).Should().Be(0);

        // The well-known default admin is never created.
        (await userManager.FindByEmailAsync(DefaultAdminEmail)).Should().BeNull();

        // Roles are still seeded (always-on).
        (await roleManager.RoleExistsAsync("Admin")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("Customer")).Should().BeTrue();

        // The bootstrap admin from the environment exists and is an Admin.
        var bootstrapAdmin = await userManager.FindByEmailAsync("owner@example.com");
        bootstrapAdmin.Should().NotBeNull();
        (await userManager.IsInRoleAsync(bootstrapAdmin!, "Admin")).Should().BeTrue();

        // The published default password must not authenticate the bootstrap admin.
        (await userManager.CheckPasswordAsync(bootstrapAdmin!, DefaultAdminPassword)).Should().BeFalse();
    }

    [Fact]
    public async Task Production_without_bootstrap_credentials_and_no_admin_fails_fast()
    {
        await using var postgres = NewPostgres();
        await postgres.StartAsync();

        await using var provider = BuildProvider(
            postgres.GetConnectionString(), "Production", new Dictionary<string, string?>());
        using var scope = provider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        (await userManager.FindByEmailAsync(DefaultAdminEmail)).Should().BeNull();
    }

    [Fact]
    public async Task Production_without_bootstrap_credentials_but_existing_admin_does_not_fail()
    {
        await using var postgres = NewPostgres();
        await postgres.StartAsync();

        await using var provider = BuildProvider(
            postgres.GetConnectionString(), "Production", new Dictionary<string, string?>());
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Admin", NormalizedName = "ADMIN" });

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existingAdmin = new ApplicationUser
        {
            UserName = "preexisting@example.com",
            Email = "preexisting@example.com",
            EmailConfirmed = true,
            FirstName = "Pre",
            LastName = "Existing"
        };
        (await userManager.CreateAsync(existingAdmin, "Pre$tinePass123")).Succeeded.Should().BeTrue();
        await userManager.AddToRoleAsync(existingAdmin, "Admin");

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        var seed = async () => await seeder.SeedAsync();
        await seed.Should().NotThrowAsync();

        (await userManager.FindByEmailAsync(DefaultAdminEmail)).Should().BeNull();
        (await db.Products.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Development_seeds_the_default_admin_and_demo_catalog()
    {
        await using var postgres = NewPostgres();
        await postgres.StartAsync();

        await using var provider = BuildProvider(
            postgres.GetConnectionString(), "Development", new Dictionary<string, string?>());
        using var scope = provider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

        await seeder.SeedAsync();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var defaultAdmin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        defaultAdmin.Should().NotBeNull();
        (await userManager.IsInRoleAsync(defaultAdmin!, "Admin")).Should().BeTrue();
        (await db.Products.CountAsync()).Should().BeGreaterThan(0);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "ClimaSite.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
