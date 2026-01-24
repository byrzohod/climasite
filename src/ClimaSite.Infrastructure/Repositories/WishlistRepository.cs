using ClimaSite.Core.Entities;
using ClimaSite.Core.Interfaces;
using ClimaSite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Infrastructure.Repositories;

public class WishlistRepository : BaseRepository<Wishlist>, IWishlistRepository
{
    public WishlistRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<Wishlist?> GetByShareTokenAsync(string shareToken, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Variants.Where(v => v.IsActive))
            .FirstOrDefaultAsync(w => w.ShareToken == shareToken && w.IsPublic, cancellationToken);
    }

    public async Task<Wishlist?> GetWithItemsAsync(Guid wishlistId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Variants.Where(v => v.IsActive))
            .FirstOrDefaultAsync(w => w.Id == wishlistId, cancellationToken);
    }

    public async Task<Wishlist?> GetWithItemsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images.Where(img => img.IsPrimary))
            .Include(w => w.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Variants.Where(v => v.IsActive))
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<bool> ContainsProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(w => w.UserId == userId)
            .SelectMany(w => w.Items)
            .AnyAsync(i => i.ProductId == productId, cancellationToken);
    }
}
