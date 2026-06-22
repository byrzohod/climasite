using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Features.Categories.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

// MockDbContext now resolves FindAsync(id) against the backing list, so the handler's post-Find
// branches (parent validation, circular-reference guard, field mutation, persistence) are reachable
// in unit tests and asserted here alongside the original NotFound path.
public class UpdateCategoryCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private UpdateCategoryCommandHandler CreateHandler() => new(_context);

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

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = "New Name" };

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NotFound_DoesNotPersistChanges()
    {
        var command = new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = "New Name", IsActive = false };

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        (await _context.Categories.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UpdatesScalarFields_WhenProvided()
    {
        var category = SeedCategory();
        var command = new UpdateCategoryCommand
        {
            Id = category.Id,
            Name = "Cooling",
            Description = "AC and chillers",
            ImageUrl = "/img/cooling.png",
            Icon = "snowflake",
            SortOrder = 7,
            MetaTitle = "Cooling Meta",
            MetaDescription = "Cooling meta description",
            IsActive = false
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        category.Name.Should().Be("Cooling");
        category.Description.Should().Be("AC and chillers");
        category.ImageUrl.Should().Be("/img/cooling.png");
        category.Icon.Should().Be("snowflake");
        category.SortOrder.Should().Be(7);
        category.MetaTitle.Should().Be("Cooling Meta");
        category.MetaDescription.Should().Be("Cooling meta description");
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_OnlyMutatesProvidedFields_LeavesOthersUnchanged()
    {
        var category = SeedCategory(name: "Original", slug: "original");
        category.SetDescription("Original description");
        var command = new UpdateCategoryCommand { Id = category.Id, Name = "Renamed" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Renamed");
        category.Description.Should().Be("Original description", "Description was not part of the command");
    }

    [Fact]
    public async Task Handle_SettingSelfAsParent_ReturnsFailure()
    {
        var category = SeedCategory();
        var command = new UpdateCategoryCommand { Id = category.Id, ParentId = category.Id };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("A category cannot be its own parent");
    }

    [Fact]
    public async Task Handle_NonExistentParent_ReturnsFailure()
    {
        var category = SeedCategory();
        var missingParentId = Guid.NewGuid();
        var command = new UpdateCategoryCommand { Id = category.Id, ParentId = missingParentId };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(missingParentId.ToString());
    }

    [Fact]
    public async Task Handle_ValidParentChange_AssignsParent()
    {
        var parent = SeedCategory(name: "Parent", slug: "parent");
        var child = SeedCategory(name: "Child", slug: "child");
        var command = new UpdateCategoryCommand { Id = child.Id, ParentId = parent.Id };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Handle_CircularParent_ReturnsFailure()
    {
        // grandParent <- parent <- child. Trying to set the grandparent's parent to the child is circular.
        var grandParent = SeedCategory(name: "Grand", slug: "grand");
        var parent = SeedCategory(name: "Parent", slug: "parent", parentId: grandParent.Id);
        var child = SeedCategory(name: "Child", slug: "child", parentId: parent.Id);

        var command = new UpdateCategoryCommand { Id = grandParent.Id, ParentId = child.Id };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("circular reference");
    }
}
