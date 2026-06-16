using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    public string? PaymentIntentId { get; init; }
    public string? PaymentMethod { get; init; }
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
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Validate user context - either authenticated user OR guest session required
        if (!userId.HasValue && string.IsNullOrEmpty(request.GuestSessionId))
        {
            return Result<OrderDto>.Failure("Either user authentication or guest session ID is required");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy is null
            ? await CreateOrderAsync()
            : await strategy.ExecuteAsync(CreateOrderAsync);

        async Task<Result<OrderDto>> CreateOrderAsync()
        {
            // Use explicit transaction with proper isolation to ensure data integrity
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

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
                    return Result<OrderDto>.Failure("Product variant is no longer available");
                }

                if (variant.StockQuantity < item.Quantity)
                {
                    return Result<OrderDto>.Failure($"Insufficient stock for '{product.Name}'");
                }
            }

            // Generate order number using timestamp + random suffix for uniqueness (race-condition safe)
            var orderNumber = GenerateUniqueOrderNumber();

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
            order.SetCurrency(CheckoutPricing.Currency);

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

                // BUG-05: decrement stock ATOMICALLY with a `stock >= qty` guard so two
                // concurrent orders for the last unit can't both succeed. The previous
                // read-then-write (AdjustStock) let both pass the earlier check and oversell.
                // Zero rows affected means another order took the stock first; the surrounding
                // transaction rolls back any earlier decrements in this order.
                var stockUpdated = await _context.TryDecrementVariantStockAsync(
                    cartItem.VariantId, cartItem.Quantity, cancellationToken);

                if (stockUpdated == 0)
                {
                    return Result<OrderDto>.Failure($"Insufficient stock for '{product.Name}'");
                }
            }

            // Pricing comes from the shared CheckoutPricing helper so the amount
            // displayed at checkout, charged via Stripe, and the persisted order
            // total are always computed identically (BUG-02).
            order.SetShippingCost(CheckoutPricing.GetShippingCost(request.ShippingMethod));
            order.SetTaxAmount(CheckoutPricing.GetTax(order.Subtotal));

            // BUG-01: a card order MUST be backed by a verified PaymentIntent. Reject a card
            // order that arrives without one — otherwise it would create an unpaid,
            // stock-depleting Pending order. Offline methods (e.g. bank transfer) are handled
            // separately and may legitimately remain pending-payment (see GAP-06).
            if (string.Equals(request.PaymentMethod, "card", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return Result<OrderDto>.Failure("A verified card payment is required to place this order.");
            }

            // BUG-01: verify the Stripe PaymentIntent server-side before persisting
            // the order. We never trust the client that the payment actually
            // succeeded for the correct amount and currency.
            if (!string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                var pi = await _paymentService.GetPaymentIntentAsync(request.PaymentIntentId);

                if (!pi.Succeeded || pi.Status != "succeeded")
                {
                    return Result<OrderDto>.Failure("Payment could not be verified");
                }

                if (!string.Equals(pi.Currency, CheckoutPricing.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "PaymentIntent {PaymentIntentId} currency mismatch: expected {Expected} but was {Actual}",
                        request.PaymentIntentId, CheckoutPricing.Currency, pi.Currency);
                    return Result<OrderDto>.Failure("Payment could not be verified");
                }

                var expectedMinorUnits = CheckoutPricing.ToMinorUnits(order.Total);
                if (pi.Amount != expectedMinorUnits)
                {
                    _logger.LogWarning(
                        "PaymentIntent {PaymentIntentId} amount mismatch: charged {Charged} but expected {Expected}",
                        request.PaymentIntentId, pi.Amount, expectedMinorUnits);
                    return Result<OrderDto>.Failure("Payment could not be verified");
                }

                // Persist the verified payment reference so the webhook can match
                // the order and flip it to Paid. Status stays Pending here.
                order.SetPaymentInfo(request.PaymentIntentId, request.PaymentMethod ?? "card");
            }

            _context.Orders.Add(order);

            // Clear cart
            cart.Clear();

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message?.Contains("payment_intent_id", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Unique-index violation on payment_intent_id: this intent already backs an
                // order (idempotency guard). Any OTHER DbUpdateException propagates so real
                // failures aren't masked and transient errors can still be retried.
                _logger.LogWarning(
                    ex,
                    "Duplicate order creation rejected for PaymentIntent {PaymentIntentId}",
                    request.PaymentIntentId);
                return Result<OrderDto>.Failure("This payment has already been used for an order.");
            }

            await transaction.CommitAsync(cancellationToken);

            return Result<OrderDto>.Success(MapToDto(order, products));
        }
    }

    /// <summary>
    /// Generates a unique order number using timestamp + random suffix.
    /// Format: ORD-YYYYMMDD-HHMMSS-XXXX where XXXX is a random alphanumeric suffix.
    /// This approach is race-condition safe as it doesn't rely on counting existing orders.
    /// </summary>
    private static string GenerateUniqueOrderNumber()
    {
        var now = DateTime.UtcNow;
        var randomSuffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
        return $"ORD-{now:yyyyMMdd}-{now:HHmmss}-{randomSuffix}";
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
