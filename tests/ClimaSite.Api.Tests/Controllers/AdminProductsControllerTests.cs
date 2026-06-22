using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration coverage for the admin product CRUD surface
/// (<c>/api/admin/products</c>). Exercises the real controller → MediatR handler → Postgres path
/// via Testcontainers and asserts the Admin-role authorization contract (401 unauthenticated,
/// 403 for an authenticated non-admin).
/// </summary>
public class AdminProductsControllerTests : IntegrationTestBase
{
    private const string AdminSecret = "configured-secret";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminProductsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authorization

    [Fact]
    public async Task GetProducts_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_Returns403_WhenAuthenticatedButNotAdmin()
    {
        // A normal (Customer) user has a valid JWT but lacks the Admin role.
        await AuthenticateAsync($"customer_{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync("/api/admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/products", new
        {
            sku = "UNAUTH-001",
            name = "Should Not Persist",
            basePrice = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region List

    [Fact]
    public async Task GetProducts_AsAdmin_ReturnsSeededProduct()
    {
        var product = new Product("ADM-LIST-001", "Admin Listed AC", "admin-listed-ac", 1299.99m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/admin/products?pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Admin Listed AC");
        content.Should().Contain("ADM-LIST-001");
        content.Should().Contain("totalCount");
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreateProduct_AsAdmin_PersistsProduct_AndReturnsCreated()
    {
        using var adminClient = await CreateAdminClientAsync();

        var sku = $"ADM-CRT-{Guid.NewGuid():N}".Substring(0, 16);
        var response = await adminClient.PostAsJsonAsync("/api/admin/products", new
        {
            sku,
            name = "Created Heat Pump",
            shortDescription = "An admin-created product",
            basePrice = 1499.50m,
            stockQuantity = 25,
            isActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<CreatedProductResponse>(content, JsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.Slug.Should().Be("created-heat-pump");

        // The handler persisted a real product row.
        var stored = DbContext.Products.FirstOrDefault(p => p.Id == created.Id);
        stored.Should().NotBeNull();
        stored!.Sku.Should().Be(sku.ToUpperInvariant());
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_Returns400_WhenNameMissing()
    {
        using var adminClient = await CreateAdminClientAsync();

        // Missing required Name → FluentValidation rejects the command.
        var response = await adminClient.PostAsJsonAsync("/api/admin/products", new
        {
            sku = $"ADM-BAD-{Guid.NewGuid():N}".Substring(0, 16),
            name = "",
            basePrice = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_Returns400_OnDuplicateSku()
    {
        var existing = new Product("ADM-DUP-001", "Existing Product", "existing-product-dup", 500m);
        DbContext.Products.Add(existing);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsJsonAsync("/api/admin/products", new
        {
            sku = "ADM-DUP-001",
            name = "Conflicting Product",
            basePrice = 600m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateProduct_AsAdmin_PersistsChange()
    {
        var product = new Product("ADM-UPD-001", "Original Name", "original-name-upd", 700m);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PutAsJsonAsync($"/api/admin/products/{product.Id}", new
        {
            id = product.Id,
            name = "Updated Name",
            basePrice = 850m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = DbContext.Products.First(p => p.Id == product.Id);
        DbContext.Entry(updated).Reload();
        updated.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProduct_AsAdmin_Returns400_OnIdMismatch()
    {
        var product = new Product("ADM-UPD-002", "Mismatch Product", "mismatch-product-upd", 700m);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PutAsJsonAsync($"/api/admin/products/{product.Id}", new
        {
            id = Guid.NewGuid(), // body id != route id
            name = "Will Not Apply"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("mismatch");
    }

    #endregion

    #region Status / Featured toggles

    [Fact]
    public async Task ToggleProductStatus_AsAdmin_DeactivatesProduct()
    {
        var product = new Product("ADM-TGL-001", "Toggle Product", "toggle-product-status", 700m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PatchAsync(
            $"/api/admin/products/{product.Id}/status",
            JsonContent.Create(new { isActive = false }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = DbContext.Products.First(p => p.Id == product.Id);
        DbContext.Entry(updated).Reload();
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleProductFeatured_AsAdmin_MarksFeatured()
    {
        var product = new Product("ADM-FEA-001", "Featured Product", "featured-product-toggle", 700m);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PatchAsync(
            $"/api/admin/products/{product.Id}/featured",
            JsonContent.Create(new { isFeatured = true }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = DbContext.Products.First(p => p.Id == product.Id);
        DbContext.Entry(updated).Reload();
        updated.IsFeatured.Should().BeTrue();
    }

    #endregion

    #region Delete (soft delete)

    [Fact]
    public async Task DeleteProduct_AsAdmin_SoftDeactivatesProduct()
    {
        var product = new Product("ADM-DEL-001", "Delete Product", "delete-product-soft", 700m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.DeleteAsync($"/api/admin/products/{product.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // DeleteProductCommand is a soft delete (IsActive = false), the row remains.
        var stored = DbContext.Products.First(p => p.Id == product.Id);
        DbContext.Entry(stored).Reload();
        stored.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProduct_AsAdmin_Returns404_ForUnknownId()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.DeleteAsync($"/api/admin/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Detail

    [Fact]
    public async Task GetProductById_AsAdmin_Returns404_ForUnknownId()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.GetAsync($"/api/admin/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    /// <summary>
    /// Registers a fresh user, elevates them to Admin via the test endpoint (which needs the admin
    /// secret configured on a dedicated client), then logs in again so the JWT carries the Admin role.
    /// </summary>
    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = Factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["TestSettings:AdminSecret"] = AdminSecret
                    });
                });
            })
            .CreateClient();

        var email = $"admin_{Guid.NewGuid():N}@example.com";
        const string password = "AdminPass123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Admin",
            lastName = "User"
        });
        register.IsSuccessStatusCode.Should().BeTrue();
        var registered = await register.Content.ReadFromJsonAsync<RegisterPayload>();

        var elevate = await client.PostAsJsonAsync("/api/test/elevate-admin", new
        {
            userId = registered!.Id,
            testSecret = AdminSecret
        });
        elevate.IsSuccessStatusCode.Should().BeTrue();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.IsSuccessStatusCode.Should().BeTrue();
        var auth = await login.Content.ReadFromJsonAsync<AuthPayload>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private record RegisterPayload(Guid Id);
    private record AuthPayload(string AccessToken);
    private record CreatedProductResponse(Guid Id, string Slug);
}
