using ClimaSite.Application.Features.Admin.Orders.Commands;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class UpdateShippingInfoCommandValidatorTests
{
    private readonly UpdateShippingInfoCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommandWithOrderId_Passes()
    {
        var command = new UpdateShippingInfoCommand
        {
            OrderId = Guid.NewGuid(),
            TrackingNumber = "TRACK-123",
            ShippingMethod = "express",
            MarkAsShipped = true
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OrderIdOnly_Passes()
    {
        // Tracking / method / mark-as-shipped are all optional; only OrderId is required.
        var command = new UpdateShippingInfoCommand
        {
            OrderId = Guid.NewGuid()
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_FailsWithOrderIdError()
    {
        var command = new UpdateShippingInfoCommand
        {
            OrderId = Guid.Empty,
            TrackingNumber = "TRACK-123"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateShippingInfoCommand.OrderId));
    }
}
