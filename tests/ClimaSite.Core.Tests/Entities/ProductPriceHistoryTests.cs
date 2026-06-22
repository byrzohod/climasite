using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ProductPriceHistoryTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var productId = Guid.NewGuid();

        var history = ProductPriceHistory.Create(
            productId, 499m, 599m, PriceChangeReason.Promotion, "Spring sale");

        history.ProductId.Should().Be(productId);
        history.Price.Should().Be(499m);
        history.CompareAtPrice.Should().Be(599m);
        history.Reason.Should().Be(PriceChangeReason.Promotion);
        history.Notes.Should().Be("Spring sale");
        history.Id.Should().NotBe(Guid.Empty);
        history.RecordedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithoutNotes_LeavesNotesNull()
    {
        var history = ProductPriceHistory.Create(
            Guid.NewGuid(), 100m, null, PriceChangeReason.PriceChange);

        history.Notes.Should().BeNull();
        history.CompareAtPrice.Should().BeNull();
    }

    [Fact]
    public void CreateInitial_UsesInitialReasonAndNotes()
    {
        var productId = Guid.NewGuid();

        var history = ProductPriceHistory.CreateInitial(productId, 249m, 299m);

        history.ProductId.Should().Be(productId);
        history.Price.Should().Be(249m);
        history.CompareAtPrice.Should().Be(299m);
        history.Reason.Should().Be(PriceChangeReason.Initial);
        history.Notes.Should().Be("Initial price");
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var a = ProductPriceHistory.Create(Guid.NewGuid(), 10m, null, PriceChangeReason.Initial);
        var b = ProductPriceHistory.Create(Guid.NewGuid(), 10m, null, PriceChangeReason.Initial);

        a.Id.Should().NotBe(b.Id);
    }
}
