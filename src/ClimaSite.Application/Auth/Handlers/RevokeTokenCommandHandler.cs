using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClimaSite.Application.Auth.Handlers;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<RevokeTokenCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("Token not found");
        }

        user.RevokeRefreshToken();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Refresh token revoked for user: {UserId}", user.Id);

        return Result<bool>.Success(true);
    }
}
