using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Auth.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Features.Auth.Commands;

public record RefreshTokenCommand : IRequest<Result<AuthResponseDto>>
{
    public string RefreshToken { get; init; } = string.Empty;
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
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

    public async Task<Result<AuthResponseDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var (isValid, userId) = _tokenService.ValidateRefreshToken(request.RefreshToken);
        if (!isValid)
        {
            return Result<AuthResponseDto>.Failure("Invalid refresh token.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.RefreshToken != request.RefreshToken)
        {
            return Result<AuthResponseDto>.Failure("Invalid refresh token.");
        }

        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            return Result<AuthResponseDto>.Failure("Refresh token has expired. Please login again.");
        }

        if (!user.IsActive)
        {
            return Result<AuthResponseDto>.Failure("Your account has been deactivated.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Refresh token issued for user {Email}", user.Email);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                PreferredLanguage = user.PreferredLanguage,
                PreferredCurrency = user.PreferredCurrency
            }
        });
    }
}
