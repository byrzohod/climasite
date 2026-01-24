namespace ClimaSite.Application.Features.Gdpr.DTOs;

/// <summary>
/// Complete export of all user data for GDPR compliance
/// </summary>
public record UserDataExportDto
{
    public UserProfileDataDto Profile { get; init; } = null!;
    public List<UserAddressDataDto> Addresses { get; init; } = [];
    public List<UserOrderDataDto> Orders { get; init; } = [];
    public List<UserReviewDataDto> Reviews { get; init; } = [];
    public List<UserWishlistItemDataDto> WishlistItems { get; init; } = [];
    public List<UserQuestionDataDto> Questions { get; init; } = [];
    public List<UserNotificationDataDto> Notifications { get; init; } = [];
    public DateTime ExportedAt { get; init; }
    public string ExportVersion { get; init; } = "1.0";
}

public record UserProfileDataDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public string PreferredLanguage { get; init; } = string.Empty;
    public string PreferredCurrency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record UserAddressDataDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? State { get; init; }
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserOrderDataDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal Total { get; init; }
    public string? PaymentMethod { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemDataDto> Items { get; init; } = [];
    public Dictionary<string, object>? ShippingAddress { get; init; }
    public Dictionary<string, object>? BillingAddress { get; init; }
}

public record OrderItemDataDto
{
    public string ProductName { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public record UserReviewDataDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsVerifiedPurchase { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserWishlistItemDataDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public DateTime AddedAt { get; init; }
}

public record UserQuestionDataDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string QuestionText { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<UserAnswerDataDto> Answers { get; init; } = [];
}

public record UserAnswerDataDto
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record UserNotificationDataDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Data category information for transparency
/// </summary>
public record DataCategoryDto
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string LegalBasis { get; init; } = string.Empty;
    public string RetentionPeriod { get; init; } = string.Empty;
}

/// <summary>
/// Request for account deletion
/// </summary>
public record DeleteAccountRequest
{
    public string Password { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public bool ConfirmDeletion { get; init; }
}
