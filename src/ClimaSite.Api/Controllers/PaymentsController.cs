using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMediator mediator,
        IPaymentService paymentService,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
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
    /// Create a payment intent for the current cart. The amount and currency are
    /// computed server-side from the cart and chosen shipping method (BUG-02);
    /// the client only supplies the shipping method and optional guest session id.
    /// Anonymous so guests can pay (GAP-07) — the amount is never client-supplied.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("create-intent")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create payment intent: {Error}", result.Error);
            return BadRequest(new { message = result.Error });
        }

        return Ok(new
        {
            paymentIntentId = result.Value!.PaymentIntentId,
            clientSecret = result.Value.ClientSecret,
            amount = result.Value.Amount,
            currency = result.Value.Currency
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
