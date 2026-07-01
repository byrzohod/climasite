#nullable enable

using System.Text.Json;
using ClimaSite.Application.Features.Products.Specifications;
using FluentAssertions;
using Xunit;

namespace ClimaSite.Application.Tests.Features.Products.Specifications;

/// <summary>
/// Proves the alias-aware HVAC spec resolver (B-016) recovers canonical values from the DISPLAY keys the
/// seeder / admin actually write (e.g. "BTU", "Noise Level", "SEER Rating", "BTU Cooling"), across CLR
/// primitives AND JsonElement (the shape after an EF JSONB round-trip), with per-key typing + unit guards.
/// </summary>
public class HvacSpecResolverTests
{
    private static Dictionary<string, object> Specs(params (string Key, object Value)[] entries)
    {
        var d = new Dictionary<string, object>();
        foreach (var (k, v) in entries) d[k] = v;
        return d;
    }

    /// <summary>Round-trips a CLR value through JSON to the JsonElement shape EF hands back from JSONB.</summary>
    private static JsonElement Json(object value) =>
        JsonSerializer.SerializeToElement(value);

    // ---- Alias resolution (case / space / punctuation insensitive) ------------------------------------

    [Theory]
    [InlineData("btu")]
    [InlineData("BTU")]
    [InlineData("BTU Rating")]
    [InlineData("btu_rating")]
    [InlineData("BTU-Rating")]
    public void GetInt_ResolvesBtu_AcrossKeySpellings(string key)
    {
        HvacSpecResolver.GetInt(Specs((key, 12000)), "btu").Should().Be(12000);
    }

    [Fact]
    public void GetInt_ResolvesNoiseLevel_FromDisplayKey()
    {
        HvacSpecResolver.GetInt(Specs(("Noise Level", "22 dB")), "noiseLevel").Should().Be(22);
    }

    [Fact]
    public void GetDecimal_ResolvesSeer_FromDisplayKey()
    {
        HvacSpecResolver.GetDecimal(Specs(("SEER Rating", 21)), "seer").Should().Be(21m);
    }

    // ---- BTU precedence: cooling wins over heating (heat pumps expose both) ----------------------------

    [Fact]
    public void GetInt_Btu_PrefersCoolingOverHeating()
    {
        var specs = Specs(("BTU Cooling", 24000), ("BTU Heating", 26000));
        HvacSpecResolver.GetInt(specs, "btu").Should().Be(24000);
    }

    [Fact]
    public void GetInt_Btu_FallsBackToHeating_WhenNoCooling()
    {
        HvacSpecResolver.GetInt(Specs(("BTU Heating", 26000)), "btu").Should().Be(26000);
    }

    // ---- Value parsing: unit-stripping, decimals, negatives, invariant culture -------------------------

    [Theory]
    [InlineData("12000 BTU", 12000)]
    [InlineData("22 dB", 22)]
    [InlineData("SEER 21", 21)]
    [InlineData("-15", -15)]
    public void GetInt_ParsesLeadingNumber_FromString(string value, int expected)
    {
        // btu accepts any leading number; use a same-typed key per value below via a generic int field.
        HvacSpecResolver.GetInt(Specs(("btu", value)), "btu").Should().Be(expected);
    }

    [Fact]
    public void GetInt_MinTemp_ParsesNegativeCelsius()
    {
        HvacSpecResolver.GetInt(Specs(("minTemp", -15)), "minTemp").Should().Be(-15);
    }

    [Fact]
    public void GetDecimal_Hspf_KeepsDecimalPrecision()
    {
        HvacSpecResolver.GetDecimal(Specs(("HSPF", 10.5)), "hspf").Should().Be(10.5m);
    }

    [Theory]
    [InlineData("12,000 BTU", 12000)]   // comma thousands separator
    [InlineData("12 000 BTU", 12000)]   // space thousands separator
    [InlineData("1,234,500", 1234500)]
    public void GetInt_HandlesThousandsSeparators(string value, int expected)
    {
        HvacSpecResolver.GetInt(Specs(("btu", value)), "btu").Should().Be(expected);
    }

