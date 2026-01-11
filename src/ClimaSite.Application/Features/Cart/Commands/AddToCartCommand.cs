using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
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
            // Use default variant if product has variants
            variant = product.Variants.FirstOrDefault(v => v.IsActive);
        }

        if (variant == null)
        {
            return Result<CartDto>.Failure("No available variants for this product.");
        }

        // Check stock
        var availableStock = variant.StockQuantity;
        if (availableStock < request.Quantity)
        {
            return Result<CartDto>.Failure($"Only {availableStock} items available in stock.");
        }

        // Get or create cart
        var cart = await GetOrCreateCartAsync(userId, request.GuestSessionId, cancellationToken);

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

        return Result<CartDto>.Success(await MapCartToDto(cart, cancellationToken));
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
