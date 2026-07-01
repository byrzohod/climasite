#nullable enable

using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// End-to-end B-016 proofs against a REAL Postgres (so specs go through a JSONB round-trip and come back as
/// JsonElement): the alias-aware resolver recovers canonical HVAC values from the DISPLAY keys the catalog
/// actually stores, and the public product DTO hides the machine-only canonical keys. Before B-016 the display
/// keys were invisible to scoring → every product collapsed to the flat fallback.
/// </summary>
public class HvacSpecResolutionIntegrationTests : IntegrationTestBase
{
    public HvacSpecResolutionIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private static Product SeederShapedProduct()
    {
        // The way DataSeeder writes an AC: DISPLAY keys ("BTU", "Noise Level" as a string), NO canonical "btu",
        // plus the machine-only canonical fields the resolver can't derive from a display key.
        var product = new Product("HVAC-DISPLAY-001", "DisplayKey AC 12000", "displaykey-ac-12000", 899.99m);
        product.SetActive(true);
        product.SetSpecifications(new Dictionary<string, object>
        {
            { "BTU", 12000 },
            { "SEER Rating", 21 },
            { "Room Size", "Up to 550 sq ft" },
            { "Noise Level", "22 dB" },
            { "Refrigerant", "R-32" },
            { "isInverter", true },
            { "minTemp", -15 },
            { "recommendedRoomTypes", new[] { "living", "bedroom", "office" } }
        });
        var variant = new ProductVariant(product.Id, "HVAC-DISPLAY-001-VAR", "Standard");
        variant.SetStockQuantity(15);
        product.Variants.Add(variant);
        return product;
    }

    [Fact]
    public async Task GetRecommendations_ForDisplayKeyProduct_ReadsBtuFromDisplayKey_AfterJsonbRoundTrip()
    {
        DbContext.Products.Add(SeederShapedProduct());
        await DbContext.SaveChangesAsync();

        // 48 m² Zone B → required 48 × 250 = 12000 BTU → the 12000 BTU unit ("BTU" display key) is a perfect fit.
        var response = await Client.GetAsync("/api/products/recommendations?area=48&type=living&zone=B");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<RecommendedProductDto>>();
        content.Should().NotBeNull();
        var rec = content!.Single(p => p.Slug == "displaykey-ac-12000");

        rec.BtuCapacity.Should().Be(12000, "resolved from the \"BTU\" display key (was 0 before B-016)");
        rec.IsInverter.Should().BeTrue();
        rec.NoiseLevel.Should().Be(22, "parsed from the \"22 dB\" string");
        rec.Score.Should().BeGreaterThan(0.9, "a real, size-matched fit — not the flat fallback");
        rec.MatchReason.Should().Be("homeV3.matchReason.perfectFit");
    }

    [Fact]
    public async Task GetProductBySlug_HidesMachineOnlyCanonicalKeys_ButKeepsDisplaySpecs()
    {
        DbContext.Products.Add(SeederShapedProduct());
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/products/displaykey-ac-12000");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Specifications.Should().NotBeNull();

        var keys = product.Specifications!.Keys;
        keys.Should().Contain("BTU", "customer-facing display rows stay visible");
        keys.Should().Contain("Noise Level");
        keys.Should().NotContain("isInverter", "machine-only canonical keys are stripped from the public DTO");
        keys.Should().NotContain("minTemp");
        keys.Should().NotContain("recommendedRoomTypes");
    }
}
