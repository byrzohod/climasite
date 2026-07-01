#nullable enable

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ClimaSite.Application.Features.Products.Specifications;

/// <summary>
/// Single source of truth for reading canonical HVAC specification values out of the free-form product
/// <c>Specifications</c> map — regardless of how the key was spelled at write time.
///
/// Products are seeded / admin-entered with DISPLAY keys (<c>"BTU"</c>, <c>"Noise Level"</c>,
/// <c>"SEER Rating"</c>, <c>"BTU Cooling"</c>) while the scoring / recommendation / facet code needs canonical
/// fields (<c>btu</c>, <c>noiseLevel</c>, <c>seer</c>, …). Without alias-aware resolution those reads silently
/// return defaults, collapsing recommendation fit-scoring to a flat fallback (B-016). ALL machine reads MUST go
/// through this resolver — it is the canonical-schema enforcement point, applied at read time so there is no
/// duplicated/stale canonical data written to the DB.
///
/// Values may be raw CLR primitives (in-memory: seeder, unit tests) OR <see cref="JsonElement"/> (after an EF
/// JSONB round-trip); both are handled. All numeric parsing is invariant-culture.
/// </summary>
public static class HvacSpecResolver
{
    private enum Kind { Int, Decimal, Bool, String, StringList }

    private sealed class Field
    {
        public Field(string canonical, Kind kind, bool facet, string[] aliases, string[]? unitReject = null)
        {
            Canonical = canonical;
            Kind = kind;
            Facet = facet;
            Aliases = aliases;
            NormAliases = aliases.Select(Normalize).ToArray();
            UnitReject = unitReject;
        }

        public string Canonical { get; }
        public Kind Kind { get; }
        public bool Facet { get; }
        public string[] Aliases { get; }
        public string[] NormAliases { get; }
        public string[]? UnitReject { get; }
    }

    // Aliases are ORDERED: the first product key that matches (by normalised form) wins. This gives precedence,
    // e.g. cooling/generic BTU is preferred over a heat pump's separate "BTU Heating".
    private static readonly Field[] Fields =
    {
        new("btu", Kind.Int, facet: true, new[]
        {
            "btu", "BTU Rating", "Cooling BTU", "BTU Cooling", "Cooling Capacity", "BTUs", "btu/h",
            "BTU Heating" // heat pumps expose cooling + heating; cooling drives the sizing fit → heating LAST
        }),
        new("noiseLevel", Kind.Int, facet: false, new[] { "noiseLevel", "Noise Level", "Noise", "Sound Level" },
            unitReject: new[] { "sone" }), // "0.8 sones" is not a dB value — must not be read as noise
        new("isInverter", Kind.Bool, facet: false, new[] { "isInverter", "Inverter", "Inverter Technology" }),
        new("minTemp", Kind.Int, facet: false, new[] { "minTemp", "Min Temp", "Minimum Temperature", "Min Operating Temp" }),
        // NOTE: "Operating Range" is deliberately NOT an alias for minTemp — it is an ambiguous °F range string.
        new("recommendedRoomTypes", Kind.StringList, facet: false, new[] { "recommendedRoomTypes", "Recommended Room Types", "Room Types" }),

        // Facet-only canonical keys (used by the filter-facet extractor). seer/eer/hspf/afue are decimal-safe;
        // the rest are strings and must never be numeric-parsed ("R-32" stays "R-32").
        new("seer", Kind.Decimal, facet: true, new[] { "seer", "SEER Rating", "SEER2" }),
        new("eer", Kind.Decimal, facet: true, new[] { "eer", "EER Rating" }),
        new("hspf", Kind.Decimal, facet: true, new[] { "hspf", "HSPF Rating" }),
        new("afue", Kind.Decimal, facet: true, new[] { "afue", "AFUE Rating" }),
        new("energyRating", Kind.String, facet: true, new[] { "energyRating", "Energy Rating", "Energy Class", "Energy Label" }),
        new("voltage", Kind.String, facet: true, new[] { "voltage", "Volts" }),
        new("refrigerantType", Kind.String, facet: true, new[] { "refrigerantType", "Refrigerant", "Refrigerant Type" }),
        new("fuelType", Kind.String, facet: true, new[] { "fuelType", "Fuel", "Fuel Type" }),
    };

    private static readonly Dictionary<string, Field> ByCanonical =
        Fields.ToDictionary(f => f.Canonical, StringComparer.OrdinalIgnoreCase);

    // Matches a grouped-thousands number ("12,000" / "12 000") OR a plain number, with an optional decimal part.
    // The grouped alt requires exactly-3-digit groups so "22 dB" (space then a unit) still parses as 22, not "22 000".
    private static readonly Regex NumberRx = new(@"-?\d{1,3}(?:[,\s]\d{3})+(?:\.\d+)?|-?\d+(?:\.\d+)?", RegexOptions.Compiled);

