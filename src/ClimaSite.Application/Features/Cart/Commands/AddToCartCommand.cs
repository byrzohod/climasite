using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Cart.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Commands;

public record AddToCartCommand : IRequest<Result<CartDto>>
{
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int Quantity { get; init; } = 1;
    public string? GuestSessionId { get; init; }
    public string? Language { get; init; }
}

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Maximum quantity is 100");
    }
}

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddToCartCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CartDto>> Handle(
        AddToCartCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Verify product exists and is active
        var product = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (product == null)
        {
            return Result<CartDto>.Failure("Product not found or not available.");
        }

        // Check variant if specified
        Core.Entities.ProductVariant? variant = null;
        if (request.VariantId.HasValue)
        {
            variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId && v.IsActive);
            if (variant == null)
            {
                return Result<CartDto>.Failure("Product variant not found or not available.");
            }
        }
        else
        {
            // INV-01 A3: with no explicit variant, prefer the first active variant that actually has
            // reservation-adjusted availability, so "PDP shows in stock" (which aggregates active variants)
            // agrees with "add succeeds". Fall back to the first active variant when none is available, so
            // the availability check below produces the honest "Only 0 items available" message.
            // Deterministic order (SortOrder, Id) — must match GetProductBySlugQuery's variant order so the PDP's
            // default-variant availability agrees with the variant add-to-cart actually selects (INV-01 A3).
            var orderedVariants = product.Variants.OrderBy(v => v.SortOrder).ThenBy(v => v.Id).ToList();
            variant = orderedVariants.FirstOrDefault(v => v.IsActive && v.StockQuantity - v.ReservedQuantity > 0)
                      ?? orderedVariants.FirstOrDefault(v => v.IsActive);
        }

        if (variant == null)
        {
            return Result<CartDto>.Failure("No available variants for this product.");
        }

        // Get or create the cart first so the availability ceiling can add back THIS cart's own hold (A3).
        var cart = await GetOrCreateCartAsync(userId, request.GuestSessionId, cancellationToken);

        // Check stock (INV-01 A3: reservation-aware — units held by OTHER carts' in-flight checkouts are not
        // available to add; this cart's own Active hold is added back so an existing line can re-grow up to
        // what it holds — 0 for the common pre-checkout cart. Advisory only, no hold is taken here). The
        // accumulated-quantity check below reuses this same reservation-adjusted ceiling.
        var ownHolds = await CartReservationAvailability.GetOwnActiveHoldsAsync(_context, cart.Id, cancellationToken);
        var availableStock = Math.Max(
            variant.StockQuantity - variant.ReservedQuantity + ownHolds.GetValueOrDefault(variant.Id), 0);
        if (availableStock < request.Quantity)
        {
            return Result<CartDto>.Failure($"Only {availableStock} items available in stock.");
        }

        // Check if item already in cart
        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId && i.VariantId == variant.Id);

        var price = product.BasePrice + variant.PriceAdjustment;

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > availableStock)
            {
                return Result<CartDto>.Failure($"Cannot add more items. Only {availableStock} available.");
            }
            existingItem.SetQuantity(newQuantity);
        }
        else
        {
            cart.AddItem(request.ProductId, variant.Id, request.Quantity, price);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CartDto>.Success(await MapCartToDto(cart, request.Language, cancellationToken));
    }

    private async Task<Core.Entities.Cart> GetOrCreateCartAsync(
        Guid? userId,
        string? guestSessionId,
        CancellationToken cancellationToken)
    {
        Core.Entities.Cart? cart = null;

        if (userId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(guestSessionId))
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == guestSessionId, cancellationToken);
        }

        if (cart == null)
        {
            // TODO: Implement guest cart expiration/cleanup job
            // Guest carts should be cleaned up after a configurable period (e.g., 30 days)
            // to prevent database bloat from abandoned sessions
            cart = new Core.Entities.Cart(userId, guestSessionId);
            _context.Carts.Add(cart);
        }

        return cart;
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
            var product = products.First(p => p.Id == item.ProductId);
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
                // INV-01 A3: reservation-aware per-line cap (adds back this cart's own hold), consistent with GetCartQuery.
                AvailableStock = CartReservationAvailability.LineAvailable(variant, item.VariantId, ownHolds),
                IsAvailable = variant != null && variant.IsActive && product.IsActive
            };
        }).ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        // TODO: Make VAT rate configurable per country (currently using 20% EU average)
        var tax = Math.Round(subtotal * 0.20m, 2); // 20% VAT

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
