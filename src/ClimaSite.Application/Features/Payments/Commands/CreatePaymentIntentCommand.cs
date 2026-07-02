using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Common.Payments;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Payments.Commands;

/// <summary>
/// Result of creating a Stripe PaymentIntent. The amount and currency are
/// computed server-side from the cart so the client can never influence what
/// will be charged (BUG-02).
/// </summary>
public record PaymentIntentDto
{
    public string PaymentIntentId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = CheckoutPricing.Currency;
}

/// <summary>
/// Creates a Stripe PaymentIntent for the current cart. The amount is derived
/// entirely from the persisted cart plus the chosen shipping method; the client
/// only supplies the shipping method and (for guests) the session id.
/// </summary>
public record CreatePaymentIntentCommand : IRequest<Result<PaymentIntentDto>>
{
    public string ShippingMethod { get; init; } = "standard";
    public string? GuestSessionId { get; init; }

    /// <summary>
    /// Optional per-attempt idempotency key supplied by the client (a fresh UUID generated
    /// once per place-order attempt). When present it dedupes a network retry of this single
    /// create-intent POST; absent/empty degrades to today's no-dedup behaviour.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}

public class CreatePaymentIntentCommandValidator : AbstractValidator<CreatePaymentIntentCommand>
{
    public CreatePaymentIntentCommandValidator()
    {
        RuleFor(x => x.ShippingMethod)
            .NotEmpty().WithMessage("Shipping method is required")
            .Must(ShippingMethods.IsAllowed)
            .WithMessage($"Shipping method must be one of: {ShippingMethods.AllowedDisplay}.")
            // Scope the allow-list check to ONLY this rule so an empty value still trips NotEmpty
            // above (rather than being skipped by a chain-wide condition).
            .When(x => !string.IsNullOrWhiteSpace(x.ShippingMethod), ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.IdempotencyKey)
            .Must(PaymentIdempotency.IsValidClientKey)
            .When(x => !string.IsNullOrEmpty(x.IdempotencyKey))
            .WithMessage("Idempotency key must be 8-200 characters of [A-Za-z0-9_-].");
    }
}

public class CreatePaymentIntentCommandHandler
    : IRequestHandler<CreatePaymentIntentCommand, Result<PaymentIntentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService;
    private readonly IStockReservationService _reservations;
    private readonly IGuestSessionAccessor _guestSession;

    public CreatePaymentIntentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        IStockReservationService reservations,
        IGuestSessionAccessor guestSession)
    {
        _context = context;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _reservations = reservations;
        _guestSession = guestSession;
    }

    public async Task<Result<PaymentIntentDto>> Handle(
        CreatePaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        Core.Entities.Cart? cart = null;
        if (userId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }
        else
        {
            // INV-01 A2 legacy-reject: a reservation-bearing flow requires the server-trusted signed guest cookie.
            // A guest presenting only a spoofable legacy id (accessor null) cannot create a hold.
            var trustedGuestId = _guestSession.GuestSessionId;
            if (string.IsNullOrEmpty(trustedGuestId))
            {
                return Result<PaymentIntentDto>.Failure(
                    "A valid checkout session is required. Please refresh the page and try again.");
            }

            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == trustedGuestId, cancellationToken);
        }

        if (cart == null || !cart.Items.Any())
        {
            return Result<PaymentIntentDto>.Failure("Cart is empty");
        }

        // Amount is computed server-side from the cart; the client cannot influence it.
        // CheckoutPricing is already case-insensitive on the shipping method, so the total is
        // unaffected by casing; the Stripe metadata, however, records the canonical value.
        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var total = CheckoutPricing.CalculateTotal(subtotal, request.ShippingMethod);
        var canonicalShipping = ShippingMethods.Canonicalize(request.ShippingMethod);

        var metadata = new Dictionary<string, string>
        {
            ["cartId"] = cart.Id.ToString(),
            ["shippingMethod"] = canonicalShipping
        };
        if (userId.HasValue)
        {
            metadata["userId"] = userId.Value.ToString();
        }

        // INV-01 A2: reserve the WHOLE cart atomically BEFORE the Stripe call — a hold per line under a per-variant
        // FOR UPDATE lock. Any short line rolls the batch back and returns a failure naming it, so the losing
        // last-unit buyer is never charged (kills charge-then-refund).
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var reservationLines = cart.Items
            .Select(i =>
            {
                var product = products.FirstOrDefault(p => p.Id == i.ProductId);
                var variant = product?.Variants.FirstOrDefault(v => v.Id == i.VariantId);
                var displayName = string.IsNullOrWhiteSpace(variant?.Name)
                    ? product?.Name ?? "item"
                    : $"{product?.Name} ({variant!.Name})";
                return new ReservationRequestLine(i.VariantId, i.Quantity, displayName);
            })
            .ToList();

        var reservation = await _reservations.ReserveCartAsync(
            cart.Id, reservationLines, ReservationKind.Card, userId, cancellationToken);
        if (!reservation.Succeeded)
        {
            return Result<PaymentIntentDto>.Failure(reservation.Error ?? "Unable to reserve stock for checkout.");
        }

        // Per-attempt key (namespaced ci_<clientKey>); null when the client supplied none.
        var idemKey = PaymentIdempotency.NormalizeClientKey(request.IdempotencyKey);

        var result = await _paymentService.CreatePaymentIntentAsync(
            total,
            CheckoutPricing.Currency,
            metadata,
            idemKey,
            cancellationToken);

        if (!result.Succeeded || string.IsNullOrEmpty(result.PaymentIntentId) || string.IsNullOrEmpty(result.ClientSecret))
        {
            return Result<PaymentIntentDto>.Failure(result.ErrorMessage ?? "Failed to create payment intent");
        }

        // Link the created intent onto the cart's Active holds (payment linkage + diagnostics). Idempotent.
        await _reservations.StampPaymentIntentAsync(cart.Id, result.PaymentIntentId, cancellationToken);

        return Result<PaymentIntentDto>.Success(new PaymentIntentDto
        {
            PaymentIntentId = result.PaymentIntentId,
            ClientSecret = result.ClientSecret,
            Amount = total,
            Currency = CheckoutPricing.Currency
        });
    }
}
