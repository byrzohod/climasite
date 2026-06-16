using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Wishlist.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Wishlist.Services;

public class WishlistApplicationService
{
    /// <summary>
    /// Postgres unique-violation SQLSTATE. Raised when a concurrent request races to
    /// insert the same wishlist item against the unique (WishlistId, ProductId) index.
    /// </summary>
    private const string UniqueViolationSqlState = "23505";

    private readonly IApplicationDbContext _context;

    public WishlistApplicationService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Runs a wishlist mutation inside a transaction using the provider execution strategy.
    /// Concurrency/idempotency is enforced by the database unique index rather than an
    /// in-process lock, so this is safe across multiple application instances.
    /// </summary>
    public async Task<T> ExecuteUserMutationAsync<T>(
        Guid userId,
        Func<Task<T>> mutation,
        CancellationToken cancellationToken)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(ExecuteInTransactionAsync);

        async Task<T> ExecuteInTransactionAsync()
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var result = await mutation();

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// Returns true when the supplied exception is a Postgres unique-constraint violation,
    /// which signals that the same wishlist item was inserted concurrently.
    /// </summary>
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var sqlStateProperty = current.GetType().GetProperty("SqlState");
            if (sqlStateProperty?.GetValue(current) is string sqlState
                && sqlState == UniqueViolationSqlState)
            {
                return true;
            }
        }

        return false;
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
        CancellationToken cancellationToken,
        string? language = null)
    {
        var wishlist = await _context.Wishlists
            .AsNoTracking()
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        return wishlist == null
            ? CreateEmptyDto(userId)
            : await MapToDtoAsync(wishlist, cancellationToken, language);
    }

    public async Task<WishlistDto?> GetSharedWishlistDtoAsync(
        string shareToken,
        CancellationToken cancellationToken,
        string? language = null)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return null;
        }

        var normalizedToken = shareToken.Trim();
        var wishlist = await _context.Wishlists
            .AsNoTracking()
            .Include(w => w.Items)
            .FirstOrDefaultAsync(
                w => w.ShareToken == normalizedToken && w.IsPublic,
                cancellationToken);

        // Anonymous shared response: omit the owner's identity (SEC-10).
        return wishlist == null
            ? null
            : await MapToDtoAsync(wishlist, cancellationToken, language, includeOwner: false);
    }

    public async Task<WishlistDto> MapToDtoAsync(
        Core.Entities.Wishlist wishlist,
        CancellationToken cancellationToken,
        string? language = null,
        bool includeOwner = true)
    {
        var productIds = wishlist.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Translations)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = wishlist.Items
            .OrderByDescending(i => i.CreatedAt)
            .Select(item => MapItem(item, products.GetValueOrDefault(item.ProductId), language))
            .OfType<WishlistItemDto>()
            .ToList();

        return new WishlistDto
        {
            Id = wishlist.Id,
            UserId = includeOwner ? wishlist.UserId : null,
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
        Core.Entities.Product? product,
        string? language)
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

        var (name, shortDescription, _, _, _) = product.GetTranslatedContent(language);

        return new WishlistItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = name,
            ProductSlug = product.Slug,
            ShortDescription = shortDescription,
            Brand = product.Brand,
            ImageUrl = primaryImage?.Url,
            PrimaryImageUrl = primaryImage?.Url,
            Price = product.BasePrice,
            SalePrice = ProductPricing.GetSalePrice(product.BasePrice, product.CompareAtPrice),
            IsOnSale = ProductPricing.IsOnSale(product.BasePrice, product.CompareAtPrice),
            DiscountPercentage = ProductPricing.GetDiscountPercentage(product.BasePrice, product.CompareAtPrice),
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
