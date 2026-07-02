using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Cart.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Queries;

public record GetCartQuery : IRequest<CartDto?>
{
    public Guid? UserId { get; init; }
    public string? GuestSessionId { get; init; }
    public string? Language { get; init; }
}

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        Core.Entities.Cart? cart = null;

        if (request.UserId.HasValue)
        {
            cart = await _context.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.GuestSessionId))
        {
            cart = await _context.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == request.GuestSessionId && DateTime.UtcNow <= c.ExpiresAt, cancellationToken);
        }

        if (cart == null || !cart.Items.Any())
        {
            return new CartDto
            {
                Id = Guid.Empty,
                UserId = request.UserId,
                GuestSessionId = request.GuestSessionId,
                Items = [],
                Subtotal = 0,
                Tax = 0,
                Total = 0,
                ItemCount = 0,
                UpdatedAt = DateTime.UtcNow
            };
        }

        return await MapCartToDto(cart, request.Language, cancellationToken);
    }

    private async Task<CartDto> MapCartToDto(Core.Entities.Cart cart, string? language, CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Translations)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // INV-01 A3: add this cart's OWN Active holds back into each line's ceiling so a held cart's
        // MaxQuantity never sits below what it already holds (0 for the common pre-checkout cart).
        var ownHolds = await CartReservationAvailability.GetOwnActiveHoldsAsync(_context, cart.Id, cancellationToken);

        var items = cart.Items.Select(item =>
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null)
            {
                return new CartItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ProductName = "Product unavailable",
                    VariantName = null,
                    Sku = null,
                    ImageUrl = null,
                    UnitPrice = item.UnitPrice,
                    SalePrice = null,
                    EffectivePrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity,
                    AvailableStock = 0,
                    IsAvailable = false
                };
            }

            var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary);
            var (productName, _, _, _, _) = product.GetTranslatedContent(language);

            return new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = productName,
                ProductSlug = product.Slug,
                VariantName = variant?.Name,
                Sku = variant?.Sku,
                ImageUrl = primaryImage?.Url,
                UnitPrice = variant != null ? product.BasePrice + variant.PriceAdjustment : product.BasePrice,
                SalePrice = ProductPricing.GetSalePrice(product.BasePrice, product.CompareAtPrice),
                EffectivePrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity,
                // INV-01 A3: per-line available cap excludes units held by OTHER carts' Active reservations,
                // but adds back this cart's own hold so the FE qty cap never falls below its held quantity.
                AvailableStock = Math.Max(
                    (variant?.StockQuantity ?? 0) - (variant?.ReservedQuantity ?? 0)
                        + ownHolds.GetValueOrDefault(item.VariantId),
                    0),
                IsAvailable = variant != null && variant.IsActive && product.IsActive
            };
        }).ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var tax = Math.Round(subtotal * 0.20m, 2);

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            GuestSessionId = cart.SessionId,
            Items = items,
            Subtotal = subtotal,
            Tax = tax,
            Total = subtotal + tax,
            ItemCount = items.Sum(i => i.Quantity),
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt
        };
    }
}
