using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Orders.Commands;

public record AddOrderNoteCommand : IRequest<Result>
{
    public Guid OrderId { get; init; }
    public string Note { get; init; } = string.Empty;
}

public class AddOrderNoteCommandValidator : AbstractValidator<AddOrderNoteCommand>
{
    public AddOrderNoteCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Note is required")
            .MaximumLength(2000).WithMessage("Note cannot exceed 2000 characters");
    }
}

public class AddOrderNoteCommandHandler : IRequestHandler<AddOrderNoteCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AddOrderNoteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AddOrderNoteCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result.Failure("Order not found");
        }

        var existingNotes = order.Notes ?? "";
        var noteEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Note}";
        order.SetNotes(string.IsNullOrEmpty(existingNotes) ? noteEntry : $"{existingNotes}\n{noteEntry}");

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
