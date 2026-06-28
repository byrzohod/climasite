using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.DTOs;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ClimaSite.Application.Auth.Handlers;

/// <summary>
/// Handles the Google OIDC ID-token sign-in. It verifies the Google ID token, then resolves the
/// application user (linked Google login → existing email → newly created account) and issues the
/// SAME app JWT + refresh token as <see cref="LoginCommandHandler"/>. On any invalid/unverified
/// token it returns a failure that the controller maps to 401.
/// </summary>
public class GoogleSignInCommandHandler : IRequestHandler<GoogleSignInCommand, Result<LoginResponseDto>>
{
    private const string LoginProvider = "Google";

    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSignInCommandHandler> _logger;

    public GoogleSignInCommandHandler(
        IGoogleTokenValidator googleTokenValidator,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<GoogleSignInCommandHandler> logger)
    {
        _googleTokenValidator = googleTokenValidator;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        // SECURITY: never log the raw ID token.
        var googleUser = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (googleUser == null)
        {
            _logger.LogWarning("Google sign-in rejected: invalid or unverifiable ID token.");
            return Result<LoginResponseDto>.Failure("Invalid Google credentials");
        }

        if (!googleUser.EmailVerified || string.IsNullOrWhiteSpace(googleUser.Email))
        {
            _logger.LogWarning("Google sign-in rejected: Google email is not verified.");
            return Result<LoginResponseDto>.Failure("Invalid Google credentials");
        }

        var user = await ResolveUserAsync(googleUser);
        if (!user.IsSuccess)
        {
            return Result<LoginResponseDto>.Failure(user.Error!);
        }

        var applicationUser = user.Value!;

        if (!applicationUser.IsActive)
        {
            _logger.LogWarning("Google sign-in for deactivated user: {Email}", applicationUser.Email);
            return Result<LoginResponseDto>.Failure("Account is deactivated");
        }

        applicationUser.RecordLogin();
        await _userManager.UpdateAsync(applicationUser);

        var roles = await _userManager.GetRolesAsync(applicationUser);
        var accessToken = GenerateAccessToken(applicationUser, roles);
        var refreshToken = GenerateRefreshToken();

        applicationUser.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _userManager.UpdateAsync(applicationUser);

        _logger.LogInformation("User signed in with Google: {Email}", applicationUser.Email);

        var userDto = new UserDto(
            applicationUser.Id,
            applicationUser.Email!,
            applicationUser.FirstName,
            applicationUser.LastName,
            applicationUser.PhoneNumber,
            applicationUser.EmailConfirmed,
            roles.FirstOrDefault() ?? "Customer",
            applicationUser.PreferredLanguage,
            applicationUser.PreferredCurrency,
            applicationUser.CreatedAt,
            applicationUser.LastLoginAt
        );

        return Result<LoginResponseDto>.Success(new LoginResponseDto(accessToken, refreshToken, userDto));
    }

    /// <summary>
    /// Resolves the application user for a verified Google identity:
    /// 1. an account already linked to this Google subject, else
    /// 2. an existing account with the same email (the Google login is linked to it), else
    /// 3. a freshly created account (with the Google login attached).
    /// </summary>
    private async Task<Result<ApplicationUser>> ResolveUserAsync(GoogleUserInfo googleUser)
    {
        // 1. Already linked to this Google subject.
        var user = await _userManager.FindByLoginAsync(LoginProvider, googleUser.Subject);
        if (user != null)
        {
            return Result<ApplicationUser>.Success(user);
        }

        // 2. Existing local account with the same email — link Google to it (never duplicate).
        user = await _userManager.FindByEmailAsync(googleUser.Email);
        if (user != null)
        {
            var linkExisting = await _userManager.AddLoginAsync(
                user, new UserLoginInfo(LoginProvider, googleUser.Subject, LoginProvider));
            if (!linkExisting.Succeeded)
            {
                _logger.LogError("Failed to link Google login to existing account {Email}: {Errors}",
                    googleUser.Email, DescribeErrors(linkExisting));
                return Result<ApplicationUser>.Failure("Could not link Google account");
            }

            return Result<ApplicationUser>.Success(user);
        }

        // 3. First-time Google sign-in — create a confirmed, active account.
        var newUser = new ApplicationUser
        {
            UserName = googleUser.Email,
            Email = googleUser.Email,
            FirstName = googleUser.GivenName ?? string.Empty,
            LastName = googleUser.FamilyName ?? string.Empty,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            var errors = DescribeErrors(createResult);
            _logger.LogWarning("Google sign-in could not create user for {Email}: {Errors}", googleUser.Email, errors);
            return Result<ApplicationUser>.Failure(errors);
        }

        await _userManager.AddToRoleAsync(newUser, "Customer");

        var linkNew = await _userManager.AddLoginAsync(
            newUser, new UserLoginInfo(LoginProvider, googleUser.Subject, LoginProvider));
        if (!linkNew.Succeeded)
        {
            _logger.LogError("Failed to attach Google login to new account {Email}: {Errors}",
                googleUser.Email, DescribeErrors(linkNew));
            return Result<ApplicationUser>.Failure("Could not link Google account");
        }

        return Result<ApplicationUser>.Success(newUser);
    }

    private static string DescribeErrors(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));

    // Mirrors LoginCommandHandler so a Google session yields an identical app JWT.
    private string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? jwtSettings["Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET or JwtSettings:Secret.");
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? jwtSettings["Issuer"]
            ?? "https://localhost:5001";
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? jwtSettings["Audience"]
            ?? "https://localhost:4200";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15"));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
