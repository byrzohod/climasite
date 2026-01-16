using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Addresses.Commands;

public record SetDefaultAddressCommand : IRequest<Result<AddressDto>>
{
    public Guid AddressId { get; init; }
}

public class SetDefaultAddressCommandHandler : IRequestHandler<SetDefaultAddressCommand, Result<AddressDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetDefaultAddressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AddressDto>> Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result<AddressDto>.Failure("User not authenticated");

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId.Value, cancellationToken);

        if (address == null)
            return Result<AddressDto>.Failure("Address not found");

        // Already default, just return it
        if (address.IsDefault)
        {
            return Result<AddressDto>.Success(MapToDto(address));
        }

        // Clear default from other addresses
        var existingDefaults = await _context.Addresses
            .Where(a => a.UserId == userId.Value && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var existingDefault in existingDefaults)
        {
            existingDefault.SetDefault(false);
        }

        address.SetDefault(true);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<AddressDto>.Success(MapToDto(address));
    }

    private static AddressDto MapToDto(ClimaSite.Core.Entities.Address address)
    {
        return new AddressDto
        {
            Id = address.Id,
            FullName = address.FullName,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            CountryCode = address.CountryCode,
            Phone = address.Phone,
            IsDefault = address.IsDefault,
            Type = address.Type.ToString(),
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }
}
