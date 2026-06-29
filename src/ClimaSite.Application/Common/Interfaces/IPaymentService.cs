namespace ClimaSite.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency = "eur", Dictionary<string, string>? metadata = null, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntentResult> CancelPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntentResult> RefundAsync(string paymentIntentId, string? idempotencyKey = null, CancellationToken cancellationToken = default);
}

public record PaymentIntentResult
{
    public bool Succeeded { get; init; }
    public string? PaymentIntentId { get; init; }
    public string? ClientSecret { get; init; }
    public string? Status { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>The PaymentIntent amount in minor units (e.g. cents), populated on retrieval.</summary>
    public long Amount { get; init; }

    /// <summary>The PaymentIntent currency (e.g. "eur"), populated on retrieval.</summary>
    public string? Currency { get; init; }

    public static PaymentIntentResult Success(string paymentIntentId, string clientSecret, string status)
        => new() { Succeeded = true, PaymentIntentId = paymentIntentId, ClientSecret = clientSecret, Status = status };

    public static PaymentIntentResult Failure(string errorMessage)
        => new() { Succeeded = false, ErrorMessage = errorMessage };
}
