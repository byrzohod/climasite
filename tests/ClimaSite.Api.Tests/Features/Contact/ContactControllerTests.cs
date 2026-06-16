using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Api.Tests.Features.Contact;

public class ContactControllerTests : IntegrationTestBase
{
    public ContactControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Submit_ValidEnquiry_PersistsMessageAndQueuesNotification_Anonymous()
    {
        // No auth header set — the contact endpoint is public.
        var subject = $"Quote {Guid.NewGuid():N}";
        var response = await Client.PostAsJsonAsync("/api/contact", new
        {
            name = "Jane Buyer",
            email = "jane@example.com",
            subject,
            message = "Please quote a 3.5kW split unit."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await DbContext.ContactMessages.AsNoTracking()
            .Where(m => m.Subject == subject)
            .ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Email.Should().Be("jane@example.com");
        stored[0].Status.Should().Be(ContactMessageStatus.New);

        // A business-notification email was queued in the outbox.
        var queued = await DbContext.OutboxMessages.AsNoTracking()
            .Where(m => m.Type == OutboxMessageTypes.Generic && m.ToEmail == "support@climasite.local")
            .ToListAsync();
        queued.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Submit_MissingEmail_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/contact", new
        {
            name = "No Email",
            email = "",
            subject = "Hi",
            message = "Body"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
