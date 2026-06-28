namespace ClimaSite.Application.Common.Interfaces;

/// <summary>
/// The complete filter passed to <see cref="IProductSearchService"/> — search term plus every facet/sort
/// both public search paths support, so neither handler silently drops a filter (e.g. InStock/OnSale/Sort).
/// Category descendants and brand normalisation are resolved by the caller (EF) before this is built.
/// </summary>
public sealed record ProductSearchFilter
{
    /// <summary>The raw user query (may contain multiple whitespace-separated terms).</summary>
    public required string RawQuery { get; init; }

    /// <summary>Resolved category ids (a single category for /api/products, or a slug + descendants for /search). Null = no category facet.</summary>
    public IReadOnlyList<Guid>? CategoryIds { get; init; }

    /// <summary>Brand filter, lower-cased. Null/empty = no brand facet.</summary>
    public IReadOnlyList<string>? Brands { get; init; }

    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStock { get; init; }
    public bool OnSale { get; init; }
    public bool IsFeatured { get; init; }

    /// <summary>"name" | "price" | "newest" | null. Null (or unknown) with a query present = relevance order.</summary>
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}

/// <summary>The ranked, paged product ids for one search, plus the total matching count (from a sibling COUNT query that shares the same predicate, so it is correct even for an out-of-range page).</summary>
public sealed record ProductSearchResult(IReadOnlyList<Guid> OrderedIds, int TotalCount);

/// <summary>
/// Postgres full-text search over products (FTS ∪ substring fallback, ranked). Implemented in Infrastructure
/// with parameterized raw SQL (a page query + a sibling COUNT sharing the same FROM/WHERE); callers hydrate
/// the returned ids via EF and re-order in memory.
/// </summary>
public interface IProductSearchService
{
    Task<ProductSearchResult> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken);
}
