using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration coverage for the Google OIDC ID-token sign-in (<c>POST /api/auth/google</c>). The real
/// <c>IGoogleTokenValidator</c> is swapped for <see cref="FakeGoogleTokenValidator"/> in
/// <see cref="TestWebApplicationFactory"/>, so these tests prove the full backend flow (verify → find/
/// link/create → issue app JWT) without contacting Google.
/// </summary>
public class GoogleAuthTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GoogleAuthTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GoogleSignIn_Returns200_WithUsableJwt_ForValidToken()
    {
        // Arrange
        var email = $"g-{Guid.NewGuid()}@example.com";
        var token = FakeGoogleTokenValidator.ValidToken($"sub-{Guid.NewGuid()}", email);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/google", new { idToken = token });

        // Assert - 200 with the same auth payload shape as password login.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync(response);
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.User!.Email.Should().Be(email);

        // The refresh token is set as an httpOnly cookie (same as login).
        response.Headers.Should().ContainKey("Set-Cookie");
        response.Headers.GetValues("Set-Cookie").FirstOrDefault().Should().Contain("refreshToken=");

        // The issued JWT is a usable app token: it authenticates against /api/auth/me.
        var meClient = Factory.CreateClient();
        meClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        var meResponse = await meClient.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>(JsonOptions);
        me!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GoogleSignIn_WithSameSubjectTwice_ReusesSingleUser()
    {
        // Arrange - the same Google identity signs in twice.
        var email = $"g-{Guid.NewGuid()}@example.com";
        var token = FakeGoogleTokenValidator.ValidToken($"sub-{Guid.NewGuid()}", email);

        // Act
        var first = await Client.PostAsJsonAsync("/api/auth/google", new { idToken = token });
        var second = await Client.PostAsJsonAsync("/api/auth/google", new { idToken = token });

        // Assert - both succeed and resolve to the SAME application user.
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstUser = (await DeserializeAsync(first)).User!;
        var secondUser = (await DeserializeAsync(second)).User!;
        secondUser.Id.Should().Be(firstUser.Id);

        // Exactly one account exists for that email.
        (CountUsersByEmail(email)).Should().Be(1);
    }

    [Fact]
    public async Task GoogleSignIn_LinksToExistingPasswordAccount_WithoutDuplicating()
    {
        // Arrange - a password account already exists for this email.
        var email = $"g-{Guid.NewGuid()}@example.com";
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "ValidPassword123!",
            firstName = "Local",
            lastName = "Account"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var existingUserId = await GetUserIdByEmailAsync(email);

        // Act - the same email signs in via Google (a brand new Google subject).
        var token = FakeGoogleTokenValidator.ValidToken($"sub-{Guid.NewGuid()}", email);
        var response = await Client.PostAsJsonAsync("/api/auth/google", new { idToken = token });

        // Assert - Google is LINKED to the existing account; no duplicate user is created.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = (await DeserializeAsync(response)).User!;
        user.Id.Should().Be(existingUserId);
        (CountUsersByEmail(email)).Should().Be(1);
    }

    [Fact]
    public async Task GoogleSignIn_Returns401_ForInvalidToken()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/google",
            new { idToken = FakeGoogleTokenValidator.InvalidToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoogleSignIn_Returns401_ForUnverifiedEmail()
    {
        // Arrange - a structurally valid token whose email Google has not verified.
        var token = FakeGoogleTokenValidator.ValidToken(
            $"sub-{Guid.NewGuid()}", $"g-{Guid.NewGuid()}@example.com", emailVerified: false);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/google", new { idToken = token });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfig_ReturnsEmptyGoogleClientId_WhenUnconfigured()
    {
        // Act - the test host leaves Authentication:Google:ClientId unset.
        var response = await Client.GetAsync("/api/auth/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<ConfigResponse>(JsonOptions);
        config!.GoogleClientId.Should().BeEmpty();
    }

    private static async Task<AuthResponse> DeserializeAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AuthResponse>(content, JsonOptions)!;
    }

    private int CountUsersByEmail(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return userManager.Users.Count(u => u.Email == email);
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        return user!.Id;
    }

    private record AuthResponse(string AccessToken, UserResponse? User);
    private record UserResponse(Guid Id, string Email, string FirstName, string LastName);
    private record MeResponse(Guid Id, string Email);
    private record ConfigResponse(string GoogleClientId);
}
