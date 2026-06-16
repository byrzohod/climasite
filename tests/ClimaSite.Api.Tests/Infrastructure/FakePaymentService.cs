using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;

namespace ClimaSite.Api.Tests.Infrastructure;

/// <summary>
/// Controllable in-memory <see cref="IPaymentService"/> so integration tests can
/// exercise the money path without hitting Stripe. Registered as a singleton in
/// <see cref="TestWebApplicationFactory"/>; tests configure it via the factory.
/// </summary>
public class FakePaymentService : IPaymentService
{
    private int _counter;

    /// <summary>Currency reported by created/retrieved intents (defaults to EUR).</summary>
    public string CurrencyToReport { get; set; } = CheckoutPricing.Currency.ToLowerInvariant();

    /// <summary>Status reported when an intent is retrieved.</summary>
    public string StatusToReport { get; set; } = "succeeded";

    /// <summary>
    /// Optional override for the amount (minor units) reported on retrieval. When
    /// null, the amount captured at creation time for that intent is returned,
    /// which models a correctly-charged intent.
    /// </summary>
    public long? AmountOverride { get; set; }

    /// <summary>Records each created intent id -> amount in minor units.</summary>
    public Dictionary<string, long> CreatedIntents { get; } = new();

    /// <summary>
    /// Records every PaymentIntent id that was refunded, in call order. Used by the money-path
    /// tests to assert an already-charged intent was compensated on post-charge failure (BUG-04).
    /// </summary>
    public List<string> Refunds { get; } = new();

    public void Reset()
    {
        _counter = 0;
        CurrencyToReport = CheckoutPricing.Currency.ToLowerInvariant();
        StatusToReport = "succeeded";
        AmountOverride = null;
        CreatedIntents.Clear();
        Refunds.Clear();
    }

    public Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, string currency = "eur", Dictionary<string, string>? metadata = null)
    {
        var id = $"pi_fake_{Interlocked.Increment(ref _counter)}_{Guid.NewGuid():N}";
        CreatedIntents[id] = CheckoutPricing.ToMinorUnits(amount);

        return Task.FromResult(PaymentIntentResult.Success(id, $"{id}_secret", "requires_payment_method") with
        {
            Amount = CheckoutPricing.ToMinorUnits(amount),
            Currency = currency.ToLowerInvariant()
        });
    }

    public Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId)
        => Task.FromResult(PaymentIntentResult.Success(paymentIntentId, $"{paymentIntentId}_secret", "succeeded"));

    public Task<PaymentIntentResult> CancelPaymentIntentAsync(string paymentIntentId)
        => Task.FromResult(PaymentIntentResult.Success(paymentIntentId, $"{paymentIntentId}_secret", "canceled"));

    public Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId)
    {
        var amount = AmountOverride
            ?? (CreatedIntents.TryGetValue(paymentIntentId, out var created) ? created : 0);

        return Task.FromResult(PaymentIntentResult.Success(
            paymentIntentId, $"{paymentIntentId}_secret", StatusToReport) with
        {
            Amount = amount,
            Currency = CurrencyToReport
        });
    }

    public Task<PaymentIntentResult> RefundAsync(
        string paymentIntentId, CancellationToken cancellationToken = default)
    {
        Refunds.Add(paymentIntentId);
        return Task.FromResult(PaymentIntentResult.Success(paymentIntentId, string.Empty, "succeeded"));
    }
}
