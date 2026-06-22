using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Application.Auth.Queries;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

        _handler = new GetUserByIdQueryHandler(_userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ReturnsMappedUserDto()
    {
        // Arrange
        var user = CreateTestUser();
        user.EmailConfirmed = true;
        user.PhoneNumber = "+359888123456";
        user.LastLoginAt = DateTime.UtcNow.AddHours(-1);

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

        // Act
        var result = await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.FirstName.Should().Be(user.FirstName);
        result.Value.LastName.Should().Be(user.LastName);
        result.Value.Phone.Should().Be(user.PhoneNumber);
        result.Value.EmailConfirmed.Should().BeTrue();
        result.Value.Role.Should().Be("Admin");
        result.Value.PreferredLanguage.Should().Be(user.PreferredLanguage);
        result.Value.PreferredCurrency.Should().Be(user.PreferredCurrency);
        result.Value.LastLoginAt.Should().Be(user.LastLoginAt);
    }

    [Fact]
    public async Task Handle_WithNoRoles_DefaultsRoleToCustomer()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    private static ApplicationUser CreateTestUser(string? email = null)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email ?? "test@example.com",
            UserName = email ?? "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            PreferredLanguage = "en",
            PreferredCurrency = "USD",
            CreatedAt = DateTime.UtcNow
        };
    }
}
