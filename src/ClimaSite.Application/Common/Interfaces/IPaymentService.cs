namespace ClimaSite.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency = "bgn", Dictionary<string, string>? metadata = null);
    Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntentResult> CancelPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId);
}

public record PaymentIntentResult
{
    public bool Succeeded { get; init; }
    public string? PaymentIntentId { get; init; }
    public string? ClientSecret { get; init; }
    public string? Status { get; init; }
    public string? ErrorMessage { get; init; }

    public static PaymentIntentResult Success(string paymentIntentId, string clientSecret, string status)
        => new() { Succeeded = true, PaymentIntentId = paymentIntentId, ClientSecret = clientSecret, Status = status };

    public static PaymentIntentResult Failure(string errorMessage)
        => new() { Succeeded = false, ErrorMessage = errorMessage };
}
