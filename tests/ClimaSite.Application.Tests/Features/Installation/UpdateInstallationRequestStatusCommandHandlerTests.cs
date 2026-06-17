using ClimaSite.Application.Features.Admin.Installation.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Tests.Features.Installation;

public class UpdateInstallationRequestStatusCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private InstallationRequest SeedRequest()
    {
        var request = new InstallationRequest(
            Guid.NewGuid(), "AC Unit", InstallationType.Standard,
            "Jane Buyer", "jane@example.com", "+359888123456",
            "12 Vitosha Blvd", "Sofia", "1000", "Bulgaria", 150m);
        _context.AddInstallationRequest(request);
        return request;
    }

    private UpdateInstallationRequestStatusCommandHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_Confirmed_SetsConfirmed()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Confirmed" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(InstallationRequestStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_Scheduled_WithFutureDate_SetsScheduled()
    {
        var request = SeedRequest();
        var date = DateTime.UtcNow.AddDays(5);

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Scheduled", ScheduledDate = date },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(InstallationRequestStatus.Scheduled);
        request.ScheduledDate.Should().BeCloseTo(date, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_Scheduled_MissingDate_Fails()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Scheduled" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        request.Status.Should().Be(InstallationRequestStatus.Pending);
    }

    [Fact]
    public async Task Handle_Scheduled_PastDate_Fails()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand
            {
                Id = request.Id,
                Status = "Scheduled",
                ScheduledDate = DateTime.UtcNow.AddDays(-1)
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        request.Status.Should().Be(InstallationRequestStatus.Pending);
    }

    [Fact]
    public async Task Handle_InProgress_SetsInProgress()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "InProgress" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(InstallationRequestStatus.InProgress);
    }

    [Fact]
    public async Task Handle_Completed_WithFinalPrice_SetsCompletedAndPrice()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Completed", FinalPrice = 199.99m },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(InstallationRequestStatus.Completed);
        request.FinalPrice.Should().Be(199.99m);
        request.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Completed_WithoutFinalPrice_FallsBackToEstimate()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Completed" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.FinalPrice.Should().Be(150m);
    }

    [Fact]
    public async Task Handle_Cancelled_SetsCancelled()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Cancelled" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(InstallationRequestStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_UnknownRequest_Fails()
    {
        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = Guid.NewGuid(), Status = "Confirmed" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_InvalidStatus_Fails()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Bogus" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PendingTransition_Fails()
    {
        var request = SeedRequest();

        var result = await CreateHandler().Handle(
            new UpdateInstallationRequestStatusCommand { Id = request.Id, Status = "Pending" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
