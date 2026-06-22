using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Features.Categories.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

// MockDbContext now resolves FindAsync(id) against the backing list, so the handler's post-Find
// guards (has-children, has-products) and the successful soft delete are reachable in unit tests and
// asserted here alongside the original NotFound path.
public class DeleteCategoryCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private DeleteCategoryCommandHandler CreateHandler() => new(_context);

    private Category SeedCategory(string name = "Heating", string slug = "heating", Guid? parentId = null)
    {
        var category = new Category(name, slug);
        if (parentId.HasValue)
        {
            category.SetParent(parentId);
        }
        _context.Categories.Add(category);
        return category;
    }

    private void SeedProductInCategory(Guid categoryId)
    {
        var product = new Product("DEL-PROD", "Delete Product", "delete-product", 100m);
        product.SetCategory(categoryId);
        _context.AddProduct(product);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteCategoryCommand { Id = Guid.NewGuid() };

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NotFound_RemovesNothing()
    {
        var command = new DeleteCategoryCommand { Id = Guid.NewGuid() };

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        (await _context.Categories.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CategoryWithChildren_ReturnsFailure_AndKeepsCategory()
    {
        var parent = SeedCategory(name: "Parent", slug: "parent");
        SeedCategory(name: "Child", slug: "child", parentId: parent.Id);

        var result = await CreateHandler().Handle(
            new DeleteCategoryCommand { Id = parent.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("child categories");
        (await _context.Categories.ToListAsync()).Should().Contain(c => c.Id == parent.Id);
    }

    [Fact]
    public async Task Handle_CategoryWithProducts_ReturnsFailure_AndKeepsCategory()
    {
        var category = SeedCategory();
        SeedProductInCategory(category.Id);

        var result = await CreateHandler().Handle(
            new DeleteCategoryCommand { Id = category.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("products");
        (await _context.Categories.ToListAsync()).Should().Contain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task Handle_EmptyCategory_IsDeleted()
    {
        var category = SeedCategory();

        var result = await CreateHandler().Handle(
            new DeleteCategoryCommand { Id = category.Id }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        (await _context.Categories.ToListAsync()).Should().NotContain(c => c.Id == category.Id);
    }
}
