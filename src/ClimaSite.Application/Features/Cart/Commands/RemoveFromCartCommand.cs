using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Commands;

public record RemoveFromCartCommand : IRequest<Result>
{
    public Guid ItemId { get; init; }
    public string? GuestSessionId { get; init; }
}

public class RemoveFromCartCommandValidator : AbstractValidator<RemoveFromCartCommand>
{
    public RemoveFromCartCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Cart item ID is required");
    }
}

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFromCartCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
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
            return Result.Failure("Cart not found.");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item == null)
        {
            return Result.Failure("Cart item not found.");
        }

        cart.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
