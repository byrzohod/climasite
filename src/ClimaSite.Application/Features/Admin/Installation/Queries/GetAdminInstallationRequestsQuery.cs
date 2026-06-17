using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Installation.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Installation.Queries;

public record GetAdminInstallationRequestsQuery : IRequest<AdminInstallationRequestsListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>Optional case-insensitive <see cref="InstallationRequestStatus"/> name to filter on.</summary>
    public string? Status { get; init; }
}

public class GetAdminInstallationRequestsQueryHandler
    : IRequestHandler<GetAdminInstallationRequestsQuery, AdminInstallationRequestsListDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminInstallationRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminInstallationRequestsListDto> Handle(
        GetAdminInstallationRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        IQueryable<InstallationRequest> query = _context.InstallationRequests;

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<InstallationRequestStatus>(request.Status, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminInstallationRequestDto
            {
                Id = r.Id,
                ProductName = r.ProductName,
                InstallationType = r.InstallationType.ToString(),
                Status = r.Status.ToString(),
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                CustomerPhone = r.CustomerPhone,
                City = r.City,
                Country = r.Country,
                PreferredDate = r.PreferredDate,
                ScheduledDate = r.ScheduledDate,
                EstimatedPrice = r.EstimatedPrice,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminInstallationRequestsListDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}
