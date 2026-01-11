using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;
    // TODO: Inject email service when implemented

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist
            _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
            return Result<bool>.Success(true);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO: Send email with reset link
        _logger.LogInformation("Password reset token generated for user: {UserId}. Token: {Token}", user.Id, token);

        // In production, send email instead of logging token
        // await _emailService.SendPasswordResetEmailAsync(user.Email!, token);

        return Result<bool>.Success(true);
    }
}
