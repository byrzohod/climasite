using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace ClimaSite.Infrastructure.Search;

/// <summary>
/// Postgres full-text product search. ONE parameterized statement does match + facets + relevance + paging +
/// total (window count), then the handler hydrates the returned ids via EF. Design (council-validated):
/// <list type="bullet">
/// <item>FTS branch: <c>search_vector @@ plainto_tsquery(...)</c> over the trigger-maintained denormalized
///   per-product vector (base fields + tags + ALL translations). <c>plainto_tsquery</c> (not websearch) gives
///   literal AND-of-terms and won't misparse model codes like <c>MSZ-AP25</c> (websearch's <c>-</c> = NOT).</item>
/// <item>Substring branch (keeps today's per-term substring recall a strict superset): EVERY term must
///   substring-match some field (incl. description/tags/translations), via <c>unnest(@terms)</c>.</item>
/// <item>Ranking: <c>ts_rank_cd</c> (weights {D,C,B,A}) + an additive exact/prefix SKU boost.</item>
/// <item>All inputs are parameters; LIKE metacharacters in terms are escaped, so <c>%</c>/<c>_</c> can't
///   widen the match and there is no injection surface.</item>
/// </list>
/// </summary>
public sealed class ProductSearchService : IProductSearchService
{
    private readonly ApplicationDbContext _db;

    public ProductSearchService(ApplicationDbContext db) => _db = db;

