using ClimaSite.Application.Features.Wishlist.DTOs;
using ClimaSite.Application.Features.Wishlist.Services;
using FluentValidation;
using MediatR;

namespace ClimaSite.Application.Features.Wishlist.Queries;

public record GetSharedWishlistQuery : IRequest<WishlistDto?>
{
    public string ShareToken { get; init; } = string.Empty;
}

public class GetSharedWishlistQueryValidator : AbstractValidator<GetSharedWishlistQuery>
{
    public GetSharedWishlistQueryValidator()
    {
        RuleFor(x => x.ShareToken)
            .NotEmpty()
            .MaximumLength(50);
    }
}

public class GetSharedWishlistQueryHandler : IRequestHandler<GetSharedWishlistQuery, WishlistDto?>
{
    private readonly WishlistApplicationService _wishlistService;

    public GetSharedWishlistQueryHandler(WishlistApplicationService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    public async Task<WishlistDto?> Handle(
        GetSharedWishlistQuery request,
        CancellationToken cancellationToken)
    {
        return await _wishlistService.GetSharedWishlistDtoAsync(request.ShareToken, cancellationToken);
    }
}
