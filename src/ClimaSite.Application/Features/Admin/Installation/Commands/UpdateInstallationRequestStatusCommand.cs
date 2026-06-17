using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Installation.Commands;

public record UpdateInstallationRequestStatusCommand : IRequest<Result>
{
    public Guid Id { get; init; }

    /// <summary>Target <see cref="InstallationRequestStatus"/> name (case-insensitive).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Required (and must be in the future) when transitioning to <c>Scheduled</c>.</summary>
    public DateTime? ScheduledDate { get; init; }

    /// <summary>Optional override applied when transitioning to <c>Completed</c>.</summary>
    public decimal? FinalPrice { get; init; }
}

public class UpdateInstallationRequestStatusCommandValidator
    : AbstractValidator<UpdateInstallationRequestStatusCommand>
{
    public UpdateInstallationRequestStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Installation request ID is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(BeValidStatus).WithMessage("Invalid installation request status.");
    }

    private static bool BeValidStatus(string status) =>
        Enum.TryParse<InstallationRequestStatus>(status, true, out _);
}

public class UpdateInstallationRequestStatusCommandHandler
    : IRequestHandler<UpdateInstallationRequestStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateInstallationRequestStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(
        UpdateInstallationRequestStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<InstallationRequestStatus>(request.Status, true, out var targetStatus))
        {
            return Result.Failure("Invalid installation request status.");
        }

        var installationRequest = await _context.InstallationRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (installationRequest == null)
        {
            return Result.Failure("Installation request not found.");
        }

        switch (targetStatus)
        {
            case InstallationRequestStatus.Confirmed:
                installationRequest.Confirm();
                break;

            case InstallationRequestStatus.Scheduled:
                if (!request.ScheduledDate.HasValue)
                {
                    return Result.Failure("A scheduled date is required to schedule an installation.");
                }
                if (request.ScheduledDate.Value <= DateTime.UtcNow)
                {
                    return Result.Failure("The scheduled date must be in the future.");
                }
                installationRequest.Schedule(request.ScheduledDate.Value);
                break;

            case InstallationRequestStatus.InProgress:
                installationRequest.MarkInProgress();
                break;

            case InstallationRequestStatus.Completed:
                installationRequest.Complete(request.FinalPrice);
                break;

            case InstallationRequestStatus.Cancelled:
                installationRequest.Cancel();
                break;

            case InstallationRequestStatus.Pending:
            default:
                return Result.Failure("Unsupported status transition.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
