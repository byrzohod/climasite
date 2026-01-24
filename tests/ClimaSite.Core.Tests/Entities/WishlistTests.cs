using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class WishlistTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithUserId_CreatesWishlist()
    {
        // Arrange & Act
        var wishlist = new Wishlist(_userId);

        // Assert
        wishlist.UserId.Should().Be(_userId);
        wishlist.Items.Should().BeEmpty();
        wishlist.IsPublic.Should().BeFalse();
        wishlist.ShareToken.Should().BeNull();
    }

    [Fact]
    public void Wishlist_CanAddItem()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);

        // Act
        var item = wishlist.AddItem(_productId, "Test note", 1);

        // Assert
        wishlist.Items.Should().HaveCount(1);
        item.ProductId.Should().Be(_productId);
        item.WishlistId.Should().Be(wishlist.Id);
        item.Note.Should().Be("Test note");
        item.Priority.Should().Be(1);
    }

    [Fact]
    public void Wishlist_CanRemoveItem()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        wishlist.AddItem(_productId);

        // Act
        wishlist.RemoveItem(_productId);

        // Assert
        wishlist.Items.Should().BeEmpty();
    }

    [Fact]
    public void Wishlist_RemoveItem_NonExisting_DoesNothing()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        wishlist.AddItem(_productId);

        // Act
        wishlist.RemoveItem(Guid.NewGuid());

        // Assert
        wishlist.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Wishlist_CannotAddDuplicate()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);

        // Act - Add the same product twice
        var firstItem = wishlist.AddItem(_productId, "First note");
        var secondItem = wishlist.AddItem(_productId, "Second note");

        // Assert - Should still have only one item, and return the existing one
        wishlist.Items.Should().HaveCount(1);
        secondItem.Should().BeSameAs(firstItem);
        secondItem.Note.Should().Be("First note"); // Original note preserved
    }

    [Fact]
    public void Wishlist_ClearRemovesAllItems()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        wishlist.AddItem(_productId);
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        // Act
        wishlist.Clear();

        // Assert
        wishlist.Items.Should().BeEmpty();
        wishlist.TotalItems.Should().Be(0);
    }

    [Fact]
    public void Wishlist_TotalItems_CalculatesCorrectly()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());

        // Assert
        wishlist.TotalItems.Should().Be(3);
    }

    [Fact]
    public void GetItem_ReturnsExistingItem()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        var addedItem = wishlist.AddItem(_productId);

        // Act
        var item = wishlist.GetItem(_productId);

        // Assert
        item.Should().NotBeNull();
        item.Should().BeSameAs(addedItem);
    }

    [Fact]
    public void GetItem_NonExisting_ReturnsNull()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);

        // Act
        var item = wishlist.GetItem(_productId);

        // Assert
        item.Should().BeNull();
    }

    [Fact]
    public void SetPublic_True_GeneratesShareToken()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);

        // Act
        wishlist.SetPublic(true);

        // Assert
        wishlist.IsPublic.Should().BeTrue();
        wishlist.ShareToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SetPublic_False_KeepsExistingToken()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        wishlist.SetPublic(true);
        var originalToken = wishlist.ShareToken;

        // Act
        wishlist.SetPublic(false);

        // Assert
        wishlist.IsPublic.Should().BeFalse();
        wishlist.ShareToken.Should().Be(originalToken);
    }

    [Fact]
    public void RegenerateShareToken_CreatesNewToken()
    {
        // Arrange
        var wishlist = new Wishlist(_userId);
        wishlist.SetPublic(true);
        var originalToken = wishlist.ShareToken;

        // Act
        wishlist.RegenerateShareToken();

        // Assert
        wishlist.ShareToken.Should().NotBeNullOrEmpty();
        wishlist.ShareToken.Should().NotBe(originalToken);
    }
}

public class WishlistItemTests
{
    private readonly Guid _wishlistId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [Fact]
    public void Constructor_CreatesItem()
    {
        // Arrange & Act
        var item = new WishlistItem(_wishlistId, _productId);

        // Assert
        item.WishlistId.Should().Be(_wishlistId);
        item.ProductId.Should().Be(_productId);
        item.Note.Should().BeNull();
        item.Priority.Should().Be(0);
        item.NotifyOnSale.Should().BeFalse();
    }

    [Fact]
    public void SetNote_UpdatesNote()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);

        // Act
        item.SetNote("My wishlist note");

        // Assert
        item.Note.Should().Be("My wishlist note");
    }

    [Fact]
    public void SetNote_TrimsWhitespace()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);

        // Act
        item.SetNote("  Note with spaces  ");

        // Assert
        item.Note.Should().Be("Note with spaces");
    }

    [Fact]
    public void SetNote_ExceedsMaxLength_ThrowsException()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);
        var longNote = new string('x', 501);

        // Act & Assert
        var act = () => item.SetNote(longNote);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*500 characters*");
    }

    [Fact]
    public void SetNote_Null_ClearsNote()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);
        item.SetNote("Original note");

        // Act
        item.SetNote(null);

        // Assert
        item.Note.Should().BeNull();
    }

    [Fact]
    public void SetPriority_UpdatesPriority()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);

        // Act
        item.SetPriority(5);

        // Assert
        item.Priority.Should().Be(5);
    }

    [Fact]
    public void SetPriority_Negative_ThrowsException()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);

        // Act & Assert
        var act = () => item.SetPriority(-1);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*negative*");
    }

    [Fact]
    public void SetPriceWhenAdded_UpdatesPrice()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);

        // Act
        item.SetPriceWhenAdded(599.99m);

        // Assert
        item.PriceWhenAdded.Should().Be(599.99m);
    }

    [Fact]
    public void SetNotifyOnSale_UpdatesFlag()
    {
        // Arrange
        var item = new WishlistItem(_wishlistId, _productId);

        // Act
        item.SetNotifyOnSale(true);

        // Assert
        item.NotifyOnSale.Should().BeTrue();
    }
}
