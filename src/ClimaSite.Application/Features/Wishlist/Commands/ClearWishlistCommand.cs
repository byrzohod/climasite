using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Application.Features.Wishlist.Services;
using MediatR;

namespace ClimaSite.Application.Features.Wishlist.Commands;

public record ClearWishlistCommand : IRequest<Result<WishlistDto>>;

public class ClearWishlistCommandHandler : IRequestHandler<ClearWishlistCommand, Result<WishlistDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly WishlistApplicationService _wishlistService;

    public ClearWishlistCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        WishlistApplicationService wishlistService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _wishlistService = wishlistService;
    }

    public async Task<Result<WishlistDto>> Handle(
        ClearWishlistCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<WishlistDto>.Failure("User must be authenticated");
        }

        return await _wishlistService.ExecuteUserMutationAsync(
            userId.Value,
            ClearWishlistAsync,
            cancellationToken);

        async Task<Result<WishlistDto>> ClearWishlistAsync()
        {
            var wishlist = await _wishlistService.GetWishlistWithItemsByUserIdAsync(userId.Value, cancellationToken);
            if (wishlist == null)
            {
                return Result<WishlistDto>.Success(_wishlistService.CreateEmptyDto(userId.Value));
            }

            _context.WishlistItems.RemoveRange(wishlist.Items);
            wishlist.Clear();
            await _context.SaveChangesAsync(cancellationToken);

            var dto = await _wishlistService.MapToDtoAsync(wishlist, cancellationToken);
            return Result<WishlistDto>.Success(dto);
        }
    }
}
