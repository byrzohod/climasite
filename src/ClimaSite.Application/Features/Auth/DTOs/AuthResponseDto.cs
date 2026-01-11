namespace ClimaSite.Application.Features.Auth.DTOs;

public record AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string Token => AccessToken; // Alias for API compatibility
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiration { get; init; }
    public UserDto User { get; init; } = null!;
    public Guid UserId => User?.Id ?? Guid.Empty; // Alias for API compatibility
}

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public IList<string> Roles { get; init; } = new List<string>();
    public string PreferredLanguage { get; init; } = "en";
    public string PreferredCurrency { get; init; } = "USD";
}
