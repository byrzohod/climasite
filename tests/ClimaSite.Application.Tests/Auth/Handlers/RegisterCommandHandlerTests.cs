using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class RegisterCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IEmailOutbox> _emailOutboxMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _emailOutboxMock = new Mock<IEmailOutbox>();
        var logger = new Mock<ILogger<RegisterCommandHandler>>();

        _handler = new RegisterCommandHandler(_userManagerMock.Object, _emailOutboxMock.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesUserAndReturnsUserDto()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "new@example.com",
            Password: "Password123!",
            FirstName: "John",
            LastName: "Doe",
            Phone: "+359888123456");

        ApplicationUser? createdUser = null;

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(command.Email);
        result.Value.FirstName.Should().Be(command.FirstName);
        result.Value.LastName.Should().Be(command.LastName);
        result.Value.Phone.Should().Be(command.Phone);
        result.Value.Role.Should().Be("Customer");

        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(command.Email);
        createdUser.UserName.Should().Be(command.Email);
        createdUser.FirstName.Should().Be(command.FirstName);
        createdUser.LastName.Should().Be(command.LastName);
        createdUser.PhoneNumber.Should().Be(command.Phone);

        // A welcome email is queued via the outbox for the new user (ARCH-05).
        _emailOutboxMock.Verify(x => x.QueueAsync(
            It.Is<OutboxMessage>(m =>
                m.Type == OutboxMessageTypes.Welcome && m.ToEmail == command.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWelcomeEmailEnqueueFails_StillSucceeds()
    {
        // Arrange — enqueue failures must never block registration.
        var command = new RegisterCommand("new@example.com", "Password123!", "John", "Doe");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });
        _emailOutboxMock.Setup(x => x.QueueAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AssignsCustomerRoleByDefault()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "Password123!", "John", "Doe");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"), Times.Once);
        result.Value!.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsFailureAndDoesNotCreate()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            UserName = "existing@example.com"
        };
        var command = new RegisterCommand("existing@example.com", "Password123!", "John", "Doe");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already registered");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCreateFails_ReturnsFailureWithIdentityErrors()
    {
        // Arrange
        var command = new RegisterCommand("new@example.com", "weak", "John", "Doe");

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 8 characters" },
                new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must contain a digit" }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Password must be at least 8 characters");
        result.Error.Should().Contain("Password must contain a digit");
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }
}
