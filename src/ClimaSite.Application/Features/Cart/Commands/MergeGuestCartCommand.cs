using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Cart.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Cart.Commands;

public record MergeGuestCartCommand : IRequest<Result<CartDto>>
{
    public string GuestSessionId { get; init; } = string.Empty;
}

public class MergeGuestCartCommandValidator : AbstractValidator<MergeGuestCartCommand>
{
    public MergeGuestCartCommandValidator()
    {
        RuleFor(x => x.GuestSessionId)
            .NotEmpty().WithMessage("Guest session ID is required");
    }
}

public class MergeGuestCartCommandHandler : IRequestHandler<MergeGuestCartCommand, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MergeGuestCartCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CartDto>> Handle(
        MergeGuestCartCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (!userId.HasValue)
        {
            return Result<CartDto>.Failure("User must be authenticated to merge carts");
        }

        // Get the guest cart
        var guestCart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == request.GuestSessionId, cancellationToken);

        if (guestCart == null || !guestCart.Items.Any())
        {
            // No guest cart to merge, return user's cart or empty cart
            var existingUserCart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (existingUserCart != null)
            {
                return Result<CartDto>.Success(await MapCartToDto(existingUserCart, cancellationToken));
            }

            return Result<CartDto>.Success(new CartDto
            {
                UserId = userId,
                Items = new List<CartItemDto>(),
                Subtotal = 0,
                Tax = 0,
                Total = 0,
                ItemCount = 0
            });
        }

        // Get or create user cart
        var userCart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (userCart == null)
        {
            userCart = new Core.Entities.Cart(userId, null);
            _context.Carts.Add(userCart);
        }

        // Get product data for stock validation
        var productIds = guestCart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Merge guest cart items into user cart
        foreach (var guestItem in guestCart.Items)
        {
            var existingItem = userCart.Items.FirstOrDefault(i =>
                i.ProductId == guestItem.ProductId && i.VariantId == guestItem.VariantId);

            var product = products.FirstOrDefault(p => p.Id == guestItem.ProductId);
            var variant = product?.Variants.FirstOrDefault(v => v.Id == guestItem.VariantId);
            var availableStock = variant?.StockQuantity ?? 0;

            if (existingItem != null)
            {
                // Merge quantities (up to available stock)
                var newQuantity = Math.Min(existingItem.Quantity + guestItem.Quantity, availableStock);
                if (newQuantity > 0)
                {
                    existingItem.SetQuantity(newQuantity);
                }
            }
            else
            {
                // Add new item (up to available stock)
                var quantity = Math.Min(guestItem.Quantity, availableStock);
                if (quantity > 0)
                {
                    userCart.AddItem(guestItem.ProductId, guestItem.VariantId, quantity, guestItem.UnitPrice);
                }
            }
        }

        // Remove the guest cart
        _context.Carts.Remove(guestCart);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CartDto>.Success(await MapCartToDto(userCart, cancellationToken));
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
