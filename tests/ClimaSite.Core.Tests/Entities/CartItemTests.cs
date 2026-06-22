using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class CartItemTests
{
    private static CartItem CreateValid(int quantity = 2, decimal unitPrice = 50m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, unitPrice);

    [Fact]
    public void Constructor_WithValidData_SetsAllFields()
    {
        var cartId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var item = new CartItem(cartId, productId, variantId, 3, 199.99m);

        item.CartId.Should().Be(cartId);
        item.ProductId.Should().Be(productId);
        item.VariantId.Should().Be(variantId);
        item.Quantity.Should().Be(3);
        item.UnitPrice.Should().Be(199.99m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetQuantity_WithNonPositiveValue_ThrowsArgumentException(int quantity)
    {
        var item = CreateValid();

        var act = () => item.SetQuantity(quantity);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void SetQuantity_WithPositiveValue_UpdatesQuantity()
    {
        var item = CreateValid();

        item.SetQuantity(7);

        item.Quantity.Should().Be(7);
    }

    [Fact]
    public void SetUnitPrice_WithNegativeValue_ThrowsArgumentException()
    {
        var item = CreateValid();

        var act = () => item.SetUnitPrice(-0.01m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Unit price cannot be negative*");
    }

    [Fact]
    public void LineTotal_MultipliesQuantityByUnitPrice()
    {
        var item = CreateValid(quantity: 3, unitPrice: 33m);

        item.LineTotal.Should().Be(99m);
    }
}
