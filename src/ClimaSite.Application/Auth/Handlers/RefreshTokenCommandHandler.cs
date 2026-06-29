using System.Security.Cryptography;
using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<TokenResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || !user.IsRefreshTokenValid())
        {
            _logger.LogWarning("Invalid or expired refresh token attempt");
            return Result<TokenResponseDto>.Failure("Invalid or expired refresh token");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Refresh token attempt for deactivated user: {UserId}", user.Id);
            return Result<TokenResponseDto>.Failure("Account is deactivated");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        // Rotate refresh token
        user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Token refreshed for user: {UserId}", user.Id);

        return Result<TokenResponseDto>.Success(new TokenResponseDto(newAccessToken, newRefreshToken));
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
