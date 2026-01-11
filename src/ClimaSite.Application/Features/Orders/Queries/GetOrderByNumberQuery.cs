using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Orders.Queries;

public record GetOrderByNumberQuery : IRequest<Result<OrderDto>>
{
    public string OrderNumber { get; init; } = string.Empty;
}

public class GetOrderByNumberQueryHandler : IRequestHandler<GetOrderByNumberQuery, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByNumberQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(
        GetOrderByNumberQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber, cancellationToken);

        if (order == null)
        {
            return Result<OrderDto>.Failure("Order not found");
        }

        // Check if user owns the order (unless admin)
        if (userId.HasValue && order.UserId != userId && !_currentUserService.IsAdmin)
        {
            return Result<OrderDto>.Failure("Access denied");
        }

        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return Result<OrderDto>.Success(MapToDto(order, products));
    }

    private static OrderDto MapToDto(Core.Entities.Order order, List<Core.Entities.Product> products)
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
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
