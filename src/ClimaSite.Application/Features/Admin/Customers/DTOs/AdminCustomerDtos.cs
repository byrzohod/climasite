namespace ClimaSite.Application.Features.Admin.Customers.DTOs;

public record AdminCustomerListItemDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public int OrderCount { get; init; }
    public decimal TotalSpent { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdminCustomerDetailDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public string PreferredLanguage { get; init; } = "en";
    public string PreferredCurrency { get; init; } = "USD";
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<CustomerAddressDto> Addresses { get; init; } = [];
    public CustomerStatsDto Stats { get; init; } = new();
    public List<CustomerOrderSummaryDto> RecentOrders { get; init; } = [];
}

public record CustomerAddressDto
{
    public Guid Id { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public record CustomerStatsDto
{
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int ReviewsWritten { get; init; }
    public int WishlistItems { get; init; }
}

public record CustomerOrderSummaryDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record AdminCustomersListDto
{
    public List<AdminCustomerListItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
