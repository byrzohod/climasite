using System.Text.Json;
using ClimaSite.Core.Entities;
using ClimaSite.Core.Interfaces;
using ClimaSite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Infrastructure.Repositories;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    private readonly ICategoryRepository _categoryRepository;

    public ProductRepository(ApplicationDbContext context, ICategoryRepository categoryRepository) : base(context)
    {
        _categoryRepository = categoryRepository;
    }

    public override async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant() && p.IsActive, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Sku == sku.ToUpperInvariant(), cancellationToken);
    }

    public async Task<PagedResult<Product>> GetPagedAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Translations)
            .AsQueryable();

        if (filter.IsActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            var category = await Context.Categories
                .FirstOrDefaultAsync(c => c.Slug == filter.CategorySlug.ToLowerInvariant(), cancellationToken);

            if (category != null)
            {
                var categoryIds = new List<Guid> { category.Id };
                var descendantIds = await _categoryRepository.GetDescendantIdsAsync(category.Id, cancellationToken);
                categoryIds.AddRange(descendantIds);

                query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
            }
        }

        if (filter.Brands != null && filter.Brands.Any())
        {
            var normalizedBrands = filter.Brands.Select(b => b.ToLowerInvariant()).ToList();
            query = query.Where(p => p.Brand != null && normalizedBrands.Contains(p.Brand.ToLower()));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);
        }

        if (filter.InStock == true)
        {
            query = query.Where(p => p.Variants.Any(v => v.IsActive && v.StockQuantity > 0));
        }

        if (filter.Tags != null && filter.Tags.Any())
        {
            var normalizedTags = filter.Tags.Select(t => t.ToLowerInvariant()).ToList();
            query = query.Where(p => p.Tags.Any(t => normalizedTags.Contains(t)));
        }

        query = ApplySorting(query, filter.SortBy);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var searchTerms = request.Query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var query = DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Translations)
            .Where(p => p.IsActive)
            .AsQueryable();

        foreach (var term in searchTerms)
        {
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(term)) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(term)) ||
                (p.Model != null && p.Model.ToLower().Contains(term)) ||
                p.Tags.Any(t => t.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var category = await Context.Categories
                .FirstOrDefaultAsync(c => c.Slug == request.CategorySlug.ToLowerInvariant(), cancellationToken);

            if (category != null)
            {
                var categoryIds = new List<Guid> { category.Id };
                var descendantIds = await _categoryRepository.GetDescendantIdsAsync(category.Id, cancellationToken);
                categoryIds.AddRange(descendantIds);

                query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
            }
        }

        if (request.Brands != null && request.Brands.Any())
        {
            var normalizedBrands = request.Brands.Select(b => b.ToLowerInvariant()).ToList();
            query = query.Where(p => p.Brand != null && normalizedBrands.Contains(p.Brand.ToLower()));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= request.MaxPrice.Value);
        }

        query = query.OrderByDescending(p =>
            (p.Name.ToLower().Contains(request.Query.ToLower()) ? 10 : 0) +
            (p.Brand != null && p.Brand.ToLower().Contains(request.Query.ToLower()) ? 5 : 0))
            .ThenBy(p => p.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Translations)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, bool includeChildren = true, CancellationToken cancellationToken = default)
    {
        var categoryIds = new List<Guid> { categoryId };

        if (includeChildren)
        {
            var descendantIds = await _categoryRepository.GetDescendantIdsAsync(categoryId, cancellationToken);
            categoryIds.AddRange(descendantIds);
        }

        return await DbSet
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Include(p => p.Translations)
            .Where(p => p.IsActive && p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetRelatedAsync(Guid productId, RelationType? relationType = null, int count = 10, CancellationToken cancellationToken = default)
    {
        var query = Context.RelatedProducts
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Category)
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Variants.Where(v => v.IsActive))
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Images.Where(i => i.IsPrimary))
            .Include(rp => rp.Related)
                .ThenInclude(p => p.Translations)
            .Where(rp => rp.ProductId == productId && rp.Related.IsActive);

        if (relationType.HasValue)
        {
            query = query.Where(rp => rp.RelationType == relationType.Value);
        }

        var relatedProducts = await query
            .OrderBy(rp => rp.SortOrder)
            .Take(count)
            .Select(rp => rp.Related)
            .ToListAsync(cancellationToken);

        return relatedProducts;
    }

    public async Task<FilterOptions> GetFilterOptionsAsync(Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.IsActive);

        if (categoryId.HasValue)
        {
            var categoryIds = new List<Guid> { categoryId.Value };
            var descendantIds = await _categoryRepository.GetDescendantIdsAsync(categoryId.Value, cancellationToken);
            categoryIds.AddRange(descendantIds);

            query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
        }

        var products = await query.ToListAsync(cancellationToken);

        var brands = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Brand))
            .GroupBy(p => p.Brand!)
            .Select(g => new BrandOption(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Name)
            .ToList();

        var prices = products.Select(p => p.BasePrice).ToList();
        var priceRange = new PriceRange(
            prices.Any() ? prices.Min() : 0,
            prices.Any() ? prices.Max() : 0
        );

        var specifications = ExtractSpecificationOptions(products);

        var tags = products
            .SelectMany(p => p.Tags)
            .GroupBy(t => t)
            .Select(g => new TagOption(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name)
            .ToList();

        return new FilterOptions(brands, priceRange, specifications, tags);
    }

    private Dictionary<string, IReadOnlyList<SpecificationOption>> ExtractSpecificationOptions(List<Product> products)
    {
        var specOptions = new Dictionary<string, List<SpecificationOption>>();
        var hvacSpecKeys = new[] { "btu", "energyRating", "seer", "eer", "hspf", "voltage", "refrigerantType", "fuelType", "afue" };

        foreach (var product in products)
        {
            foreach (var spec in product.Specifications)
            {
                if (!hvacSpecKeys.Contains(spec.Key, StringComparer.OrdinalIgnoreCase))
                    continue;

                var key = spec.Key;
                var value = spec.Value?.ToString() ?? string.Empty;

                if (spec.Value is JsonElement jsonElement)
                {
                    value = jsonElement.ValueKind switch
                    {
                        JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                        JsonValueKind.Number => jsonElement.GetRawText(),
                        _ => jsonElement.GetRawText()
                    };
                }

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (!specOptions.ContainsKey(key))
                {
                    specOptions[key] = new List<SpecificationOption>();
                }

                var existingOption = specOptions[key].FirstOrDefault(o => o.Value == value);
                if (existingOption != null)
                {
                    var index = specOptions[key].IndexOf(existingOption);
                    specOptions[key][index] = existingOption with { Count = existingOption.Count + 1 };
                }
                else
                {
                    var label = FormatSpecificationLabel(key, value);
                    specOptions[key].Add(new SpecificationOption(value, label, 1));
                }
            }
        }

        return specOptions.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<SpecificationOption>)kvp.Value
                .OrderBy(o => GetSortableValue(o.Value))
                .ToList()
        );
    }

    private static string FormatSpecificationLabel(string key, string value)
    {
        return key.ToLowerInvariant() switch
        {
            "btu" => $"{value} BTU",
            "seer" => $"SEER {value}",
            "eer" => $"EER {value}",
            "hspf" => $"HSPF {value}",
            "afue" => $"{value}% AFUE",
            "voltage" => value.Contains("V") ? value : $"{value}V",
            _ => value
        };
    }

    private static double GetSortableValue(string value)
    {
        if (double.TryParse(value.Replace(",", ""), out var numericValue))
        {
            return numericValue;
        }
        return 0;
    }

    public async Task<IReadOnlyList<string>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.IsActive && !string.IsNullOrWhiteSpace(p.Brand))
            .Select(p => p.Brand!)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.ToLowerInvariant();
        var query = DbSet.Where(p => p.Slug == normalizedSlug);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> SkuExistsAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.ToUpperInvariant();
        var query = DbSet.Where(p => p.Sku == normalizedSku);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, ProductSortBy sortBy)
    {
        return sortBy switch
        {
            ProductSortBy.Newest => query.OrderByDescending(p => p.CreatedAt),
            ProductSortBy.Oldest => query.OrderBy(p => p.CreatedAt),
            ProductSortBy.PriceAsc => query.OrderBy(p => p.BasePrice),
            ProductSortBy.PriceDesc => query.OrderByDescending(p => p.BasePrice),
            ProductSortBy.NameAsc => query.OrderBy(p => p.Name),
            ProductSortBy.NameDesc => query.OrderByDescending(p => p.Name),
            ProductSortBy.Popular => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt),
            ProductSortBy.Rating => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }
}
