using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// SEC-05/B-011 end-to-end proof: a JWT minted by <c>POST /api/auth/login</c> (issuance, via
/// TokenService + IOptions&lt;JwtOptions&gt;) authenticates a <c>[Authorize]</c> endpoint (bearer
/// validation). Because both sides read the SAME resolved secret/issuer/audience, the token is
/// accepted; an absent/garbage token is rejected with 401.
/// </summary>
public class JwtIssuanceValidationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public JwtIssuanceValidationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LoginIssuedToken_AuthenticatesAuthorizeEndpoint()
    {
        var email = $"jwt-roundtrip-{Guid.NewGuid()}@example.com";
        const string password = "ValidPassword123!";

        await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Round",
            lastName = "Trip"
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = JsonSerializer.Deserialize<LoginResponse>(
            await loginResponse.Content.ReadAsStringAsync(), JsonOptions);
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Issued token validates against the same secret → [Authorize] endpoint returns 200.
        using var authedClient = Factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);

        var authed = await authedClient.GetAsync("/api/orders");
        authed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthorizeEndpoint_Returns401_WithoutToken()
    {
        var response = await Client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthorizeEndpoint_Returns401_WithTokenSignedByAnotherSecret()
    {
        // A token that is well-formed but signed with a secret the server doesn't know must be rejected,
        // proving validation actually checks the signature against the resolved secret.
        using var forgedClient = Factory.CreateClient();
        forgedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ForgedToken());

        var response = await forgedClient.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string ForgedToken()
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("a-foreign-secret-not-known-to-the-server-32+xx"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "https://climasite.local",
            audience: "https://climasite.local",
            claims: [new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);
        return handler.WriteToken(token);
    }

    private record LoginResponse(string AccessToken);
}
