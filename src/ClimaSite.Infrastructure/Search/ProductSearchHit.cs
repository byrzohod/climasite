namespace ClimaSite.Infrastructure.Search;

/// <summary>
/// Keyless projection for the ranked page of the FTS query (registered <c>HasNoKey().ToView(null)</c> so it
/// maps to no table). One row per matching product on the page: its id and relevance score. The total match
/// count is fetched by a sibling COUNT query that shares the exact same FROM/WHERE (so it is correct even for
/// an out-of-range page, which returns zero rows).
/// </summary>
public sealed class ProductSearchHit
{
    public Guid Id { get; set; }
    public double Score { get; set; }
}
