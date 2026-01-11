using ClimaSite.Core.Entities;

namespace ClimaSite.Core.Interfaces;

public interface IWishlistRepository : IRepository<Wishlist>
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Wishlist?> GetByShareTokenAsync(string shareToken, CancellationToken cancellationToken = default);
    Task<Wishlist?> GetWithItemsAsync(Guid wishlistId, CancellationToken cancellationToken = default);
    Task<Wishlist?> GetWithItemsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ContainsProductAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
}
