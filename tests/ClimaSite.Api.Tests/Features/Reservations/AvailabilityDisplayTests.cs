using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Cart.DTOs;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Features.Reservations;

/// <summary>
/// INV-01 A3 over the real stack (Testcontainers): reservation-adjusted availability is surfaced on the PDP
/// and the cart (available = stock − reserved), while the browse/list path deliberately stays on RAW stock
/// (the owner-decided scope guard). An Active <see cref="StockReservation"/> holds units against a variant.
/// </summary>
public class AvailabilityDisplayTests : IntegrationTestBase
{
    public AvailabilityDisplayTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProductBySlug_WithActiveReservation_ReportsAvailabilityMinusReserved()
    {
        // 10 in stock, 4 held by an in-flight checkout -> the PDP must offer only 6.
        var (product, _) = await SeedProductWithReservationAsync(stock: 10, reserved: 4);

        var response = await Client.GetAsync($"/api/products/{product.Slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        var variant = dto!.Variants.Should().ContainSingle().Subject;
        variant.StockQuantity.Should().Be(10);
        variant.ReservedQuantity.Should().Be(4);
        variant.AvailableQuantity.Should().Be(6, "10 stock − 4 reserved");
    }

    [Fact]
    public async Task GetCart_WithActiveReservation_ReducesAvailableStock()
    {
        // 10 in stock, 4 reserved -> the cart line's available cap (AvailableStock / MaxQuantity) is 6.
        var (product, variant) = await SeedProductWithReservationAsync(stock: 10, reserved: 4);
        var guestSessionId = Guid.NewGuid().ToString();

        var add = await Client.PostAsJsonAsync("/api/cart/items", new
        {
            productId = product.Id,
            variantId = variant.Id,
            quantity = 2,
            guestSessionId
        });
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await Client.GetAsync($"/api/cart?guestSessionId={guestSessionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Should().ContainSingle();
        cart.Items[0].AvailableStock.Should().Be(6, "8 remaining minus the 4 held elsewhere -> 6");
        cart.Items[0].MaxQuantity.Should().Be(6, "MaxQuantity aliases AvailableStock");
    }

    [Fact]
    public async Task ScopeGuard_ListStillReportsRawInStock_WhilePdpShowsFullyReserved()
    {
        // 4 in stock, all 4 held -> PDP available = 0, but the browse/list path must stay on RAW stock
        // (InStock = any variant with stock > 0 -> true). This pins the owner-decided scope guard.
        var (product, _) = await SeedProductWithReservationAsync(stock: 4, reserved: 4);

        var pdp = await Client.GetAsync($"/api/products/{product.Slug}");
        var pdpDto = await pdp.Content.ReadFromJsonAsync<ProductDto>();
        pdpDto!.Variants.Single().AvailableQuantity.Should().Be(0, "all 4 units are held");

        // Cache-bust so a prior list read for another test can't serve a stale page (output cache keys on
        // the full query string).
        var list = await Client.GetAsync($"/api/products?pageSize=50&_cb={Guid.NewGuid():N}");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await list.Content.ReadFromJsonAsync<ProductListPage>();
        var listed = page!.Items.Should().ContainSingle(p => p.Slug == product.Slug).Subject;
        listed.InStock.Should().BeTrue("browse/list stays on raw StockQuantity, not reservation-adjusted");
    }

    [Fact]
    public async Task PdpAvailability_IsFresh_ReflectsAReserveWithoutWaitingForCacheExpiry()
    {
        // [High regression] GetProductBySlugQuery is NOT response-cached, so a hold taken AFTER a first PDP
        // read is reflected on the very next read (same slug+lang => same would-be cache key) with no TTL
        // wait. If the query were cached again, read #2 would still report 10 and this fails (live break-probe
        // wherever a Redis backs IDistributedCache; the deterministic guard is the unit test
        // GetProductBySlugQuery_IsNotCacheable).
        var (product, variant) = await SeedProductWithReservationAsync(stock: 10, reserved: 0);

        var first = await Client.GetAsync($"/api/products/{product.Slug}");
        (await first.Content.ReadFromJsonAsync<ProductDto>())!.Variants.Single()
            .AvailableQuantity.Should().Be(10);

        // Take a hold on 4 units and commit it.
        variant.SetReservedQuantity(4);
        DbContext.StockReservations.Add(new StockReservation(
            variant.Id, cartId: null, quantity: 4, expiresAt: DateTime.UtcNow.AddMinutes(15), ReservationKind.Card));
        await DbContext.SaveChangesAsync();

        var second = await Client.GetAsync($"/api/products/{product.Slug}");
        (await second.Content.ReadFromJsonAsync<ProductDto>())!.Variants.Single()
            .AvailableQuantity.Should().Be(6, "PDP availability must be fresh, not served stale from cache");
    }

    [Fact]
    public async Task ScopeGuard_SearchAndRecommendations_StayOnRawStock()
    {
        // A fully-reserved-but-physically-stocked product (stock 4 / reserved 4, available 0) must still be
        // reported InStock=true by BOTH the FTS search (SearchProductsQuery) and the recommendations engine
        // (GetRecommendationsQueryHandler) — the two paths most likely to be wrongly "fixed" onto adjusted
        // availability later. If either switched to available, the product would drop out / show out-of-stock.
        var sku = $"ZEPHYRON-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, "Zephyron Living Inverter", $"zephyron-{Guid.NewGuid():N}"[..24], 999m);
        product.SetActive(true);
        product.SetSpecifications(new Dictionary<string, object>
        {
            { "btu", 6000 }, // perfect fit for 24 m² at 250 BTU/m²
            { "isInverter", true },
            { "minTemp", -5 },
            { "recommendedRoomTypes", new List<string> { "living" } },
            { "noiseLevel", 22 }
        });
        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetStockQuantity(4);
        variant.SetReservedQuantity(4);
        product.Variants.Add(variant);
        DbContext.Products.Add(product);
        DbContext.StockReservations.Add(new StockReservation(
            variant.Id, cartId: null, quantity: 4, expiresAt: DateTime.UtcNow.AddMinutes(15), ReservationKind.Card));
        await DbContext.SaveChangesAsync();

        // SearchProductsQuery (FTS): a distinctive name token returns the product, still InStock (raw).
        var search = await Client.GetAsync("/api/products/search?q=Zephyron");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchPage = await search.Content.ReadFromJsonAsync<ProductListPage>();
        searchPage!.Items.Should().ContainSingle(p => p.Slug == product.Slug)
            .Which.InStock.Should().BeTrue("search stays on raw StockQuantity");

        // GetRecommendationsQueryHandler: raw-stock inclusion, so the fully-reserved product still ranks.
        var recs = await Client.GetAsync("/api/products/recommendations?area=24&type=living&zone=B");
        recs.StatusCode.Should().Be(HttpStatusCode.OK);
        var recommendations = await recs.Content.ReadFromJsonAsync<List<RecommendedProductDto>>();
        recommendations!.Should().ContainSingle(p => p.Slug == product.Slug)
            .Which.InStock.Should().BeTrue("recommendations stay on raw StockQuantity");
    }

    private async Task<(Product product, ProductVariant variant)> SeedProductWithReservationAsync(int stock, int reserved)
    {
        var sku = $"A3-{Guid.NewGuid():N}"[..16];
        var product = new Product(sku, $"Availability {sku}", $"a3-{sku}", 100m);
        product.SetActive(true);
        var variant = new ProductVariant(product.Id, $"{sku}-V", "Default");
        variant.SetStockQuantity(stock);
        variant.SetReservedQuantity(reserved);
        variant.SetActive(true);
        product.Variants.Add(variant);
        DbContext.Products.Add(product);

        if (reserved > 0)
        {
            // Coherent ledger: an Active hold whose quantity equals the denormalized reserved_quantity counter.
            DbContext.StockReservations.Add(new StockReservation(
                variant.Id, cartId: null, quantity: reserved, expiresAt: DateTime.UtcNow.AddMinutes(15), ReservationKind.Card));
        }

        await DbContext.SaveChangesAsync();
        return (product, variant);
    }

    // Minimal shape of the paginated list response — PaginatedList&lt;T&gt;'s constructor params don't bind
    // under System.Text.Json, so we deserialize only the Items we assert on.
    private sealed record ProductListPage(List<ProductBriefDto> Items);
}
