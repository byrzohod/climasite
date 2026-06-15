using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Application.Features.Wishlist.Services;
using MediatR;

namespace ClimaSite.Application.Features.Wishlist.Commands;

public record RegenerateWishlistShareTokenCommand : IRequest<Result<WishlistDto>>;

public class RegenerateWishlistShareTokenCommandHandler
    : IRequestHandler<RegenerateWishlistShareTokenCommand, Result<WishlistDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly WishlistApplicationService _wishlistService;

    public RegenerateWishlistShareTokenCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        WishlistApplicationService wishlistService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _wishlistService = wishlistService;
    }

    public async Task<Result<WishlistDto>> Handle(
        RegenerateWishlistShareTokenCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<WishlistDto>.Failure("User must be authenticated");
        }

        return await _wishlistService.ExecuteUserMutationAsync(
            userId.Value,
            RegenerateWishlistShareTokenAsync,
            cancellationToken);

        async Task<Result<WishlistDto>> RegenerateWishlistShareTokenAsync()
        {
            var wishlist = await _wishlistService.GetOrCreateWishlistAsync(userId.Value, cancellationToken);
            wishlist.SetPublic(true);
            wishlist.RegenerateShareToken();
            await _context.SaveChangesAsync(cancellationToken);

            var dto = await _wishlistService.MapToDtoAsync(wishlist, cancellationToken);
            return Result<WishlistDto>.Success(dto);
        }
    }
}
