using Microsoft.Extensions.DependencyInjection;
using ClimaSite.Infrastructure.Data;
using System.Net.Http.Json;

namespace ClimaSite.Api.Tests.Infrastructure;

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected IServiceScope Scope = default!;
    protected ApplicationDbContext DbContext = default!;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public virtual async Task InitializeAsync()
    {
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Clean database before each test
        await CleanDatabaseAsync();
    }

    public virtual Task DisposeAsync()
    {
        Scope?.Dispose();
        Client?.Dispose();
        return Task.CompletedTask;
    }

    protected async Task CleanDatabaseAsync()
    {
        // This will be implemented once we have entities
        await DbContext.Database.EnsureCreatedAsync();
    }

    protected async Task<string> AuthenticateAsync(
        string email = "test@test.com",
        string password = "Password123!")
    {
        // Register user
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

        if (!registerResponse.IsSuccessStatusCode)
        {
            // User might already exist, try to login
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
            if (loginResponse.IsSuccessStatusCode)
            {
                var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
                SetAuthToken(loginResult?.Token ?? string.Empty);
                return loginResult?.Token ?? string.Empty;
            }
        }

        // Get token from registration or login
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

        SetAuthToken(result?.Token ?? string.Empty);
        return result?.Token ?? string.Empty;
    }

    protected void SetAuthToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthToken()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    private record AuthResponse(string AccessToken)
    {
        public string Token => AccessToken;
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
