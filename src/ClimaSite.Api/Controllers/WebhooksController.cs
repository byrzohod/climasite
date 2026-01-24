using ClimaSite.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace ClimaSite.Api.Controllers;

/// <summary>
/// Handles incoming webhooks from third-party services.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IMediator mediator,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Stripe webhook endpoint for payment events.
    /// Handles: payment_intent.succeeded, payment_intent.payment_failed, charge.refunded
    /// </summary>
    /// <remarks>
    /// This endpoint verifies the Stripe signature to ensure the webhook is authentic.
    /// Configure the webhook URL in Stripe Dashboard: https://your-domain.com/api/webhooks/stripe
    /// </remarks>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(stripeSignature))
        {
            _logger.LogWarning("Stripe webhook received without signature header");
            return BadRequest(new { message = "Missing Stripe-Signature header" });
        }

        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("Stripe WebhookSecret is not configured");
            return StatusCode(500, new { message = "Webhook secret not configured" });
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Failed to verify Stripe webhook signature");
            return BadRequest(new { message = "Invalid signature" });
        }

        _logger.LogInformation(
            "Received Stripe webhook: {EventType} (ID: {EventId})",
            stripeEvent.Type,
            stripeEvent.Id);

        var command = CreateCommandFromEvent(stripeEvent);
        if (command != null)
        {
            var result = await _mediator.Send(command);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to process Stripe webhook {EventType}: {Error}",
                    stripeEvent.Type,
                    result.Error);
            }
        }

        // Always return 200 to acknowledge receipt
        // Stripe will retry if we return an error, which could cause duplicate processing
        return Ok(new { received = true });
    }

    private HandleStripeWebhookCommand? CreateCommandFromEvent(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
            case "payment_intent.payment_failed":
                if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                {
                    return new HandleStripeWebhookCommand
                    {
                        EventType = stripeEvent.Type,
                        PaymentIntentId = paymentIntent.Id,
                        FailureMessage = paymentIntent.LastPaymentError?.Message
                    };
                }
                break;

            case "charge.refunded":
                if (stripeEvent.Data.Object is Charge charge)
                {
                    return new HandleStripeWebhookCommand
                    {
                        EventType = stripeEvent.Type,
                        PaymentIntentId = charge.PaymentIntentId,
                        ChargeId = charge.Id,
                        AmountRefunded = charge.AmountRefunded
                    };
                }
                break;
        }

        _logger.LogDebug(
            "Stripe webhook event {EventType} not handled",
            stripeEvent.Type);

        return null;
    }
}
