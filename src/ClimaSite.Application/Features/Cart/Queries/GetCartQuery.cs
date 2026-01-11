using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Cart.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Queries;

public record GetCartQuery : IRequest<CartDto?>
{
    public Guid? UserId { get; init; }
    public string? GuestSessionId { get; init; }
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
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.GuestSessionId))
        {
            cart = await _context.Carts
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

        return await MapCartToDto(cart, cancellationToken);
    }

    private async Task<CartDto> MapCartToDto(Core.Entities.Cart cart, CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

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

            return new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                VariantName = variant?.Name,
                Sku = variant?.Sku,
                ImageUrl = primaryImage?.Url,
                UnitPrice = variant != null ? product.BasePrice + variant.PriceAdjustment : product.BasePrice,
                SalePrice = product.CompareAtPrice,
                EffectivePrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity,
                AvailableStock = variant?.StockQuantity ?? 0,
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