    [Fact]
    public void GetStringList_ResolvesRoomTypes_FromJsonElementCsvString()
    {
        // Admin-entered CSV round-trips through JSONB as a JSON STRING (not an array) — must still split.
        var specs = Specs(("Room Types", Json("living, office")));
        HvacSpecResolver.GetStringList(specs, "recommendedRoomTypes")
            .Should().BeEquivalentTo("living", "office");
    }

    // ---- Unit guards / semantic rejections (council Medium) -------------------------------------------

    [Fact]
    public void GetInt_NoiseLevel_RejectsSones_NotDb()
    {
        // "0.8 sones" is not a dB value — must NOT be read as noise (would falsely pass the <30 quiet check).
        HvacSpecResolver.GetInt(Specs(("Noise Level", "0.8 sones")), "noiseLevel").Should().BeNull();
    }

    [Fact]
    public void GetInt_MinTemp_DoesNotResolveFromOperatingRange()
    {
        // "Operating Range" is an ambiguous °F range string — deliberately NOT an alias for minTemp.
        HvacSpecResolver.GetInt(Specs(("Operating Range", "-15°F to 115°F")), "minTemp").Should().BeNull();
    }

    [Fact]
    public void GetString_Refrigerant_IsNotNumericParsed()
    {
        HvacSpecResolver.GetString(Specs(("Refrigerant", "R-32")), "refrigerantType").Should().Be("R-32");
    }

    // ---- Bool + string-list -------------------------------------------------------------------------

    [Fact]
    public void GetBool_ResolvesInverter()
    {
        HvacSpecResolver.GetBool(Specs(("isInverter", true)), "isInverter").Should().BeTrue();
        HvacSpecResolver.GetBool(Specs(("Inverter", "yes")), "isInverter").Should().BeTrue();
    }

    [Fact]
    public void GetStringList_ResolvesRoomTypes_FromArray()
    {
        var specs = Specs(("recommendedRoomTypes", new[] { "living", "bedroom" }));
        HvacSpecResolver.GetStringList(specs, "recommendedRoomTypes")
            .Should().BeEquivalentTo("living", "bedroom");
    }

    [Fact]
    public void GetStringList_ResolvesRoomTypes_FromCsvString()
    {
        var specs = Specs(("Room Types", "living, office"));
        HvacSpecResolver.GetStringList(specs, "recommendedRoomTypes")
            .Should().BeEquivalentTo("living", "office");
    }

    // ---- JsonElement (EF JSONB round-trip) shape ------------------------------------------------------

    [Fact]
    public void Resolves_AllTypes_FromJsonElement()
    {
        var specs = Specs(
            ("BTU", Json(12000)),
            ("Noise Level", Json("26 dB")),
            ("isInverter", Json(true)),
            ("minTemp", Json(-15)),
            ("recommendedRoomTypes", Json(new[] { "living", "office" })));

        HvacSpecResolver.GetInt(specs, "btu").Should().Be(12000);
        HvacSpecResolver.GetInt(specs, "noiseLevel").Should().Be(26);
        HvacSpecResolver.GetBool(specs, "isInverter").Should().BeTrue();
        HvacSpecResolver.GetInt(specs, "minTemp").Should().Be(-15);
        HvacSpecResolver.GetStringList(specs, "recommendedRoomTypes").Should().BeEquivalentTo("living", "office");
    }

    // ---- Missing / empty ----------------------------------------------------------------------------

    [Fact]
    public void Returns_NullOrEmpty_WhenMissing()
    {
        var specs = Specs(("Refrigerant", "R-32"));
        HvacSpecResolver.GetInt(specs, "btu").Should().BeNull();
        HvacSpecResolver.GetBool(specs, "isInverter").Should().BeNull();
        HvacSpecResolver.GetStringList(specs, "recommendedRoomTypes").Should().BeEmpty();
        HvacSpecResolver.GetInt(null, "btu").Should().BeNull();
    }

