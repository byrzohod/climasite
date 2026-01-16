using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Addresses.Commands;

public record UpdateAddressCommand : IRequest<Result<AddressDto>>
{
    public Guid AddressId { get; set; }
    public string FullName { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? State { get; init; }
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsDefault { get; init; }
    public string Type { get; init; } = "Shipping";
}

public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result<AddressDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAddressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AddressDto>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result<AddressDto>.Failure("User not authenticated");

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId.Value, cancellationToken);

        if (address == null)
            return Result<AddressDto>.Failure("Address not found");

        try
        {
            address.SetFullName(request.FullName);
            address.SetAddressLine1(request.AddressLine1);
            address.SetAddressLine2(request.AddressLine2);
            address.SetCity(request.City);
            address.SetState(request.State);
            address.SetPostalCode(request.PostalCode);
            address.SetCountry(request.Country, request.CountryCode);
            address.SetPhone(request.Phone);

            if (Enum.TryParse<AddressType>(request.Type, out var addressType))
            {
                address.SetType(addressType);
            }

            // Handle default logic
            if (request.IsDefault && !address.IsDefault)
            {
                // Clear default from other addresses
                var existingDefaults = await _context.Addresses
                    .Where(a => a.UserId == userId.Value && a.IsDefault && a.Id != request.AddressId)
                    .ToListAsync(cancellationToken);

                foreach (var existingDefault in existingDefaults)
                {
                    existingDefault.SetDefault(false);
                }

                address.SetDefault(true);
            }
            else if (!request.IsDefault && address.IsDefault)
            {
                // Don't allow removing default if it's the only address
                var addressCount = await _context.Addresses
                    .CountAsync(a => a.UserId == userId.Value, cancellationToken);

                if (addressCount == 1)
                {
                    // Keep it as default if it's the only address
                }
                else
                {
                    address.SetDefault(false);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<AddressDto>.Success(new AddressDto
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
            });
        }
        catch (ArgumentException ex)
        {
            return Result<AddressDto>.Failure(ex.Message);
        }
    }
}
