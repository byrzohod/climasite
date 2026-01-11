using ClimaSite.Core.Entities;

namespace ClimaSite.Core.Interfaces;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<Cart?> GetWithItemsAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<Cart?> GetWithItemsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Cart?> GetWithItemsBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task MergeCartsAsync(Guid sourceCartId, Guid targetCartId, CancellationToken cancellationToken = default);
    Task CleanupExpiredCartsAsync(CancellationToken cancellationToken = default);
}
