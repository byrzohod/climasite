using System.Security.Cryptography;
using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.DTOs;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
            return Result<LoginResponseDto>.Failure("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user: {Email}", request.Email);
            return Result<LoginResponseDto>.Failure("Account is deactivated");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login attempt for locked out user: {Email}", request.Email);
            return Result<LoginResponseDto>.Failure("Account is temporarily locked. Please try again later.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Failed login attempt for: {Email}", request.Email);
            return Result<LoginResponseDto>.Failure("Invalid email or password");
        }

        // Reset access failed count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);
        user.RecordLogin();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.EmailConfirmed,
            roles.FirstOrDefault() ?? "Customer",
            user.PreferredLanguage,
            user.PreferredCurrency,
            user.CreatedAt,
            user.LastLoginAt
        );

        return Result<LoginResponseDto>.Success(new LoginResponseDto(accessToken, refreshToken, userDto));
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
