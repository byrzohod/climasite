using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Application.Features.Wishlist.Services;
using MediatR;

namespace ClimaSite.Application.Features.Wishlist.Queries;

public record GetWishlistQuery : IRequest<WishlistDto?>;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistDto?>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly WishlistApplicationService _wishlistService;

    public GetWishlistQueryHandler(
        ICurrentUserService currentUserService,
        WishlistApplicationService wishlistService)
    {
        _currentUserService = currentUserService;
        _wishlistService = wishlistService;
    }

    public async Task<WishlistDto?> Handle(
        GetWishlistQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return null;
        }

        return await _wishlistService.GetWishlistDtoByUserIdAsync(userId.Value, cancellationToken);
    }
}
