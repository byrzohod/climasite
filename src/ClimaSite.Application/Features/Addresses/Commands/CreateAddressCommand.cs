using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Addresses.Commands;

public record CreateAddressCommand : IRequest<Result<AddressDto>>
{
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

public class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, Result<AddressDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateAddressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AddressDto>> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return Result<AddressDto>.Failure("User not authenticated");

        try
        {
            var address = new Address(
                userId.Value,
                request.FullName,
                request.AddressLine1,
                request.City,
                request.PostalCode,
                request.Country,
                request.CountryCode
            );

            address.SetAddressLine2(request.AddressLine2);
            address.SetState(request.State);
            address.SetPhone(request.Phone);

            if (Enum.TryParse<AddressType>(request.Type, out var addressType))
            {
                address.SetType(addressType);
            }

            // If this is the first address or marked as default, handle default logic
            if (request.IsDefault)
            {
                // Clear default from other addresses
                var existingAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId.Value && a.IsDefault)
                    .ToListAsync(cancellationToken);

                foreach (var existingAddress in existingAddresses)
                {
                    existingAddress.SetDefault(false);
                }

                address.SetDefault(true);
            }
            else
            {
                // If no addresses exist, make this the default
                var hasAddresses = await _context.Addresses
                    .AnyAsync(a => a.UserId == userId.Value, cancellationToken);

                if (!hasAddresses)
                {
                    address.SetDefault(true);
                }
            }

            _context.Addresses.Add(address);
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
