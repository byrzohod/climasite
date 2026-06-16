using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Outbox;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailOutbox _emailOutbox;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailOutbox emailOutbox,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailOutbox = emailOutbox;
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

        // Queue the reset link for durable delivery via the outbox (GAP-03/ARCH-05). Enqueue is
        // best-effort: a DB hiccup must not 500 the request or reveal whether the address exists,
        // and the token (a credential) is NEVER logged.
        try
        {
            await _emailOutbox.QueueAsync(
                OutboxMessage.ForPasswordReset(user.Email!, token),
                cancellationToken);
            _logger.LogInformation("Password reset email queued for user {UserId}.", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue password reset email for user {UserId}.", user.Id);
        }

        return Result<bool>.Success(true);
    }
}
