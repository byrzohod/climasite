using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Orders.Commands;

public record CancelOrderCommand : IRequest<Result<OrderDto>>
{
    public Guid OrderId { get; init; }
    public string? CancellationReason { get; init; }
}

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");
    }
}

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CancelOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<OrderDto>.Failure("Order not found");
        }

        // Check if user owns the order (unless admin)
        if (userId.HasValue && order.UserId != userId && !_currentUserService.IsAdmin)
        {
            return Result<OrderDto>.Failure("Access denied");
        }

        // Check if order can be cancelled
        if (!order.CanBeCancelled)
        {
            return Result<OrderDto>.Failure($"Order cannot be cancelled. Current status: {order.Status}");
        }

        // Restore stock for all items
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            var variant = product?.Variants.FirstOrDefault(v => v.Id == item.VariantId);
            if (variant != null)
            {
                variant.AdjustStock(item.Quantity); // Add back the stock
            }
        }

        // Set cancellation
        order.SetCancellationReason(request.CancellationReason);
        order.SetStatus(OrderStatus.Cancelled);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<OrderDto>.Success(MapToDto(order, products));
    }

    private static OrderDto MapToDto(Order order, List<Product> products)
    {
        AddressDto? shippingAddress = null;
        if (order.ShippingAddress.Any())
        {
            shippingAddress = new AddressDto
            {
                FirstName = order.ShippingAddress.GetValueOrDefault("firstName")?.ToString() ?? "",
                LastName = order.ShippingAddress.GetValueOrDefault("lastName")?.ToString() ?? "",
                AddressLine1 = order.ShippingAddress.GetValueOrDefault("addressLine1")?.ToString() ?? "",
                AddressLine2 = order.ShippingAddress.GetValueOrDefault("addressLine2")?.ToString(),
                City = order.ShippingAddress.GetValueOrDefault("city")?.ToString() ?? "",
                State = order.ShippingAddress.GetValueOrDefault("state")?.ToString(),
                PostalCode = order.ShippingAddress.GetValueOrDefault("postalCode")?.ToString() ?? "",
                Country = order.ShippingAddress.GetValueOrDefault("country")?.ToString() ?? "",
                Phone = order.ShippingAddress.GetValueOrDefault("phone")?.ToString()
            };
        }

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
            ShippingAddress = shippingAddress,
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
            Events = order.Events.OrderByDescending(e => e.CreatedAt).Select(e => new OrderEventDto
            {
                Id = e.Id,
                OrderId = e.OrderId,
                Status = e.Status.ToString(),
                Description = e.Description,
                Notes = e.Notes,
                CreatedAt = e.CreatedAt
            }).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
