using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Notifications.Queries;

public record GetNotificationsQuery : IRequest<NotificationsListDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool? IsRead { get; init; }
    public string? Type { get; init; }
}

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationsListDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationsListDto> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return new NotificationsListDto();
        }

        var query = _context.Notifications
            .Where(n => n.UserId == userId.Value)
            .AsQueryable();

        // Filter by read status
        if (request.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == request.IsRead.Value);
        }

        // Filter by type
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(n => n.Type == request.Type);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await _context.Notifications
            .CountAsync(n => n.UserId == userId.Value && !n.IsRead, cancellationToken);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                Link = n.Link,
                Data = n.Data,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new NotificationsListDto
        {
            Items = notifications,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
