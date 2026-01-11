namespace ClimaSite.Application.Auth.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool EmailConfirmed,
    string Role,
    string PreferredLanguage,
    string PreferredCurrency,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
