using ClimaSite.Application.Features.Admin.Customers.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Customers;

public class UpdateCustomerStatusCommandTests
{
    private readonly MockDbContext _context = new();

    private UpdateCustomerStatusCommandHandler CreateHandler() => new(_context);

    private ApplicationUser SeedUser(bool isActive = true, string? refreshToken = "rt-token")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "customer@test.com",
            UserName = "customer@test.com",
            FirstName = "Cus",
            LastName = "Tomer",
            IsActive = isActive,
            RefreshToken = refreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        _context.Users.Add(user);
        return user;
    }

    [Fact]
    public async Task Handle_DeactivatesCustomer_AndRevokesRefreshToken()
    {
        var user = SeedUser(isActive: true);

        var result = await CreateHandler().Handle(
            new UpdateCustomerStatusCommand { CustomerId = user.Id, IsActive = false },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        // Deactivating must revoke the refresh token so the user is logged out everywhere.
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ActivatesCustomer_WithoutTouchingRefreshToken()
    {
        var user = SeedUser(isActive: false);

        var result = await CreateHandler().Handle(
            new UpdateCustomerStatusCommand { CustomerId = user.Id, IsActive = true },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        // Re-activating should not revoke an existing refresh token.
        user.RefreshToken.Should().Be("rt-token");
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenCustomerNotFound()
    {
        var result = await CreateHandler().Handle(
            new UpdateCustomerStatusCommand { CustomerId = Guid.NewGuid(), IsActive = false },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Customer not found");
    }

    [Fact]
    public void Validator_Passes_WithCustomerId()
    {
        var validator = new UpdateCustomerStatusCommandValidator();

        var result = validator.Validate(
            new UpdateCustomerStatusCommand { CustomerId = Guid.NewGuid(), IsActive = true });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_Fails_WhenCustomerIdIsEmpty()
    {
        var validator = new UpdateCustomerStatusCommandValidator();

        var result = validator.Validate(
            new UpdateCustomerStatusCommand { CustomerId = Guid.Empty, IsActive = true });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateCustomerStatusCommand.CustomerId)
            && e.ErrorMessage == "Customer ID is required");
    }
}
