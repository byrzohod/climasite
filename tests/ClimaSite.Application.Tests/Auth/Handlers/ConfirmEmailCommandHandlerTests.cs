using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var logger = new Mock<ILogger<ConfirmEmailCommandHandler>>();

        _handler = new ConfirmEmailCommandHandler(_userManagerMock.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidToken_ConfirmsEmail()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new ConfirmEmailCommand("valid-token", user.Email!);

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _userManagerMock.Verify(x => x.ConfirmEmailAsync(user, command.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsFailureWithoutConfirming()
    {
        // Arrange
        var command = new ConfirmEmailCommand("valid-token", "nobody@example.com");
        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Generic message avoids leaking whether the email exists.
        result.Error.Should().Be("Invalid token or email");
        _userManagerMock.Verify(
            x => x.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsFailureWithErrors()
    {
        // Arrange
        var user = CreateTestUser();
        var command = new ConfirmEmailCommand("bad-token", user.Email!);

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock
            .Setup(x => x.ConfirmEmailAsync(user, command.Token))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid token.");
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
