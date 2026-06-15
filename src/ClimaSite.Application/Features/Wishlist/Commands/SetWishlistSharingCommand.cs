using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Application.Features.Wishlist.Services;
using FluentValidation;
using MediatR;

namespace ClimaSite.Application.Features.Wishlist.Commands;

public record SetWishlistSharingCommand : IRequest<Result<WishlistDto>>
{
    public bool IsPublic { get; init; }
}

public class SetWishlistSharingCommandValidator : AbstractValidator<SetWishlistSharingCommand>
{
    public SetWishlistSharingCommandValidator()
    {
        RuleFor(x => x.IsPublic).NotNull();
    }
}

public class SetWishlistSharingCommandHandler : IRequestHandler<SetWishlistSharingCommand, Result<WishlistDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly WishlistApplicationService _wishlistService;

    public SetWishlistSharingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        WishlistApplicationService wishlistService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _wishlistService = wishlistService;
    }

    public async Task<Result<WishlistDto>> Handle(
        SetWishlistSharingCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Result<WishlistDto>.Failure("User must be authenticated");
        }

        return await _wishlistService.ExecuteUserMutationAsync(
            userId.Value,
            SetWishlistSharingAsync,
            cancellationToken);

        async Task<Result<WishlistDto>> SetWishlistSharingAsync()
        {
            var wishlist = await _wishlistService.GetOrCreateWishlistAsync(userId.Value, cancellationToken);
            wishlist.SetPublic(request.IsPublic);
            await _context.SaveChangesAsync(cancellationToken);

            var dto = await _wishlistService.MapToDtoAsync(wishlist, cancellationToken);
            return Result<WishlistDto>.Success(dto);
        }
    }
}
