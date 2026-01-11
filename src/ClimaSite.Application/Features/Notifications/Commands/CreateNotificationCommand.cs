using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;

namespace ClimaSite.Application.Features.Notifications.Commands;

public record CreateNotificationCommand : IRequest<Result<Guid>>
{
    public Guid UserId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Link { get; init; }
    public Dictionary<string, object>? Data { get; init; }
}

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Notification type is required")
            .MaximumLength(50).WithMessage("Type cannot exceed 50 characters");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters");

        RuleFor(x => x.Link)
            .MaximumLength(500).When(x => x.Link != null)
            .WithMessage("Link cannot exceed 500 characters");
    }
}

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateNotificationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification(
            request.UserId,
            request.Type,
            request.Title,
            request.Message);

        if (!string.IsNullOrWhiteSpace(request.Link))
        {
            notification.SetLink(request.Link);
        }

        if (request.Data != null)
        {
            notification.SetData(request.Data);
        }

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(notification.Id);
    }
}
