namespace ClimaSite.Api.Common;

/// <summary>
/// Clamps untrusted pagination/count query parameters at the public API boundary (B-036).
///
/// Without this, <c>pageSize=0</c> divides by zero in <c>PaginatedList.TotalPages</c>, a huge
/// <c>pageNumber</c> overflows <c>(pageNumber-1)*pageSize</c> into a negative <c>Skip</c> (a 500), and
/// <c>pageSize</c>/<c>count</c> = 100000 fetches the whole table (a cheap DoS). Clamping at the edge keeps
/// every downstream query bounded.
/// </summary>
public static class QueryBounds
{
    public const int MaxPageSize = 100;
    public const int MaxItemCount = 24;

    // Cap the page number so (PageNumber-1)*PageSize stays well within Int32 (1e5 * 100 = 1e7 « 2.1e9).
    public const int MaxPageNumber = 100_000;

    // Cap a look-back window (e.g. price history) so a huge value can't fetch all history or overflow
    // a DateTime.AddDays(-n). 730 days = 2 years, more than any real UI range.
    public const int MaxLookbackDays = 730;

    public static int PageNumber(int value) => Math.Clamp(value, 1, MaxPageNumber);

    public static int PageSize(int value) => Math.Clamp(value, 1, MaxPageSize);

    public static int Count(int value) => Math.Clamp(value, 1, MaxItemCount);

    public static int Days(int value) => Math.Clamp(value, 1, MaxLookbackDays);
}
