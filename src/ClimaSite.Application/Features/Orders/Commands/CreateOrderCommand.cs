using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Orders.Commands;

public record CreateOrderCommand : IRequest<Result<OrderDto>>
{
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public AddressDto ShippingAddress { get; init; } = null!;
    public AddressDto? BillingAddress { get; init; }
    public string ShippingMethod { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? GuestSessionId { get; init; }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required");

        RuleFor(x => x.ShippingAddress.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.AddressLine1)
            .NotEmpty().WithMessage("Address is required")
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.City)
            .NotEmpty().WithMessage("City is required")
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingAddress.Country)
            .NotEmpty().WithMessage("Country is required")
            .When(x => x.ShippingAddress != null);

        RuleFor(x => x.ShippingMethod)
            .NotEmpty().WithMessage("Shipping method is required");
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Get cart
        Core.Entities.Cart? cart = null;
        if (userId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(request.GuestSessionId))
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == request.GuestSessionId, cancellationToken);
        }

        if (cart == null || !cart.Items.Any())
        {
            return Result<OrderDto>.Failure("Cart is empty");
        }

        // Validate stock and get product data
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in cart.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null || !product.IsActive)
            {
                return Result<OrderDto>.Failure($"Product '{item.ProductId}' is no longer available");
            }

            var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            if (variant == null || !variant.IsActive)
            {
                return Result<OrderDto>.Failure($"Product variant is no longer available");
            }

            if (variant.StockQuantity < item.Quantity)
            {
                return Result<OrderDto>.Failure($"Insufficient stock for '{product.Name}'");
            }
        }

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        // Create order
        var order = new Order(orderNumber, request.CustomerEmail);
        order.SetUser(userId);
        order.SetCustomerPhone(request.CustomerPhone);
        order.SetShippingAddress(ConvertAddressToDict(request.ShippingAddress));
        if (request.BillingAddress != null)
        {
            order.SetBillingAddress(ConvertAddressToDict(request.BillingAddress));
        }
        order.SetShippingMethod(request.ShippingMethod);
        order.SetNotes(request.Notes);
        order.SetCurrency("EUR");

        // Add order items
        foreach (var cartItem in cart.Items)
        {
            var product = products.First(p => p.Id == cartItem.ProductId);
            var variant = product.Variants.First(v => v.Id == cartItem.VariantId);

            order.AddItem(
                cartItem.ProductId,
                cartItem.VariantId,
                product.Name,
                variant.Name ?? "",
                variant.Sku,
                cartItem.Quantity,
                cartItem.UnitPrice
            );

            // Reduce stock
            variant.AdjustStock(-cartItem.Quantity);
        }

        // Calculate shipping cost (simplified)
        var shippingCost = request.ShippingMethod switch
        {
            "express" => 15.99m,
            "standard" => 5.99m,
            "free" => 0m,
            _ => 9.99m
        };
        order.SetShippingCost(shippingCost);

        // Calculate tax (20% VAT)
        var taxAmount = Math.Round(order.Subtotal * 0.20m, 2);
        order.SetTaxAmount(taxAmount);

        _context.Orders.Add(order);

        // Clear cart
        cart.Clear();

        await _context.SaveChangesAsync(cancellationToken);

        return Result<OrderDto>.Success(MapToDto(order, products));
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Orders
            .CountAsync(o => o.CreatedAt.Year == year, cancellationToken);

        return $"ORD-{year}-{(count + 1):D6}";
    }

    private static Dictionary<string, object> ConvertAddressToDict(AddressDto address)
    {
        return new Dictionary<string, object>
        {
            ["firstName"] = address.FirstName,
            ["lastName"] = address.LastName,
            ["addressLine1"] = address.AddressLine1,
            ["addressLine2"] = address.AddressLine2 ?? "",
            ["city"] = address.City,
            ["state"] = address.State ?? "",
            ["postalCode"] = address.PostalCode,
            ["country"] = address.Country,
            ["phone"] = address.Phone ?? ""
        };
    }

    private static OrderDto MapToDto(Order order, List<Product> products)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            Status = order.Status.ToString(),
            Subtotal = order.Subtotal,
            ShippingCost = order.ShippingCost,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            Total = order.Total,
            Currency = order.Currency,
            ShippingMethod = order.ShippingMethod,
            TrackingNumber = order.TrackingNumber,
            PaymentMethod = order.PaymentMethod,
            PaidAt = order.PaidAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            Notes = order.Notes,
            Items = order.Items.Select(i =>
            {
                var product = products.FirstOrDefault(p => p.Id == i.ProductId);
                var variant = product?.Variants.FirstOrDefault(v => v.Id == i.VariantId);
                var image = product?.Images.FirstOrDefault(img => img.IsPrimary);

                return new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    VariantId = i.VariantId,
                    ProductName = i.ProductName,
                    VariantName = i.VariantName,
                    Sku = i.Sku,
                    ImageUrl = image?.Url,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                };
            }).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
