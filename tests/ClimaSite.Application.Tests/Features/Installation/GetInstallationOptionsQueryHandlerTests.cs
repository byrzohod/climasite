using ClimaSite.Application.Features.Installation.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Installation;

public class GetInstallationOptionsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetInstallationOptionsQueryHandler CreateHandler() => new(_context);

    private Product SeedProduct(decimal basePrice)
    {
        var product = new Product("INS-001", "Installable AC", "installable-ac", basePrice);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_WhenProductMissing_Throws()
    {
        var act = () => CreateHandler().Handle(
            new GetInstallationOptionsQuery { ProductId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_WhenPriceBelowThreshold_InstallationUnavailable()
    {
        var product = SeedProduct(basePrice: 150m);

        var result = await CreateHandler().Handle(
            new GetInstallationOptionsQuery { ProductId = product.Id },
            CancellationToken.None);

        result.InstallationAvailable.Should().BeFalse();
        result.Options.Should().BeEmpty();
        result.ProductName.Should().Be("Installable AC");
    }

    [Fact]
    public async Task Handle_WhenPriceAtThreshold_OffersThreeOptions()
    {
        var product = SeedProduct(basePrice: 1000m);

        var result = await CreateHandler().Handle(
            new GetInstallationOptionsQuery { ProductId = product.Id },
            CancellationToken.None);

        result.InstallationAvailable.Should().BeTrue();
        result.Options.Should().HaveCount(3);
        result.Options.Select(o => o.Type).Should().BeEquivalentTo(
            new[]
            {
                InstallationType.Standard.ToString(),
                InstallationType.Premium.ToString(),
                InstallationType.Express.ToString()
            });
    }

    [Fact]
    public async Task Handle_PricesEachOptionAsPercentageOfBasePrice()
    {
        var product = SeedProduct(basePrice: 1000m);

        var result = await CreateHandler().Handle(
            new GetInstallationOptionsQuery { ProductId = product.Id },
            CancellationToken.None);

        var standard = result.Options.Single(o => o.Type == InstallationType.Standard.ToString());
        var premium = result.Options.Single(o => o.Type == InstallationType.Premium.ToString());
        var express = result.Options.Single(o => o.Type == InstallationType.Express.ToString());

        standard.Price.Should().Be(150m, "15% of 1000");
        premium.Price.Should().Be(250m, "25% of 1000");
        express.Price.Should().Be(350m, "35% of 1000");
        express.EstimatedDays.Should().Be(2);
    }
}
