namespace ClimaSite.Application.Features.Notifications.DTOs;

public record NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Link { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record NotificationsListDto
{
    public List<NotificationDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int UnreadCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
}

public record NotificationSummaryDto
{
    public int TotalCount { get; init; }
    public int UnreadCount { get; init; }
    public List<NotificationDto> RecentItems { get; init; } = [];
}
