using System.Reflection;
using ClimaSite.Application.Features.Admin.Installation.Queries;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Installation;

public class GetAdminInstallationRequestsQueryHandlerTests
{
    private readonly MockDbContext _context = new();

    private static InstallationRequest CreateRequest(
        string productName = "AC Unit",
        InstallationType type = InstallationType.Standard,
        decimal estimatedPrice = 150m,
        DateTime? createdAt = null)
    {
        var request = new InstallationRequest(
            Guid.NewGuid(), productName, type,
            "Jane Buyer", "jane@example.com", "+359888123456",
            "12 Vitosha Blvd", "Sofia", "1000", "Bulgaria", estimatedPrice);

        if (createdAt.HasValue)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt))!
                .SetValue(request, createdAt.Value);
        }

        return request;
    }

    private GetAdminInstallationRequestsQueryHandler CreateHandler() => new(_context);

    [Fact]
    public async Task Handle_ReturnsMappedList_NewestFirst()
    {
        var older = CreateRequest("Older Unit", InstallationType.Standard, 100m,
            createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = CreateRequest("Newer Unit", InstallationType.Premium, 250m,
            createdAt: DateTime.UtcNow);
        _context.AddInstallationRequest(older);
        _context.AddInstallationRequest(newer);

        var result = await CreateHandler().Handle(new GetAdminInstallationRequestsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].ProductName.Should().Be("Newer Unit");
        result.Items[0].InstallationType.Should().Be("Premium");
        result.Items[0].Status.Should().Be("Pending");
        result.Items[0].CustomerEmail.Should().Be("jane@example.com");
        result.Items[1].ProductName.Should().Be("Older Unit");
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        var pending = CreateRequest("Pending Unit");
        var confirmed = CreateRequest("Confirmed Unit");
        confirmed.Confirm();
        _context.AddInstallationRequest(pending);
        _context.AddInstallationRequest(confirmed);

        var result = await CreateHandler().Handle(
            new GetAdminInstallationRequestsQuery { Status = "confirmed" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.ProductName == "Confirmed Unit");
    }

    [Fact]
    public async Task Handle_Paginates()
    {
        for (var i = 0; i < 25; i++)
        {
            _context.AddInstallationRequest(CreateRequest($"Unit {i}", createdAt: DateTime.UtcNow.AddMinutes(-i)));
        }

        var page2 = await CreateHandler().Handle(
            new GetAdminInstallationRequestsQuery { PageNumber = 2, PageSize = 10 }, CancellationToken.None);

        page2.TotalCount.Should().Be(25);
        page2.PageNumber.Should().Be(2);
        page2.PageSize.Should().Be(10);
        page2.TotalPages.Should().Be(3);
        page2.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        var result = await CreateHandler().Handle(new GetAdminInstallationRequestsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
    }
}
