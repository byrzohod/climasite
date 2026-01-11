using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Commands;

public record ClearCartCommand : IRequest<Result>
{
    public string? GuestSessionId { get; init; }
}

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClearCartCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        Core.Entities.Cart? cart = null;

        if (userId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.GuestSessionId))
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == request.GuestSessionId, cancellationToken);
        }

        if (cart == null)
        {
            return Result.Success();
        }

        cart.Clear();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
