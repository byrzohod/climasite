using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Common.Models;

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        // Defensive floor (B-036): pageSize 0 would divide by zero in TotalPages and pageNumber < 1
        // breaks HasPreviousPage. Callers should clamp untrusted input at the edge (see Api QueryBounds);
        // this is the last-line guard so no caller can produce a NaN/garbage page count.
        var safePageSize = pageSize < 1 ? 1 : pageSize;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)safePageSize);
        TotalCount = count;
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Mirror the constructor's floor so Skip/Take can't go negative/zero for a direct CreateAsync caller.
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 1 : pageSize;

        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, safePageNumber, safePageSize);
    }
}
