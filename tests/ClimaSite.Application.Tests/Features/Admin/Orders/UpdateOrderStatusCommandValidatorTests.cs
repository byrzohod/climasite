using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class UpdateOrderStatusCommandValidatorTests
{
    private readonly UpdateOrderStatusCommandValidator _validator = new();

    [Theory]
    [InlineData(nameof(OrderStatus.Paid))]
    [InlineData(nameof(OrderStatus.Shipped))]
    [InlineData(nameof(OrderStatus.Cancelled))]
    [InlineData("paid")]   // case-insensitive parse
    [InlineData("SHIPPED")]
    public void Validate_ValidStatus_Passes(string status)
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = Guid.NewGuid(),
            Status = status
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_FailsWithOrderIdError()
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = Guid.Empty,
            Status = nameof(OrderStatus.Paid)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOrderStatusCommand.OrderId));
    }

    [Fact]
    public void Validate_EmptyStatus_FailsWithStatusError()
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = Guid.NewGuid(),
            Status = ""
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOrderStatusCommand.Status));
    }

    [Theory]
    [InlineData("NotARealStatus")]
    [InlineData("Done")]
    [InlineData("ship ped")]
    public void Validate_InvalidStatus_FailsWithStatusError(string status)
    {
        var command = new UpdateOrderStatusCommand
        {
            OrderId = Guid.NewGuid(),
            Status = status
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateOrderStatusCommand.Status));
    }
}