    /// <summary>
    /// Canonical machine-only keys hidden from the PUBLIC product DTO — they are scoring inputs, not marketing
    /// specs, and render badly in the spec table. Only these three have NO display-key equivalent, so removing
    /// them can never strip a customer-facing row (note <c>Normalize("BTU") == Normalize("btu")</c>, so
    /// <c>btu</c>/<c>noiseLevel</c> are intentionally NOT listed — they alias from the display "BTU"/"Noise Level"
    /// rows we keep).
    /// </summary>
    public static readonly IReadOnlyList<string> MachineOnlyKeys = new[] { "isInverter", "minTemp", "recommendedRoomTypes" };

    /// <summary>True if <paramref name="rawKey"/> is one of the machine-only canonical keys hidden from the public DTO.</summary>
    public static bool IsMachineOnlyKey(string rawKey) =>
        MachineOnlyKeys.Any(k => string.Equals(k, rawKey, StringComparison.OrdinalIgnoreCase));

    // ---- Public typed accessors ---------------------------------------------------------------------------

    public static int? GetInt(IReadOnlyDictionary<string, object>? specs, string canonicalKey)
    {
        var (field, raw) = Resolve(specs, canonicalKey);
        return field is null || raw is null ? null : ToInt(raw, field);
    }

    public static decimal? GetDecimal(IReadOnlyDictionary<string, object>? specs, string canonicalKey)
    {
        var (field, raw) = Resolve(specs, canonicalKey);
        return field is null || raw is null ? null : ToDecimal(raw, field);
    }

    public static bool? GetBool(IReadOnlyDictionary<string, object>? specs, string canonicalKey)
    {
        var (field, raw) = Resolve(specs, canonicalKey);
        return field is null || raw is null ? null : ToBool(raw);
    }

    public static string? GetString(IReadOnlyDictionary<string, object>? specs, string canonicalKey)
    {
        var (field, raw) = Resolve(specs, canonicalKey);
        return field is null || raw is null ? null : ToStringValue(raw);
    }

    public static List<string> GetStringList(IReadOnlyDictionary<string, object>? specs, string canonicalKey)
    {
        var (field, raw) = Resolve(specs, canonicalKey);
        return field is null || raw is null ? new List<string>() : ToStringList(raw);
    }

    /// <summary>
    /// If <paramref name="rawKey"/> is a recognised HVAC FACET key, returns the canonical facet key + a
    /// normalised string value; otherwise null. Lets the filter-facet extractor group aliases
    /// ("SEER Rating" + "seer" → one "seer" bucket) with a consistent value so "12000" and "12000 BTU" don't
    /// split into two buckets. Only facet-appropriate fields participate (not noiseLevel/isInverter/minTemp/roomTypes).
    /// </summary>
    public static (string canonicalKey, string value)? ResolveFacet(string rawKey, object? rawValue)
    {
        if (rawValue is null) return null;
        var norm = Normalize(rawKey);
        foreach (var field in Fields)
        {
            if (!field.Facet) continue;
            if (Array.IndexOf(field.NormAliases, norm) < 0) continue;

            var value = FormatFacetValue(field, rawValue);
            return string.IsNullOrWhiteSpace(value) ? null : (field.Canonical, value!);
        }
        return null;
    }

    /// <summary>
    /// Resolve ALL HVAC facet values for one product — ONE value per canonical facet key, honouring alias
    /// precedence (a heat pump's "BTU Cooling" beats "BTU Heating", so it yields a single "btu" facet; a product
    /// carrying both "SEER Rating" and "seer" yields a single "seer" facet). This is the correct grouping for
    /// facet extraction: resolving each raw spec independently would double-count multi-alias fields.
    /// </summary>
    public static IEnumerable<(string canonicalKey, string value)> ResolveFacets(IReadOnlyDictionary<string, object>? specs)
    {
        if (specs is null || specs.Count == 0) yield break;

        var normMap = new Dictionary<string, object>(specs.Count);
        foreach (var kvp in specs)
        {
            var nk = Normalize(kvp.Key);
            if (!normMap.ContainsKey(nk)) normMap[nk] = kvp.Value;
        }

        foreach (var field in Fields)
        {
            if (!field.Facet) continue;

            object? raw = null;
            foreach (var normAlias in field.NormAliases)
                if (normMap.TryGetValue(normAlias, out raw)) break;
            if (raw is null) continue;

            var value = FormatFacetValue(field, raw);
            if (!string.IsNullOrWhiteSpace(value))
                yield return (field.Canonical, value!);
        }
    }

