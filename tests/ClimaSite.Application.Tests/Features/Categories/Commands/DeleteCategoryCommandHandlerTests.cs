using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Features.Categories.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Categories.Commands;

// NOTE (MockDbContext limit): DeleteCategoryCommandHandler.Handle starts with
// _context.Categories.FindAsync(...). MockDbContext does not configure FindAsync, so it always
// returns null regardless of seeded data — every Handle call throws NotFoundException before the
// has-children / has-products guards or the successful delete can run. Those post-Find branches are
// unreachable in unit tests with this mock and are covered by Api integration tests instead. The
// reachable NotFound path is asserted here.
public class DeleteCategoryCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private DeleteCategoryCommandHandler CreateHandler() => new(_context);

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
}
