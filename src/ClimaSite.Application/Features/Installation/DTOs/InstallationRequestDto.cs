using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Features.Installation.DTOs;

public class InstallationRequestDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string InstallationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public string? PreferredTimeSlot { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Notes { get; set; }
    public decimal EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InstallationOptionDto
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
    public int EstimatedDays { get; set; }
}

public class ProductInstallationOptionsDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public bool InstallationAvailable { get; set; }
    public List<InstallationOptionDto> Options { get; set; } = new();
}
