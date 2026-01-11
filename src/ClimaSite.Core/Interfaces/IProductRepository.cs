using ClimaSite.Core.Entities;

namespace ClimaSite.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> GetPagedAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, bool includeChildren = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetRelatedAsync(Guid productId, RelationType? relationType = null, int count = 10, CancellationToken cancellationToken = default);
    Task<FilterOptions> GetFilterOptionsAsync(Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public record ProductFilterRequest(
    int Page = 1,
    int PageSize = 20,
    string? CategorySlug = null,
    IEnumerable<string>? Brands = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStock = null,
    Dictionary<string, IEnumerable<string>>? Specifications = null,
    IEnumerable<string>? Tags = null,
    ProductSortBy SortBy = ProductSortBy.Newest,
    bool IsActive = true
);

public record ProductSearchRequest(
    string Query,
    int Page = 1,
    int PageSize = 20,
    string? CategorySlug = null,
    IEnumerable<string>? Brands = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
);

public enum ProductSortBy
{
    Newest,
    Oldest,
    PriceAsc,
    PriceDesc,
    NameAsc,
    NameDesc,
    Popular,
    Rating
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public record FilterOptions(
    IReadOnlyList<BrandOption> Brands,
    PriceRange PriceRange,
    IReadOnlyDictionary<string, IReadOnlyList<SpecificationOption>> Specifications,
    IReadOnlyList<TagOption> Tags
);

public record BrandOption(string Name, int Count);
public record PriceRange(decimal Min, decimal Max);
public record SpecificationOption(string Value, string Label, int Count);
public record TagOption(string Name, int Count);