    // ---- Machine-only key detection (public-DTO strip) ----------------------------------------------

    [Theory]
    [InlineData("isInverter", true)]
    [InlineData("minTemp", true)]
    [InlineData("recommendedRoomTypes", true)]
    [InlineData("btu", false)]
    [InlineData("BTU", false)]        // display key must NOT be stripped
    [InlineData("seer", false)]
    [InlineData("Noise Level", false)]
    public void IsMachineOnlyKey_HidesOnlyNoDisplayEquivalentKeys(string key, bool expected)
    {
        HvacSpecResolver.IsMachineOnlyKey(key).Should().Be(expected);
    }

    // ---- Facet grouping: aliases merge to canonical; non-facets ignored -----------------------------

    [Theory]
    [InlineData("SEER Rating", "seer")]
    [InlineData("seer", "seer")]
    [InlineData("BTU Cooling", "btu")]
    [InlineData("BTU", "btu")]
    public void ResolveFacet_GroupsAliasesToCanonical(string rawKey, string expectedCanonical)
    {
        var result = HvacSpecResolver.ResolveFacet(rawKey, 12000);
        result.Should().NotBeNull();
        result!.Value.canonicalKey.Should().Be(expectedCanonical);
    }

    [Fact]
    public void ResolveFacet_NormalisesValue_SoDisplayAndCanonicalMerge()
    {
        // "12000" and "12000 BTU" must produce the same facet value so they don't split into two buckets.
        HvacSpecResolver.ResolveFacet("BTU", "12000 BTU")!.Value.value.Should().Be("12000");
        HvacSpecResolver.ResolveFacet("btu", 12000)!.Value.value.Should().Be("12000");
    }

    [Fact]
    public void ResolveFacet_KeepsRefrigerantString()
    {
        HvacSpecResolver.ResolveFacet("Refrigerant", "R-32")!.Value.Should().Be(("refrigerantType", "R-32"));
    }

    [Theory]
    [InlineData("noiseLevel")]
    [InlineData("Noise Level")]
    [InlineData("isInverter")]
    [InlineData("minTemp")]
    [InlineData("Room Size")]
    public void ResolveFacet_IgnoresNonFacetKeys(string rawKey)
    {
        HvacSpecResolver.ResolveFacet(rawKey, "whatever").Should().BeNull();
    }

    // ---- Product-level facet resolution (precedence + one value per canonical) ----------------------

    [Fact]
    public void ResolveFacets_HeatPump_EmitsSingleBtu_PreferringCooling()
    {
        // A heat pump exposes both cooling + heating BTU; the facet must be ONE "btu" bucket (cooling), not two.
        var specs = Specs(("BTU Cooling", 24000), ("BTU Heating", 26000), ("SEER Rating", 22));

        var facets = HvacSpecResolver.ResolveFacets(specs).ToList();

        facets.Where(f => f.canonicalKey == "btu").Should().ContainSingle()
            .Which.value.Should().Be("24000");
        facets.Should().Contain(("seer", "22"));
    }

    [Fact]
    public void ResolveFacets_NormalisesDecimalFacetValues_SoTheyMerge()
    {
        // 21m / 21.0m / 21.00m carry different decimal scales but must all yield the same "21" facet value.
        foreach (var seer in new[] { 21m, 21.0m, 21.00m })
        {
            var facets = HvacSpecResolver.ResolveFacets(Specs(("SEER Rating", seer))).ToList();
            facets.Should().Contain(("seer", "21"), "decimal {0} must normalise to \"21\"", seer);
        }
    }

    [Fact]
    public void ResolveFacets_IgnoresNonFacetFields()
    {
        var specs = Specs(("Noise Level", "22 dB"), ("isInverter", true), ("minTemp", -15));
        HvacSpecResolver.ResolveFacets(specs).Should().BeEmpty();
    }
}