    /// <summary>Canonical string form of a facet value: ints plain, decimals G29-normalised (21 / 21.0 / 21.00
    /// all → "21" so they don't split into separate facet buckets), everything else the trimmed raw string.</summary>
    private static string? FormatFacetValue(Field field, object raw) => field.Kind switch
    {
        Kind.Int => ToInt(raw, field)?.ToString(CultureInfo.InvariantCulture),
        Kind.Decimal => ToDecimal(raw, field)?.ToString("G29", CultureInfo.InvariantCulture),
        _ => ToStringValue(raw)
    };

    // ---- Resolution -------------------------------------------------------------------------------------

    private static (Field? field, object? raw) Resolve(IReadOnlyDictionary<string, object>? specs, string canonicalKey)
    {
        if (specs is null || specs.Count == 0) return (null, null);
        if (!ByCanonical.TryGetValue(canonicalKey, out var field)) return (null, null);

        // Normalised view of the product's keys (first spelling wins on any duplicate).
        var normMap = new Dictionary<string, object>(specs.Count);
        foreach (var kvp in specs)
        {
            var nk = Normalize(kvp.Key);
            if (!normMap.ContainsKey(nk)) normMap[nk] = kvp.Value;
        }

        // First alias (in precedence order) that the product actually has wins.
        foreach (var normAlias in field.NormAliases)
            if (normMap.TryGetValue(normAlias, out var v)) return (field, v);

        return (field, null);
    }

    // ---- Value coercion ---------------------------------------------------------------------------------

    private static int? ToInt(object raw, Field field)
    {
        var d = ToDecimal(raw, field);
        return d is null ? null : (int)Math.Round(d.Value, MidpointRounding.AwayFromZero);
    }

    private static decimal? ToDecimal(object raw, Field field)
    {
        switch (raw)
        {
            case int i: return i;
            case long l: return l;
            case double db: return (decimal)db;
            case decimal m: return m;
            case float f: return (decimal)f;
            case bool: return null;
            case string s: return ParseNumberFromString(s, field);
            case JsonElement je:
                return je.ValueKind switch
                {
                    JsonValueKind.Number => je.TryGetDecimal(out var dec) ? dec : null,
                    JsonValueKind.String => ParseNumberFromString(je.GetString(), field),
                    _ => null
                };
            default:
                return null;
        }
    }

    private static decimal? ParseNumberFromString(string? s, Field field)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (field.UnitReject is not null)
        {
            foreach (var bad in field.UnitReject)
                if (s.Contains(bad, StringComparison.OrdinalIgnoreCase)) return null; // wrong unit — don't guess
        }
        var match = NumberRx.Match(s);
        if (!match.Success) return null;
        // Strip thousands separators (comma / whitespace) before invariant parse; the decimal point survives.
        var cleaned = match.Value.Replace(",", string.Empty).Replace(" ", string.Empty);
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var val) ? val : null;
    }

    private static bool? ToBool(object raw)
    {
        switch (raw)
        {
            case bool b: return b;
            case string s:
                var t = s.Trim().ToLowerInvariant();
                return t switch { "true" or "yes" or "1" => true, "false" or "no" or "0" => false, _ => (bool?)null };
            case JsonElement je:
                return je.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => ToBool(je.GetString() ?? string.Empty),
                    _ => null
                };
            default:
                return null;
        }
    }

    private static string? ToStringValue(object raw)
    {
        switch (raw)
        {
            case string s: return s.Trim();
            case JsonElement je:
                return je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString()?.Trim(),
                    JsonValueKind.Number => je.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };
            default:
                return raw.ToString();
        }
    }

    private static List<string> ToStringList(object raw)
    {
        switch (raw)
        {
            case List<string> ls: return ls;
            case string[] arr: return arr.ToList();
            case IEnumerable<string> en: return en.ToList();
            case string s:
                return SplitCsv(s);
            case JsonElement je when je.ValueKind == JsonValueKind.Array:
                var list = new List<string>();
                foreach (var el in je.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.String) continue;
                    var v = el.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) list.Add(v.Trim());
                }
                return list;
            case JsonElement je when je.ValueKind == JsonValueKind.String:
                // Admin-entered CSV survives a JSONB round-trip as a JSON string — split it the same way.
                return SplitCsv(je.GetString());
            default:
                return new List<string>();
        }
    }

    private static List<string> SplitCsv(string? s) =>
        string.IsNullOrWhiteSpace(s)
            ? new List<string>()
            : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string Normalize(string key)
    {
        var sb = new StringBuilder(key.Length);
        foreach (var ch in key)
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }
}
