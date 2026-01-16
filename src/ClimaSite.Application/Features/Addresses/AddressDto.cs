namespace ClimaSite.Application.Features.Addresses;

public record AddressDto
{
    public Guid Id { get; init; }
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
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
