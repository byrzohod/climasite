using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;

namespace ClimaSite.Api.Tests.Infrastructure;

/// <summary>
/// Controllable in-memory <see cref="IPaymentService"/> so integration tests can
/// exercise the money path without hitting Stripe. Registered as a singleton in
/// <see cref="TestWebApplicationFactory"/>; tests configure it via the factory.
/// Models Stripe's idempotency-key semantics on create so the dedup behaviour is testable.
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
    /// Models Stripe's per-key cache: idempotency key -> the intent it first created plus the
    /// (amount, currency, metadata fingerprint) it was created with. A repeat of the same key replays
    /// the same intent only when amount AND currency AND metadata all match; if any differs it fails,
    /// mirroring real Stripe (which hashes the full request body, metadata included).
    /// </summary>
    private readonly Dictionary<string, (string Id, long Amount, string Currency, string Metadata)> _byIdempotencyKey = new();

    /// <summary>Every idempotency key passed to create, in call order (null when none supplied).</summary>
    public List<string?> CreateIdempotencyKeys { get; } = new();

    /// <summary>The idempotency key passed to the most recent create call (null when none).</summary>
    public string? LastCreateIdempotencyKey { get; private set; }

    /// <summary>
    /// Records every PaymentIntent id that was refunded, in call order. Used by the money-path
    /// tests to assert an already-charged intent was compensated on post-charge failure (BUG-04).
    /// </summary>
    public List<string> Refunds { get; } = new();

    /// <summary>Every idempotency key passed to refund, in call order (null when none supplied).</summary>
    public List<string?> RefundIdempotencyKeys { get; } = new();

    public void Reset()
    {
        _counter = 0;
        CurrencyToReport = CheckoutPricing.Currency.ToLowerInvariant();
        StatusToReport = "succeeded";
        AmountOverride = null;
        CreatedIntents.Clear();
        _byIdempotencyKey.Clear();
        CreateIdempotencyKeys.Clear();
        LastCreateIdempotencyKey = null;
        Refunds.Clear();
        RefundIdempotencyKeys.Clear();
    }

    public Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, string currency = "eur", Dictionary<string, string>? metadata = null, string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateIdempotencyKeys.Add(idempotencyKey);
        LastCreateIdempotencyKey = idempotencyKey;

        var amountMinor = CheckoutPricing.ToMinorUnits(amount);
        var normalizedCurrency = currency.ToLowerInvariant();
        var metadataFingerprint = FingerprintMetadata(metadata);

        if (!string.IsNullOrEmpty(idempotencyKey)
            && _byIdempotencyKey.TryGetValue(idempotencyKey, out var existing))
        {
            // Same key seen before: Stripe replays the original intent only when EVERY create param
            // matches (amount + currency + metadata), and rejects the call with a 400 when any differs.
            if (existing.Amount == amountMinor
                && existing.Currency == normalizedCurrency
                && existing.Metadata == metadataFingerprint)
            {
                return Task.FromResult(
                    PaymentIntentResult.Success(existing.Id, $"{existing.Id}_secret", "requires_payment_method") with
                    {
                        Amount = existing.Amount,
                        Currency = existing.Currency
                    });
            }

            return Task.FromResult(
                PaymentIntentResult.Failure("idempotency key reused with different parameters"));
        }

        var id = $"pi_fake_{Interlocked.Increment(ref _counter)}_{Guid.NewGuid():N}";
        CreatedIntents[id] = amountMinor;
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            _byIdempotencyKey[idempotencyKey] = (id, amountMinor, normalizedCurrency, metadataFingerprint);
        }

        return Task.FromResult(PaymentIntentResult.Success(id, $"{id}_secret", "requires_payment_method") with
        {
            Amount = amountMinor,
            Currency = normalizedCurrency
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
        string paymentIntentId, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        Refunds.Add(paymentIntentId);
        RefundIdempotencyKeys.Add(idempotencyKey);
        return Task.FromResult(PaymentIntentResult.Success(paymentIntentId, string.Empty, "succeeded"));
    }

    /// <summary>
    /// Deterministic, order-independent fingerprint of the create metadata so a reused idempotency key
    /// with different metadata is treated as a param mismatch (null/empty metadata => empty fingerprint).
    /// </summary>
    private static string FingerprintMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("&", metadata
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
