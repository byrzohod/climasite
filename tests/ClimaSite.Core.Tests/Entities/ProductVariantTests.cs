using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductVariantTests
{
    private static ProductVariant CreateValid() =>
        new(Guid.NewGuid(), "SKU-001", "12000 BTU");

    [Fact]
    public void Constructor_WithValidData_CreatesActiveVariant()
    {
        var productId = Guid.NewGuid();

        var variant = new ProductVariant(productId, "sku-1", "Variant");

        variant.ProductId.Should().Be(productId);
        variant.Sku.Should().Be("SKU-1");
        variant.Name.Should().Be("Variant");
        variant.IsActive.Should().BeTrue();
        variant.LowStockThreshold.Should().Be(5);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSku_WithEmptyValue_ThrowsArgumentException(string sku)
    {
        var variant = CreateValid();

        var act = () => variant.SetSku(sku);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*SKU cannot be empty*");
    }

    [Fact]
    public void SetSku_ExceedingMaxLength_ThrowsArgumentException()
    {
        var variant = CreateValid();

        var act = () => variant.SetSku(new string('a', 51));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 50 characters*");
    }

    [Fact]
    public void SetSku_NormalizesToUpperInvariant()
    {
        var variant = CreateValid();

        variant.SetSku("  abc-123  ");

        variant.Sku.Should().Be("ABC-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WithEmptyValue_ThrowsArgumentException(string name)
    {
        var variant = CreateValid();

        var act = () => variant.SetName(name);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*name cannot be empty*");
    }

    [Fact]
    public void SetName_ExceedingMaxLength_ThrowsArgumentException()
    {
        var variant = CreateValid();

        var act = () => variant.SetName(new string('a', 101));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 100 characters*");
    }

    [Fact]
    public void SetStockQuantity_WithNegativeValue_ThrowsArgumentException()
    {
        var variant = CreateValid();

        var act = () => variant.SetStockQuantity(-1);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Stock quantity cannot be negative*");
    }

    [Fact]
    public void AdjustStock_Increasing_AddsToStock()
    {
        var variant = CreateValid();
        variant.SetStockQuantity(10);

        variant.AdjustStock(5);

        variant.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void AdjustStock_Decreasing_SubtractsFromStock()
    {
        var variant = CreateValid();
        variant.SetStockQuantity(10);

        variant.AdjustStock(-4);

        variant.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void AdjustStock_BelowZero_ThrowsInvalidOperationException()
    {
        var variant = CreateValid();
        variant.SetStockQuantity(3);

        var act = () => variant.AdjustStock(-5);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot reduce stock below zero*");
    }

    [Fact]
    public void SetLowStockThreshold_WithNegativeValue_ThrowsArgumentException()
    {
        var variant = CreateValid();

        var act = () => variant.SetLowStockThreshold(-1);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Low stock threshold cannot be negative*");
    }

    [Fact]
    public void IsLowStock_WhenStockAtOrBelowThreshold_ReturnsTrue()
    {
        var variant = CreateValid();
        variant.SetLowStockThreshold(5);
        variant.SetStockQuantity(5);

        variant.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_WhenStockAboveThreshold_ReturnsFalse()
    {
        var variant = CreateValid();
        variant.SetLowStockThreshold(5);
        variant.SetStockQuantity(10);

        variant.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void InStock_WhenStockPositive_ReturnsTrue()
    {
        var variant = CreateValid();
        variant.SetStockQuantity(1);

        variant.InStock.Should().BeTrue();
    }

    [Fact]
    public void InStock_WhenStockZero_ReturnsFalse()
    {
        var variant = CreateValid();
        variant.SetStockQuantity(0);

        variant.InStock.Should().BeFalse();
    }

    [Fact]
    public void GetPrice_AddsPriceAdjustmentToBasePrice()
    {
        var variant = CreateValid();
        variant.SetPriceAdjustment(50m);

        variant.GetPrice(599m).Should().Be(649m);
    }

    [Fact]
    public void GetPrice_WithNegativeAdjustment_DiscountsBasePrice()
    {
        var variant = CreateValid();
        variant.SetPriceAdjustment(-100m);

        variant.GetPrice(599m).Should().Be(499m);
    }

    [Fact]
    public void SetAttributes_WithNull_DefaultsToEmptyDictionary()
    {
        var variant = CreateValid();

        variant.SetAttributes(null);

        variant.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void SetAttribute_AddsKeyValuePair()
    {
        var variant = CreateValid();

        variant.SetAttribute("color", "white");

        variant.Attributes.Should().ContainKey("color")
            .WhoseValue.Should().Be("white");
    }

    [Fact]
    public void SetAttribute_ExistingKey_OverwritesValue()
    {
        var variant = CreateValid();
        variant.SetAttribute("color", "white");

        variant.SetAttribute("color", "black");

        variant.Attributes["color"].Should().Be("black");
    }

    [Fact]
    public void SetActiveAndSortOrder_UpdateProperties()
    {
        var variant = CreateValid();

        variant.SetActive(false);
        variant.SetSortOrder(7);

        variant.IsActive.Should().BeFalse();
        variant.SortOrder.Should().Be(7);
    }
}
