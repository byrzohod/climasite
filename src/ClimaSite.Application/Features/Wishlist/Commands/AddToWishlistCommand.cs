using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Application.Features.Wishlist.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Wishlist.Commands;

public record AddToWishlistCommand : IRequest<Result<WishlistDto>>
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

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result<WishlistDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly WishlistApplicationService _wishlistService;

    public AddToWishlistCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        WishlistApplicationService wishlistService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _wishlistService = wishlistService;
    }

    public async Task<Result<WishlistDto>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<WishlistDto>.Failure("User must be authenticated");
        }

        return await _wishlistService.ExecuteUserMutationAsync(
            userId.Value,
            AddToWishlistAsync,
            cancellationToken);

        async Task<Result<WishlistDto>> AddToWishlistAsync()
        {
            var productExists = await _context.Products
                .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

            if (!productExists)
            {
                return Result<WishlistDto>.Failure("Product not found");
            }

            var wishlist = await _wishlistService.GetOrCreateWishlistAsync(userId.Value, cancellationToken);

            if (wishlist.Items.Any(i => i.ProductId == request.ProductId))
            {
                var existingDto = await _wishlistService.MapToDtoAsync(wishlist, cancellationToken);
                return Result<WishlistDto>.Success(existingDto);
            }

            wishlist.AddItem(request.ProductId);
            await _context.SaveChangesAsync(cancellationToken);

            var dto = await _wishlistService.MapToDtoAsync(wishlist, cancellationToken);
            return Result<WishlistDto>.Success(dto);
        }
    }
}
