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
    private readonly IStockReservationService _reservations;

    public ClearCartCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IStockReservationService reservations)
    {
        _context = context;
        _currentUserService = currentUserService;
        _reservations = reservations;
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

        // INV-01 A2: release any Active checkout holds this cart owns (usually a no-op pre-checkout) BEFORE the
        // clear — release runs its own execution-strategy transaction and clears the tracker, so it must precede
        // the tracked cart mutation below.
        await _reservations.ReleaseCartAsync(cart.Id, cancellationToken);

        cart.Clear();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
