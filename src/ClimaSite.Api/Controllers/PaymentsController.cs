using ClimaSite.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get the Stripe publishable key for frontend use
    /// </summary>
    [AllowAnonymous]
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var publishableKey = _configuration["Stripe:PublishableKey"];
        return Ok(new { publishableKey });
    }

    /// <summary>
    /// Create a payment intent for the specified amount
    /// </summary>
    [HttpPost("create-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than 0" });
        }

        var metadata = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.OrderReference))
        {
            metadata["orderReference"] = request.OrderReference;
        }

        var result = await _paymentService.CreatePaymentIntentAsync(
            request.Amount,
            request.Currency ?? "bgn",
            metadata);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create payment intent: {Error}", result.ErrorMessage);
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            paymentIntentId = result.PaymentIntentId,
            clientSecret = result.ClientSecret
        });
    }

    /// <summary>
    /// Get the status of a payment intent
    /// </summary>
    [HttpGet("intent/{paymentIntentId}")]
    public async Task<IActionResult> GetPaymentIntent(string paymentIntentId)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
        {
            return BadRequest(new { message = "Payment intent ID is required" });
        }

        var result = await _paymentService.GetPaymentIntentAsync(paymentIntentId);

        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            paymentIntentId = result.PaymentIntentId,
            status = result.Status
        });
    }

    /// <summary>
    /// Cancel a payment intent
    /// </summary>
    [HttpPost("cancel-intent/{paymentIntentId}")]
    public async Task<IActionResult> CancelPaymentIntent(string paymentIntentId)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
        {
            return BadRequest(new { message = "Payment intent ID is required" });
        }

        var result = await _paymentService.CancelPaymentIntentAsync(paymentIntentId);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Payment intent cancelled" });
    }
}

public record CreatePaymentIntentRequest
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? OrderReference { get; init; }
}
