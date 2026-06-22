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
/// Integration coverage for the admin category CRUD surface
/// (<c>/api/admin/categories</c>). Real controller → MediatR handler → Postgres path via
/// Testcontainers, asserting the Admin-role authorization contract and the create/update/delete/
/// reorder/toggle behaviours.
/// </summary>
public class AdminCategoriesControllerTests : IntegrationTestBase
{
    private const string AdminSecret = "configured-secret";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminCategoriesControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Authorization

    [Fact]
    public async Task GetCategories_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategories_Returns403_WhenAuthenticatedButNotAdmin()
    {
        await AuthenticateAsync($"customer_{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync("/api/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCategory_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/categories", new
        {
            name = "Should Not Persist"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region List

    [Fact]
    public async Task GetCategories_AsAdmin_ReturnsSeededCategory()
    {
        // Create the admin client first so demo seeding runs against the clean DB before we add our
        // own category (seeding categories without products would otherwise break the seeder's invariant).
        using var adminClient = await CreateAdminClientAsync();

        var category = new Category("Admin Listed Category", "admin-listed-category", "A category");
        category.SetActive(true);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.GetAsync("/api/admin/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Admin Listed Category");
    }

    #endregion

    #region Create

    [Fact]
    public async Task CreateCategory_AsAdmin_PersistsCategory_AndReturnsCreated()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsJsonAsync("/api/admin/categories", new
        {
            name = "Heat Pumps Admin",
            description = "Admin-created category",
            isActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<CreatedCategoryResponse>(content, JsonOptions);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();

        var stored = DbContext.Categories.FirstOrDefault(c => c.Id == created.Id);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Heat Pumps Admin");
        stored.Slug.Should().Be("heat-pumps-admin");
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_Returns400_WhenNameMissing()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsJsonAsync("/api/admin/categories", new
        {
            name = "",
            description = "Invalid"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_Returns400_WhenParentDoesNotExist()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.PostAsJsonAsync("/api/admin/categories", new
        {
            name = "Orphan Category",
            parentId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateCategory_AsAdmin_PersistsChange()
    {
        using var adminClient = await CreateAdminClientAsync();

        var category = new Category("Original Category", "original-category-upd", null);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id}", new
        {
            id = category.Id,
            name = "Renamed Category"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = DbContext.Categories.First(c => c.Id == category.Id);
        DbContext.Entry(updated).Reload();
        updated.Name.Should().Be("Renamed Category");
    }

    [Fact]
    public async Task UpdateCategory_AsAdmin_Returns400_OnIdMismatch()
    {
        using var adminClient = await CreateAdminClientAsync();

        var category = new Category("Mismatch Category", "mismatch-category-upd", null);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.PutAsJsonAsync($"/api/admin/categories/{category.Id}", new
        {
            id = Guid.NewGuid(),
            name = "Will Not Apply"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("mismatch");
    }

    #endregion

    #region Status toggle

    [Fact]
    public async Task ToggleCategoryStatus_AsAdmin_DeactivatesCategory()
    {
        using var adminClient = await CreateAdminClientAsync();

        var category = new Category("Toggle Category", "toggle-category-status", null);
        category.SetActive(true);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.PatchAsync(
            $"/api/admin/categories/{category.Id}/status",
            JsonContent.Create(new { isActive = false }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = DbContext.Categories.First(c => c.Id == category.Id);
        DbContext.Entry(updated).Reload();
        updated.IsActive.Should().BeFalse();
    }

    #endregion

    #region Reorder

    [Fact]
    public async Task ReorderCategories_AsAdmin_UpdatesSortOrder()
    {
        using var adminClient = await CreateAdminClientAsync();

        var first = new Category("Reorder First", "reorder-first-cat", null);
        var second = new Category("Reorder Second", "reorder-second-cat", null);
        DbContext.Categories.AddRange(first, second);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.PatchAsync(
            "/api/admin/categories/reorder",
            JsonContent.Create(new
            {
                items = new[]
                {
                    new { id = first.Id, sortOrder = 5 },
                    new { id = second.Id, sortOrder = 1 }
                }
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedFirst = DbContext.Categories.First(c => c.Id == first.Id);
        DbContext.Entry(updatedFirst).Reload();
        updatedFirst.SortOrder.Should().Be(5);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteCategory_AsAdmin_RemovesEmptyCategory()
    {
        using var adminClient = await CreateAdminClientAsync();

        var category = new Category("Empty Category", "empty-category-del", null);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.DeleteAsync($"/api/admin/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = DbContext.Categories.FirstOrDefault(c => c.Id == category.Id);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_Returns404_ForUnknownId()
    {
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.DeleteAsync($"/api/admin/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_Returns400_WhenCategoryHasProducts()
    {
        using var adminClient = await CreateAdminClientAsync();

        var category = new Category("Has Products Category", "has-products-category-del", null);
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var product = new Product("ADM-CAT-PROD-001", "Category Product", "category-product-del", 500m);
        product.SetCategory(category.Id);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var response = await adminClient.DeleteAsync($"/api/admin/categories/{category.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("products");
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
    private record CreatedCategoryResponse(Guid Id);
}
