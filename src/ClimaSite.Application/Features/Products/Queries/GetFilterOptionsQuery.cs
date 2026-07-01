using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Products.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public record GetFilterOptionsQuery : IRequest<FilterOptionsDto>, ICacheableQuery
{
    public string? CategorySlug { get; init; }

    public string CacheKey => $"filter_options_{CategorySlug ?? "all"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

public record FilterOptionsDto
{
    public List<BrandOptionDto> Brands { get; init; } = new();
    public PriceRangeDto PriceRange { get; init; } = new(0, 0);
    public Dictionary<string, List<SpecificationOptionDto>> Specifications { get; init; } = new();
    public List<TagOptionDto> Tags { get; init; } = new();
}

public record BrandOptionDto(string Name, int Count);
public record PriceRangeDto(decimal Min, decimal Max);
public record SpecificationOptionDto(string Value, string Label, int Count);
public record TagOptionDto(string Name, int Count);

public class GetFilterOptionsQueryHandler : IRequestHandler<GetFilterOptionsQuery, FilterOptionsDto>
{
    private readonly IApplicationDbContext _context;

    public GetFilterOptionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FilterOptionsDto> Handle(
        GetFilterOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var categoryIds = await GetCategoryIdsWithDescendantsAsync(request.CategorySlug, cancellationToken);
            if (categoryIds.Any())
            {
                query = query.Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));
            }
        }

        var products = await query.ToListAsync(cancellationToken);

        var brands = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Brand))
            .GroupBy(p => p.Brand!)
            .Select(g => new BrandOptionDto(g.Key, g.Count()))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Name)
            .ToList();

        var prices = products.Select(p => p.BasePrice).ToList();
        var priceRange = new PriceRangeDto(
            prices.Any() ? prices.Min() : 0,
            prices.Any() ? prices.Max() : 0
        );

        var specifications = ExtractSpecificationOptions(products);

        var tags = products
            .SelectMany(p => p.Tags)
            .GroupBy(t => t)
            .Select(g => new TagOptionDto(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name)
            .ToList();

        return new FilterOptionsDto
        {
            Brands = brands,
            PriceRange = priceRange,
            Specifications = specifications,
            Tags = tags
        };
    }

    private async Task<List<Guid>> GetCategoryIdsWithDescendantsAsync(string categorySlug, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Slug == categorySlug.ToLowerInvariant(), cancellationToken);

        if (category == null)
            return new List<Guid>();

        var allCategoryIds = new List<Guid> { category.Id };
        await GetDescendantIdsAsync(category.Id, allCategoryIds, cancellationToken);

        return allCategoryIds;
    }

    private async Task GetDescendantIdsAsync(Guid parentId, List<Guid> ids, CancellationToken cancellationToken)
    {
        var childIds = await _context.Categories
            .Where(c => c.ParentId == parentId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        ids.AddRange(childIds);

        foreach (var childId in childIds)
        {
            await GetDescendantIdsAsync(childId, ids, cancellationToken);
        }
    }

    private static Dictionary<string, List<SpecificationOptionDto>> ExtractSpecificationOptions(List<Core.Entities.Product> products)
    {
        var specOptions = new Dictionary<string, List<SpecificationOptionDto>>();

        foreach (var product in products)
        {
            // Resolve ONE value per canonical facet key via the shared resolver (alias precedence + dedupe):
            // "SEER Rating"/"seer" → one "seer"; a heat pump's "BTU Cooling"/"BTU Heating" → a single "btu"
            // (cooling wins). Each product contributes at most one option per canonical facet (B-016 council).
            foreach (var (key, value) in HvacSpecResolver.ResolveFacets(product.Specifications))
            {
                if (!specOptions.TryGetValue(key, out var options))
                {
                    options = new List<SpecificationOptionDto>();
                    specOptions[key] = options;
                }

                var existingOption = options.FirstOrDefault(o => o.Value == value);
                if (existingOption != null)
                {
                    var index = options.IndexOf(existingOption);
                    options[index] = existingOption with { Count = existingOption.Count + 1 };
                }
                else
                {
                    options.Add(new SpecificationOptionDto(value, FormatSpecificationLabel(key, value), 1));
                }
            }
        }

        return specOptions.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(o => GetSortableValue(o.Value)).ToList()
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
}
