using ClimaSite.Application.Features.Admin.Products.Commands;
using ClimaSite.Application.Features.Admin.Products.DTOs;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Admin.Products;

public class CreateProductCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private CreateProductCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_ValidCommand_PersistsProductAndReturnsId()
    {
        var command = new CreateProductCommand
        {
            Name = "Test AC Unit",
            Sku = "AC-100",
            BasePrice = 599.99m
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var products = await _context.Products.ToListAsync();
        products.Should().ContainSingle(p => p.Id == result.Value);
        var product = products.Single();
        product.Name.Should().Be("Test AC Unit");
        // SKU is normalised to upper-invariant by the domain entity.
        product.Sku.Should().Be("AC-100");
        product.BasePrice.Should().Be(599.99m);
    }

    [Fact]
    public async Task Handle_GeneratesSlugFromName()
    {
        var command = new CreateProductCommand
        {
            Name = "Cool & Quiet AC's \"Pro\"",
            Sku = "AC-SLUG",
            BasePrice = 100m
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var product = (await _context.Products.ToListAsync()).Single(p => p.Id == result.Value);
        // " " -> "-", "&" -> "and", "'" and '"' stripped, then lower-invariant.
        product.Slug.Should().Be("cool-and-quiet-acs-pro");
    }

    [Fact]
    public async Task Handle_CreatesDefaultVariantWithStock()
    {
        var command = new CreateProductCommand
        {
            Name = "Variant Product",
            Sku = "AC-VAR",
            BasePrice = 200m
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var variants = await _context.ProductVariants.ToListAsync();
        variants.Should().ContainSingle(v => v.ProductId == result.Value);
        var variant = variants.Single();
        variant.Sku.Should().Be("AC-VAR-DEFAULT");
        variant.Name.Should().Be("Default");
        variant.StockQuantity.Should().Be(50);
    }

    [Fact]
    public async Task Handle_AppliesOptionalFieldsFeaturesTagsAndSpecifications()
    {
        var command = new CreateProductCommand
        {
            Name = "Full Product",
            Sku = "AC-FULL",
            BasePrice = 300m,
            ShortDescription = "Short",
            Description = "Long description",
            CompareAtPrice = 350m,
            CostPrice = 150m,
            Brand = "Daikin",
            Model = "X1",
            IsActive = false,
            IsFeatured = true,
            RequiresInstallation = true,
            WarrantyMonths = 24,
            WeightKg = 12.5m,
            MetaTitle = "Meta",
            MetaDescription = "MetaDesc",
            Specifications = new Dictionary<string, object> { ["btu"] = 12000 },
            Features = new List<ProductFeatureDto>
            {
                new() { Title = "Quiet", Description = "Silent operation", Icon = "volume" }
            },
            Tags = new List<string> { "Inverter", "WiFi" }
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var product = (await _context.Products.ToListAsync()).Single(p => p.Id == result.Value);

        product.ShortDescription.Should().Be("Short");
        product.Description.Should().Be("Long description");
        product.CompareAtPrice.Should().Be(350m);
        product.CostPrice.Should().Be(150m);
        product.Brand.Should().Be("Daikin");
        product.Model.Should().Be("X1");
        product.IsActive.Should().BeFalse();
        product.IsFeatured.Should().BeTrue();
        product.RequiresInstallation.Should().BeTrue();
        product.WarrantyMonths.Should().Be(24);
        product.WeightKg.Should().Be(12.5m);
        product.MetaTitle.Should().Be("Meta");
        product.MetaDescription.Should().Be("MetaDesc");
        product.Specifications.Should().ContainKey("btu");
        product.Features.Should().ContainSingle(f => f.Title == "Quiet");
        // Tags are normalised to lower-invariant by the domain entity.
        product.Tags.Should().BeEquivalentTo(new[] { "inverter", "wifi" });
    }
}
