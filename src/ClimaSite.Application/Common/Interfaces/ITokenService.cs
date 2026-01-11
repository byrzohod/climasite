using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    (bool IsValid, Guid UserId) ValidateRefreshToken(string refreshToken);
}
