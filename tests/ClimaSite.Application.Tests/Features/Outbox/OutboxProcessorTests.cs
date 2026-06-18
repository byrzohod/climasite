using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Outbox;

public class OutboxProcessorTests
{
    private readonly MockDbContext _context = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly EmailOutboxOptions _options = new()
    {
        BatchSize = 25,
        MaxAttempts = 3,
        BaseRetryDelaySeconds = 30
    };

    private OutboxProcessor CreateSut() =>
        new(_context, _emailService.Object, _options, Mock.Of<ILogger<OutboxProcessor>>());

    [Fact]
    public async Task ProcessPendingAsync_SendsDueMessage_AndMarksSent()
    {
        var message = OutboxMessage.ForWelcome("user@example.com", "Ada");
        _context.AddOutboxMessage(message);

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(1);
        message.Status.Should().Be(OutboxMessageStatus.Sent);
        message.ProcessedAt.Should().NotBeNull();
        message.AttemptCount.Should().Be(1);
        _emailService.Verify(x => x.SendWelcomeEmailAsync("user@example.com", "Ada", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingAsync_DispatchesEachTypeToTheRightEmailMethod()
    {
        var orderId = Guid.NewGuid();
        _context.AddOutboxMessage(OutboxMessage.ForOrderConfirmation("a@x.com", orderId));
        _context.AddOutboxMessage(OutboxMessage.ForOrderShipped("b@x.com", orderId, "TRK1"));
        _context.AddOutboxMessage(OutboxMessage.ForPasswordReset("c@x.com", "tok"));
        _context.AddOutboxMessage(OutboxMessage.ForGeneric("d@x.com", "Subj", "Body"));

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(4);
        _emailService.Verify(x => x.SendOrderConfirmationEmailAsync("a@x.com", orderId, It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(x => x.SendOrderShippedEmailAsync("b@x.com", orderId, "TRK1", It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(x => x.SendPasswordResetEmailAsync("c@x.com", "tok", It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(x => x.SendEmailAsync("d@x.com", "Subj", "Body", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingAsync_SkipsMessagesScheduledInTheFuture()
    {
        var future = OutboxMessage.ForGeneric("later@example.com", "S", "B");
        future.ScheduleRetry("seed", DateTime.UtcNow.AddMinutes(10));
        _context.AddOutboxMessage(future);

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(0);
        future.Status.Should().Be(OutboxMessageStatus.Pending);
        future.AttemptCount.Should().Be(0);
        _emailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingAsync_OnTransientFailure_SchedulesRetryWithBackoff()
    {
        var message = OutboxMessage.ForGeneric("fail@example.com", "S", "B");
        _context.AddOutboxMessage(message);
        _emailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var before = DateTime.UtcNow;
        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(0);
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.AttemptCount.Should().Be(1);
        message.LastError.Should().Contain("smtp down");
        message.NextAttemptAt.Should().BeAfter(before, "the retry must be deferred by the backoff delay");
        message.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingAsync_AfterMaxAttempts_MarksPermanentlyFailed()
    {
        _options.MaxAttempts = 1;
        var message = OutboxMessage.ForGeneric("dead@example.com", "S", "B");
        _context.AddOutboxMessage(message);
        _emailService
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("permanent"));

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(0);
        message.Status.Should().Be(OutboxMessageStatus.Failed);
        message.AttemptCount.Should().Be(1);
        message.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingAsync_DoesNotReprocessSentMessages()
    {
        var message = OutboxMessage.ForGeneric("once@example.com", "S", "B");
        _context.AddOutboxMessage(message);

        await CreateSut().ProcessPendingAsync();
        var secondPass = await CreateSut().ProcessPendingAsync();

        secondPass.Should().Be(0);
        _emailService.Verify(x => x.SendEmailAsync("once@example.com", "S", "B", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingAsync_ReclaimsStaleProcessingMessage_AndResends()
    {
        // A worker crashed mid-attempt: the row was saved as Processing but never reached a
        // terminal state. BeginAttempt() stamps UpdatedAt before ProcessPendingAsync captures its
        // own `now`, so with a 0-minute reclaim window the row is stale (UpdatedAt < now).
        _options.ProcessingReclaimMinutes = 0;
        var stale = OutboxMessage.ForWelcome("stranded@example.com", "Grace");
        stale.BeginAttempt(); // -> Processing, AttemptCount = 1
        _context.AddOutboxMessage(stale);

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(1);
        stale.Status.Should().Be(OutboxMessageStatus.Sent);
        stale.AttemptCount.Should().Be(2, "the reclaimed row runs through BeginAttempt() again");
        _emailService.Verify(x => x.SendWelcomeEmailAsync("stranded@example.com", "Grace", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingAsync_DoesNotReclaimFreshProcessingMessage()
    {
        // With the default 10-minute window a just-now Processing row (a genuinely in-flight
        // message from a concurrent run) must NOT be reclaimed.
        var inFlight = OutboxMessage.ForWelcome("inflight@example.com", "Ida");
        inFlight.BeginAttempt();
        _context.AddOutboxMessage(inFlight);

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(0);
        inFlight.AttemptCount.Should().Be(1, "a fresh Processing row is left alone");
        _emailService.Verify(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingAsync_UnknownType_MarksFailedAfterOneAttempt_NoRetry()
    {
        _options.MaxAttempts = 5; // plenty of retries available; a poison message must not use them
        var poison = new OutboxMessage("totally-unknown-type", "poison@example.com", "{}");
        _context.AddOutboxMessage(poison);

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(0);
        poison.Status.Should().Be(OutboxMessageStatus.Failed, "an unknown type can never succeed, so it fails immediately");
        poison.AttemptCount.Should().Be(1, "a poison message is not retried");
        poison.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingAsync_PasswordReset_ClearsPayloadAfterSuccessfulSend()
    {
        var reset = OutboxMessage.ForPasswordReset("user@example.com", "secret-reset-token");
        _context.AddOutboxMessage(reset);
        reset.Payload.Should().Contain("secret-reset-token");

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(1);
        reset.Status.Should().Be(OutboxMessageStatus.Sent);
        reset.Payload.Should().Be("{}", "the live reset token must not be retained after send");
        reset.Payload.Should().NotContain("secret-reset-token");
    }

    [Fact]
    public async Task ProcessPendingAsync_PasswordReset_ClearsPayloadAfterPermanentFailure()
    {
        _options.MaxAttempts = 1;
        var reset = OutboxMessage.ForPasswordReset("user@example.com", "secret-reset-token");
        _context.AddOutboxMessage(reset);
        _emailService
            .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var sent = await CreateSut().ProcessPendingAsync();

        sent.Should().Be(0);
        reset.Status.Should().Be(OutboxMessageStatus.Failed);
        reset.Payload.Should().Be("{}", "the reset token must not be retained even on permanent failure");
    }

    [Fact]
    public async Task ProcessPendingAsync_NonPasswordReset_RetainsPayloadForAudit()
    {
        var welcome = OutboxMessage.ForWelcome("user@example.com", "Ada");
        _context.AddOutboxMessage(welcome);

        await CreateSut().ProcessPendingAsync();

        welcome.Status.Should().Be(OutboxMessageStatus.Sent);
        welcome.Payload.Should().Contain("Ada", "non-credential payloads are kept for audit");
    }
}
