using ClimaSite.Application.Features.Admin.Orders.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Features.Admin.Orders;

public class AddOrderNoteCommandHandlerTests
{
    private readonly MockDbContext _context = new();

    private AddOrderNoteCommandHandler CreateHandler() => new(_context);

    private Order SeedOrder(string email = "buyer@test.com")
    {
        var order = new Order("ORD-NOTE-0001", email);
        _context.AddOrder(order);
        return order;
    }

    [Fact]
    public async Task Handle_AddsNote_ToOrderWithNoExistingNotes()
    {
        var order = SeedOrder();

        var result = await CreateHandler().Handle(new AddOrderNoteCommand
        {
            OrderId = order.Id,
            Note = "Customer called about delivery"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Notes.Should().NotBeNullOrEmpty();
        order.Notes.Should().Contain("Customer called about delivery");
    }

    [Fact]
    public async Task Handle_AppendsNote_PreservingExistingNotes()
    {
        var order = SeedOrder();
        order.SetNotes("[2026-01-01 10:00] First note");

        var result = await CreateHandler().Handle(new AddOrderNoteCommand
        {
            OrderId = order.Id,
            Note = "Second note"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Notes.Should().Contain("First note");
        order.Notes.Should().Contain("Second note");
        // The two notes should be on separate lines.
        order.Notes!.Split('\n').Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        var result = await CreateHandler().Handle(new AddOrderNoteCommand
        {
            OrderId = Guid.NewGuid(),
            Note = "Note for a missing order"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Order not found");
    }
}

public class AddOrderNoteCommandValidatorTests
{
    private readonly AddOrderNoteCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new AddOrderNoteCommand
        {
            OrderId = Guid.NewGuid(),
            Note = "A valid note"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderId_FailsWithOrderIdError()
    {
        var command = new AddOrderNoteCommand
        {
            OrderId = Guid.Empty,
            Note = "A valid note"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrderNoteCommand.OrderId));
    }

    [Fact]
    public void Validate_EmptyNote_FailsWithNoteError()
    {
        var command = new AddOrderNoteCommand
        {
            OrderId = Guid.NewGuid(),
            Note = ""
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrderNoteCommand.Note));
    }

    [Fact]
    public void Validate_NoteTooLong_FailsWithNoteError()
    {
        var command = new AddOrderNoteCommand
        {
            OrderId = Guid.NewGuid(),
            Note = new string('x', 2001)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddOrderNoteCommand.Note));
    }

    [Fact]
    public void Validate_NoteAtMaxLength_Passes()
    {
        var command = new AddOrderNoteCommand
        {
            OrderId = Guid.NewGuid(),
            Note = new string('x', 2000)
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
