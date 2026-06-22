using System.Text.Json;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class OutboxMessageTests
{
    private static OutboxMessage CreateValid() =>
        new(OutboxMessageTypes.Generic, "user@test.com", "{\"a\":1}");

    [Fact]
    public void Constructor_WithValidData_InitializesPendingMessage()
    {
        var message = CreateValid();

        message.Type.Should().Be(OutboxMessageTypes.Generic);
        message.ToEmail.Should().Be("user@test.com");
        message.Payload.Should().Be("{\"a\":1}");
        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.AttemptCount.Should().Be(0);
        message.ProcessedAt.Should().BeNull();
        message.LastError.Should().BeNull();
        message.NextAttemptAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_TrimsRecipientEmail()
    {
        var message = new OutboxMessage(OutboxMessageTypes.Generic, "  user@test.com  ", "{}");

        message.ToEmail.Should().Be("user@test.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyType_ThrowsArgumentException(string type)
    {
        var act = () => new OutboxMessage(type, "user@test.com", "{}");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*type cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyRecipient_ThrowsArgumentException(string email)
    {
        var act = () => new OutboxMessage(OutboxMessageTypes.Generic, email, "{}");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*recipient cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyPayload_DefaultsToEmptyJsonObject(string? payload)
    {
        var message = new OutboxMessage(OutboxMessageTypes.Generic, "user@test.com", payload!);

        message.Payload.Should().Be("{}");
    }

    [Fact]
    public void BeginAttempt_SetsProcessingAndIncrementsAttemptCount()
    {
        var message = CreateValid();

        message.BeginAttempt();

        message.Status.Should().Be(OutboxMessageStatus.Processing);
        message.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void BeginAttempt_CalledTwice_IncrementsAttemptCountEachTime()
    {
        var message = CreateValid();

        message.BeginAttempt();
        message.BeginAttempt();

        message.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void MarkSent_SetsSentStatusAndProcessedAtAndClearsError()
    {
        var message = CreateValid();
        message.ScheduleRetry("boom", DateTime.UtcNow.AddMinutes(5));

        message.MarkSent();

        message.Status.Should().Be(OutboxMessageStatus.Sent);
        message.ProcessedAt.Should().NotBeNull();
        message.LastError.Should().BeNull();
    }

    [Fact]
    public void ScheduleRetry_SetsPendingWithErrorAndNextAttempt()
    {
        var message = CreateValid();
        var next = DateTime.UtcNow.AddMinutes(10);

        message.ScheduleRetry("smtp down", next);

        message.Status.Should().Be(OutboxMessageStatus.Pending);
        message.LastError.Should().Be("smtp down");
        message.NextAttemptAt.Should().Be(next);
    }

    [Fact]
    public void ScheduleRetry_TruncatesErrorLongerThan2000Chars()
    {
        var message = CreateValid();
        var longError = new string('x', 2500);

        message.ScheduleRetry(longError, DateTime.UtcNow.AddMinutes(1));

        message.LastError.Should().HaveLength(2000);
    }

    [Fact]
    public void MarkFailed_SetsFailedStatusErrorAndProcessedAt()
    {
        var message = CreateValid();

        message.MarkFailed("permanent failure");

        message.Status.Should().Be(OutboxMessageStatus.Failed);
        message.LastError.Should().Be("permanent failure");
        message.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void ClearPayload_ReplacesPayloadWithEmptyJsonObject()
    {
        var message = new OutboxMessage(OutboxMessageTypes.PasswordReset, "user@test.com", "{\"resetToken\":\"secret\"}");

        message.ClearPayload();

        message.Payload.Should().Be("{}");
    }

    [Fact]
    public void ForWelcome_BuildsWelcomeMessageWithSerializedPayload()
    {
        var message = OutboxMessage.ForWelcome("user@test.com", "Ada");

        message.Type.Should().Be(OutboxMessageTypes.Welcome);
        message.ToEmail.Should().Be("user@test.com");
        var payload = JsonSerializer.Deserialize<WelcomeEmailPayload>(message.Payload);
        payload!.FirstName.Should().Be("Ada");
    }

    [Fact]
    public void ForPasswordReset_BuildsPasswordResetMessageWithToken()
    {
        var message = OutboxMessage.ForPasswordReset("user@test.com", "tok-123");

        message.Type.Should().Be(OutboxMessageTypes.PasswordReset);
        var payload = JsonSerializer.Deserialize<PasswordResetEmailPayload>(message.Payload);
        payload!.ResetToken.Should().Be("tok-123");
    }

    [Fact]
    public void ForOrderConfirmation_BuildsOrderConfirmationMessageWithOrderId()
    {
        var orderId = Guid.NewGuid();

        var message = OutboxMessage.ForOrderConfirmation("user@test.com", orderId);

        message.Type.Should().Be(OutboxMessageTypes.OrderConfirmation);
        var payload = JsonSerializer.Deserialize<OrderEmailPayload>(message.Payload);
        payload!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void ForOrderShipped_BuildsOrderShippedMessageWithOrderIdAndTracking()
    {
        var orderId = Guid.NewGuid();

        var message = OutboxMessage.ForOrderShipped("user@test.com", orderId, "TRK-9");

        message.Type.Should().Be(OutboxMessageTypes.OrderShipped);
        var payload = JsonSerializer.Deserialize<OrderShippedEmailPayload>(message.Payload);
        payload!.OrderId.Should().Be(orderId);
        payload.TrackingNumber.Should().Be("TRK-9");
    }

    [Fact]
    public void ForGeneric_BuildsGenericMessageWithSubjectAndBody()
    {
        var message = OutboxMessage.ForGeneric("user@test.com", "Hi", "Body text");

        message.Type.Should().Be(OutboxMessageTypes.Generic);
        var payload = JsonSerializer.Deserialize<GenericEmailPayload>(message.Payload);
        payload!.Subject.Should().Be("Hi");
        payload.Body.Should().Be("Body text");
    }
}
