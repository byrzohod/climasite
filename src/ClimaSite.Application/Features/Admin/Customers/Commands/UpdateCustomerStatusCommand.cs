using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Customers.Commands;

public record UpdateCustomerStatusCommand : IRequest<Result>
{
    public Guid CustomerId { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateCustomerStatusCommandValidator : AbstractValidator<UpdateCustomerStatusCommand>
{
    public UpdateCustomerStatusCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");
    }
}

public class UpdateCustomerStatusCommandHandler : IRequestHandler<UpdateCustomerStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateCustomerStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateCustomerStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.CustomerId, cancellationToken);

        if (user == null)
        {
            return Result.Failure("Customer not found");
        }

        user.IsActive = request.IsActive;
        user.SetUpdatedAt();

        // If deactivating, revoke refresh token
        if (!request.IsActive)
        {
            user.RevokeRefreshToken();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
