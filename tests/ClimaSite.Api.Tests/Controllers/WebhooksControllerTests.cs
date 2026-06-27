using System.Net;
using System.Text;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Webhook security tests. We deliberately do NOT forge a valid Stripe signature
/// (that would require the live signing secret). Instead we prove that the endpoint
/// rejects unsigned / invalid requests, which is the security contract that matters.
/// </summary>
public class WebhooksControllerTests : IntegrationTestBase
{
    public WebhooksControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private static StringContent EventPayload() =>
        new(
            "{\"id\":\"evt_test\",\"type\":\"payment_intent.succeeded\"}",
            Encoding.UTF8,
            "application/json");

    /// <summary>
    /// Client with a known Stripe webhook secret configured, so the signature
    /// verification step actually runs (and rejects our bogus signature).
    /// </summary>
    private HttpClient CreateClientWithWebhookSecret(string secret) =>
        Factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Stripe:WebhookSecret"] = secret
                    });
                });
            })
            .CreateClient();

    [Fact]
    public async Task HandleStripeWebhook_ReturnsBadRequest_WhenSignatureHeaderMissing()
    {
        // Act - no Stripe-Signature header at all
        var response = await Client.PostAsync("/api/webhooks/stripe", EventPayload());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Missing Stripe-Signature header");
    }

    [Fact]
    public async Task HandleStripeWebhook_ReturnsBadRequest_WhenSignatureIsInvalid()
    {
        // Arrange - secret is configured so signature verification executes
        using var client = CreateClientWithWebhookSecret("test-webhook-secret-value");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = EventPayload()
        };
        request.Headers.Add("Stripe-Signature", "t=123,v1=deadbeefnotarealsignature");

        // Act
        var response = await client.SendAsync(request);

        // Assert - forged signature fails EventUtility.ConstructEvent => 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid signature");
    }

    [Fact]
    public async Task HandleStripeWebhook_ReturnsBadRequest_WhenSignatureIsMalformed()
    {
        // Arrange
        using var client = CreateClientWithWebhookSecret("test-webhook-secret-value");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = EventPayload()
        };
        // A completely malformed signature header (no timestamp / scheme) must still be rejected.
        request.Headers.Add("Stripe-Signature", "garbage");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleStripeWebhook_DoesNotProcessEvent_WhenUnsigned()
    {
        // Arrange - an empty body with no signature should never reach the handler.
        var emptyBody = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/webhooks/stripe", emptyBody);

        // Assert - rejected before any command dispatch
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // No orders/payments should have been touched (nothing was seeded, sanity check it stays empty).
        DbContext.Orders.Should().BeEmpty();
    }
}
