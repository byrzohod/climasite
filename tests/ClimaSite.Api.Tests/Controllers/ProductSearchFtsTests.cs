using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// SEARCH-01-fts — the REAL contract for the Postgres full-text product search, against a real Postgres
/// (Testcontainers). The pre-existing E2E/integration tests only assert presence/empty-state (no ranking,
/// no recall), so THESE are the meaningful safety net: ranking, the recall superset (substring in
/// description/tags/cross-language multi-term), diacritics, facets+paging, and injection/wildcard safety.
///
/// Mutation gate (proves the suite checks ORDERING, not just presence): flip the weight array in
/// ProductSearchService.SqlHead to <c>'{1.0,0.4,0.2,0.1}'</c> (rank reversal) — every presence/recall test
/// here still passes, but <see cref="Ranking_TermInName_RanksAboveTermInDescriptionOnly"/> MUST fail.
/// </summary>
public class ProductSearchFtsTests : IntegrationTestBase
{
    public ProductSearchFtsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private sealed record SearchResponse(List<SearchItem> Items, int TotalCount, int PageNumber);
    private sealed record SearchItem(Guid Id, string Name, string Slug, string? Brand);

    // ---- helpers ----------------------------------------------------------------------------------

    private async Task<Product> SeedAsync(
        string sku, string name, string slug, Action<Product>? configure = null)
    {
        var p = new Product(sku, name, slug, 100m);
        p.SetActive(true);
        configure?.Invoke(p);
        DbContext.Products.Add(p);
        await DbContext.SaveChangesAsync();
        return p;
    }

    private async Task AddTranslationAsync(Guid productId, string lang, string name, string? description = null)
    {
        var tr = new ProductTranslation(productId, lang, name);
        if (description != null) tr.Description = description;
        DbContext.ProductTranslations.Add(tr);
        await DbContext.SaveChangesAsync();
    }

