using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Register Tests

    [Fact]
    public async Task Register_CreatesUser_WithValidData()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid()}@example.com";
        var request = new
        {
            email,
            password = "ValidPassword123!",
            firstName = "John",
            lastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(email);
        content.Should().Contain("John");
        content.Should().Contain("Doe");
    }

    [Fact]
    public async Task Register_Returns400_WithInvalidEmail()
    {
        // Arrange
        var request = new
        {
            email = "invalid-email",
            password = "ValidPassword123!",
            firstName = "John",
            lastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Returns400_WithWeakPassword()
    {
        // Arrange
        var request = new
        {
            email = $"test-{Guid.NewGuid()}@example.com",
            password = "weak", // Too short, no uppercase, no special char
            firstName = "John",
            lastName = "Doe"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        // Should contain password requirement errors
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_Returns400_WithDuplicateEmail()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid()}@example.com";
        var request = new
        {
            email,
            password = "ValidPassword123!",
            firstName = "John",
            lastName = "Doe"
        };

        // First registration should succeed
        var firstResponse = await Client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Second registration with same email
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already registered");
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ReturnsTokens_WithValidCredentials()
    {
        // Arrange
        var email = $"login-test-{Guid.NewGuid()}@example.com";
        var password = "ValidPassword123!";

        // Register user first
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginResponse>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(email);

        // Check refresh token cookie
        response.Headers.Should().ContainKey("Set-Cookie");
        var setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        setCookie.Should().Contain("refreshToken=");
    }

    [Fact]
    public async Task Login_Returns401_WithInvalidPassword()
    {
        // Arrange
        var email = $"login-invalid-{Guid.NewGuid()}@example.com";
        var password = "ValidPassword123!";

        // Register user first
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

        // Act - Login with wrong password
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_Returns401_WithNonExistentUser()
    {
        // Arrange
        var email = $"nonexistent-{Guid.NewGuid()}@example.com";

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "SomePassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_ReturnsNewTokens_WithValidRefresh()
    {
        // Arrange
        var email = $"refresh-test-{Guid.NewGuid()}@example.com";
        var password = "ValidPassword123!";

        // Register and login
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Extract refresh token from cookie
        var setCookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        var refreshToken = ExtractCookieValue(setCookie, "refreshToken");

        // Create new client with refresh token cookie
        var handler = new HttpClientHandler();
        var cookieClient = Factory.CreateClient();
        cookieClient.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        // Act
        var response = await cookieClient.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RefreshResponse>(content, JsonOptions);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();

        // New refresh token cookie should be set
        response.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task RefreshToken_Returns401_WithExpiredToken()
    {
        // Arrange - Use an invalid/expired token
        var cookieClient = Factory.CreateClient();
        cookieClient.DefaultRequestHeaders.Add("Cookie", "refreshToken=invalid-or-expired-token");

        // Act
        var response = await cookieClient.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or expired refresh token");
    }

    [Fact]
    public async Task RefreshToken_Returns401_WithoutToken()
    {
        // Act - No refresh token cookie
        var response = await Client.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Refresh token is required");
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_InvalidatesRefreshToken()
    {
        // Arrange
        var email = $"logout-test-{Guid.NewGuid()}@example.com";
        var password = "ValidPassword123!";

        // Register and login
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponse>(loginContent, JsonOptions);
        var accessToken = loginResult!.AccessToken;

        var setCookie = loginResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        var refreshToken = ExtractCookieValue(setCookie, "refreshToken");

        // Create authenticated client
        var authClient = Factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        authClient.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        // Act - Logout
        var logoutResponse = await authClient.PostAsync("/api/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify refresh token no longer works
        var refreshClient = Factory.CreateClient();
        refreshClient.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");
        var refreshResponse = await refreshClient.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Forgot Password Tests

    [Fact]
    public async Task ForgotPassword_ReturnsOk_WithValidEmail()
    {
        // Arrange
        var email = $"forgot-password-{Guid.NewGuid()}@example.com";

        // Register user first
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "ValidPassword123!",
            firstName = "Test",
            lastName = "User"
        });

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        // Assert - Always returns success to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("password reset link has been sent");
    }

    [Fact]
    public async Task ForgotPassword_ReturnsOk_WithNonExistentEmail()
    {
        // Arrange - Email that doesn't exist
        var email = $"nonexistent-{Guid.NewGuid()}@example.com";

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        // Assert - Should still return OK to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("password reset link has been sent");
    }

    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPassword_UpdatesPassword_WithValidToken()
    {
        // Arrange
        var email = $"reset-password-{Guid.NewGuid()}@example.com";
        var originalPassword = "OriginalPassword123!";
        var newPassword = "NewPassword456!";

        // Register user
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = originalPassword,
            firstName = "Test",
            lastName = "User"
        });

        // Get password reset token directly from UserManager
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user!);

        // Act - Reset password with valid token
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            email,
            newPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Password has been reset successfully");

        // Verify new password works
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = newPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify old password doesn't work
        var oldLoginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = originalPassword
        });
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_Returns400_WithInvalidToken()
    {
        // Arrange
        var email = $"reset-invalid-{Guid.NewGuid()}@example.com";

        // Register user
        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "ValidPassword123!",
            firstName = "Test",
            lastName = "User"
        });

        // Act - Reset password with invalid token
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "invalid-token",
            email,
            newPassword = "NewPassword456!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_Returns400_WithNonExistentEmail()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "some-token",
            email = $"nonexistent-{Guid.NewGuid()}@example.com",
            newPassword = "NewPassword456!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid token or email");
    }

    #endregion

    #region Helper Methods

    private static string? ExtractCookieValue(string? setCookie, string cookieName)
    {
        if (string.IsNullOrEmpty(setCookie))
            return null;

        var cookies = setCookie.Split(',').Select(c => c.Trim());
        foreach (var cookie in cookies)
        {
            var parts = cookie.Split(';')[0].Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() == cookieName)
            {
                return parts[1];
            }
        }
        return null;
    }

    #endregion

    #region Response DTOs

    private record LoginResponse(string AccessToken, UserResponse? User);
    private record UserResponse(Guid Id, string Email, string FirstName, string LastName);
    private record RefreshResponse(string AccessToken);

    #endregion
}
