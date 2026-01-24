using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Auth.Commands;
using ClimaSite.Application.Features.Auth.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Auth.Commands;

public class RegisterCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<RegisterCommandHandler>> _loggerMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<RegisterCommandHandler>>();

        _handler = new RegisterCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            PreferredLanguage = "en",
            PreferredCurrency = "USD"
        };

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

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.User.Email.Should().Be(command.Email);
        result.Value.User.FirstName.Should().Be(command.FirstName);
        result.Value.User.LastName.Should().Be(command.LastName);
        result.Value.User.Roles.Should().Contain("Customer");

        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be(command.Email);
        createdUser.FirstName.Should().Be(command.FirstName);
        createdUser.LastName.Should().Be(command.LastName);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            UserName = "existing@example.com"
        };

        var command = new RegisterCommand
        {
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CallsCreateAsyncWithHashedPassword()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // UserManager.CreateAsync handles password hashing internally
        // We verify it's called with the password (which will be hashed by Identity)
        _userManagerMock.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AssignsCustomerRoleByDefault()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"),
            Times.Once);

        result.Value!.User.Roles.Should().Contain("Customer");
    }

    [Fact]
    public async Task Handle_WhenCreateFails_ReturnsFailureWithErrors()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak",
            FirstName = "John",
            LastName = "Doe"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 8 characters" },
                new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must contain a digit" }
            ));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Password must be at least 8 characters");
        result.Errors.Should().Contain("Password must contain a digit");
    }

    [Fact]
    public async Task Handle_SetsRefreshTokenOnUser()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        ApplicationUser? updatedUser = null;

        _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Customer"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(user => updatedUser = user)
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("generated-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        updatedUser.Should().NotBeNull();
        updatedUser!.RefreshToken.Should().Be("generated-refresh-token");
        updatedUser.RefreshTokenExpiryTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_SetsDefaultLanguageAndCurrency_WhenNotProvided()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            PreferredLanguage = null,
            PreferredCurrency = null
        };

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

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        createdUser.Should().NotBeNull();
        createdUser!.PreferredLanguage.Should().Be("en");
        createdUser.PreferredCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_SetsUserAsActive()
    {
        // Arrange
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

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

        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        createdUser.Should().NotBeNull();
        createdUser!.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithValidCommand_PassesValidation()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithEmptyEmail_FailsValidation()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = "",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validator_WithInvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = "not-an-email",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("Invalid email"));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar1")]
    public void Validator_WithWeakPassword_FailsValidation(string password)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validator_WithMismatchedPasswords_FailsValidation()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword" && e.ErrorMessage.Contains("do not match"));
    }
}
