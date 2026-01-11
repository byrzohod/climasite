using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Wishlist.Commands;

public record AddToWishlistCommand : IRequest<Result>
{
    public Guid ProductId { get; init; }
}

public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");
    }
}

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddToWishlistCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result.Failure("User must be authenticated");
        }

        // Check if product exists
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (!productExists)
        {
            return Result.Failure("Product not found");
        }

        // Get or create wishlist
        var wishlist = await _context.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wishlist == null)
        {
            wishlist = new Core.Entities.Wishlist(userId.Value);
            _context.Wishlists.Add(wishlist);
        }

        // Check if item already in wishlist
        if (wishlist.Items.Any(i => i.ProductId == request.ProductId))
        {
            return Result.Success(); // Already in wishlist, not an error
        }

        wishlist.AddItem(request.ProductId);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
