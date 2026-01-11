using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Wishlist.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Wishlist.Queries;

public record GetWishlistQuery : IRequest<WishlistDto?>;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWishlistQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        var wishlist = await _context.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wishlist == null)
        {
            return new WishlistDto
            {
                Id = Guid.Empty,
                UserId = userId.Value,
                Items = [],
                ItemCount = 0,
                UpdatedAt = DateTime.UtcNow
            };
        }

        var productIds = wishlist.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var items = wishlist.Items.Select(item =>
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            var primaryImage = product?.Images.FirstOrDefault(i => i.IsPrimary);
            var inStock = product?.Variants.Any(v => v.IsActive && v.StockQuantity > 0) ?? false;

            return new WishlistItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = product?.Name ?? "Product unavailable",
                ProductSlug = product?.Slug ?? "",
                ImageUrl = primaryImage?.Url,
                Price = product?.BasePrice ?? 0,
                SalePrice = product?.CompareAtPrice,
                IsOnSale = product?.IsOnSale ?? false,
                InStock = inStock,
                AddedAt = item.CreatedAt
            };
        }).ToList();

        return new WishlistDto
        {
            Id = wishlist.Id,
            UserId = wishlist.UserId,
            Items = items,
            ItemCount = items.Count,
            UpdatedAt = wishlist.UpdatedAt
        };
    }
}
