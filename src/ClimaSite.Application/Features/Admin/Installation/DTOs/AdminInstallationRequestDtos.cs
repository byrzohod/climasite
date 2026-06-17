namespace ClimaSite.Application.Features.Admin.Installation.DTOs;

public record AdminInstallationRequestDto
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string InstallationType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public DateTime? PreferredDate { get; init; }
    public DateTime? ScheduledDate { get; init; }
    public decimal EstimatedPrice { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdminInstallationRequestsListDto
{
    public List<AdminInstallationRequestDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
