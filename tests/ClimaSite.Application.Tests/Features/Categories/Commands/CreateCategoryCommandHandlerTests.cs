using ClimaSite.Application.Features.Categories.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

public class CreateCategoryCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private CreateCategoryCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_ValidCommand_CreatesCategory_AndReturnsId()
    {
        var command = new CreateCategoryCommand
        {
            Name = "Air Conditioners",
            Description = "Cooling units",
            Icon = "ac",
            ImageUrl = "https://cdn.test/ac.png",
            SortOrder = 5,
            MetaTitle = "Air Conditioners",
            MetaDescription = "Browse our air conditioners",
            IsActive = true
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var created = await _context.Categories.SingleAsync();
        created.Id.Should().Be(result.Value);
        created.Name.Should().Be("Air Conditioners");
        created.Description.Should().Be("Cooling units");
        created.Icon.Should().Be("ac");
        created.ImageUrl.Should().Be("https://cdn.test/ac.png");
        created.SortOrder.Should().Be(5);
        created.MetaTitle.Should().Be("Air Conditioners");
        created.MetaDescription.Should().Be("Browse our air conditioners");
        created.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_GeneratesSlug_FromName()
    {
        var command = new CreateCategoryCommand { Name = "Heat Pumps & More" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var created = await _context.Categories.SingleAsync();
        created.Slug.Should().Be("heat-pumps-more");
    }

    [Fact]
    public async Task Handle_DuplicateSlug_AppendsNumericSuffix()
    {
        _context.Categories.Add(new Category("Cooling", "cooling"));

        var result = await CreateHandler().Handle(new CreateCategoryCommand { Name = "Cooling" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var created = (await _context.Categories.ToListAsync()).Single(c => c.Id == result.Value);
        created.Slug.Should().Be("cooling-1");
    }

    [Fact]
    public async Task Handle_WithExistingParent_SetsParentId()
    {
        var parent = new Category("Climate", "climate");
        _context.Categories.Add(parent);

        var result = await CreateHandler().Handle(
            new CreateCategoryCommand { Name = "Splits", ParentId = parent.Id },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var created = (await _context.Categories.ToListAsync()).Single(c => c.Id == result.Value);
        created.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task Handle_MissingParent_ReturnsFailure_AndDoesNotCreate()
    {
        var missingParentId = Guid.NewGuid();

        var result = await CreateHandler().Handle(
            new CreateCategoryCommand { Name = "Orphan", ParentId = missingParentId },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain(missingParentId.ToString());
        (await _context.Categories.ToListAsync()).Should().BeEmpty();
    }
}
