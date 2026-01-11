using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Notifications.Queries;

public record GetNotificationSummaryQuery : IRequest<NotificationSummaryDto>
{
    public int RecentCount { get; init; } = 5;
}

public class GetNotificationSummaryQueryHandler : IRequestHandler<GetNotificationSummaryQuery, NotificationSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationSummaryDto> Handle(GetNotificationSummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return new NotificationSummaryDto();
        }

        var totalCount = await _context.Notifications
            .CountAsync(n => n.UserId == userId.Value, cancellationToken);

        var unreadCount = await _context.Notifications
            .CountAsync(n => n.UserId == userId.Value && !n.IsRead, cancellationToken);

        var recentNotifications = await _context.Notifications
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.RecentCount)
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

        return new NotificationSummaryDto
        {
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            RecentItems = recentNotifications
        };
    }
}
