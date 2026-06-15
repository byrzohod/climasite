using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// BUG-07 regression: the password-reset flow works end-to-end (a valid token resets the
/// password and the new credentials authenticate), and an invalid token is rejected.
/// </summary>
public class PasswordResetFlowTests : IntegrationTestBase
{
    public PasswordResetFlowTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_UpdatesPasswordAndAllowsLogin()
    {
        var email = $"reset-flow-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "OldPassw0rd!";
        const string newPassword = "NewPassw0rd!";

        var register = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = oldPassword,
            firstName = "Reset",
            lastName = "Flow"
        });
        register.IsSuccessStatusCode.Should().BeTrue();

        // Generate a reset token exactly as ForgotPassword does. The factory is a single host,
        // so this UserManager shares the request pipeline's data-protection keys and the token
        // is valid for the reset-password endpoint.
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            var token = await userManager.GeneratePasswordResetTokenAsync(user!);

            var reset = await Client.PostAsJsonAsync("/api/auth/reset-password", new
            {
                token,
                email,
                newPassword
            });
            reset.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var loginNew = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        loginNew.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginOld = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = oldPassword });
        loginOld.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Fails()
    {
        var email = $"reset-bad-{Guid.NewGuid():N}@example.com";
        var register = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "OldPassw0rd!",
            firstName = "Reset",
            lastName = "Bad"
        });
        register.IsSuccessStatusCode.Should().BeTrue();

        var reset = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "not-a-valid-token",
            email,
            newPassword = "NewPassw0rd!"
        });

        reset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
