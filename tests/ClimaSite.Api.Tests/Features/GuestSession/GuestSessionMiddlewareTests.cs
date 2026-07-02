using System.Net;
using ClimaSite.Api.Services;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Features.GuestSession;

/// <summary>
/// INV-01 Wave A0 — the guest-session middleware end-to-end (cookie shipped DARK: minted + validated +
/// published, NOT yet authoritative for cart-keying). Proves minting is SCOPED to checkout paths (mints on
/// <c>/api/cart</c>, never on <c>/health</c> or the cacheable <c>/api/products</c>), that a forged cookie is
/// rejected and re-minted, and that the token is the 3-part signed form. The exact per-IP budget boundary is
/// covered deterministically in <see cref="GuestSessionMintLimiterTests"/>; here it is exercised through the
/// real pipeline. Each test uses its OWN isolated factory (a fresh <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>)
/// so the per-IP mint bucket — now keyed on the TestServer loopback address, not a spoofable header — is not
/// shared with the rest of the collection.
/// </summary>
public class GuestSessionMiddlewareTests : IntegrationTestBase
{
    private const string CookieName = "cs_guest"; // Testing env is non-Production, so the unprefixed name is used.

    // A per-test host with its own DI container (hence its own mint-counter cache), sharing the base test
    // database container so the schema seeded by the base class is visible.
    private readonly WebApplicationFactory<Program> _isolatedFactory;

    public GuestSessionMiddlewareTests(TestWebApplicationFactory factory) : base(factory)
    {
        // This suite exercises the REAL per-IP mint cap through the pipeline, so restore the production
        // GuestSessionMintLimiter that the base factory replaces with an always-allow one (which the other
        // guest-checkout tests need so the shared-loopback-IP suite doesn't exhaust the budget). Each test's
        // isolated factory still gets its own fresh mint-counter cache.
        _isolatedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGuestSessionMintLimiter));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddSingleton<IGuestSessionMintLimiter, GuestSessionMintLimiter>();
            }));
    }

    public override async Task DisposeAsync()
    {
        _isolatedFactory.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Mints_OnACartPath_WithASignedHttpOnlyCookie()
    {
        using var client = CreateCookielessClient();

        var response = await client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var setCookie = GetSetCookieHeader(response);
        setCookie.Should().NotBeNull("a cookieless cart request must be minted a fresh guest identity");
        setCookie!.Should().ContainEquivalentOf("httponly", "the guest cookie must not be readable from JS");

        // The value is the signed "{id}.{exp}.{signature}" token — never a bare id.
        ExtractTokenValue(setCookie).Split('.').Should().HaveCount(3);
    }

    [Fact]
    public async Task DoesNotMint_OnANonCheckoutPath_Health()
    {
        using var client = CreateCookielessClient();

        var response = await client.GetAsync("/health");

        GetSetCookieHeader(response).Should().BeNull("infrastructure paths must never mint a guest cookie");
    }

    [Fact]
    public async Task DoesNotMint_OnTheCacheableProductsPath()
    {
        using var client = CreateCookielessClient();

        var response = await client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetSetCookieHeader(response).Should().BeNull(
            "cacheable catalog GETs are outside MintPathPrefixes — no budget burn, no output-cache Set-Cookie interaction");
    }

    [Fact]
    public async Task ForgedCookie_IsNotTrusted_AndAFreshCookieIsMintedOnACartPath()
    {
        var tokenService = _isolatedFactory.Services.GetRequiredService<IGuestSessionTokenService>();
        var parts = tokenService.Issue().Split('.'); // id.exp.signature
        var realId = parts[0];

        // Same id + exp, corrupted signature → must fail verification and be treated as if no cookie were sent.
        var tamperedLast = parts[2][^1] == 'A' ? 'B' : 'A';
        var forged = $"{parts[0]}.{parts[1]}.{parts[2][..^1]}{tamperedLast}";

        using var client = CreateCookielessClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieName}={forged}");

        var response = await client.GetAsync("/api/cart");

        var setCookie = GetSetCookieHeader(response);
        setCookie.Should().NotBeNull("a tampered cookie is rejected, so the request is minted a new identity");
        ExtractTokenValue(setCookie!).Split('.')[0].Should().NotBe(realId,
            "the server must not adopt the id from an unverified token");
    }

    [Fact]
    public async Task PerIpMintCap_StopsIssuingCookies_PastTheDefaultBudget()
    {
        // This test's isolated factory has a fresh mint-counter cache, so the loopback bucket is clean and the
        // default budget of 20 is deterministic even though all requests share the TestServer loopback IP.
        const int budget = 20;
        using var client = CreateCookielessClient();

        var mintedPerRequest = new List<bool>();
        for (var i = 0; i < budget + 2; i++)
        {
            var response = await client.GetAsync("/api/cart");
            mintedPerRequest.Add(GetSetCookieHeader(response) != null);
        }

        mintedPerRequest.Take(budget).Should().OnlyContain(minted => minted,
            "every request within the per-IP budget is minted a cookie");
        mintedPerRequest.Skip(budget).Should().OnlyContain(minted => !minted,
            "requests past the per-IP budget receive no cookie");
    }

    private HttpClient CreateCookielessClient() =>
        _isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string? GetSetCookieHeader(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(c => c.StartsWith($"{CookieName}=", StringComparison.Ordinal))
            : null;
    }

    private static string ExtractTokenValue(string setCookieHeader)
    {
        var value = setCookieHeader[$"{CookieName}=".Length..];
        var semicolon = value.IndexOf(';');
        return semicolon >= 0 ? value[..semicolon] : value;
    }
}
