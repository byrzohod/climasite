using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Installation.Commands;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Installation;

public class CreateInstallationRequestCommandHandlerTests
{
    private readonly MockDbContext _context = new();
    private readonly ContactOptions _options = new() { RecipientEmail = "biz@climasite.test" };
    private readonly Product _product;

    public CreateInstallationRequestCommandHandlerTests()
    {
        _product = new Product("AC-100", "DualZone Pro 12000", "dualzone-pro-12000", 1000m);
        _context.AddProduct(_product);
    }

    private CreateInstallationRequestCommandHandler CreateHandler() =>
        new(_context, new EmailOutbox(_context), _options,
            Mock.Of<ILogger<CreateInstallationRequestCommandHandler>>());

    private CreateInstallationRequestCommand ValidCommand() => new()
    {
        ProductId = _product.Id,
        InstallationType = "Premium",
        CustomerName = "Jane Buyer",
        CustomerEmail = "jane@example.com",
        CustomerPhone = "+359888123456",
        AddressLine1 = "12 Vitosha Blvd",
        City = "Sofia",
        PostalCode = "1000",
        Country = "Bulgaria",
        PreferredDate = DateTime.UtcNow.AddDays(7),
        PreferredTimeSlot = "Morning"
    };

    [Fact]
    public async Task Handle_PersistsRequest_AndQueuesBusinessNotification()
    {
        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Id.Should().NotBe(Guid.Empty);
        result.Status.Should().Be(InstallationRequestStatus.Pending.ToString());

        var stored = await _context.InstallationRequests.ToListAsync();
        stored.Should().ContainSingle();
        stored[0].CustomerEmail.Should().Be("jane@example.com");
        stored[0].ProductName.Should().Be("DualZone Pro 12000");

        // The business is notified in the same unit of work, to the configured recipient.
        var queued = await _context.OutboxMessages.ToListAsync();
        queued.Should().ContainSingle(m =>
            m.Type == OutboxMessageTypes.Generic && m.ToEmail == "biz@climasite.test");
    }

    [Fact]
    public async Task Handle_NotificationBody_IncludesCustomerAndProductDetails()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var message = (await _context.OutboxMessages.ToListAsync()).Single();
        // The generic payload serializes the subject + body; assert the key fields are present.
        message.Payload.Should().Contain("DualZone Pro 12000");
        message.Payload.Should().Contain("Premium");
        message.Payload.Should().Contain("Jane Buyer");
        message.Payload.Should().Contain("jane@example.com");
        // "+" is escaped to + in the serialized JSON payload; assert the digits instead.
        message.Payload.Should().Contain("359888123456");
        message.Payload.Should().Contain("Sofia");
    }

    [Fact]
    public async Task Handle_UnknownProduct_Throws()
    {
        var command = ValidCommand() with { ProductId = Guid.NewGuid() };

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
