using ClimaSite.Application.Features.Categories.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Categories.Queries;

public class GetCategoryTreeQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private GetCategoryTreeQueryHandler CreateHandler() => new(_context);

    private Category SeedCategory(
        string name,
        string slug,
        bool isActive = true,
        Guid? parentId = null,
        int sortOrder = 0)
    {
        var category = new Category(name, slug);
        category.SetActive(isActive);
        category.SetSortOrder(sortOrder);
        if (parentId.HasValue)
        {
            category.SetParent(parentId);
        }
        _context.Categories.Add(category);
        return category;
    }

    [Fact]
    public async Task Handle_BuildsNestedTree_FromRootsAndChildren()
    {
        var root = SeedCategory("Climate", "climate");
        var child = SeedCategory("Air Conditioners", "air-conditioners", parentId: root.Id);
        SeedCategory("Split Systems", "split-systems", parentId: child.Id);

        var result = await CreateHandler().Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        var rootDto = result[0];
        rootDto.Id.Should().Be(root.Id);
        rootDto.Name.Should().Be("Climate");
        rootDto.Children.Should().ContainSingle();

        var childDto = rootDto.Children[0];
        childDto.Id.Should().Be(child.Id);
        childDto.Children.Should().ContainSingle();
        childDto.Children[0].Name.Should().Be("Split Systems");
    }

    [Fact]
    public async Task Handle_ExcludesInactiveCategories()
    {
        SeedCategory("Visible Root", "visible-root");
        SeedCategory("Hidden Root", "hidden-root", isActive: false);

        var result = await CreateHandler().Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Visible Root");
    }

    [Fact]
    public async Task Handle_OrdersRootsBySortOrder()
    {
        SeedCategory("Second", "second", sortOrder: 2);
        SeedCategory("First", "first", sortOrder: 1);
        SeedCategory("Third", "third", sortOrder: 3);

        var result = await CreateHandler().Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.Select(c => c.Name).Should().ContainInOrder("First", "Second", "Third");
    }

    [Fact]
    public async Task Handle_WithNameFilter_ReturnsFlatMatchingList()
    {
        var root = SeedCategory("Heating", "heating");
        SeedCategory("Heating Accessories", "heating-accessories", parentId: root.Id);
        SeedCategory("Cooling", "cooling");

        var result = await CreateHandler().Handle(
            new GetCategoryTreeQuery { NameFilter = "Heating" },
            CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Name.Contains("Heating"));
        // Flat list: matched children carry no nested children.
        result.Should().OnlyContain(c => c.Children.Count == 0);
    }

    [Fact]
    public async Task Handle_NoCategories_ReturnsEmptyList()
    {
        var result = await CreateHandler().Handle(new GetCategoryTreeQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
