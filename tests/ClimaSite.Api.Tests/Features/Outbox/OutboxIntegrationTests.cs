using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Features.Outbox;

public class OutboxIntegrationTests : IntegrationTestBase
{
    public OutboxIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_EnqueuesWelcomeEmail_AndProcessorDeliversIt()
    {
        var email = $"outbox-{Guid.NewGuid():N}@example.com";

        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            firstName = "Outbox",
            lastName = "Tester"
        });

        response.IsSuccessStatusCode.Should().BeTrue();

        // Registration wrote a durable, pending welcome email to the outbox (ARCH-05).
        var pending = await DbContext.OutboxMessages.AsNoTracking()
            .Where(m => m.ToEmail == email)
            .ToListAsync();

        pending.Should().ContainSingle();
        pending[0].Type.Should().Be(OutboxMessageTypes.Welcome);
        pending[0].Status.Should().Be(OutboxMessageStatus.Pending);

        // Draining the outbox delivers the message and marks it sent.
        using var scope = Factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        var sent = await processor.ProcessPendingAsync();

        sent.Should().Be(1);

        var afterDrain = await DbContext.OutboxMessages.AsNoTracking()
            .Where(m => m.ToEmail == email)
            .ToListAsync();

        afterDrain[0].Status.Should().Be(OutboxMessageStatus.Sent);
        afterDrain[0].ProcessedAt.Should().NotBeNull();
        afterDrain[0].AttemptCount.Should().Be(1);
    }
}
