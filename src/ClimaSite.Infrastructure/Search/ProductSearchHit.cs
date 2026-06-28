namespace ClimaSite.Infrastructure.Search;

/// <summary>
/// Keyless projection for the raw FTS ranking query (registered <c>HasNoKey().ToView(null)</c> so it maps to
/// no table). One row per matching product: its id, its relevance score, and the window-counted total of all
/// matches (so page + total come from ONE statement — no separate-COUNT predicate drift).
/// </summary>
public sealed class ProductSearchHit
{
    public Guid Id { get; set; }
    public double Score { get; set; }
    public long TotalCount { get; set; }
}