    // The FTS config name here MUST equal the one the migration (AddProductFullTextSearch) creates and the
    // trigger uses to build search_vector — the query config must match the vector config or lexemes won't
    // align. The weight array ARRAY[0.1,0.2,0.4,1.0] is order {D,C,B,A} (A=name highest); both are constants
    // here, never user input. count(*) OVER() returns the total in the same statement so page + total can't
    // drift. NB: weights use ARRAY[...] (square brackets) NOT '{...}' — a literal "{0.1...}" collides with
    // FromSqlRaw's {n} placeholder parser and throws a FormatException before the query ever runs.
    private const string SqlHead = @"
SELECT p.id AS ""Id"",
       (ts_rank_cd(ARRAY[0.1, 0.2, 0.4, 1.0]::real[], p.search_vector, q.query)
        + CASE WHEN lower(p.sku) = lower(@rawQuery) THEN 2.0
               WHEN p.sku ILIKE @skuPrefix ESCAPE '\' THEN 1.0
               ELSE 0 END)::double precision AS ""Score"",
       count(*) OVER() AS ""TotalCount""
FROM products p
CROSS JOIN (SELECT plainto_tsquery('climasite_search', @rawQuery) AS query) q
WHERE p.is_active
  AND (
        p.search_vector @@ q.query
        OR NOT EXISTS (
            SELECT 1 FROM unnest(@terms::text[]) AS term
            WHERE NOT (
                 p.name ILIKE '%' || term || '%' ESCAPE '\'
              OR (p.brand IS NOT NULL AND p.brand ILIKE '%' || term || '%' ESCAPE '\')
              OR (p.model IS NOT NULL AND p.model ILIKE '%' || term || '%' ESCAPE '\')
              OR p.sku ILIKE '%' || term || '%' ESCAPE '\'
              OR (p.short_description IS NOT NULL AND p.short_description ILIKE '%' || term || '%' ESCAPE '\')
              OR (p.description IS NOT NULL AND p.description ILIKE '%' || term || '%' ESCAPE '\')
              OR EXISTS (SELECT 1 FROM unnest(p.tags) tg WHERE tg ILIKE '%' || term || '%' ESCAPE '\')
              OR EXISTS (SELECT 1 FROM product_translations t
                         WHERE t.product_id = p.id
                           AND (t.name ILIKE '%' || term || '%' ESCAPE '\'
                             OR (t.short_description IS NOT NULL AND t.short_description ILIKE '%' || term || '%' ESCAPE '\')
                             OR (t.description IS NOT NULL AND t.description ILIKE '%' || term || '%' ESCAPE '\')))
            )
        )
      )
  AND (@catIds::uuid[] IS NULL OR p.category_id = ANY(@catIds::uuid[]))
  AND (@brands::text[] IS NULL OR (p.brand IS NOT NULL AND lower(p.brand) = ANY(@brands::text[])))
  AND (@minPrice::numeric IS NULL OR p.base_price >= @minPrice::numeric)
  AND (@maxPrice::numeric IS NULL OR p.base_price <= @maxPrice::numeric)
  AND (NOT @inStock OR EXISTS (SELECT 1 FROM product_variants v WHERE v.product_id = p.id AND v.stock_quantity > 0))
  AND (NOT @onSale OR (p.compare_at_price IS NOT NULL AND p.compare_at_price > p.base_price))
  AND (NOT @featured OR p.is_featured)
ORDER BY ";

    private const string SqlTail = @"
OFFSET @skip LIMIT @take";

    public async Task<ProductSearchResult> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken)
    {
        var terms = filter.RawQuery
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(EscapeLike)
            .ToArray();

        // Defensive: a blank/whitespace query reaches here only if a caller's guard is bypassed. With no terms
        // the substring branch's NOT EXISTS(unnest(empty)) is vacuously true and would match the whole catalog,
        // so return an empty page instead.
        if (terms.Length == 0)
            return new ProductSearchResult(Array.Empty<Guid>(), 0);

        // OrderByClause returns a whitelisted constant clause — NEVER user input — so this concatenation is safe.
        var sql = SqlHead + OrderByClause(filter) + SqlTail;

        var parameters = new object[]
        {
            new NpgsqlParameter("rawQuery", NpgsqlDbType.Text) { Value = filter.RawQuery },
            new NpgsqlParameter("skuPrefix", NpgsqlDbType.Text) { Value = EscapeLike(filter.RawQuery.ToLowerInvariant()) + "%" },
            new NpgsqlParameter("terms", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = terms },
            ArrayParam("catIds", NpgsqlDbType.Uuid, filter.CategoryIds?.ToArray()),
            ArrayParam("brands", NpgsqlDbType.Text, filter.Brands?.ToArray()),
            NullableNumeric("minPrice", filter.MinPrice),
            NullableNumeric("maxPrice", filter.MaxPrice),
            new NpgsqlParameter("inStock", NpgsqlDbType.Boolean) { Value = filter.InStock },
            new NpgsqlParameter("onSale", NpgsqlDbType.Boolean) { Value = filter.OnSale },
            new NpgsqlParameter("featured", NpgsqlDbType.Boolean) { Value = filter.IsFeatured },
            new NpgsqlParameter("skip", NpgsqlDbType.Integer) { Value = (filter.PageNumber - 1) * filter.PageSize },
            new NpgsqlParameter("take", NpgsqlDbType.Integer) { Value = filter.PageSize },
        };

        var hits = await _db.Set<ProductSearchHit>()
            .FromSqlRaw(sql, parameters)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var total = hits.Count > 0 ? (int)hits[0].TotalCount : 0;
        return new ProductSearchResult(hits.Select(h => h.Id).ToList(), total);
    }

    /// <summary>Whitelisted ORDER BY (never user input). Relevance when no explicit sort + a query is present.</summary>
    private static string OrderByClause(ProductSearchFilter filter) => filter.SortBy?.ToLowerInvariant() switch
    {
        "name" => filter.SortDescending ? "p.name DESC" : "p.name ASC",
        "price" => filter.SortDescending ? "p.base_price DESC" : "p.base_price ASC",
        "newest" => "p.created_at DESC",
        _ => "\"Score\" DESC, p.name ASC",
    };

    /// <summary>Escapes LIKE metacharacters so a term like "%" or "_" matches literally (with ESCAPE '\').</summary>
    private static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static NpgsqlParameter ArrayParam<T>(string name, NpgsqlDbType elementType, T[]? value) =>
        new(name, NpgsqlDbType.Array | elementType) { Value = (object?)value ?? DBNull.Value };

    private static NpgsqlParameter NullableNumeric(string name, decimal? value) =>
        new(name, NpgsqlDbType.Numeric) { Value = (object?)value ?? DBNull.Value };
}
