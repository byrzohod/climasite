using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Cart.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

public class CartControllerTests : IntegrationTestBase
{
    public CartControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region GetCart Tests

    [Fact]
    public async Task GetCart_ReturnsEmptyCart_WhenNoItems()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();

        // Act
        var response = await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().BeEmpty();
        cart.ItemCount.Should().Be(0);
        cart.Subtotal.Should().Be(0);
        cart.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetCart_ReturnsCartWithItems_WhenItemsExist()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync();

        // Add item to cart first
        var addResponse = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].ProductId.Should().Be(product.Id);
        cart.Items[0].Quantity.Should().Be(2);
        cart.ItemCount.Should().Be(2);
    }

    #endregion

    #region AddToCart Tests

    [Fact]
    public async Task AddToCart_AddsItem_WhenProductExists()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 1,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].ProductId.Should().Be(product.Id);
        cart.Items[0].VariantId.Should().Be(variant.Id);
        cart.Items[0].Quantity.Should().Be(1);
        cart.Items[0].ProductName.Should().Be(product.Name);
    }

    [Fact]
    public async Task AddToCart_IncrementsQuantity_WhenItemAlreadyInCart()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(stockQuantity: 10);

        // Add item first time
        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });

        // Act - Add same item again
        var response = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 3,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(5); // 2 + 3
    }

    [Fact]
    public async Task AddToCart_Returns400_WhenProductNotFound()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var nonExistentProductId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = nonExistentProductId,
            quantity = 1,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task AddToCart_Returns400_WhenQuantityExceedsStock()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(stockQuantity: 5);

        // Act
        var response = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 10, // More than available stock
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("available");
    }

    [Fact]
    public async Task AddToCart_Returns400_WhenProductNotActive()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(isActive: false);

        // Act
        var response = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 1,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    #endregion

    #region UpdateQuantity Tests

    [Fact]
    public async Task UpdateQuantity_UpdatesItem_WhenValid()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(stockQuantity: 10);

        // Add item to cart first
        var addResponse = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDto>();
        var itemId = addedCart!.Items[0].Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/cart/items/{itemId}", new
        {
            quantity = 5,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateQuantity_RemovesItem_WhenQuantityIsZero()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync();

        // Add item to cart first
        var addResponse = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDto>();
        var itemId = addedCart!.Items[0].Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/cart/items/{itemId}", new
        {
            quantity = 0,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateQuantity_Returns400_WhenQuantityExceedsStock()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(stockQuantity: 5);

        // Add item to cart first
        var addResponse = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDto>();
        var itemId = addedCart!.Items[0].Id;

        // Act
        var response = await Client.PutAsJsonAsync($"/api/cart/items/{itemId}", new
        {
            quantity = 10, // More than available stock
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("available");
    }

    [Fact]
    public async Task UpdateQuantity_Returns400_WhenItemNotFound()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var nonExistentItemId = Guid.NewGuid();

        // Create a cart first
        await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/cart/items/{nonExistentItemId}", new
        {
            quantity = 5,
            guestSessionId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public async Task RemoveItem_RemovesItem_WhenExists()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync();

        // Add item to cart first
        var addResponse = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });
        var addedCart = await addResponse.Content.ReadFromJsonAsync<CartDto>();
        var itemId = addedCart!.Items[0].Id;

        // Act
        var response = await Client.DeleteAsync($"/api/cart/items/{itemId}?guestSessionId={guestSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify cart is now empty
        var getResponse = await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");
        var cart = await getResponse.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItem_Returns400_WhenItemNotFound()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var nonExistentItemId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/cart/items/{nonExistentItemId}?guestSessionId={guestSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ClearCart Tests

    [Fact]
    public async Task ClearCart_RemovesAllItems()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product1, variant1) = await CreateTestProductWithVariantAsync(sku: "TEST-001");
        var (product2, variant2) = await CreateTestProductWithVariantAsync(sku: "TEST-002");

        // Add multiple items to cart
        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product1.Id,
            variantId = variant1.Id,
            quantity = 2,
            guestSessionId
        });
        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product2.Id,
            variantId = variant2.Id,
            quantity = 3,
            guestSessionId
        });

        // Verify cart has items
        var beforeResponse = await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");
        var beforeCart = await beforeResponse.Content.ReadFromJsonAsync<CartDto>();
        beforeCart!.Items.Should().HaveCount(2);

        // Act
        var response = await Client.DeleteAsync($"/api/cart?guestSessionId={guestSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify cart is now empty
        var afterResponse = await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");
        var afterCart = await afterResponse.Content.ReadFromJsonAsync<CartDto>();
        afterCart!.Items.Should().BeEmpty();
    }

    #endregion

    #region MergeCart Tests

    [Fact]
    public async Task MergeCart_MergesGuestCart_WhenLoggedIn()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(stockQuantity: 10);

        // Add item to guest cart
        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 3,
            guestSessionId
        });

        // Authenticate user
        await AuthenticateAsync($"merge-test-{Guid.NewGuid()}@test.com");

        // Act
        var response = await Client.PostAsync($"/api/cart/merge?guestSessionId={guestSessionId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(3);
        cart.UserId.Should().NotBeNull();
    }

    [Fact]
    public async Task MergeCart_CombinesQuantities_WhenUserHasExistingItems()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        var (product, variant) = await CreateTestProductWithVariantAsync(stockQuantity: 20);

        // Add item to guest cart
        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 3,
            guestSessionId
        });

        // Authenticate and add same item to user cart
        await AuthenticateAsync($"merge-existing-{Guid.NewGuid()}@test.com");
        await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2
        });

        // Act
        var response = await Client.PostAsync($"/api/cart/merge?guestSessionId={guestSessionId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(5); // 2 + 3
    }

    [Fact]
    public async Task MergeCart_ReturnsUnauthorized_WhenNotLoggedIn()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString();
        ClearAuthToken();

        // Act
        var response = await Client.PostAsync($"/api/cart/merge?guestSessionId={guestSessionId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MergeCart_ReturnsEmptyCart_WhenNoGuestCart()
    {
        // Arrange
        var guestSessionId = Guid.NewGuid().ToString(); // Non-existent guest cart
        await AuthenticateAsync($"merge-empty-{Guid.NewGuid()}@test.com");

        // Act
        var response = await Client.PostAsync($"/api/cart/merge?guestSessionId={guestSessionId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart.Should().NotBeNull();
        cart!.Items.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private async Task<(Product product, ProductVariant variant)> CreateTestProductWithVariantAsync(
        string sku = "TEST-SKU",
        int stockQuantity = 100,
        bool isActive = true)
    {
        var uniqueSku = $"{sku}-{Guid.NewGuid():N}".Substring(0, 40);
        var product = new Product(uniqueSku, $"Test Product {uniqueSku}", $"test-product-{uniqueSku}", 99.99m);
        product.SetShortDescription("A test product for cart testing");
        product.SetActive(isActive);

        var variant = new ProductVariant(product.Id, $"{uniqueSku}-VAR", "Default Variant");
        variant.SetStockQuantity(stockQuantity);
        variant.SetActive(true);

        product.Variants.Add(variant);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return (product, variant);
    }

    #endregion
}
