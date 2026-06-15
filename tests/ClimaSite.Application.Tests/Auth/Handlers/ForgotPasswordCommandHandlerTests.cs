using ClimaSite.Application.Auth.Commands;
using ClimaSite.Application.Auth.Handlers;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Auth.Handlers;

/// <summary>
/// BUG-07 regression: forgot-password must dispatch the reset email and must never log the
/// reset token (a credential). Targets the LIVE handler in ClimaSite.Application.Auth.Handlers.
/// </summary>
public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _logger;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _emailService = new Mock<IEmailService>();
        _logger = new Mock<ILogger<ForgotPasswordCommandHandler>>();
        _handler = new ForgotPasswordCommandHandler(_userManager.Object, _emailService.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_SendsResetEmailAndNeverLogsTheToken()
    {
        const string email = "user@example.com";
        const string token = "super-secret-reset-token-xyz";
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };

        _userManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);

        var result = await _handler.Handle(new ForgotPasswordCommand(email), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(
            x => x.SendPasswordResetEmailAsync(email, token, It.IsAny<CancellationToken>()), Times.Once);

        // The reset token is a credential and must never be written to the log.
        _logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(token)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailDispatchThrows_StillSucceedsAndDoesNotLogTheToken()
    {
        const string email = "user@example.com";
        const string token = "another-secret-token";
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };

        _userManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);
        _emailService
            .Setup(x => x.SendPasswordResetEmailAsync(email, token, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await _handler.Handle(new ForgotPasswordCommand(email), CancellationToken.None);

        // Best-effort delivery: the request still succeeds even when the email send fails.
        result.IsSuccess.Should().BeTrue();

        // The token must never reach the log, including the failure path.
        _logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(token)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ReturnsSuccessWithoutSendingEmail()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _handler.Handle(
            new ForgotPasswordCommand("ghost@example.com"), CancellationToken.None);

        // Success either way (no account enumeration), but no email is dispatched.
        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(
            x => x.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
