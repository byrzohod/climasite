using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Addresses.Commands;

public record DeleteAddressCommand : IRequest<Result<bool>>
{
    public Guid AddressId { get; init; }
}

public class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAddressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result<bool>.Failure("User not authenticated");

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId.Value, cancellationToken);

        if (address == null)
            return Result<bool>.Failure("Address not found");

        // If deleting the default address, set another address as default
        if (address.IsDefault)
        {
            var anotherAddress = await _context.Addresses
                .Where(a => a.UserId == userId.Value && a.Id != request.AddressId)
                .FirstOrDefaultAsync(cancellationToken);

            if (anotherAddress != null)
            {
                anotherAddress.SetDefault(true);
            }
        }

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
