using ClimaSite.Application.Features.Admin.Products.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Products;

public class UpdateProductCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private UpdateProductCommandHandler CreateHandler() => new(_context);

    private Product SeedProduct(string sku = "AC-OLD", decimal basePrice = 100m)
    {
        var product = new Product(sku, "Old Name", "old-name", basePrice);
        _context.AddProduct(product);
        return product;
    }

    [Fact]
    public async Task Handle_ExistingProduct_UpdatesFields()
    {
        var product = SeedProduct();

        var result = await CreateHandler().Handle(new UpdateProductCommand
        {
            Id = product.Id,
            Name = "New Name",
            Sku = "AC-NEW",
            Slug = "new-name",
            BasePrice = 250m,
            Brand = "Mitsubishi",
            IsActive = false,
            IsFeatured = true,
            WarrantyMonths = 36
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("New Name");
        product.Sku.Should().Be("AC-NEW");
        product.Slug.Should().Be("new-name");
        product.BasePrice.Should().Be(250m);
        product.Brand.Should().Be("Mitsubishi");
        product.IsActive.Should().BeFalse();
        product.IsFeatured.Should().BeTrue();
        product.WarrantyMonths.Should().Be(36);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        var result = await CreateHandler().Handle(new UpdateProductCommand
        {
            Id = Guid.NewGuid(),
            Name = "Ghost",
            Sku = "GHOST",
            Slug = "ghost",
            BasePrice = 10m
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Fact]
    public async Task Handle_PriceChanged_RecordsPriceHistory()
    {
        var product = SeedProduct(basePrice: 100m);

        var result = await CreateHandler().Handle(new UpdateProductCommand
        {
            Id = product.Id,
            Name = "Old Name",
            Sku = "AC-OLD",
            Slug = "old-name",
            BasePrice = 175m
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var history = await _context.ProductPriceHistory.ToListAsync();
        history.Should().ContainSingle(h => h.ProductId == product.Id && h.Price == 175m);
    }

    [Fact]
    public async Task Handle_PriceUnchanged_DoesNotRecordPriceHistory()
    {
        var product = SeedProduct(basePrice: 100m);

        var result = await CreateHandler().Handle(new UpdateProductCommand
        {
            Id = product.Id,
            Name = "Renamed",
            Sku = "AC-OLD",
            Slug = "old-name",
            BasePrice = 100m
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.ProductPriceHistory.ToListAsync()).Should().BeEmpty();
    }
}
