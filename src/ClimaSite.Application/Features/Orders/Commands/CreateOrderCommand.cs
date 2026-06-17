using System.Globalization;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Orders.DTOs;
using ClimaSite.Application.Features.Outbox;
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

        // GAP-06: only the real payment methods are accepted. The fake "paypal" option (and any
        // unknown value) is rejected here so it can never reach an order.
        RuleFor(x => x.PaymentMethod)
            .Must(BeASupportedPaymentMethod)
            .WithMessage("Payment method must be either 'card' or 'bank'.")
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentMethod));
    }

    private static bool BeASupportedPaymentMethod(string? paymentMethod) =>
        string.Equals(paymentMethod, "card", StringComparison.OrdinalIgnoreCase)
        || string.Equals(paymentMethod, "bank", StringComparison.OrdinalIgnoreCase);
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private const string BankPaymentMethod = "bank";

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService;
    private readonly IEmailOutbox _emailOutbox;
    private readonly BankTransferOptions _bankTransferOptions;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        IEmailOutbox emailOutbox,
        BankTransferOptions bankTransferOptions,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _emailOutbox = emailOutbox;
        _bankTransferOptions = bankTransferOptions;
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
                // Cart could have been cleared (another tab) after the card was charged;
                // refund rather than orphan the charge.
                await RefundOrphanedChargeAsync(request.PaymentIntentId);
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
                    // The card may already be charged (the client confirms before this call);
                    // refund rather than orphan the charge if the product is now unavailable.
                    await RefundOrphanedChargeAsync(request.PaymentIntentId);
                    return Result<OrderDto>.Failure($"Product '{item.ProductId}' is no longer available");
                }

                var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
                if (variant == null || !variant.IsActive)
                {
                    await RefundOrphanedChargeAsync(request.PaymentIntentId);
                    return Result<OrderDto>.Failure("Product variant is no longer available");
                }

                // BUG-04: do NOT short-circuit on stock here. The card is already charged by
                // the time this handler runs, and the authoritative stock gate is the atomic
                // decrement in Loop 2 (after intent verification), which refunds the charge if
                // stock is unavailable. An early read-only check here would return "Insufficient
                // stock" before verification and leave the charge orphaned.
            }

            // Generate order number using timestamp + random suffix for uniqueness (race-condition safe)
            var orderNumber = GenerateUniqueOrderNumber();

            // Create order
            var order = new Order(orderNumber, request.CustomerEmail);
            order.SetUser(userId);

            // GAP-07: guest orders get a high-entropy access token so the buyer can view their
            // confirmation without an account. It is returned only on this creation response.
            if (!userId.HasValue)
            {
                order.SetGuestAccessToken(GenerateGuestAccessToken());
            }

            order.SetCustomerPhone(request.CustomerPhone);
            order.SetShippingAddress(ConvertAddressToDict(request.ShippingAddress));
            if (request.BillingAddress != null)
            {
                order.SetBillingAddress(ConvertAddressToDict(request.BillingAddress));
            }
            order.SetShippingMethod(request.ShippingMethod);
            order.SetNotes(request.Notes);
            order.SetCurrency(CheckoutPricing.Currency);

            // BUG-04: verify (and record) the PaymentIntent BEFORE any stock is taken so a
            // failed verification never leaves an orphaned charge. Loop 1 only builds the
            // order lines (setting Subtotal); stock is decremented later in Loop 2, after the
            // charge has been verified. Any post-verification failure refunds the charge.

            // Loop 1: add order items (sets Subtotal; NO stock change yet).
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
            }

            // Pricing comes from the shared CheckoutPricing helper so the amount
            // displayed at checkout, charged via Stripe, and the persisted order
            // total are always computed identically (BUG-02). Total is now final, so the
            // verified intent amount can be compared against it below.
            order.SetShippingCost(CheckoutPricing.GetShippingCost(request.ShippingMethod));
            order.SetTaxAmount(CheckoutPricing.GetTax(order.Subtotal));

            // BUG-01: a card order MUST be backed by a verified PaymentIntent. Reject a card
            // order that arrives without one — otherwise it would create an unpaid,
            // stock-depleting Pending order. Nothing has been charged in this case, so there
            // is no refund to issue. Offline methods (e.g. bank transfer) are handled
            // separately and may legitimately remain pending-payment (see GAP-06).
            if (string.Equals(request.PaymentMethod, "card", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return Result<OrderDto>.Failure("A verified card payment is required to place this order.");
            }

            // BUG-01: verify the Stripe PaymentIntent server-side before persisting
            // the order. We never trust the client that the payment actually
            // succeeded for the correct amount and currency.
            // BUG-04: the card was already charged client-side, so any verification failure
            // must refund the intent — otherwise the charge is orphaned (captured with no order).
            if (!string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                var pi = await _paymentService.GetPaymentIntentAsync(request.PaymentIntentId);

                if (!pi.Succeeded || pi.Status != "succeeded")
                {
                    await RefundOrphanedChargeAsync(request.PaymentIntentId);
                    return Result<OrderDto>.Failure("Payment could not be verified");
                }

                if (!string.Equals(pi.Currency, CheckoutPricing.Currency, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "PaymentIntent {PaymentIntentId} currency mismatch: expected {Expected} but was {Actual}",
                        request.PaymentIntentId, CheckoutPricing.Currency, pi.Currency);
                    await RefundOrphanedChargeAsync(request.PaymentIntentId);
                    return Result<OrderDto>.Failure("Payment could not be verified");
                }

                var expectedMinorUnits = CheckoutPricing.ToMinorUnits(order.Total);
                if (pi.Amount != expectedMinorUnits)
                {
                    _logger.LogWarning(
                        "PaymentIntent {PaymentIntentId} amount mismatch: charged {Charged} but expected {Expected}",
                        request.PaymentIntentId, pi.Amount, expectedMinorUnits);
                    await RefundOrphanedChargeAsync(request.PaymentIntentId);
                    return Result<OrderDto>.Failure("Payment could not be verified");
                }

                // Persist the verified payment reference so the webhook can match
                // the order and flip it to Paid. Status stays Pending here.
                order.SetPaymentInfo(request.PaymentIntentId, request.PaymentMethod ?? "card");
            }
            else if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                // GAP-06: offline order (e.g. bank transfer) — no PaymentIntent to verify, but the
                // chosen method must still be persisted so the buyer and admin see how it will be
                // paid. The order legitimately stays Pending until payment is received.
                order.SetPaymentMethod(request.PaymentMethod);
            }

            // Loop 2: now that the charge is verified, take stock atomically.
            // BUG-05: decrement stock with a `stock >= qty` guard so two concurrent orders
            // for the last unit can't both succeed. Zero rows affected means another order
            // took the stock first; the surrounding transaction rolls back the order and any
            // earlier decrements in this order.
            // BUG-04: a card order is already charged at this point, so insufficient stock
            // must refund the intent — otherwise the charge is orphaned.
            foreach (var cartItem in cart.Items)
            {
                var stockUpdated = await _context.TryDecrementVariantStockAsync(
                    cartItem.VariantId, cartItem.Quantity, cancellationToken);

                if (stockUpdated == 0)
                {
                    var product = products.First(p => p.Id == cartItem.ProductId);
                    await RefundOrphanedChargeAsync(request.PaymentIntentId);
                    return Result<OrderDto>.Failure($"Insufficient stock for '{product.Name}'");
                }
            }

            _context.Orders.Add(order);

            // GAP-03: queue the order-confirmation email in the SAME transaction as the order, so
            // the email can never be lost after a successful order (nor sent for an order that
            // rolled back). The background worker delivers it asynchronously.
            _emailOutbox.Add(OutboxMessage.ForOrderConfirmation(order.CustomerEmail, order.Id));

            // GAP-06: a bank-transfer order also gets the wiring instructions (amount, payment
            // reference = order number, and the bank account details) staged in the SAME unit of
            // work. The order remains Pending until the wire is received and reconciled.
            if (string.Equals(request.PaymentMethod, BankPaymentMethod, StringComparison.OrdinalIgnoreCase))
            {
                _emailOutbox.Add(BuildBankTransferEmail(order));
            }

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

            // BUG-04: best-effort compensation for an already-charged card intent when the
            // order cannot be created. Only card orders carry a PaymentIntentId; non-card
            // orders pass null here and are never refunded (nothing was charged). A failed
            // refund is logged but does not change the (already failing) outcome.
            async Task RefundOrphanedChargeAsync(string? paymentIntentId)
            {
                if (string.IsNullOrWhiteSpace(paymentIntentId))
                {
                    return;
                }

                var refund = await _paymentService.RefundAsync(paymentIntentId, cancellationToken);
                if (!refund.Succeeded)
                {
                    _logger.LogError(
                        "Failed to refund orphaned charge for PaymentIntent {PaymentIntentId}: {Error}",
                        paymentIntentId, refund.ErrorMessage);
                }
            }
        }
    }

    /// <summary>
    /// Builds the bank-transfer instructions email (GAP-06): the amount to wire, the payment
    /// reference (the order number, so the wire can be reconciled), and the destination account.
    /// </summary>
    private OutboxMessage BuildBankTransferEmail(Order order)
    {
        var subject = $"Bank transfer instructions — {order.OrderNumber}";
        var amount = order.Total.ToString("0.00", CultureInfo.InvariantCulture);

        var body =
            $"Thank you for your order {order.OrderNumber}.\n\n" +
            "Please complete your purchase with a bank transfer using the details below. " +
            "Your order will be processed once we receive the payment.\n\n" +
            $"Amount: {amount} {order.Currency}\n" +
            $"Payment reference: {order.OrderNumber}\n" +
            $"Account name: {_bankTransferOptions.AccountName}\n" +
            $"IBAN: {_bankTransferOptions.Iban}\n" +
            $"Bank: {_bankTransferOptions.BankName}\n\n" +
            "Important: include the payment reference so we can match your transfer to this order. " +
            "The order stays pending until payment is received.";

        return OutboxMessage.ForGeneric(order.CustomerEmail, subject, body);
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

    /// <summary>
    /// 256 bits of cryptographic randomness, URL-safe base64 — the guest's bearer credential to
    /// read their own order confirmation. Not guessable/enumerable (unlike the order number).
    /// </summary>
    private static string GenerateGuestAccessToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
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
            GuestAccessToken = order.GuestAccessToken,
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
