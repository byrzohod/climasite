using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Orders.Queries;

public record GetAdminOrderByIdQuery : IRequest<AdminOrderDetailDto?>
{
    public Guid Id { get; init; }
}

public class GetAdminOrderByIdQueryHandler : IRequestHandler<GetAdminOrderByIdQuery, AdminOrderDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAdminOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminOrderDetailDto?> Handle(GetAdminOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
        {
            return null;
        }

        // Get product images for order items
        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var productImages = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Include(p => p.Images)
            .ToDictionaryAsync(
                p => p.Id,
                p => p.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                cancellationToken);

        return new AdminOrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.User != null
                ? $"{order.User.FirstName} {order.User.LastName}".Trim()
                : order.CustomerEmail,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            Status = order.Status.ToString(),
            Subtotal = order.Subtotal,
            ShippingCost = order.ShippingCost,
            TaxAmount = order.TaxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.Total,
            Currency = order.Currency,
            ShippingAddress = order.ShippingAddress,
            BillingAddress = order.BillingAddress,
            ShippingMethod = order.ShippingMethod,
            TrackingNumber = order.TrackingNumber,
            PaymentMethod = order.PaymentMethod,
            PaidAt = order.PaidAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            Notes = order.Notes,
            Items = order.Items.Select(i => new AdminOrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                ProductName = i.ProductName,
                VariantName = i.VariantName,
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                ImageUrl = productImages.GetValueOrDefault(i.ProductId)
            }).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
