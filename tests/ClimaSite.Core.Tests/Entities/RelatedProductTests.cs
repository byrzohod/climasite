using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class RelatedProductTests
{
    [Fact]
    public void Constructor_WithValidData_SetsFields()
    {
        var productId = Guid.NewGuid();
        var relatedId = Guid.NewGuid();

        var related = new RelatedProduct(productId, relatedId, RelationType.Accessory);

        related.ProductId.Should().Be(productId);
        related.RelatedProductId.Should().Be(relatedId);
        related.RelationType.Should().Be(RelationType.Accessory);
        related.SortOrder.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithSameProductIds_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();

        var act = () => new RelatedProduct(id, id, RelationType.Similar);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be related to itself*");
    }

    [Fact]
    public void SetRelationType_UpdatesValue()
    {
        var related = new RelatedProduct(Guid.NewGuid(), Guid.NewGuid(), RelationType.Similar);

        related.SetRelationType(RelationType.Bundle);

        related.RelationType.Should().Be(RelationType.Bundle);
    }

    [Fact]
    public void SetSortOrder_UpdatesValue()
    {
        var related = new RelatedProduct(Guid.NewGuid(), Guid.NewGuid(), RelationType.Similar);

        related.SetSortOrder(9);

        related.SortOrder.Should().Be(9);
    }
}
