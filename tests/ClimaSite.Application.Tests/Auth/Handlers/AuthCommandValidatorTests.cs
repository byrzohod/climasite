using ClimaSite.Application.Auth.Commands;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Auth.Handlers;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var command = new RegisterCommand("user@example.com", "Password123!", "John", "Doe", "+359888123456");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithoutOptionalPhone_Passes()
    {
        var command = new RegisterCommand("user@example.com", "Password123!", "John", "Doe");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var command = new RegisterCommand(email, "Password123!", "John", "Doe");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short7")]
    public void Validate_WithTooShortPassword_Fails(string password)
    {
        var command = new RegisterCommand("user@example.com", password, "John", "Doe");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WithEmptyFirstName_Fails()
    {
        var command = new RegisterCommand("user@example.com", "Password123!", "", "Doe");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.FirstName));
    }

    [Fact]
    public void Validate_WithEmptyLastName_Fails()
    {
        var command = new RegisterCommand("user@example.com", "Password123!", "John", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.LastName));
    }

    [Fact]
    public void Validate_WithOverlongName_Fails()
    {
        var command = new RegisterCommand("user@example.com", "Password123!", new string('a', 101), "Doe");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.FirstName));
    }
}

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var command = new LoginCommand("user@example.com", "Password123!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var command = new LoginCommand(email, "Password123!");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Email));
    }

    [Fact]
    public void Validate_WithEmptyPassword_Fails()
    {
        var command = new LoginCommand("user@example.com", "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidToken_Passes()
    {
        var command = new RefreshTokenCommand("a-non-empty-refresh-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyToken_Fails(string? token)
    {
        var command = new RefreshTokenCommand(token!);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshTokenCommand.RefreshToken));
    }
}
