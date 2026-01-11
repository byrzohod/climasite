using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class CartTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _variantId = Guid.NewGuid();
    private const string SessionId = "test-session-123";

    [Fact]
    public void Constructor_WithUserId_CreatesCart()
    {
        // Arrange & Act
        var cart = new Cart(_userId, null);

        // Assert
        cart.UserId.Should().Be(_userId);
        cart.SessionId.Should().BeNull();
        cart.IsGuestCart.Should().BeFalse();
        cart.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithSessionId_CreatesGuestCart()
    {
        // Arrange & Act
        var cart = new Cart(null, SessionId);

        // Assert
        cart.UserId.Should().BeNull();
        cart.SessionId.Should().Be(SessionId);
        cart.IsGuestCart.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNoIdentifier_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new Cart(null, null);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*userId or sessionId must be provided*");
    }

    [Fact]
    public void Constructor_WithEmptySessionId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new Cart(null, "");
        act.Should().Throw<ArgumentException>()
           .WithMessage("*userId or sessionId must be provided*");
    }

    [Fact]
    public void SetUser_UpdatesUserIdAndClearsSessionId()
    {
        // Arrange
        var cart = new Cart(null, SessionId);

        // Act
        cart.SetUser(_userId);

        // Assert
        cart.UserId.Should().Be(_userId);
        cart.SessionId.Should().BeNull();
        cart.IsGuestCart.Should().BeFalse();
    }

    [Fact]
    public void ExtendExpiration_ExtendsExpirationDate()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        var originalExpiration = cart.ExpiresAt;

        // Act
        cart.ExtendExpiration(14);

        // Assert
        cart.ExpiresAt.Should().BeAfter(originalExpiration);
    }

    [Fact]
    public void AddItem_AddsNewItem()
    {
        // Arrange
        var cart = new Cart(_userId, null);

        // Act
        var item = cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Assert
        cart.Items.Should().HaveCount(1);
        item.ProductId.Should().Be(_productId);
        item.VariantId.Should().Be(_variantId);
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(999.99m);
    }

    [Fact]
    public void AddItem_ExistingVariant_IncrementsQuantity()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        cart.AddItem(_productId, _variantId, 3, 999.99m);

        // Assert
        cart.Items.Should().HaveCount(1);
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void RemoveItem_RemovesExistingItem()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        cart.RemoveItem(_variantId);

        // Assert
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistingItem_DoesNothing()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        cart.RemoveItem(Guid.NewGuid());

        // Assert
        cart.Items.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateItemQuantity_UpdatesQuantity()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        cart.UpdateItemQuantity(_variantId, 5);

        // Assert
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateItemQuantity_WithZero_RemovesItem()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        cart.UpdateItemQuantity(_variantId, 0);

        // Assert
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void UpdateItemQuantity_WithNegative_RemovesItem()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        cart.UpdateItemQuantity(_variantId, -1);

        // Assert
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);
        cart.AddItem(_productId, Guid.NewGuid(), 1, 499.99m);

        // Act
        cart.Clear();

        // Assert
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void TotalItems_CalculatesCorrectly()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);
        cart.AddItem(_productId, Guid.NewGuid(), 3, 499.99m);

        // Assert
        cart.TotalItems.Should().Be(5);
    }

    [Fact]
    public void Subtotal_CalculatesCorrectly()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 100m);
        cart.AddItem(_productId, Guid.NewGuid(), 3, 50m);

        // Assert
        cart.Subtotal.Should().Be(350m); // (2 * 100) + (3 * 50)
    }

    [Fact]
    public void GetItem_ReturnsExistingItem()
    {
        // Arrange
        var cart = new Cart(_userId, null);
        cart.AddItem(_productId, _variantId, 2, 999.99m);

        // Act
        var item = cart.GetItem(_variantId);

        // Assert
        item.Should().NotBeNull();
        item!.VariantId.Should().Be(_variantId);
    }

    [Fact]
    public void GetItem_NonExisting_ReturnsNull()
    {
        // Arrange
        var cart = new Cart(_userId, null);

        // Act
        var item = cart.GetItem(_variantId);

        // Assert
        item.Should().BeNull();
    }
}
