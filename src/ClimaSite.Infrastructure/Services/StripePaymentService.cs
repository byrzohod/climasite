using ClimaSite.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace ClimaSite.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;

    public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
    {
        _logger = logger;
        var secretKey = configuration["Stripe:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("Stripe SecretKey is not configured");
        }

        StripeConfiguration.ApiKey = secretKey;
        _paymentIntentService = new PaymentIntentService();
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount,
        string currency = "bgn",
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Stripe requires amount in smallest currency unit (e.g., cents)
            var amountInSmallestUnit = (long)(amount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInSmallestUnit,
                Currency = currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            _logger.LogInformation(
                "Created PaymentIntent {PaymentIntentId} for amount {Amount} {Currency}",
                paymentIntent.Id, amount, currency);

            return PaymentIntentResult.Success(
                paymentIntent.Id,
                paymentIntent.ClientSecret,
                paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create PaymentIntent for amount {Amount} {Currency}", amount, currency);
            return PaymentIntentResult.Failure(ex.Message);
        }
    }

    public async Task<PaymentIntentResult> ConfirmPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.ConfirmAsync(paymentIntentId);

            _logger.LogInformation(
                "Confirmed PaymentIntent {PaymentIntentId}, status: {Status}",
                paymentIntent.Id, paymentIntent.Status);

            return PaymentIntentResult.Success(
                paymentIntent.Id,
                paymentIntent.ClientSecret,
                paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to confirm PaymentIntent {PaymentIntentId}", paymentIntentId);
            return PaymentIntentResult.Failure(ex.Message);
        }
    }

    public async Task<PaymentIntentResult> CancelPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.CancelAsync(paymentIntentId);

            _logger.LogInformation("Cancelled PaymentIntent {PaymentIntentId}", paymentIntent.Id);

            return PaymentIntentResult.Success(
                paymentIntent.Id,
                paymentIntent.ClientSecret,
                paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel PaymentIntent {PaymentIntentId}", paymentIntentId);
            return PaymentIntentResult.Failure(ex.Message);
        }
    }

    public async Task<PaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(paymentIntentId);

            return PaymentIntentResult.Success(
                paymentIntent.Id,
                paymentIntent.ClientSecret,
                paymentIntent.Status);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get PaymentIntent {PaymentIntentId}", paymentIntentId);
            return PaymentIntentResult.Failure(ex.Message);
        }
    }
}
