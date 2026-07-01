using ClimaSite.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Commands;

/// <summary>
/// Migrates a returning guest's LEGACY (client-supplied) cart onto the trusted signed-cookie session id
/// (INV-01 A1). Convergent and idempotent: a fast no-op when there is nothing to move, an atomic re-key when
/// the cookie has no cart yet, or a per-item merge (then delete of the legacy cart) when both exist. The whole
/// body runs inside one execution-strategy transaction and RE-DERIVES its decision from freshly-read state on
/// every attempt, so a commit-unknown retry (<c>EnableRetryOnFailure</c>) converges to the same end state
/// ("cart under the cookie id, no legacy cart") — this is a convergent operation, NOT a relative toggle, so
/// re-reading inside the delegate is safe (unlike the B-039 vote toggle).
/// </summary>
public record MigrateGuestCartCommand(string LegacySessionId, string CookieSessionId) : IRequest;

public class MigrateGuestCartCommandHandler : IRequestHandler<MigrateGuestCartCommand>
{
    private readonly IApplicationDbContext _context;

    public MigrateGuestCartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MigrateGuestCartCommand request, CancellationToken cancellationToken)
    {
        var legacyId = request.LegacySessionId;
        var cookieId = request.CookieSessionId;

        // Fast no-op: nothing to migrate, same id, or no legacy cart exists. Keeps the (overwhelmingly common)
        // steady-state request — where the cookie id is already the cart's key — off the transaction path,
        // costing only one cheap indexed EXISTS.
        if (string.IsNullOrEmpty(legacyId)
            || string.IsNullOrEmpty(cookieId)
            || legacyId == cookieId
            || !await _context.Carts.AnyAsync(c => c.SessionId == legacyId, cancellationToken))
        {
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        if (strategy is null)
        {
            await MigrateAsync();
        }
        else
        {
            await strategy.ExecuteAsync(MigrateAsync);
        }

        async Task MigrateAsync()
        {
            // EnableRetryOnFailure reuses the request-scoped context and does NOT reset the tracker on a
            // rollback, so clear it FIRST — every attempt must re-derive cookieCart/legacyCart from committed
            // state, never from a prior attempt's stale merge mutations (which would double-count). Safe here
            // because the migration runs before the main cart/order handler, which re-queries fresh afterward.
            _context.ClearChangeTracker();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Serialize concurrent same-cookie migrations. Without this, two racing migrations that both see a
            // pre-existing cookie cart each try to merge into it — colliding on the unique (cart_id, variant_id)
            // index (duplicate item INSERT) and/or a 0-row legacy DELETE — a checkout-adjacent 500. The
            // transaction-scoped advisory lock makes the loser wait, re-read (legacy now gone), and no-op, so
            // the winner's single-counted merge stands. Auto-released on commit/rollback.
            await _context.AcquireGuestCartMigrationLockAsync(cookieId, cancellationToken);

            var cookieCart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == cookieId, cancellationToken);

            if (cookieCart is null)
            {
                // No destination cart yet — atomically re-key the legacy cart onto the cookie id. Affects 0
                // rows (and is a harmless no-op) if a prior holder of the lock already moved it.
                await _context.RekeyGuestCartAsync(legacyId, cookieId, cancellationToken);
            }
            else
            {
                var legacyCart = await _context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.SessionId == legacyId, cancellationToken);

                if (legacyCart is not null)
                {
                    MergeLegacyIntoCookie(legacyCart, cookieCart, await LoadVariantStockAsync(legacyCart, cancellationToken));
                    _context.Carts.Remove(legacyCart);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }

    private async Task<Dictionary<Guid, int>> LoadVariantStockAsync(
        Core.Entities.Cart legacyCart,
        CancellationToken cancellationToken)
    {
        var productIds = legacyCart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return products
            .SelectMany(p => p.Variants)
            .GroupBy(v => v.Id)
            .ToDictionary(g => g.Key, g => g.First().StockQuantity);
    }

    // Mirrors MergeGuestCartCommand's per-item merge: combine quantities (or add), each capped at the
    // variant's current stock; anything that can't fit is dropped.
    private static void MergeLegacyIntoCookie(
        Core.Entities.Cart legacyCart,
        Core.Entities.Cart cookieCart,
        IReadOnlyDictionary<Guid, int> variantStock)
    {
        foreach (var legacyItem in legacyCart.Items)
        {
            var availableStock = variantStock.TryGetValue(legacyItem.VariantId, out var stock) ? stock : 0;

            var existingItem = cookieCart.Items.FirstOrDefault(i =>
                i.ProductId == legacyItem.ProductId && i.VariantId == legacyItem.VariantId);

            if (existingItem is not null)
            {
                var newQuantity = Math.Min(existingItem.Quantity + legacyItem.Quantity, availableStock);
                if (newQuantity > 0)
                {
                    existingItem.SetQuantity(newQuantity);
                }
            }
            else
            {
                var quantity = Math.Min(legacyItem.Quantity, availableStock);
                if (quantity > 0)
                {
                    cookieCart.AddItem(legacyItem.ProductId, legacyItem.VariantId, quantity, legacyItem.UnitPrice);
                }
            }
        }
    }
}
