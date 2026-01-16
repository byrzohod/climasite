using ClimaSite.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Addresses.Queries;

public record GetUserAddressesQuery : IRequest<List<AddressDto>>;

public class GetUserAddressesQueryHandler : IRequestHandler<GetUserAddressesQuery, List<AddressDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserAddressesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<AddressDto>> Handle(GetUserAddressesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
            return new List<AddressDto>();

        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId.Value)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken);

        return addresses.Select(a => new AddressDto
        {
            Id = a.Id,
            FullName = a.FullName,
            AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2,
            City = a.City,
            State = a.State,
            PostalCode = a.PostalCode,
            Country = a.Country,
            CountryCode = a.CountryCode,
            Phone = a.Phone,
            IsDefault = a.IsDefault,
            Type = a.Type.ToString(),
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();
    }
}
