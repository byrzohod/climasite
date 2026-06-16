using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Outbox;

public class EmailOutboxTests
{
    private readonly MockDbContext _context = new();

    [Fact]
    public async Task QueueAsync_PersistsMessageImmediately()
    {
        var outbox = new EmailOutbox(_context);

        await outbox.QueueAsync(OutboxMessage.ForWelcome("user@example.com", "Grace"));

        var stored = await _context.OutboxMessages.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Type.Should().Be(OutboxMessageTypes.Welcome);
        stored[0].ToEmail.Should().Be("user@example.com");
        stored[0].Status.Should().Be(OutboxMessageStatus.Pending);
    }

    [Fact]
    public void Add_StagesMessageOnTheContext()
    {
        var outbox = new EmailOutbox(_context);

        outbox.Add(OutboxMessage.ForOrderConfirmation("buyer@example.com", Guid.NewGuid()));

        // Add stages without saving; the mock's DbSet.Add writes straight to the backing store,
        // so the staged message is observable here.
        _context.OutboxMessages.Should().ContainSingle();
    }

    [Fact]
    public async Task QueueAsync_NullMessage_Throws()
    {
        var outbox = new EmailOutbox(_context);

        await FluentActions.Awaiting(() => outbox.QueueAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}
