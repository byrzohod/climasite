using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Wishlist.DTOs;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Wishlist.Services;

public class WishlistApplicationService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserMutationLocks = new();

    private readonly IApplicationDbContext _context;

    public WishlistApplicationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteUserMutationAsync<T>(
        Guid userId,
        Func<Task<T>> mutation,
        CancellationToken cancellationToken)
    {
        var userLock = UserMutationLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);

        try
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return strategy is null
                ? await ExecuteInTransactionAsync()
                : await strategy.ExecuteAsync(ExecuteInTransactionAsync);
        }
        finally
        {
            userLock.Release();
        }

        async Task<T> ExecuteInTransactionAsync()
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var result = await mutation();

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
    }

    public async Task<Core.Entities.Wishlist> GetOrCreateWishlistAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var wishlist = await GetWishlistWithItemsByUserIdAsync(userId, cancellationToken);
        if (wishlist != null)
        {
            return wishlist;
        }

        wishlist = new Core.Entities.Wishlist(userId);
        _context.Wishlists.Add(wishlist);
        return wishlist;
    }

    public async Task<Core.Entities.Wishlist?> GetWishlistWithItemsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<WishlistDto> GetWishlistDtoByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var wishlist = await GetWishlistWithItemsByUserIdAsync(userId, cancellationToken);
        return wishlist == null
            ? CreateEmptyDto(userId)
            : await MapToDtoAsync(wishlist, cancellationToken);
    }

    public async Task<WishlistDto?> GetSharedWishlistDtoAsync(
        string shareToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return null;
        }

        var normalizedToken = shareToken.Trim();
        var wishlist = await _context.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(
                w => w.ShareToken == normalizedToken && w.IsPublic,
                cancellationToken);

        return wishlist == null
            ? null
            : await MapToDtoAsync(wishlist, cancellationToken);
    }

    public async Task<WishlistDto> MapToDtoAsync(
        Core.Entities.Wishlist wishlist,
        CancellationToken cancellationToken)
    {
        var productIds = wishlist.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = wishlist.Items
            .OrderByDescending(i => i.CreatedAt)
            .Select(item => MapItem(item, products.GetValueOrDefault(item.ProductId)))
            .OfType<WishlistItemDto>()
            .ToList();

        return new WishlistDto
        {
            Id = wishlist.Id,
            UserId = wishlist.UserId,
            IsPublic = wishlist.IsPublic,
            ShareToken = wishlist.ShareToken,
            Items = items,
            ItemCount = items.Count,
            UpdatedAt = wishlist.UpdatedAt
        };
    }

    public WishlistDto CreateEmptyDto(Guid userId) => new()
    {
        Id = Guid.Empty,
        UserId = userId,
        IsPublic = false,
        ShareToken = null,
        Items = [],
        ItemCount = 0,
        UpdatedAt = DateTime.UtcNow
    };

    private static WishlistItemDto? MapItem(
        Core.Entities.WishlistItem item,
        Core.Entities.Product? product)
    {
        if (product == null)
        {
            return null;
        }

        var primaryImage = product.Images
            .OrderBy(i => i.SortOrder)
            .FirstOrDefault(i => i.IsPrimary)
            ?? product.Images.OrderBy(i => i.SortOrder).FirstOrDefault();

        var inStock = product.Variants.Any(v => v.IsActive && v.StockQuantity > 0);

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = product.Name,
            ProductSlug = product.Slug,
            ShortDescription = product.ShortDescription,
            Brand = product.Brand,
            ImageUrl = primaryImage?.Url,
            PrimaryImageUrl = primaryImage?.Url,
            Price = product.BasePrice,
            SalePrice = product.CompareAtPrice,
            IsOnSale = product.IsOnSale,
            DiscountPercentage = product.DiscountPercentage ?? 0,
            AverageRating = 0,
            ReviewCount = 0,
            InStock = inStock,
            Note = item.Note,
            Priority = item.Priority,
            PriceWhenAdded = item.PriceWhenAdded,
            NotifyOnSale = item.NotifyOnSale,
            AddedAt = item.CreatedAt
        };
    }
}