    private async Task<SearchResponse> SearchProductsAsync(string query, string? extraQs = null)
    {
        var url = $"/api/products?searchTerm={Uri.EscapeDataString(query)}" + extraQs;
        var resp = await Client.GetAsync(url);
        var bodyText = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.OK, "search must not error; body was: {0}", bodyText);
        var body = await resp.Content.ReadFromJsonAsync<SearchResponse>();
        body.Should().NotBeNull();
        return body!;
    }

    // ---- tests ------------------------------------------------------------------------------------

    [Fact] // 1
    public async Task Ranking_TermInName_RanksAboveTermInDescriptionOnly()
    {
        await SeedAsync("RANK-DESC", "Plain Box", "rank-desc",
            p => p.SetDescription("a quiet inverter mechanism inside"));
        await SeedAsync("RANK-NAME", "Inverter Pro", "rank-name",
            p => p.SetDescription("nothing special here"));

        var result = await SearchProductsAsync("inverter");

        result.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Items[0].Slug.Should().Be("rank-name",
            "a name match (weight A) must rank above a description-only match (weight D)");
    }

    [Fact] // 2
    public async Task Search_ExactSku_RanksFirst()
    {
        await SeedAsync("ABC123", "Alpha Unit", "sku-alpha");          // matches via exact-SKU boost only
        await SeedAsync("ZZZ999", "ABC123 Special", "sku-named");      // matches via name FTS, no boost

        var result = await SearchProductsAsync("ABC123");

        result.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Items[0].Slug.Should().Be("sku-alpha", "an exact SKU match must rank first (+2 boost)");
    }

    [Fact] // 3
    public async Task Search_CodeSubstring_MatchesModelViaTrigram()
    {
        await SeedAsync("AC-1000", "Wall Mount", "code-sub", p => p.SetModel("MSZ-AP25"));

        var result = await SearchProductsAsync("AP2"); // FTS can't lex 'AP2'; the pg_trgm ILIKE fallback can

        result.Items.Should().ContainSingle(i => i.Slug == "code-sub");
    }

    [Fact] // 4
    public async Task Search_SubstringInDescription_IsStillReturned_SupersetRecall()
    {
        await SeedAsync("DESC-SUB", "Box", "desc-sub",
            p => p.SetDescription("a powerful air conditioner unit"));

        // "condition" is a mid-word substring of "conditioner" — whole-lexeme FTS misses it, the substring
        // branch keeps it (this is the recall superset the council required).
        var result = await SearchProductsAsync("condition");

        result.Items.Should().ContainSingle(i => i.Slug == "desc-sub");
    }

    [Fact] // 5
    public async Task Search_TagSubstring_IsStillReturned()
    {
        await SeedAsync("TAG-SUB", "Gadget", "tag-sub", p => p.SetTags(new List<string> { "supercooler" }));

        var result = await SearchProductsAsync("cooler"); // mid-word substring of the tag

        result.Items.Should().ContainSingle(i => i.Slug == "tag-sub");
    }

    [Fact] // 6
    public async Task Search_CrossLanguageMultiTerm_IsReturned()
    {
        var p = await SeedAsync("XLANG", "Climate Unit", "xlang", x => x.SetBrand("Mitsubishi"));
        await AddTranslationAsync(p.Id, "bg", "Климатик Инвертор"); // 'инвертор' lives ONLY in the BG name

        // One term is in the base brand, the other only in a BG translation — the denormalised vector ANDs both.
        var result = await SearchProductsAsync("mitsubishi инвертор");

        result.Items.Should().ContainSingle(i => i.Slug == "xlang");
    }

    [Fact] // 7
    public async Task Search_MultiTerm_RequiresAllTerms()
    {
        await SeedAsync("AND-1", "Quiet Inverter AC", "and-both", null);
        await SeedAsync("AND-2", "Quiet Fan", "and-one", null);

        var result = await SearchProductsAsync("quiet inverter");

        result.Items.Should().ContainSingle(i => i.Slug == "and-both");
        result.Items.Should().NotContain(i => i.Slug == "and-one", "the second term must also match (AND)");
    }

    [Fact] // 8
    public async Task Search_IsCaseInsensitive()
    {
        await SeedAsync("CASE-1", "Quiet AC", "case-1", null);

        var upper = await SearchProductsAsync("QUIET");
        var lower = await SearchProductsAsync("quiet");

        upper.Items.Select(i => i.Slug).Should().Contain("case-1");
        lower.Items.Select(i => i.Slug).Should().Contain("case-1");
    }

    [Fact] // 9
    public async Task Search_FoldsDiacritics_ViaUnaccent()
    {
        await SeedAsync("DIA-1", "Gerät Cooler", "dia-1", null);

        // 'gerat' (no umlaut) must match 'Gerät' — only possible because unaccent is baked into the config.
        var result = await SearchProductsAsync("gerat");

        result.Items.Should().ContainSingle(i => i.Slug == "dia-1");
    }

    [Fact] // 10
    public async Task Search_WithBrandFacet_AppliesFacet_AndWindowCountIsCorrect()
    {
        await SeedAsync("FAC-1", "Cooler One", "fac-1", p => p.SetBrand("Acme"));
        await SeedAsync("FAC-2", "Cooler Two", "fac-2", p => p.SetBrand("Acme"));
        await SeedAsync("FAC-3", "Cooler Three", "fac-3", p => p.SetBrand("Other"));

        // Search + brand facet + a page smaller than the match count: total must be the FULL faceted count.
        var result = await SearchProductsAsync("cooler", "&brand=Acme&pageNumber=1&pageSize=1");

        result.TotalCount.Should().Be(2, "only the two Acme coolers match the facet");
        result.Items.Should().HaveCount(1, "page size is 1");
        result.Items.Should().OnlyContain(i => i.Brand == "Acme");
    }

    [Fact] // 11a
    public async Task Search_PunctuationOnlyQuery_ReturnsNoResults_NoCrash()
    {
        await SeedAsync("PUNCT-1", "Normal Product", "punct-1", null);

        var result = await SearchProductsAsync("!!!");

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact] // 11b
    public async Task SearchEndpoint_BlankQuery_Returns400()
    {
        var resp = await Client.GetAsync("/api/products/search?q=%20");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact] // 12
    public async Task Search_WildcardTerm_IsTreatedLiterally_NotWholeCatalog()
    {
        await SeedAsync("WILD-1", "First Product", "wild-1", null);
        await SeedAsync("WILD-2", "Second Product", "wild-2", null);

        // A bare '%' must NOT match every product — LIKE metacharacters are escaped.
        var result = await SearchProductsAsync("%");

        result.TotalCount.Should().Be(0, "a '%' term is escaped to a literal, so it matches nothing here");
    }
}
