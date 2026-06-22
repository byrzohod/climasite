using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class InstallationRequestTests
{
    private static InstallationRequest CreateValid(decimal estimatedPrice = 200m) =>
        new(
            Guid.NewGuid(),
            "AC Unit",
            InstallationType.Standard,
            "John Doe",
            "john@test.com",
            "+1234567890",
            "1 Main St",
            "Sofia",
            "1000",
            "Bulgaria",
            estimatedPrice);

    [Fact]
    public void Constructor_WithValidData_CreatesPendingRequest()
    {
        var request = CreateValid();

        request.ProductName.Should().Be("AC Unit");
        request.InstallationType.Should().Be(InstallationType.Standard);
        request.CustomerName.Should().Be("John Doe");
        request.CustomerEmail.Should().Be("john@test.com");
        request.Status.Should().Be(InstallationRequestStatus.Pending);
        request.EstimatedPrice.Should().Be(200m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyCustomerName_ThrowsArgumentException(string name)
    {
        var act = () => new InstallationRequest(
            Guid.NewGuid(), "AC", InstallationType.Standard,
            name, "john@test.com", "+123", "1 Main St", "Sofia", "1000", "BG", 100m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyEmail_ThrowsArgumentException(string email)
    {
        var act = () => new InstallationRequest(
            Guid.NewGuid(), "AC", InstallationType.Standard,
            "John", email, "+123", "1 Main St", "Sofia", "1000", "BG", 100m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer email is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyPhone_ThrowsArgumentException(string phone)
    {
        var act = () => new InstallationRequest(
            Guid.NewGuid(), "AC", InstallationType.Standard,
            "John", "john@test.com", phone, "1 Main St", "Sofia", "1000", "BG", 100m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer phone is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyAddress_ThrowsArgumentException(string address)
    {
        var act = () => new InstallationRequest(
            Guid.NewGuid(), "AC", InstallationType.Standard,
            "John", "john@test.com", "+123", address, "Sofia", "1000", "BG", 100m);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Address is required*");
    }

    [Fact]
    public void SetUser_SetsUserId()
    {
        var request = CreateValid();
        var userId = Guid.NewGuid();

        request.SetUser(userId);

        request.UserId.Should().Be(userId);
    }

    [Fact]
    public void SetOrder_SetsOrderId()
    {
        var request = CreateValid();
        var orderId = Guid.NewGuid();

        request.SetOrder(orderId);

        request.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void SetAddressLine2_TrimsValue()
    {
        var request = CreateValid();

        request.SetAddressLine2("  Apt 4  ");

        request.AddressLine2.Should().Be("Apt 4");
    }

    [Fact]
    public void SetPreferredSchedule_SetsDateAndTrimmedTimeSlot()
    {
        var request = CreateValid();
        var date = DateTime.UtcNow.AddDays(3);

        request.SetPreferredSchedule(date, "  morning  ");

        request.PreferredDate.Should().Be(date);
        request.PreferredTimeSlot.Should().Be("morning");
    }

    [Fact]
    public void Schedule_WithPastDate_ThrowsArgumentException()
    {
        var request = CreateValid();

        var act = () => request.Schedule(DateTime.UtcNow.AddDays(-1));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Scheduled date cannot be in the past*");
    }

    [Fact]
    public void Schedule_WithFutureDate_SetsScheduledStatusAndDate()
    {
        var request = CreateValid();
        var date = DateTime.UtcNow.AddDays(5);

        request.Schedule(date);

        request.Status.Should().Be(InstallationRequestStatus.Scheduled);
        request.ScheduledDate.Should().Be(date);
    }

    [Fact]
    public void Confirm_SetsConfirmedStatus()
    {
        var request = CreateValid();

        request.Confirm();

        request.Status.Should().Be(InstallationRequestStatus.Confirmed);
    }

    [Fact]
    public void MarkInProgress_SetsInProgressStatus()
    {
        var request = CreateValid();

        request.MarkInProgress();

        request.Status.Should().Be(InstallationRequestStatus.InProgress);
    }

    [Fact]
    public void Complete_WithoutFinalPrice_UsesEstimatedPrice()
    {
        var request = CreateValid(estimatedPrice: 200m);

        request.Complete();

        request.Status.Should().Be(InstallationRequestStatus.Completed);
        request.CompletedAt.Should().NotBeNull();
        request.FinalPrice.Should().Be(200m);
    }

    [Fact]
    public void Complete_WithFinalPrice_UsesProvidedPrice()
    {
        var request = CreateValid(estimatedPrice: 200m);

        request.Complete(275m);

        request.FinalPrice.Should().Be(275m);
    }

    [Fact]
    public void Cancel_SetsCancelledStatus()
    {
        var request = CreateValid();

        request.Cancel();

        request.Status.Should().Be(InstallationRequestStatus.Cancelled);
    }

    [Fact]
    public void SetNotesAndTechnicianNotes_TrimValues()
    {
        var request = CreateValid();

        request.SetNotes("  customer note  ");
        request.SetTechnicianNotes("  tech note  ");

        request.Notes.Should().Be("customer note");
        request.TechnicianNotes.Should().Be("tech note");
    }
}
