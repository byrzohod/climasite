using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.DTOs;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        if (request.FirstName != null)
            user.FirstName = request.FirstName;
        if (request.LastName != null)
            user.LastName = request.LastName;
        if (request.Phone != null)
            user.PhoneNumber = request.Phone;
        if (request.PreferredLanguage != null)
            user.PreferredLanguage = request.PreferredLanguage;
        if (request.PreferredCurrency != null)
            user.PreferredCurrency = request.PreferredCurrency;

        user.SetUpdatedAt();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Profile update failed for {UserId}: {Errors}", request.UserId, errors);
            return Result<UserDto>.Failure(errors);
        }

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("Profile updated for user: {UserId}", user.Id);

        return Result<UserDto>.Success(new UserDto(
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
        ));
    }
}
