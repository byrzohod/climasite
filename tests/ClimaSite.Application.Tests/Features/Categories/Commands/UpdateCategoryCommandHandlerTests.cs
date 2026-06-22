using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Features.Categories.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

// NOTE (MockDbContext limit): UpdateCategoryCommandHandler.Handle starts with
// _context.Categories.FindAsync(...). MockDbContext does not configure FindAsync, so it always
// returns null regardless of seeded data — every Handle call throws NotFoundException before any
// of the parent/circular/Result.Failure branches can run. The happy path and the post-Find failure
// branches are therefore unreachable in unit tests with this mock and are covered by Api integration
// tests instead. The reachable NotFound path is asserted here.
public class UpdateCategoryCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private UpdateCategoryCommandHandler CreateHandler() => new(_context);

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
        (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(_context.Categories)).Should().BeEmpty();
    }
}
