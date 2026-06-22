using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductImageTests
{
    private static ProductImage CreateValid() =>
        new(Guid.NewGuid(), "https://cdn.test/img.jpg");

    [Fact]
    public void Constructor_WithValidData_SetsProductIdAndUrl()
    {
        var productId = Guid.NewGuid();

        var image = new ProductImage(productId, "https://cdn.test/a.jpg");

        image.ProductId.Should().Be(productId);
        image.Url.Should().Be("https://cdn.test/a.jpg");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetUrl_WithEmptyValue_ThrowsArgumentException(string url)
    {
        var image = CreateValid();

        var act = () => image.SetUrl(url);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Image URL cannot be empty*");
    }

    [Fact]
    public void SetUrl_ExceedingMaxLength_ThrowsArgumentException()
    {
        var image = CreateValid();

        var act = () => image.SetUrl(new string('a', 501));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot exceed 500 characters*");
    }

    [Fact]
    public void SetUrl_TrimsValue()
    {
        var image = CreateValid();

        image.SetUrl("  https://cdn.test/b.jpg  ");

        image.Url.Should().Be("https://cdn.test/b.jpg");
    }

    [Fact]
    public void SetAltText_ExceedingMaxLength_ThrowsArgumentException()
    {
        var image = CreateValid();

        var act = () => image.SetAltText(new string('a', 256));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Alt text cannot exceed 255 characters*");
    }

    [Fact]
    public void SetAltText_WithNull_IsAllowed()
    {
        var image = CreateValid();

        image.SetAltText(null);

        image.AltText.Should().BeNull();
    }

    [Fact]
    public void SetAltText_TrimsValue()
    {
        var image = CreateValid();

        image.SetAltText("  Front view  ");

        image.AltText.Should().Be("Front view");
    }

    [Fact]
    public void SetSortOrder_UpdatesValue()
    {
        var image = CreateValid();

        image.SetSortOrder(4);

        image.SortOrder.Should().Be(4);
    }

    [Fact]
    public void SetPrimary_UpdatesValue()
    {
        var image = CreateValid();

        image.SetPrimary(true);

        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SetVariant_SetsVariantId()
    {
        var image = CreateValid();
        var variantId = Guid.NewGuid();

        image.SetVariant(variantId);

        image.VariantId.Should().Be(variantId);
    }

    [Fact]
    public void SetVariant_WithNull_ClearsVariantId()
    {
        var image = CreateValid();
        image.SetVariant(Guid.NewGuid());

        image.SetVariant(null);

        image.VariantId.Should().BeNull();
    }
}
