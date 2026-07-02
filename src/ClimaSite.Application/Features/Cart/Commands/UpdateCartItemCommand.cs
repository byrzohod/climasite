using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Cart.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Commands;

public record UpdateCartItemCommand : IRequest<Result<CartDto>>
{
    public Guid ItemId { get; init; }
    public int Quantity { get; init; }
    public string? GuestSessionId { get; init; }
    public string? Language { get; init; }
}

public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Cart item ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity must be 0 or greater")
            .LessThanOrEqualTo(100).WithMessage("Maximum quantity is 100");
    }
}

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStockReservationService _reservations;

    public UpdateCartItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IStockReservationService reservations)
    {
        _context = context;
        _currentUserService = currentUserService;
        _reservations = reservations;
    }

    public async Task<Result<CartDto>> Handle(
        UpdateCartItemCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var cart = await GetCartAsync(userId, request.GuestSessionId, cancellationToken);
        if (cart == null)
        {
            return Result<CartDto>.Failure("Cart not found.");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == request.ItemId);
        if (item == null)
        {
            return Result<CartDto>.Failure("Cart item not found.");
        }

        if (request.Quantity == 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            // INV-01 A3: only an INCREASE needs an availability check. A decrease/no-change never needs
            // more stock, and guarding it against (stock − reserved) would wrongly block shrinking a cart
            // that itself holds the reservation (e.g. this cart holds 7/10 and wants to drop to 4). The
            // reservation-aware ceiling is advisory (no hold is taken here) and applies to growth only.
            if (request.Quantity > item.Quantity)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == item.VariantId, cancellationToken);

                if (variant != null)
                {
                    // Add back this cart's OWN Active hold so re-growing up to what it already holds isn't
                    // blocked by its own reservation (0 for a pre-checkout cart with no hold).
                    var ownHolds = await CartReservationAvailability.GetOwnActiveHoldsAsync(_context, cart.Id, cancellationToken);
                    var ownHold = ownHolds.GetValueOrDefault(item.VariantId);
                    var availableStock = Math.Max(variant.StockQuantity - variant.ReservedQuantity + ownHold, 0);
                    if (availableStock < request.Quantity)
                    {
                        return Result<CartDto>.Failure($"Only {availableStock} items available in stock.");
                    }
                }
            }

            item.SetQuantity(request.Quantity);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // INV-01 A2: shrink/release any Active checkout holds to the cart's new quantities (a decrease or removal;
        // never grows). Usually a no-op pre-checkout. Runs after SaveChanges so the ledger reflects the new state.
        var quantities = cart.Items.ToDictionary(i => i.VariantId, i => i.Quantity);
        await _reservations.ReconcileCartToQuantitiesAsync(cart.Id, quantities, cancellationToken);

        return Result<CartDto>.Success(await MapCartToDto(cart, request.Language, cancellationToken));
    }

    private async Task<Core.Entities.Cart?> GetCartAsync(
        Guid? userId,
        string? guestSessionId,
        CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(guestSessionId))
        {
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == guestSessionId, cancellationToken);
        }

        return null;
    }

    private async Task<CartDto> MapCartToDto(Core.Entities.Cart cart, string? language, CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Translations)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // INV-01 A3: this cart's own Active holds so every returned line's available cap adds them back.
        var ownHolds = await CartReservationAvailability.GetOwnActiveHoldsAsync(_context, cart.Id, cancellationToken);

        var items = cart.Items.Select(item =>
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            var variant = product?.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            var primaryImage = product?.Images.FirstOrDefault(i => i.IsPrimary);
            var productName = product?.GetTranslatedContent(language).Name ?? "Product unavailable";

            return new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = productName,
                VariantName = variant?.Name,
                Sku = variant?.Sku,
                ImageUrl = primaryImage?.Url,
                UnitPrice = product != null && variant != null ? product.BasePrice + variant.PriceAdjustment : item.UnitPrice,
                SalePrice = product != null ? ProductPricing.GetSalePrice(product.BasePrice, product.CompareAtPrice) : null,
                EffectivePrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity,
                // INV-01 A3: reservation-aware per-line cap (adds back this cart's own hold), consistent with GetCartQuery.
                AvailableStock = CartReservationAvailability.LineAvailable(variant, item.VariantId, ownHolds),
                IsAvailable = variant != null && variant.IsActive && (product?.IsActive ?? false)
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
            UpdatedAt = cart.UpdatedAt
        };
    }
}
