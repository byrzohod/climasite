using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class WishlistControllerTests : IntegrationTestBase
{
    public WishlistControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetWishlist_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/wishlist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddGetRemoveWishlistItem_PersistsForAuthenticatedUser()
    {
        await AuthenticateAsync($"wishlist-{Guid.NewGuid()}@example.com");
        var product = await CreateProductAsync("API Wishlist AC", "api-wishlist-ac");

        var addResponse = await Client.PostAsync($"/api/wishlist/items/{product.Id}", null);

        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var addedWishlist = await addResponse.Content.ReadFromJsonAsync<WishlistDto>();
        addedWishlist.Should().NotBeNull();
        addedWishlist!.Items.Should().ContainSingle();
        addedWishlist.Items[0].ProductId.Should().Be(product.Id);
        addedWishlist.Items[0].ProductName.Should().Be(product.Name);

        var getResponse = await Client.GetAsync("/api/wishlist");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var persistedWishlist = await getResponse.Content.ReadFromJsonAsync<WishlistDto>();
        persistedWishlist!.Items.Should().ContainSingle(i => i.ProductId == product.Id);

        var removeResponse = await Client.DeleteAsync($"/api/wishlist/items/{product.Id}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var removedWishlist = await removeResponse.Content.ReadFromJsonAsync<WishlistDto>();
        removedWishlist!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddWishlistItem_IsIdempotent_WhenRequestsAreConcurrent()
    {
        await AuthenticateAsync($"wishlist-concurrent-{Guid.NewGuid()}@example.com");
        var product = await CreateProductAsync("API Concurrent Wishlist AC", "api-concurrent-wishlist-ac");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 6)
                .Select(_ => Client.PostAsync($"/api/wishlist/items/{product.Id}", null)));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);

        var getResponse = await Client.GetAsync("/api/wishlist");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var persistedWishlist = await getResponse.Content.ReadFromJsonAsync<WishlistDto>();
        persistedWishlist!.Items.Should().ContainSingle(i => i.ProductId == product.Id);
    }

    [Fact]
    public async Task ClearWishlist_RemovesServerItems()
    {
        await AuthenticateAsync($"wishlist-clear-{Guid.NewGuid()}@example.com");
        var productOne = await CreateProductAsync("API Clear AC 1", "api-clear-ac-1");
        var productTwo = await CreateProductAsync("API Clear AC 2", "api-clear-ac-2");
        await Client.PostAsync($"/api/wishlist/items/{productOne.Id}", null);
        await Client.PostAsync($"/api/wishlist/items/{productTwo.Id}", null);

        var response = await Client.DeleteAsync("/api/wishlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wishlist = await response.Content.ReadFromJsonAsync<WishlistDto>();
        wishlist!.Items.Should().BeEmpty();

        var getResponse = await Client.GetAsync("/api/wishlist");
        var persistedWishlist = await getResponse.Content.ReadFromJsonAsync<WishlistDto>();
        persistedWishlist!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SharedWishlist_ReturnsPublicWishlistForAnonymousUser()
    {
        await AuthenticateAsync($"wishlist-share-{Guid.NewGuid()}@example.com");
        var product = await CreateProductAsync("API Shared AC", "api-shared-ac");
        await Client.PostAsync($"/api/wishlist/items/{product.Id}", null);

        var shareResponse = await Client.PutAsJsonAsync("/api/wishlist/share", new { isPublic = true });

        shareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sharedWishlist = await shareResponse.Content.ReadFromJsonAsync<WishlistDto>();
        sharedWishlist!.IsPublic.Should().BeTrue();
        sharedWishlist.ShareToken.Should().NotBeNullOrWhiteSpace();

        ClearAuthToken();
        var anonymousResponse = await Client.GetAsync($"/api/wishlist/shared/{sharedWishlist.ShareToken}");

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var anonymousWishlist = await anonymousResponse.Content.ReadFromJsonAsync<WishlistDto>();
        anonymousWishlist!.Items.Should().ContainSingle(i => i.ProductId == product.Id);

        // SEC-10: the anonymous shared response must not expose the owner's identity.
        // The property is either omitted entirely or serialized as null; never a real GUID.
        var rawJson = await anonymousResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(rawJson);
        if (document.RootElement.TryGetProperty("userId", out var userIdElement))
        {
            userIdElement.ValueKind.Should().Be(JsonValueKind.Null);
        }
        anonymousWishlist.UserId.Should().BeNull();
    }

    [Fact]
    public async Task SharedWishlist_TranslatesProductName_WhenLangIsBulgarian()
    {
        await AuthenticateAsync($"wishlist-shared-bg-{Guid.NewGuid()}@example.com");
        var product = await CreateProductAsync(
            "API Shared BG AC",
            "api-shared-bg-ac",
            bulgarianName: "Споделен климатик");
        await Client.PostAsync($"/api/wishlist/items/{product.Id}", null);

        var shareResponse = await Client.PutAsJsonAsync("/api/wishlist/share", new { isPublic = true });
        var sharedWishlist = await shareResponse.Content.ReadFromJsonAsync<WishlistDto>();

        ClearAuthToken();
        var anonymousResponse = await Client.GetAsync($"/api/wishlist/shared/{sharedWishlist!.ShareToken}?lang=bg");

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var anonymousWishlist = await anonymousResponse.Content.ReadFromJsonAsync<WishlistDto>();
        anonymousWishlist!.Items.Should().ContainSingle();
        anonymousWishlist.Items[0].ProductName.Should().Be("Споделен климатик");
    }

    [Fact]
    public async Task AddWishlistItem_TranslatesProductName_WhenLangIsBulgarian()
    {
        await AuthenticateAsync($"wishlist-bg-{Guid.NewGuid()}@example.com");
        var product = await CreateProductAsync(
            "API Wishlist BG AC",
            "api-wishlist-bg-ac",
            bulgarianName: "Климатик от списъка с желания");

        var getResponse = await Client.GetAsync("/api/wishlist?lang=bg");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await Client.PostAsync($"/api/wishlist/items/{product.Id}?lang=bg", null);

        var translated = await Client.GetAsync("/api/wishlist?lang=bg");
        var translatedWishlist = await translated.Content.ReadFromJsonAsync<WishlistDto>();
        translatedWishlist!.Items.Should().ContainSingle();
        translatedWishlist.Items[0].ProductName.Should().Be("Климатик от списъка с желания");

        var english = await Client.GetAsync("/api/wishlist");
        var englishWishlist = await english.Content.ReadFromJsonAsync<WishlistDto>();
        englishWishlist!.Items[0].ProductName.Should().Be(product.Name);
    }

    [Fact]
    public async Task AddWishlistItem_IsIdempotent_OnSequentialDuplicateAdds()
    {
        await AuthenticateAsync($"wishlist-dup-{Guid.NewGuid()}@example.com");
        var product = await CreateProductAsync("API Duplicate Wishlist AC", "api-duplicate-wishlist-ac");

        var first = await Client.PostAsync($"/api/wishlist/items/{product.Id}", null);
        var second = await Client.PostAsync($"/api/wishlist/items/{product.Id}", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondWishlist = await second.Content.ReadFromJsonAsync<WishlistDto>();
        secondWishlist!.Items.Should().ContainSingle(i => i.ProductId == product.Id);
    }

    [Fact]
    public async Task SharedWishlist_Returns404_WhenSharingDisabled()
    {
        await AuthenticateAsync($"wishlist-private-{Guid.NewGuid()}@example.com");
        var enableResponse = await Client.PutAsJsonAsync("/api/wishlist/share", new { isPublic = true });
        var enabledWishlist = await enableResponse.Content.ReadFromJsonAsync<WishlistDto>();

        var disableResponse = await Client.PutAsJsonAsync("/api/wishlist/share", new { isPublic = false });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        ClearAuthToken();
        var anonymousResponse = await Client.GetAsync($"/api/wishlist/shared/{enabledWishlist!.ShareToken}");

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegenerateShareToken_ReplacesToken()
    {
        await AuthenticateAsync($"wishlist-token-{Guid.NewGuid()}@example.com");
        var enableResponse = await Client.PutAsJsonAsync("/api/wishlist/share", new { isPublic = true });
        var enabledWishlist = await enableResponse.Content.ReadFromJsonAsync<WishlistDto>();

        var regenerateResponse = await Client.PostAsync("/api/wishlist/share-token", null);

        regenerateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regeneratedWishlist = await regenerateResponse.Content.ReadFromJsonAsync<WishlistDto>();
        regeneratedWishlist!.IsPublic.Should().BeTrue();
        regeneratedWishlist.ShareToken.Should().NotBeNullOrWhiteSpace();
        regeneratedWishlist.ShareToken.Should().NotBe(enabledWishlist!.ShareToken);
    }

    private async Task<Product> CreateProductAsync(string name, string slug, string? bulgarianName = null)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var product = new Product($"WISH-{unique}", name, $"{slug}-{unique}", 899.99m);
        product.SetBrand("CDL");
        product.SetShortDescription($"{name} short description");
        product.SetCompareAtPrice(999.99m);

        var image = new ProductImage(product.Id, $"/images/{slug}.webp");
        image.SetPrimary(true);
        product.Images.Add(image);

        var variant = new ProductVariant(product.Id, $"WISH-{unique}-STD", "Standard");
        variant.SetStockQuantity(12);
        product.Variants.Add(variant);

        if (bulgarianName is not null)
        {
            product.Translations.Add(new ProductTranslation(product.Id, "bg", bulgarianName));
        }

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return product;
    }
}
