using ClimaSite.Application.Features.Notifications.Commands;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Notifications.Commands;

public class CreateNotificationCommandValidatorTests
{
    private readonly CreateNotificationCommandValidator _validator = new();

    private static CreateNotificationCommand ValidCommand() => new()
    {
        UserId = Guid.NewGuid(),
        Type = NotificationTypes.OrderPlaced,
        Title = "Order placed",
        Message = "Your order has been placed.",
        Link = "/account/orders/1"
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUserId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { UserId = Guid.Empty });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.UserId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyType_Fails(string type)
    {
        var result = _validator.Validate(ValidCommand() with { Type = type });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Type));
    }

    [Fact]
    public void Validate_TypeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Type = new string('a', 51) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Type));
    }

    [Fact]
    public void Validate_EmptyTitle_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Title = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Title));
    }

    [Fact]
    public void Validate_TitleTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Title = new string('a', 201) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Title));
    }

    [Fact]
    public void Validate_EmptyMessage_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Message = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Message));
    }

    [Fact]
    public void Validate_MessageTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Message = new string('a', 1001) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Message));
    }

    [Fact]
    public void Validate_LinkTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Link = new string('a', 501) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationCommand.Link));
    }

    [Fact]
    public void Validate_NullLink_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { Link = null });

        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateNotificationCommand.Link));
    }
}
