using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class OrderItemTests
{
    private static OrderItem CreateValid(int quantity = 2, decimal unitPrice = 100m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "AC Unit", "12000 BTU", "SKU-1", quantity, unitPrice);

    [Fact]
    public void Constructor_WithValidData_SetsAllFields()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var item = new OrderItem(orderId, productId, variantId,
            "AC Unit", "12000 BTU", "SKU-1", 3, 99.99m);

        item.OrderId.Should().Be(orderId);
        item.ProductId.Should().Be(productId);
        item.VariantId.Should().Be(variantId);
        item.ProductName.Should().Be("AC Unit");
        item.VariantName.Should().Be("12000 BTU");
        item.Sku.Should().Be("SKU-1");
        item.Quantity.Should().Be(3);
        item.UnitPrice.Should().Be(99.99m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetProductName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        var item = CreateValid();

        var act = () => item.SetProductName(name);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Product name cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetVariantName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        var item = CreateValid();

        var act = () => item.SetVariantName(name);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Variant name cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSku_WithEmptyValue_ThrowsArgumentException(string sku)
    {
        var item = CreateValid();

        var act = () => item.SetSku(sku);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*SKU cannot be empty*");
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
    public void SetUnitPrice_WithNegativeValue_ThrowsArgumentException()
    {
        var item = CreateValid();

        var act = () => item.SetUnitPrice(-1m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Unit price cannot be negative*");
    }

    [Fact]
    public void LineTotal_MultipliesQuantityByUnitPrice()
    {
        var item = CreateValid(quantity: 4, unitPrice: 25m);

        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public void LineTotal_WithZeroUnitPrice_IsZero()
    {
        var item = CreateValid(quantity: 3, unitPrice: 0m);

        item.LineTotal.Should().Be(0m);
    }
}
