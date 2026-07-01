using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Common.Options;
using ClimaSite.Application.Features.Cart.DTOs;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClimaSite.Api.Tests.Features.GuestSession;

/// <summary>
/// INV-01 A1 — the guest-identity switch. The signed cookie is now AUTHORITATIVE for cart + checkout keying,
/// and a returning guest's legacy cart is migrated onto the cookie id. Uses an isolated factory (fresh mint
/// cache) so the cookie mints reliably on the TestServer loopback IP; a HandleCookies client then replays it
/// like a browser.
/// </summary>
public class GuestIdentitySwitchTests : IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _isolatedFactory;

    public GuestIdentitySwitchTests(TestWebApplicationFactory factory) : base(factory)
    {
        _isolatedFactory = factory.WithWebHostBuilder(_ => { });
    }

    public override async Task DisposeAsync()
    {
        _isolatedFactory.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task ReturningLegacyGuest_MigratesCartOntoTheCookie_AndCookieOnlyRequestsSeeIt()
    {
        var (product, variant) = await SeedProductAsync();
        var legacyId = $"legacy-{Guid.NewGuid():N}";
        await SeedGuestCartAsync(legacyId, product.Id, variant.Id, quantity: 2);

        using var client = _isolatedFactory.CreateClient();

        // First request carries the minted cookie AND the legacy id → the middleware mints the cookie and the
        // controller folds the legacy cart onto it, then resolves to the cookie id.
        var migrateResponse = await client.GetAsync($"/api/cart?guestSessionId={legacyId}");
        migrateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var migratedCart = await migrateResponse.Content.ReadFromJsonAsync<CartDto>();
        migratedCart!.Items.Should().ContainSingle().Which.Quantity.Should().Be(2);
        migratedCart.GuestSessionId.Should().NotBe(legacyId, "the cart is re-keyed onto the trusted cookie id");

        // A cookie-only follow-up (no legacy query param) still sees the cart.
        var cookieOnly = await client.GetAsync("/api/cart");
        var cookieCart = await cookieOnly.Content.ReadFromJsonAsync<CartDto>();
        cookieCart!.Items.Should().ContainSingle().Which.ProductId.Should().Be(product.Id);

        // The legacy-keyed cart no longer exists — it was moved, not copied.
        (await CountCartsWithSessionAsync(legacyId)).Should().Be(0);
    }

    [Fact]
    public async Task Checkout_ResolvesTheGuestCartViaTheCookie_NotTheLegacyBodyId()
    {
        var (product, variant) = await SeedProductAsync(basePrice: 100m);
        using var client = _isolatedFactory.CreateClient();

        // Build the cart on the cart path (cookie minted here). The body's legacy id is ignored in favour of
        // the cookie, so the cart is owned by the cookie id.
        var addResponse = await client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 1,
            guestSessionId = $"legacy-{Guid.NewGuid():N}"
        });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // create-intent carries the cookie (+ a differing legacy id) → resolves to the cookie's cart.
        var intentResponse = await client.PostAsJsonAsync("/api/payments/create-intent", new
        {
            shippingMethod = "standard",
            guestSessionId = $"legacy-{Guid.NewGuid():N}"
        });
        intentResponse.StatusCode.Should().Be(HttpStatusCode.OK, "the cookie resolves the cart the guest built");

        // create-order (bank, no Stripe) likewise resolves via the cookie and places the order.
        var orderResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            customerEmail = "a1-checkout@test.com",
            shippingAddress = ValidAddress(),
            shippingMethod = "standard",
            paymentMethod = "bank",
            guestSessionId = $"legacy-{Guid.NewGuid():N}"
        });
        orderResponse.StatusCode.Should().Be(HttpStatusCode.Created, "the order is placed against the cookie's cart");
    }

    [Fact]
    public async Task AllowLegacyIdFalse_NoCookie_CannotResolveALegacyCart()
    {
        var (product, variant) = await SeedProductAsync();
        var legacyId = $"legacy-{Guid.NewGuid():N}";
        await SeedGuestCartAsync(legacyId, product.Id, variant.Id, quantity: 1);

        // AllowLegacyId=false + minting disabled → a request with no cookie and the legacy id resolves to
        // nothing, so the legacy cart is unreachable.
        using var strictFactory = FactoryWithOptions(new GuestSessionOptions
        {
            AllowLegacyId = false,
            MintPathPrefixes = []
        });
        using var strictClient = strictFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var strictResponse = await strictClient.GetAsync($"/api/cart?guestSessionId={legacyId}");
        var strictCart = await strictResponse.Content.ReadFromJsonAsync<CartDto>();
        strictCart!.Items.Should().BeEmpty("a legacy id is not trusted when AllowLegacyId is false");

        // Control: with AllowLegacyId=true (still no cookie) the same legacy id DOES resolve the cart.
        using var lenientFactory = FactoryWithOptions(new GuestSessionOptions
        {
            AllowLegacyId = true,
            MintPathPrefixes = []
        });
        using var lenientClient = lenientFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var lenientResponse = await lenientClient.GetAsync($"/api/cart?guestSessionId={legacyId}");
        var lenientCart = await lenientResponse.Content.ReadFromJsonAsync<CartDto>();
        lenientCart!.Items.Should().ContainSingle("the legacy fallback is honoured when AllowLegacyId is true");
    }

    private WebApplicationFactory<Program> FactoryWithOptions(GuestSessionOptions options) =>
        Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<GuestSessionOptions>();
            services.AddSingleton(options);
        }));

    private async Task<int> CountCartsWithSessionAsync(string sessionId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Carts.AsNoTracking().CountAsync(c => c.SessionId == sessionId);
    }

    private async Task SeedGuestCartAsync(string sessionId, Guid productId, Guid variantId, int quantity)
    {
        var cart = new Core.Entities.Cart(null, sessionId);
        cart.AddItem(productId, variantId, quantity, 100m);
        DbContext.Carts.Add(cart);
        await DbContext.SaveChangesAsync();
    }

    private async Task<(Product product, ProductVariant variant)> SeedProductAsync(decimal basePrice = 99.99m)
    {
        var uniqueSku = $"A1-{Guid.NewGuid():N}"[..20];
        var product = new Product(uniqueSku, $"A1 Product {uniqueSku}", $"a1-product-{uniqueSku}", basePrice);
        product.SetShortDescription("Guest-identity-switch test product");
        product.SetActive(true);

        var variant = new ProductVariant(product.Id, $"{uniqueSku}-V", "Default Variant");
        variant.SetStockQuantity(50);
        variant.SetActive(true);
        product.Variants.Add(variant);

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return (product, variant);
    }

    private static object ValidAddress() => new
    {
        firstName = "Jane",
        lastName = "Doe",
        addressLine1 = "1 Test Way",
        city = "Sofia",
        state = "Sofia",
        postalCode = "1000",
        country = "Bulgaria",
        phone = "+359888000000"
    };
}
