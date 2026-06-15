using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal whether the account exists, and don't log the attempted address.
            _logger.LogInformation("Password reset requested for a non-existent account.");
            return Result<bool>.Success(true);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Send the reset link by email — best-effort. A delivery failure must not 500 the
        // request or reveal whether the address exists, and the token (a credential) is NEVER
        // logged. (Reliable delivery via an outbox is GAP-03/ARCH-05.)
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email!, token, cancellationToken);
            _logger.LogInformation("Password reset email dispatched for user {UserId}.", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch password reset email for user {UserId}.", user.Id);
        }

        return Result<bool>.Success(true);
    }
}
