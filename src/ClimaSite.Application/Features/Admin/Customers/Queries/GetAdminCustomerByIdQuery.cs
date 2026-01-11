using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Customers.DTOs;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Customers.Queries;

public record GetAdminCustomerByIdQuery : IRequest<AdminCustomerDetailDto?>
{
    public Guid Id { get; init; }
}

public class GetAdminCustomerByIdQueryHandler : IRequestHandler<GetAdminCustomerByIdQuery, AdminCustomerDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAdminCustomerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminCustomerDetailDto?> Handle(GetAdminCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            return null;
        }

        // Get order stats
        var orders = await _context.Orders
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var completedOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var totalSpent = completedOrders.Sum(o => o.Total);
        var averageOrderValue = completedOrders.Count > 0 ? totalSpent / completedOrders.Count : 0;

        // Get review count
        var reviewCount = await _context.Reviews
            .CountAsync(r => r.UserId == user.Id, cancellationToken);

        // Get wishlist item count
        var wishlistCount = await _context.WishlistItems
            .Include(wi => wi.Wishlist)
            .CountAsync(wi => wi.Wishlist.UserId == user.Id, cancellationToken);

        return new AdminCustomerDetailDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            PreferredLanguage = user.PreferredLanguage,
            PreferredCurrency = user.PreferredCurrency,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Addresses = user.Addresses.Select(a => new CustomerAddressDto
            {
                Id = a.Id,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                IsDefault = a.IsDefault
            }).ToList(),
            Stats = new CustomerStatsDto
            {
                TotalOrders = completedOrders.Count,
                TotalSpent = totalSpent,
                AverageOrderValue = averageOrderValue,
                ReviewsWritten = reviewCount,
                WishlistItems = wishlistCount
            },
            RecentOrders = orders.Take(10).Select(o => new CustomerOrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Total = o.Total,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            }).ToList()
        };
    }
}
