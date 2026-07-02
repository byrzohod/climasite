using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart;

/// <summary>
/// INV-01 A3 — reservation-aware "available to THIS cart" helper. Global availability is
/// <c>stock − reserved</c> (reserved = Σ Active holds across ALL carts). For a cart's OWN advisory
/// ceiling we add back the units THIS cart already holds, otherwise a mid-checkout cart is blocked from
/// re-growing to — or is capped below — what it legitimately holds:
/// <c>availableForThisCart = max(stock − reserved + ownActiveHold, 0)</c>.
/// Pre-checkout no cart holds anything (holds are minted at checkout-start, A2), so ownActiveHold is 0 and
/// this is a no-op for the common case. Read-only/advisory — the authoritative gate remains the
/// checkout-time reserve under a per-variant row lock.
/// </summary>
internal static class CartReservationAvailability
{
    /// <summary>This cart's Active reservation quantity per variant (a variant with no hold ⇒ absent ⇒ 0).</summary>
    public static async Task<Dictionary<Guid, int>> GetOwnActiveHoldsAsync(
        IApplicationDbContext context, Guid cartId, CancellationToken cancellationToken)
    {
        var holds = await context.StockReservations
            .AsNoTracking()
            .Where(r => r.CartId == cartId && r.Status == ReservationStatus.Active)
            .ToListAsync(cancellationToken);

        return holds
            .GroupBy(r => r.VariantId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
    }

    /// <summary>Reservation-aware per-cart-line available cap: <c>max(stock − reserved + this cart's own hold, 0)</c>.
    /// Use for EVERY cart response DTO that drives FE state so a held cart never sees a cap below its own quantity.</summary>
    public static int LineAvailable(ProductVariant? variant, Guid variantId, IReadOnlyDictionary<Guid, int> ownHolds)
        => Math.Max((variant?.StockQuantity ?? 0) - (variant?.ReservedQuantity ?? 0) + ownHolds.GetValueOrDefault(variantId), 0);
}
