using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Cart.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Orders.Commands;

public record ReorderCommand : IRequest<Result<ReorderResultDto>>
{
    public Guid OrderId { get; init; }
    public string? GuestSessionId { get; init; }
}

public class ReorderCommandValidator : AbstractValidator<ReorderCommand>
{
    public ReorderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");
    }
}

public class ReorderResultDto
{
    public CartDto Cart { get; init; } = null!;
    public int ItemsAdded { get; init; }
    public int ItemsSkipped { get; init; }
    public List<string> SkippedReasons { get; init; } = [];
}

public class ReorderCommandHandler : IRequestHandler<ReorderCommand, Result<ReorderResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ReorderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReorderResultDto>> Handle(
        ReorderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Get the order
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<ReorderResultDto>.Failure("Order not found");
        }

        // Verify user owns the order
        if (userId.HasValue && order.UserId != userId && !_currentUserService.IsAdmin)
        {
            return Result<ReorderResultDto>.Failure("Access denied");
        }

        if (!order.Items.Any())
        {
            return Result<ReorderResultDto>.Failure("Order has no items to reorder");
        }

        // Get or create cart
        var cart = await GetOrCreateCartAsync(userId, request.GuestSessionId, cancellationToken);

        // Get all products for the order items
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var itemsAdded = 0;
        var itemsSkipped = 0;
        var skippedReasons = new List<string>();

        foreach (var orderItem in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == orderItem.ProductId);

            // Check if product is available
            if (product == null || !product.IsActive)
            {
                itemsSkipped++;
                skippedReasons.Add($"'{orderItem.ProductName}' is no longer available");
                continue;
            }

            // Find variant
            var variant = product.Variants.FirstOrDefault(v => v.Id == orderItem.VariantId);
            if (variant == null || !variant.IsActive)
            {
                // Try to find an active variant
                variant = product.Variants.FirstOrDefault(v => v.IsActive);
                if (variant == null)
                {
                    itemsSkipped++;
                    skippedReasons.Add($"'{orderItem.ProductName}' has no available variants");
                    continue;
                }
            }

            // Check stock
            var requestedQty = orderItem.Quantity;
            var availableStock = variant.StockQuantity;

            // Check existing cart items
            var existingCartItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == product.Id && i.VariantId == variant.Id);
            var currentCartQty = existingCartItem?.Quantity ?? 0;

            var maxAddableQty = availableStock - currentCartQty;
            if (maxAddableQty <= 0)
            {
                itemsSkipped++;
                skippedReasons.Add($"'{orderItem.ProductName}' is already at max quantity in cart");
                continue;
            }

            var qtyToAdd = Math.Min(requestedQty, maxAddableQty);
            var price = product.BasePrice + variant.PriceAdjustment;

            if (existingCartItem != null)
            {
                existingCartItem.SetQuantity(existingCartItem.Quantity + qtyToAdd);
            }
            else
            {
                cart.AddItem(product.Id, variant.Id, qtyToAdd, price);
            }

            itemsAdded++;

            if (qtyToAdd < requestedQty)
            {
                skippedReasons.Add($"'{orderItem.ProductName}': only {qtyToAdd} of {requestedQty} added (limited stock)");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var cartDto = await MapCartToDto(cart, cancellationToken);

        return Result<ReorderResultDto>.Success(new ReorderResultDto
        {
            Cart = cartDto,
            ItemsAdded = itemsAdded,
            ItemsSkipped = itemsSkipped,
            SkippedReasons = skippedReasons
        });
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
            cart = new Core.Entities.Cart(userId, guestSessionId);
            _context.Carts.Add(cart);
        }

        return cart;
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
            var product = products.First(p => p.Id == item.ProductId);
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
