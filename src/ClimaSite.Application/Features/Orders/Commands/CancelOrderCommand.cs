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
    private readonly IStockReservationService _reservations;

    public CancelOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IStockReservationService reservations)
    {
        _context = context;
        _currentUserService = currentUserService;
        _reservations = reservations;
    }

    public async Task<Result<OrderDto>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy is null
            ? await CancelOrderAsync()
            : await strategy.ExecuteAsync(CancelOrderAsync);

        async Task<Result<OrderDto>> CancelOrderAsync()
        {
            // Re-derive from committed state on every execution-strategy attempt (the retry reuses this scoped
            // context and does not reset the tracker on a rollback) — needed now that restock/release go through
            // raw from-state SQL alongside the tracked order.SetStatus.
            _context.ClearChangeTracker();

            // Use explicit transaction to ensure stock restoration and order status update are atomic
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // INV-01 B [council High #2]: lock the ORDER row FIRST (before any variant lock), then RE-READ the
            // order under it — so the restock-vs-release branch below is decided against the order's committed
            // status, never a stale snapshot that races a concurrent admin mark-paid (which also takes this lock).
            await _context.LockOrderForUpdateAsync(request.OrderId, cancellationToken);

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

            if (!userId.HasValue && !_currentUserService.IsAdmin)
            {
                return Result<OrderDto>.Failure("Authentication required");
            }

            // Check if order can be cancelled
            if (!order.CanBeCancelled)
            {
                return Result<OrderDto>.Failure($"Order cannot be cancelled. Current status: {order.Status}");
            }

            // Load the order's products (for the response DTO's images/variants).
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // INV-01 B: restock depends on whether stock was physically taken. An unpaid bank-transfer order still
            // HOLDS its stock (reserved, never decremented) → RELEASE the hold (drops reserved, no restock). Any
            // other cancellable order — a card order, or a bank order already marked Paid — had its stock
            // physically decremented, so restock it atomically (ExecuteUpdate, never the tracked AdjustStock which
            // is a lost-update footgun under a concurrent decrement).
            var hasActiveBankHold = await _context.StockReservations.AsNoTracking().AnyAsync(
                r => r.OrderId == order.Id && r.Kind == ReservationKind.BankTransfer
                  && r.Status == ReservationStatus.Active,
                cancellationToken);

            if (hasActiveBankHold)
            {
                await _reservations.ReleaseBankOrderAsync(order.Id, cancellationToken);
            }
            else
            {
                foreach (var item in order.Items.OrderBy(i => i.VariantId))
                {
                    await _context.IncrementVariantStockAsync(item.VariantId, item.Quantity, cancellationToken);
                }
            }

            // Set cancellation
            order.SetCancellationReason(request.CancellationReason);
            order.SetStatus(OrderStatus.Cancelled);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<OrderDto>.Success(MapToDto(order, products));
        }
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
