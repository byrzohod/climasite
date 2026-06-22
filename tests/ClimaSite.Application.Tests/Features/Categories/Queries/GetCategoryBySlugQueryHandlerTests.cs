using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Features.Categories.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Categories.Queries;

public class GetCategoryBySlugQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetCategoryBySlugQueryHandler CreateHandler() => new(_context);

    private Category SeedCategory(
        string name,
        string slug,
        bool isActive = true,
        Guid? parentId = null)
    {
        var category = new Category(name, slug, $"{name} description");
        category.SetActive(isActive);
        category.SetIcon("icon");
        category.SetImageUrl("https://cdn.test/img.png");
        if (parentId.HasValue)
        {
            category.SetParent(parentId);
        }
        _context.Categories.Add(category);
        return category;
    }

    [Fact]
    public async Task Handle_ActiveCategory_ReturnsDto()
    {
        var category = SeedCategory("Air Conditioners", "air-conditioners");

        var result = await CreateHandler().Handle(
            new GetCategoryBySlugQuery { Slug = "air-conditioners" },
            CancellationToken.None);

        result.Id.Should().Be(category.Id);
        result.Name.Should().Be("Air Conditioners");
        result.Slug.Should().Be("air-conditioners");
        result.Description.Should().Be("Air Conditioners description");
        result.Icon.Should().Be("icon");
        result.ImageUrl.Should().Be("https://cdn.test/img.png");
        result.IsActive.Should().BeTrue();
        result.ParentCategory.Should().BeNull();
        result.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SlugIsLowercasedBeforeLookup()
    {
        SeedCategory("Heating", "heating");

        var result = await CreateHandler().Handle(
            new GetCategoryBySlugQuery { Slug = "HEATING" },
            CancellationToken.None);

        result.Slug.Should().Be("heating");
    }

    [Fact]
    public async Task Handle_CountsActiveProductsInCategory()
    {
        var category = SeedCategory("Cooling", "cooling");

        var activeProduct = new Product("SKU-A", "Active Unit", "active-unit", 100m);
        activeProduct.SetCategory(category.Id);
        activeProduct.SetActive(true);
        _context.AddProduct(activeProduct);

        var inactiveProduct = new Product("SKU-B", "Inactive Unit", "inactive-unit", 100m);
        inactiveProduct.SetCategory(category.Id);
        inactiveProduct.SetActive(false);
        _context.AddProduct(inactiveProduct);

        var result = await CreateHandler().Handle(
            new GetCategoryBySlugQuery { Slug = "cooling" },
            CancellationToken.None);

        result.ProductCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_InactiveCategory_ThrowsNotFound()
    {
        SeedCategory("Hidden", "hidden", isActive: false);

        var act = () => CreateHandler().Handle(
            new GetCategoryBySlugQuery { Slug = "hidden" },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_UnknownSlug_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(
            new GetCategoryBySlugQuery { Slug = "does-not-exist" },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
