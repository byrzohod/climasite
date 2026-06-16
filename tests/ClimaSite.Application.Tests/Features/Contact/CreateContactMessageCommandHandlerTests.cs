using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Contact.Commands;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Contact;

public class CreateContactMessageCommandHandlerTests
{
    private readonly MockDbContext _context = new();
    private readonly ContactOptions _options = new() { RecipientEmail = "biz@climasite.test" };

    private CreateContactMessageCommandHandler CreateHandler() =>
        new(_context, new EmailOutbox(_context), _options,
            Mock.Of<ILogger<CreateContactMessageCommandHandler>>());

    private static CreateContactMessageCommand ValidCommand() => new()
    {
        Name = "Jane Buyer",
        Email = "jane@example.com",
        Subject = "Quote request",
        Message = "Please quote a 3.5kW split unit."
    };

    [Fact]
    public async Task Handle_PersistsMessage_AndQueuesBusinessNotification()
    {
        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        var stored = await _context.ContactMessages.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].Email.Should().Be("jane@example.com");
        stored[0].Status.Should().Be(ContactMessageStatus.New);

        // The business is notified in the same unit of work, to the configured recipient.
        var queued = await _context.OutboxMessages.ToListAsync();
        queued.Should().ContainSingle(m =>
            m.Type == OutboxMessageTypes.Generic && m.ToEmail == "biz@climasite.test");
    }

    [Fact]
    public async Task Handle_StoresTheSubmittedContent()
    {
        var command = ValidCommand();

        await CreateHandler().Handle(command, CancellationToken.None);

        var stored = (await _context.ContactMessages.ToListAsync()).Single();
        stored.Name.Should().Be(command.Name);
        stored.Subject.Should().Be(command.Subject);
        stored.Message.Should().Be(command.Message);
    }
}
