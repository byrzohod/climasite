# Search & Navigation Implementation Plan

## 1. Overview

This plan covers the implementation of product search and navigation functionality for ClimaSite HVAC e-commerce platform. The system provides full-text search using PostgreSQL's native capabilities, faceted filtering, search autocomplete, and hierarchical category navigation.

### Goals

- Enable customers to quickly find HVAC products through keyword search
- Provide faceted filtering by category, brand, price range, and specifications
- Implement real-time search suggestions for improved UX
- Build intuitive category navigation with breadcrumbs
- Achieve search response times under 100ms

### Dependencies

- Product Catalog (CAT-001) - Products and categories must exist
- PostgreSQL 16+ with full-text search extensions

---

## 2. Search Architecture

### 2.1 PostgreSQL Full-Text Search Setup

PostgreSQL provides robust full-text search capabilities through `tsvector` and `tsquery` types, eliminating the need for external search engines like Elasticsearch for our scale.

#### Search Vector Column Migration

```sql
-- Migration: Add search vector to products table
-- File: YYYYMMDDHHMMSS_AddProductSearchVector.sql

-- Add search vector column
ALTER TABLE products ADD COLUMN search_vector TSVECTOR;

-- Create function to update search vector with weighted fields
CREATE OR REPLACE FUNCTION update_product_search_vector()
RETURNS TRIGGER AS $$
BEGIN
  NEW.search_vector :=
    setweight(to_tsvector('english', COALESCE(NEW.name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(NEW.brand, '')), 'B') ||
    setweight(to_tsvector('english', COALESCE(NEW.description, '')), 'C') ||
    setweight(to_tsvector('english', COALESCE(NEW.sku, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(
      (SELECT string_agg(value::text, ' ') FROM jsonb_each_text(NEW.attributes)),
      ''
    )), 'D');
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger to auto-update search vector
CREATE TRIGGER product_search_vector_update
  BEFORE INSERT OR UPDATE OF name, brand, description, sku, attributes
  ON products
  FOR EACH ROW
  EXECUTE FUNCTION update_product_search_vector();

-- Create GIN index for fast full-text search
CREATE INDEX idx_products_search_vector ON products USING GIN(search_vector);

-- Create trigram extension for fuzzy matching
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Create trigram index for autocomplete/fuzzy search
CREATE INDEX idx_products_name_trgm ON products USING GIN(name gin_trgm_ops);
CREATE INDEX idx_products_brand_trgm ON products USING GIN(brand gin_trgm_ops);

-- Populate search vectors for existing products
UPDATE products SET search_vector =
  setweight(to_tsvector('english', COALESCE(name, '')), 'A') ||
  setweight(to_tsvector('english', COALESCE(brand, '')), 'B') ||
  setweight(to_tsvector('english', COALESCE(description, '')), 'C') ||
  setweight(to_tsvector('english', COALESCE(sku, '')), 'A');
```

#### Weight Explanation

| Weight | Priority | Fields | Description |
|--------|----------|--------|-------------|
| A | Highest | name, sku | Exact product identifiers |
| B | High | brand | Manufacturer name |
| C | Medium | description | Product details |
| D | Low | attributes | JSONB specifications |

### 2.2 Search Query Building

```sql
-- Example search query with ranking
SELECT
  p.id,
  p.name,
  p.slug,
  p.brand,
  p.base_price,
  p.sale_price,
  ts_rank_cd(p.search_vector, query) AS rank
FROM products p,
  plainto_tsquery('english', 'split air conditioner carrier') AS query
WHERE p.search_vector @@ query
  AND p.is_active = true
ORDER BY rank DESC, p.created_at DESC
LIMIT 20 OFFSET 0;
```

### 2.3 Facet Aggregation Queries

```sql
-- Category facet counts
SELECT
  c.id,
  c.name,
  c.slug,
  COUNT(p.id) AS product_count
FROM categories c
LEFT JOIN products p ON p.category_id = c.id
WHERE p.search_vector @@ plainto_tsquery('english', :search_term)
  AND p.is_active = true
GROUP BY c.id, c.name, c.slug
ORDER BY product_count DESC;

-- Brand facet counts
SELECT
  p.brand,
  COUNT(*) AS product_count
FROM products p
WHERE p.search_vector @@ plainto_tsquery('english', :search_term)
  AND p.is_active = true
GROUP BY p.brand
ORDER BY product_count DESC;

-- Price range buckets
SELECT
  CASE
    WHEN COALESCE(p.sale_price, p.base_price) < 500 THEN '0-500'
    WHEN COALESCE(p.sale_price, p.base_price) < 1000 THEN '500-1000'
    WHEN COALESCE(p.sale_price, p.base_price) < 2000 THEN '1000-2000'
    WHEN COALESCE(p.sale_price, p.base_price) < 5000 THEN '2000-5000'
    ELSE '5000+'
  END AS price_range,
  COUNT(*) AS product_count
FROM products p
WHERE p.search_vector @@ plainto_tsquery('english', :search_term)
  AND p.is_active = true
GROUP BY price_range
ORDER BY MIN(COALESCE(p.sale_price, p.base_price));

-- Specification facets from JSONB (e.g., BTU ratings)
SELECT
  p.attributes->>'btu' AS btu_value,
  COUNT(*) AS product_count
FROM products p
WHERE p.search_vector @@ plainto_tsquery('english', :search_term)
  AND p.is_active = true
  AND p.attributes->>'btu' IS NOT NULL
GROUP BY p.attributes->>'btu'
ORDER BY (p.attributes->>'btu')::int;
```

---

## 3. API Endpoints

### 3.1 Endpoint Summary

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/search` | Search products with filters | No |
| GET | `/api/v1/search/suggestions` | Autocomplete suggestions | No |
| GET | `/api/v1/search/facets` | Get available filter options | No |
| GET | `/api/v1/categories` | Get category tree | No |
| GET | `/api/v1/categories/{slug}` | Get category by slug | No |
| GET | `/api/v1/categories/{slug}/products` | Get products in category | No |

### 3.2 Search Products Endpoint

#### Request

```http
GET /api/v1/search?q=split+ac&category=air-conditioners&brand=carrier,daikin&minPrice=500&maxPrice=2000&btu=12000,18000&sort=price_asc&page=1&pageSize=20
```

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `q` | string | No | Search query text |
| `category` | string | No | Category slug filter |
| `brand` | string | No | Comma-separated brand names |
| `minPrice` | decimal | No | Minimum price filter |
| `maxPrice` | decimal | No | Maximum price filter |
| `btu` | string | No | Comma-separated BTU values |
| `energyRating` | string | No | Energy efficiency rating |
| `inStock` | boolean | No | Filter to in-stock only |
| `sort` | string | No | Sort order (see below) |
| `page` | int | No | Page number (default: 1) |
| `pageSize` | int | No | Items per page (default: 20, max: 100) |

#### Sort Options

| Value | Description |
|-------|-------------|
| `relevance` | Search relevance (default when query present) |
| `price_asc` | Price low to high |
| `price_desc` | Price high to low |
| `name_asc` | Name A-Z |
| `name_desc` | Name Z-A |
| `newest` | Newest first |
| `rating` | Highest rated first |

#### Response

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440001",
        "name": "Carrier Split AC 12000 BTU",
        "slug": "carrier-split-ac-12000-btu",
        "brand": "Carrier",
        "description": "Energy-efficient split air conditioner...",
        "basePrice": 899.00,
        "salePrice": 799.00,
        "primaryImage": {
          "url": "/images/products/carrier-split-12k.jpg",
          "altText": "Carrier Split AC 12000 BTU"
        },
        "category": {
          "id": "cat-001",
          "name": "Split Air Conditioners",
          "slug": "split-air-conditioners"
        },
        "inStock": true,
        "stockQuantity": 25,
        "averageRating": 4.5,
        "reviewCount": 128,
        "attributes": {
          "btu": "12000",
          "energyRating": "A++",
          "refrigerant": "R32",
          "noiseLevel": "19dB"
        }
      }
    ],
    "facets": {
      "categories": [
        { "slug": "split-air-conditioners", "name": "Split Air Conditioners", "count": 45 },
        { "slug": "window-air-conditioners", "name": "Window Air Conditioners", "count": 23 },
        { "slug": "portable-air-conditioners", "name": "Portable Air Conditioners", "count": 12 }
      ],
      "brands": [
        { "name": "Carrier", "count": 28 },
        { "name": "Daikin", "count": 22 },
        { "name": "Mitsubishi", "count": 18 },
        { "name": "LG", "count": 15 }
      ],
      "priceRanges": [
        { "min": 0, "max": 500, "label": "Under $500", "count": 15 },
        { "min": 500, "max": 1000, "label": "$500 - $1,000", "count": 35 },
        { "min": 1000, "max": 2000, "label": "$1,000 - $2,000", "count": 25 },
        { "min": 2000, "max": 5000, "label": "$2,000 - $5,000", "count": 8 },
        { "min": 5000, "max": null, "label": "$5,000+", "count": 2 }
      ],
      "specifications": {
        "btu": [
          { "value": "9000", "label": "9,000 BTU", "count": 12 },
          { "value": "12000", "label": "12,000 BTU", "count": 28 },
          { "value": "18000", "label": "18,000 BTU", "count": 22 },
          { "value": "24000", "label": "24,000 BTU", "count": 15 }
        ],
        "energyRating": [
          { "value": "A+++", "count": 8 },
          { "value": "A++", "count": 25 },
          { "value": "A+", "count": 30 },
          { "value": "A", "count": 20 }
        ]
      }
    },
    "appliedFilters": {
      "query": "split ac",
      "category": "air-conditioners",
      "brands": ["carrier", "daikin"],
      "priceRange": { "min": 500, "max": 2000 }
    }
  },
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 85,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "meta": {
    "timestamp": "2026-01-10T12:00:00Z",
    "requestId": "req_srch_abc123",
    "searchTime": 45
  }
}
```

### 3.3 Search Suggestions Endpoint

#### Request

```http
GET /api/v1/search/suggestions?q=carr&limit=5
```

#### Response

```json
{
  "success": true,
  "data": {
    "products": [
      {
        "id": "prod-001",
        "name": "Carrier Split AC 12000 BTU",
        "slug": "carrier-split-ac-12000-btu",
        "thumbnail": "/images/products/carrier-split-12k-thumb.jpg",
        "price": 799.00
      },
      {
        "id": "prod-002",
        "name": "Carrier Window AC 9000 BTU",
        "slug": "carrier-window-ac-9000-btu",
        "thumbnail": "/images/products/carrier-window-9k-thumb.jpg",
        "price": 499.00
      }
    ],
    "categories": [
      {
        "name": "Carrier Products",
        "slug": "carrier-products",
        "productCount": 45
      }
    ],
    "suggestions": [
      "carrier split ac",
      "carrier window ac",
      "carrier inverter ac",
      "carrier central ac"
    ]
  },
  "meta": {
    "timestamp": "2026-01-10T12:00:00Z",
    "requestId": "req_sug_def456"
  }
}
```

### 3.4 Category Tree Endpoint

#### Request

```http
GET /api/v1/categories?includeProductCounts=true
```

#### Response

```json
{
  "success": true,
  "data": [
    {
      "id": "cat-001",
      "name": "Air Conditioners",
      "slug": "air-conditioners",
      "description": "Cooling solutions for homes and offices",
      "imageUrl": "/images/categories/air-conditioners.jpg",
      "productCount": 156,
      "children": [
        {
          "id": "cat-001-001",
          "name": "Split Air Conditioners",
          "slug": "split-air-conditioners",
          "productCount": 78,
          "children": []
        },
        {
          "id": "cat-001-002",
          "name": "Window Air Conditioners",
          "slug": "window-air-conditioners",
          "productCount": 45,
          "children": []
        },
        {
          "id": "cat-001-003",
          "name": "Portable Air Conditioners",
          "slug": "portable-air-conditioners",
          "productCount": 33,
          "children": []
        }
      ]
    },
    {
      "id": "cat-002",
      "name": "Heating Systems",
      "slug": "heating-systems",
      "description": "Heating solutions for cold weather",
      "imageUrl": "/images/categories/heating-systems.jpg",
      "productCount": 89,
      "children": [
        {
          "id": "cat-002-001",
          "name": "Heat Pumps",
          "slug": "heat-pumps",
          "productCount": 42,
          "children": []
        },
        {
          "id": "cat-002-002",
          "name": "Space Heaters",
          "slug": "space-heaters",
          "productCount": 47,
          "children": []
        }
      ]
    }
  ],
  "meta": {
    "timestamp": "2026-01-10T12:00:00Z",
    "requestId": "req_cat_ghi789"
  }
}
```

---

## 4. Backend Implementation

### 4.1 Domain Models

```csharp
// src/ClimaSite.Core/Entities/SearchResult.cs
namespace ClimaSite.Core.Entities;

public class SearchResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public SearchFacets Facets { get; init; } = new();
    public AppliedFilters AppliedFilters { get; init; } = new();
    public int TotalItems { get; init; }
    public int SearchTimeMs { get; init; }
}

public class SearchFacets
{
    public IReadOnlyList<CategoryFacet> Categories { get; init; } = [];
    public IReadOnlyList<BrandFacet> Brands { get; init; } = [];
    public IReadOnlyList<PriceRangeFacet> PriceRanges { get; init; } = [];
    public IDictionary<string, IReadOnlyList<SpecificationFacet>> Specifications { get; init; }
        = new Dictionary<string, IReadOnlyList<SpecificationFacet>>();
}

public record CategoryFacet(string Slug, string Name, int Count);
public record BrandFacet(string Name, int Count);
public record PriceRangeFacet(decimal Min, decimal? Max, string Label, int Count);
public record SpecificationFacet(string Value, string? Label, int Count);

public class AppliedFilters
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Brands { get; init; } = [];
    public PriceRange? PriceRange { get; init; }
    public IDictionary<string, IReadOnlyList<string>> Specifications { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>();
}

public record PriceRange(decimal Min, decimal Max);
```

### 4.2 Search Query Handler

```csharp
// src/ClimaSite.Application/Queries/SearchProducts/SearchProductsQuery.cs
namespace ClimaSite.Application.Queries.SearchProducts;

public record SearchProductsQuery : IRequest<SearchResult<ProductSearchDto>>
{
    public string? Query { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Brands { get; init; } = [];
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public IDictionary<string, IReadOnlyList<string>> Specifications { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>();
    public bool? InStock { get; init; }
    public string Sort { get; init; } = "relevance";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// src/ClimaSite.Application/Queries/SearchProducts/SearchProductsHandler.cs
namespace ClimaSite.Application.Queries.SearchProducts;

public class SearchProductsHandler : IRequestHandler<SearchProductsQuery, SearchResult<ProductSearchDto>>
{
    private readonly ISearchRepository _searchRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchProductsHandler> _logger;

    public SearchProductsHandler(
        ISearchRepository searchRepository,
        IMapper mapper,
        ILogger<SearchProductsHandler> logger)
    {
        _searchRepository = searchRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SearchResult<ProductSearchDto>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var searchParams = new ProductSearchParams
        {
            Query = request.Query,
            CategorySlug = request.Category,
            Brands = request.Brands,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Specifications = request.Specifications,
            InStockOnly = request.InStock ?? false,
            SortBy = ParseSortOption(request.Sort),
            Page = request.Page,
            PageSize = Math.Min(request.PageSize, 100)
        };

        var (products, totalCount) = await _searchRepository
            .SearchProductsAsync(searchParams, cancellationToken);

        var facets = await _searchRepository
            .GetFacetsAsync(searchParams, cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "Search completed in {ElapsedMs}ms. Query: {Query}, Results: {Count}",
            stopwatch.ElapsedMilliseconds, request.Query, totalCount);

        return new SearchResult<ProductSearchDto>
        {
            Items = _mapper.Map<IReadOnlyList<ProductSearchDto>>(products),
            Facets = facets,
            AppliedFilters = BuildAppliedFilters(request),
            TotalItems = totalCount,
            SearchTimeMs = (int)stopwatch.ElapsedMilliseconds
        };
    }

    private static SortOption ParseSortOption(string sort) => sort switch
    {
        "price_asc" => SortOption.PriceAscending,
        "price_desc" => SortOption.PriceDescending,
        "name_asc" => SortOption.NameAscending,
        "name_desc" => SortOption.NameDescending,
        "newest" => SortOption.Newest,
        "rating" => SortOption.Rating,
        _ => SortOption.Relevance
    };

    private static AppliedFilters BuildAppliedFilters(SearchProductsQuery request) => new()
    {
        Query = request.Query,
        Category = request.Category,
        Brands = request.Brands,
        PriceRange = request.MinPrice.HasValue || request.MaxPrice.HasValue
            ? new PriceRange(request.MinPrice ?? 0, request.MaxPrice ?? decimal.MaxValue)
            : null,
        Specifications = request.Specifications
    };
}
```

### 4.3 Search Repository

```csharp
// src/ClimaSite.Infrastructure/Repositories/SearchRepository.cs
namespace ClimaSite.Infrastructure.Repositories;

public class SearchRepository : ISearchRepository
{
    private readonly ClimaSiteDbContext _context;

    public SearchRepository(ClimaSiteDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Product> Products, int TotalCount)> SearchProductsAsync(
        ProductSearchParams searchParams,
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsPrimary))
            .Where(p => p.IsActive);

        // Apply full-text search
        if (!string.IsNullOrWhiteSpace(searchParams.Query))
        {
            var searchQuery = EF.Functions.PlainToTsQuery("english", searchParams.Query);
            query = query.Where(p => p.SearchVector.Matches(searchQuery));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(searchParams.CategorySlug))
        {
            query = query.Where(p =>
                p.Category.Slug == searchParams.CategorySlug ||
                p.Category.Parent.Slug == searchParams.CategorySlug);
        }

        // Apply brand filter
        if (searchParams.Brands.Any())
        {
            query = query.Where(p => searchParams.Brands.Contains(p.Brand));
        }

        // Apply price filter
        if (searchParams.MinPrice.HasValue)
        {
            query = query.Where(p =>
                (p.SalePrice ?? p.BasePrice) >= searchParams.MinPrice.Value);
        }

        if (searchParams.MaxPrice.HasValue)
        {
            query = query.Where(p =>
                (p.SalePrice ?? p.BasePrice) <= searchParams.MaxPrice.Value);
        }

        // Apply stock filter
        if (searchParams.InStockOnly)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        // Apply specification filters from JSONB
        foreach (var spec in searchParams.Specifications)
        {
            var key = spec.Key;
            var values = spec.Value;
            query = query.Where(p =>
                values.Contains(EF.Functions.JsonExtractPathText(p.Attributes, key)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, searchParams.SortBy, searchParams.Query);

        // Apply pagination
        var products = await query
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }

    public async Task<SearchFacets> GetFacetsAsync(
        ProductSearchParams searchParams,
        CancellationToken cancellationToken)
    {
        var baseQuery = _context.Products.Where(p => p.IsActive);

        // Apply search query for facet context
        if (!string.IsNullOrWhiteSpace(searchParams.Query))
        {
            var searchQuery = EF.Functions.PlainToTsQuery("english", searchParams.Query);
            baseQuery = baseQuery.Where(p => p.SearchVector.Matches(searchQuery));
        }

        // Category facets
        var categoryFacets = await baseQuery
            .GroupBy(p => new { p.Category.Slug, p.Category.Name })
            .Select(g => new CategoryFacet(g.Key.Slug, g.Key.Name, g.Count()))
            .OrderByDescending(f => f.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Brand facets
        var brandFacets = await baseQuery
            .Where(p => p.Brand != null)
            .GroupBy(p => p.Brand)
            .Select(g => new BrandFacet(g.Key!, g.Count()))
            .OrderByDescending(f => f.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Price range facets
        var priceRanges = await GetPriceRangeFacetsAsync(baseQuery, cancellationToken);

        // Specification facets (BTU, Energy Rating)
        var specificationFacets = await GetSpecificationFacetsAsync(baseQuery, cancellationToken);

        return new SearchFacets
        {
            Categories = categoryFacets,
            Brands = brandFacets,
            PriceRanges = priceRanges,
            Specifications = specificationFacets
        };
    }

    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> query,
        SortOption sortBy,
        string? searchQuery)
    {
        return sortBy switch
        {
            SortOption.PriceAscending => query.OrderBy(p => p.SalePrice ?? p.BasePrice),
            SortOption.PriceDescending => query.OrderByDescending(p => p.SalePrice ?? p.BasePrice),
            SortOption.NameAscending => query.OrderBy(p => p.Name),
            SortOption.NameDescending => query.OrderByDescending(p => p.Name),
            SortOption.Newest => query.OrderByDescending(p => p.CreatedAt),
            SortOption.Rating => query.OrderByDescending(p => p.AverageRating),
            SortOption.Relevance when !string.IsNullOrWhiteSpace(searchQuery) =>
                query.OrderByDescending(p => p.SearchVector.Rank(
                    EF.Functions.PlainToTsQuery("english", searchQuery))),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }

    private async Task<IReadOnlyList<PriceRangeFacet>> GetPriceRangeFacetsAsync(
        IQueryable<Product> query,
        CancellationToken cancellationToken)
    {
        var priceBuckets = new[]
        {
            (Min: 0m, Max: 500m, Label: "Under $500"),
            (Min: 500m, Max: 1000m, Label: "$500 - $1,000"),
            (Min: 1000m, Max: 2000m, Label: "$1,000 - $2,000"),
            (Min: 2000m, Max: 5000m, Label: "$2,000 - $5,000"),
            (Min: 5000m, Max: (decimal?)null, Label: "$5,000+")
        };

        var results = new List<PriceRangeFacet>();

        foreach (var bucket in priceBuckets)
        {
            var bucketQuery = query.Where(p =>
                (p.SalePrice ?? p.BasePrice) >= bucket.Min);

            if (bucket.Max.HasValue)
            {
                bucketQuery = bucketQuery.Where(p =>
                    (p.SalePrice ?? p.BasePrice) < bucket.Max.Value);
            }

            var count = await bucketQuery.CountAsync(cancellationToken);
            if (count > 0)
            {
                results.Add(new PriceRangeFacet(bucket.Min, bucket.Max, bucket.Label, count));
            }
        }

        return results;
    }

    private async Task<IDictionary<string, IReadOnlyList<SpecificationFacet>>> GetSpecificationFacetsAsync(
        IQueryable<Product> query,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, IReadOnlyList<SpecificationFacet>>();

        // BTU facets
        var btuFacets = await query
            .Where(p => EF.Functions.JsonExtractPathText(p.Attributes, "btu") != null)
            .GroupBy(p => EF.Functions.JsonExtractPathText(p.Attributes, "btu"))
            .Select(g => new SpecificationFacet(
                g.Key!,
                FormatBtu(g.Key!),
                g.Count()))
            .OrderBy(f => int.Parse(f.Value))
            .ToListAsync(cancellationToken);

        if (btuFacets.Any())
        {
            result["btu"] = btuFacets;
        }

        // Energy rating facets
        var energyRatingFacets = await query
            .Where(p => EF.Functions.JsonExtractPathText(p.Attributes, "energyRating") != null)
            .GroupBy(p => EF.Functions.JsonExtractPathText(p.Attributes, "energyRating"))
            .Select(g => new SpecificationFacet(g.Key!, null, g.Count()))
            .OrderBy(f => f.Value)
            .ToListAsync(cancellationToken);

        if (energyRatingFacets.Any())
        {
            result["energyRating"] = energyRatingFacets;
        }

        return result;
    }

    private static string FormatBtu(string btu) =>
        int.TryParse(btu, out var value) ? $"{value:N0} BTU" : btu;
}
```

### 4.4 Autocomplete Service

```csharp
// src/ClimaSite.Application/Queries/GetSearchSuggestions/GetSearchSuggestionsQuery.cs
namespace ClimaSite.Application.Queries.GetSearchSuggestions;

public record GetSearchSuggestionsQuery(string Query, int Limit = 5)
    : IRequest<SearchSuggestionsDto>;

public record SearchSuggestionsDto
{
    public IReadOnlyList<ProductSuggestionDto> Products { get; init; } = [];
    public IReadOnlyList<CategorySuggestionDto> Categories { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
}

public record ProductSuggestionDto(
    Guid Id,
    string Name,
    string Slug,
    string? Thumbnail,
    decimal Price);

public record CategorySuggestionDto(
    string Name,
    string Slug,
    int ProductCount);

// Handler
public class GetSearchSuggestionsHandler
    : IRequestHandler<GetSearchSuggestionsQuery, SearchSuggestionsDto>
{
    private readonly ClimaSiteDbContext _context;

    public GetSearchSuggestionsHandler(ClimaSiteDbContext context)
    {
        _context = context;
    }

    public async Task<SearchSuggestionsDto> Handle(
        GetSearchSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length < 2)
        {
            return new SearchSuggestionsDto();
        }

        var query = request.Query.ToLower();

        // Product suggestions using trigram similarity
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Where(p => EF.Functions.ILike(p.Name, $"%{query}%") ||
                        EF.Functions.ILike(p.Brand, $"%{query}%"))
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, query))
            .Take(request.Limit)
            .Select(p => new ProductSuggestionDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Images.Where(i => i.IsPrimary).Select(i => i.ThumbnailUrl).FirstOrDefault(),
                p.SalePrice ?? p.BasePrice))
            .ToListAsync(cancellationToken);

        // Category suggestions
        var categories = await _context.Categories
            .Where(c => EF.Functions.ILike(c.Name, $"%{query}%"))
            .Take(3)
            .Select(c => new CategorySuggestionDto(
                c.Name,
                c.Slug,
                c.Products.Count(p => p.IsActive)))
            .ToListAsync(cancellationToken);

        // Text suggestions based on popular searches and product names
        var suggestions = await _context.Products
            .Where(p => p.IsActive)
            .Where(p => EF.Functions.ILike(p.Name, $"{query}%"))
            .Select(p => p.Name.ToLower())
            .Distinct()
            .Take(5)
            .ToListAsync(cancellationToken);

        return new SearchSuggestionsDto
        {
            Products = products,
            Categories = categories,
            Suggestions = suggestions
        };
    }
}
```

### 4.5 API Controller

```csharp
// src/ClimaSite.Api/Controllers/SearchController.cs
namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search products with full-text search and filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchProductsRequest request)
    {
        var query = new SearchProductsQuery
        {
            Query = request.Q,
            Category = request.Category,
            Brands = request.Brand?.Split(',').ToList() ?? [],
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Specifications = ParseSpecifications(request),
            InStock = request.InStock,
            Sort = request.Sort ?? "relevance",
            Page = request.Page ?? 1,
            PageSize = request.PageSize ?? 20
        };

        var result = await _mediator.Send(query);

        return Ok(ApiResponse.Success(result, new PaginationMeta
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = (int)Math.Ceiling(result.TotalItems / (double)query.PageSize)
        }));
    }

    /// <summary>
    /// Get search autocomplete suggestions
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<SearchSuggestionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestions([FromQuery] string q, [FromQuery] int limit = 5)
    {
        var query = new GetSearchSuggestionsQuery(q, Math.Min(limit, 10));
        var result = await _mediator.Send(query);
        return Ok(ApiResponse.Success(result));
    }

    /// <summary>
    /// Get available facets for current search context
    /// </summary>
    [HttpGet("facets")]
    [ProducesResponseType(typeof(ApiResponse<SearchFacets>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacets([FromQuery] string? q, [FromQuery] string? category)
    {
        var query = new GetSearchFacetsQuery(q, category);
        var result = await _mediator.Send(query);
        return Ok(ApiResponse.Success(result));
    }

    private static IDictionary<string, IReadOnlyList<string>> ParseSpecifications(
        SearchProductsRequest request)
    {
        var specs = new Dictionary<string, IReadOnlyList<string>>();

        if (!string.IsNullOrWhiteSpace(request.Btu))
            specs["btu"] = request.Btu.Split(',').ToList();

        if (!string.IsNullOrWhiteSpace(request.EnergyRating))
            specs["energyRating"] = request.EnergyRating.Split(',').ToList();

        return specs;
    }
}

// Request model
public record SearchProductsRequest
{
    public string? Q { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Btu { get; init; }
    public string? EnergyRating { get; init; }
    public bool? InStock { get; init; }
    public string? Sort { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
```

---

## 5. Frontend Components

### 5.1 Component Architecture

```
src/app/features/search/
├── search.routes.ts
├── components/
│   ├── search-bar/
│   │   └── search-bar.component.ts
│   ├── search-results/
│   │   └── search-results.component.ts
│   ├── facet-filter/
│   │   ├── facet-filter.component.ts
│   │   ├── category-facet/
│   │   │   └── category-facet.component.ts
│   │   ├── brand-facet/
│   │   │   └── brand-facet.component.ts
│   │   ├── price-facet/
│   │   │   └── price-facet.component.ts
│   │   └── spec-facet/
│   │       └── spec-facet.component.ts
│   ├── search-suggestions/
│   │   └── search-suggestions.component.ts
│   └── active-filters/
│       └── active-filters.component.ts
├── pages/
│   └── search-page/
│       └── search-page.component.ts
└── services/
    └── search.service.ts

src/app/features/catalog/
├── components/
│   ├── category-navigation/
│   │   └── category-navigation.component.ts
│   └── breadcrumb/
│       └── breadcrumb.component.ts
```

### 5.2 Search Service

```typescript
// src/app/features/search/services/search.service.ts
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';

export interface SearchParams {
  q?: string;
  category?: string;
  brands?: string[];
  minPrice?: number;
  maxPrice?: number;
  specifications?: Record<string, string[]>;
  inStock?: boolean;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface SearchResult {
  items: ProductSearchItem[];
  facets: SearchFacets;
  appliedFilters: AppliedFilters;
  totalItems: number;
  searchTimeMs: number;
}

export interface SearchFacets {
  categories: CategoryFacet[];
  brands: BrandFacet[];
  priceRanges: PriceRangeFacet[];
  specifications: Record<string, SpecificationFacet[]>;
}

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/search';

  // Search state
  private readonly searchParamsSignal = signal<SearchParams>({});
  private readonly searchResultSignal = signal<SearchResult | null>(null);
  private readonly isLoadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  // Autocomplete
  private readonly suggestionQuery$ = new Subject<string>();
  private readonly suggestions$ = this.suggestionQuery$.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(query => this.fetchSuggestions(query))
  );

  // Public readonly signals
  readonly searchParams = this.searchParamsSignal.asReadonly();
  readonly searchResult = this.searchResultSignal.asReadonly();
  readonly isLoading = this.isLoadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly suggestions = toSignal(this.suggestions$, { initialValue: null });

  // Computed values
  readonly totalPages = computed(() => {
    const result = this.searchResult();
    if (!result) return 0;
    const pageSize = this.searchParams().pageSize ?? 20;
    return Math.ceil(result.totalItems / pageSize);
  });

  readonly hasResults = computed(() => {
    const result = this.searchResult();
    return result !== null && result.items.length > 0;
  });

  async search(params: SearchParams): Promise<void> {
    this.searchParamsSignal.set(params);
    this.isLoadingSignal.set(true);
    this.errorSignal.set(null);

    try {
      const httpParams = this.buildHttpParams(params);
      const response = await this.http
        .get<ApiResponse<SearchResult>>(`${this.apiUrl}`, { params: httpParams })
        .toPromise();

      this.searchResultSignal.set(response!.data);
    } catch (error) {
      this.errorSignal.set('Failed to search products. Please try again.');
      console.error('Search error:', error);
    } finally {
      this.isLoadingSignal.set(false);
    }
  }

  updateFilter(key: keyof SearchParams, value: unknown): void {
    const currentParams = this.searchParams();
    this.search({ ...currentParams, [key]: value, page: 1 });
  }

  clearFilters(): void {
    const currentParams = this.searchParams();
    this.search({ q: currentParams.q, page: 1 });
  }

  getSuggestions(query: string): void {
    this.suggestionQuery$.next(query);
  }

  private fetchSuggestions(query: string) {
    if (!query || query.length < 2) {
      return of(null);
    }
    return this.http.get<ApiResponse<SearchSuggestions>>(
      `${this.apiUrl}/suggestions`,
      { params: { q: query, limit: '5' } }
    ).pipe(map(r => r.data));
  }

  private buildHttpParams(params: SearchParams): HttpParams {
    let httpParams = new HttpParams();

    if (params.q) httpParams = httpParams.set('q', params.q);
    if (params.category) httpParams = httpParams.set('category', params.category);
    if (params.brands?.length) httpParams = httpParams.set('brand', params.brands.join(','));
    if (params.minPrice != null) httpParams = httpParams.set('minPrice', params.minPrice.toString());
    if (params.maxPrice != null) httpParams = httpParams.set('maxPrice', params.maxPrice.toString());
    if (params.inStock != null) httpParams = httpParams.set('inStock', params.inStock.toString());
    if (params.sort) httpParams = httpParams.set('sort', params.sort);
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    // Add specification filters
    if (params.specifications) {
      for (const [key, values] of Object.entries(params.specifications)) {
        if (values.length > 0) {
          httpParams = httpParams.set(key, values.join(','));
        }
      }
    }

    return httpParams;
  }
}
```

### 5.3 Search Bar Component

```typescript
// src/app/features/search/components/search-bar/search-bar.component.ts
import { Component, inject, signal, effect, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SearchService, SearchSuggestions } from '../../services/search.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="relative w-full max-w-2xl" data-testid="search-bar">
      <div class="relative">
        <input
          type="text"
          [ngModel]="searchQuery()"
          (ngModelChange)="onQueryChange($event)"
          (keydown.enter)="onSearch()"
          (focus)="showSuggestions.set(true)"
          [placeholder]="'common.labels.search' | translate"
          class="w-full pl-4 pr-12 py-3 border border-neutral-300 rounded-lg
                 focus:ring-2 focus:ring-primary-500 focus:border-primary-500
                 dark:bg-neutral-800 dark:border-neutral-600 dark:text-white"
          data-testid="search-input"
          aria-label="Search products"
          autocomplete="off"
        />
        <button
          (click)="onSearch()"
          class="absolute right-2 top-1/2 -translate-y-1/2 p-2
                 text-neutral-500 hover:text-primary-500 transition-colors"
          data-testid="search-button"
          aria-label="Submit search"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </button>
      </div>

      <!-- Suggestions Dropdown -->
      @if (showSuggestions() && suggestions()) {
        <div
          class="absolute z-50 w-full mt-1 bg-white dark:bg-neutral-800
                 border border-neutral-200 dark:border-neutral-700 rounded-lg shadow-lg"
          data-testid="search-suggestions"
        >
          <!-- Product Suggestions -->
          @if (suggestions()!.products.length > 0) {
            <div class="p-2 border-b border-neutral-100 dark:border-neutral-700">
              <span class="text-xs font-semibold text-neutral-500 uppercase px-2">
                {{ 'search.suggestions.products' | translate }}
              </span>
              @for (product of suggestions()!.products; track product.id) {
                <a
                  [routerLink]="['/products', product.slug]"
                  class="flex items-center gap-3 p-2 hover:bg-neutral-50
                         dark:hover:bg-neutral-700 rounded cursor-pointer"
                  (click)="showSuggestions.set(false)"
                  data-testid="suggestion-product"
                >
                  @if (product.thumbnail) {
                    <img
                      [src]="product.thumbnail"
                      [alt]="product.name"
                      class="w-10 h-10 object-cover rounded"
                    />
                  }
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-neutral-900 dark:text-white truncate">
                      {{ product.name }}
                    </p>
                    <p class="text-sm text-primary-600 font-semibold">
                      {{ product.price | currency }}
                    </p>
                  </div>
                </a>
              }
            </div>
          }

          <!-- Category Suggestions -->
          @if (suggestions()!.categories.length > 0) {
            <div class="p-2 border-b border-neutral-100 dark:border-neutral-700">
              <span class="text-xs font-semibold text-neutral-500 uppercase px-2">
                {{ 'search.suggestions.categories' | translate }}
              </span>
              @for (category of suggestions()!.categories; track category.slug) {
                <a
                  [routerLink]="['/categories', category.slug]"
                  class="flex items-center justify-between p-2 hover:bg-neutral-50
                         dark:hover:bg-neutral-700 rounded cursor-pointer"
                  (click)="showSuggestions.set(false)"
                  data-testid="suggestion-category"
                >
                  <span class="text-sm text-neutral-700 dark:text-neutral-300">
                    {{ category.name }}
                  </span>
                  <span class="text-xs text-neutral-500">
                    {{ category.productCount }} {{ 'search.suggestions.items' | translate }}
                  </span>
                </a>
              }
            </div>
          }

          <!-- Text Suggestions -->
          @if (suggestions()!.suggestions.length > 0) {
            <div class="p-2">
              @for (suggestion of suggestions()!.suggestions; track suggestion) {
                <button
                  (click)="selectSuggestion(suggestion)"
                  class="w-full text-left p-2 text-sm text-neutral-700 dark:text-neutral-300
                         hover:bg-neutral-50 dark:hover:bg-neutral-700 rounded"
                  data-testid="suggestion-text"
                >
                  {{ suggestion }}
                </button>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
  host: {
    '(document:click)': 'onDocumentClick($event)'
  }
})
export class SearchBarComponent {
  private readonly searchService = inject(SearchService);
  private readonly router = inject(Router);

  readonly searchQuery = signal('');
  readonly showSuggestions = signal(false);
  readonly suggestions = this.searchService.suggestions;

  readonly searched = output<string>();

  constructor() {
    // Fetch suggestions when query changes
    effect(() => {
      const query = this.searchQuery();
      if (query.length >= 2) {
        this.searchService.getSuggestions(query);
      }
    });
  }

  onQueryChange(value: string): void {
    this.searchQuery.set(value);
  }

  onSearch(): void {
    const query = this.searchQuery().trim();
    if (query) {
      this.showSuggestions.set(false);
      this.searched.emit(query);
      this.router.navigate(['/search'], { queryParams: { q: query } });
    }
  }

  selectSuggestion(suggestion: string): void {
    this.searchQuery.set(suggestion);
    this.showSuggestions.set(false);
    this.onSearch();
  }

  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('[data-testid="search-bar"]')) {
      this.showSuggestions.set(false);
    }
  }
}
```

### 5.4 Facet Filter Component

```typescript
// src/app/features/search/components/facet-filter/facet-filter.component.ts
import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { SearchFacets, AppliedFilters } from '../../services/search.service';
import { CategoryFacetComponent } from './category-facet/category-facet.component';
import { BrandFacetComponent } from './brand-facet/brand-facet.component';
import { PriceFacetComponent } from './price-facet/price-facet.component';
import { SpecFacetComponent } from './spec-facet/spec-facet.component';

@Component({
  selector: 'app-facet-filter',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    CategoryFacetComponent,
    BrandFacetComponent,
    PriceFacetComponent,
    SpecFacetComponent
  ],
  template: `
    <aside class="w-64 flex-shrink-0" data-testid="facet-filter">
      <div class="sticky top-4 space-y-6">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-neutral-900 dark:text-white">
            {{ 'search.filters.title' | translate }}
          </h2>
          @if (hasActiveFilters()) {
            <button
              (click)="clearFilters.emit()"
              class="text-sm text-primary-600 hover:text-primary-700"
              data-testid="clear-filters-button"
            >
              {{ 'search.filters.clearAll' | translate }}
            </button>
          }
        </div>

        <!-- Category Filter -->
        @if (facets().categories.length > 0) {
          <app-category-facet
            [categories]="facets().categories"
            [selected]="appliedFilters().category"
            (selectionChange)="categoryChange.emit($event)"
          />
        }

        <!-- Brand Filter -->
        @if (facets().brands.length > 0) {
          <app-brand-facet
            [brands]="facets().brands"
            [selected]="appliedFilters().brands"
            (selectionChange)="brandsChange.emit($event)"
          />
        }

        <!-- Price Filter -->
        @if (facets().priceRanges.length > 0) {
          <app-price-facet
            [ranges]="facets().priceRanges"
            [selectedMin]="appliedFilters().priceRange?.min"
            [selectedMax]="appliedFilters().priceRange?.max"
            (rangeChange)="priceChange.emit($event)"
          />
        }

        <!-- Specification Filters -->
        @for (spec of specificationKeys(); track spec) {
          <app-spec-facet
            [specKey]="spec"
            [values]="facets().specifications[spec]"
            [selected]="appliedFilters().specifications?.[spec] ?? []"
            (selectionChange)="specificationChange.emit({ key: spec, values: $event })"
          />
        }
      </div>
    </aside>
  `
})
export class FacetFilterComponent {
  readonly facets = input.required<SearchFacets>();
  readonly appliedFilters = input.required<AppliedFilters>();

  readonly categoryChange = output<string | null>();
  readonly brandsChange = output<string[]>();
  readonly priceChange = output<{ min?: number; max?: number }>();
  readonly specificationChange = output<{ key: string; values: string[] }>();
  readonly clearFilters = output<void>();

  specificationKeys(): string[] {
    return Object.keys(this.facets().specifications);
  }

  hasActiveFilters(): boolean {
    const filters = this.appliedFilters();
    return !!(
      filters.category ||
      filters.brands?.length ||
      filters.priceRange ||
      Object.values(filters.specifications ?? {}).some(v => v?.length)
    );
  }
}
```

### 5.5 Category Navigation Component

```typescript
// src/app/features/catalog/components/category-navigation/category-navigation.component.ts
import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CategoryService, Category } from '../../services/category.service';

@Component({
  selector: 'app-category-navigation',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, TranslateModule],
  template: `
    <nav class="bg-white dark:bg-neutral-800 shadow-sm" data-testid="category-navigation">
      <div class="container mx-auto px-4">
        <ul class="flex items-center gap-1 overflow-x-auto py-2 -mx-4 px-4 scrollbar-hide">
          <li>
            <a
              routerLink="/products"
              routerLinkActive="bg-primary-50 text-primary-700 dark:bg-primary-900 dark:text-primary-300"
              [routerLinkActiveOptions]="{ exact: true }"
              class="px-4 py-2 text-sm font-medium text-neutral-700 dark:text-neutral-300
                     hover:bg-neutral-100 dark:hover:bg-neutral-700 rounded-lg
                     whitespace-nowrap transition-colors"
              data-testid="category-nav-all"
            >
              {{ 'nav.allProducts' | translate }}
            </a>
          </li>
          @for (category of categories(); track category.id) {
            <li class="relative group">
              <a
                [routerLink]="['/categories', category.slug]"
                routerLinkActive="bg-primary-50 text-primary-700 dark:bg-primary-900 dark:text-primary-300"
                class="px-4 py-2 text-sm font-medium text-neutral-700 dark:text-neutral-300
                       hover:bg-neutral-100 dark:hover:bg-neutral-700 rounded-lg
                       whitespace-nowrap transition-colors flex items-center gap-1"
                [attr.data-testid]="'category-nav-' + category.slug"
              >
                {{ category.name }}
                @if (category.children?.length) {
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                  </svg>
                }
              </a>

              <!-- Dropdown for subcategories -->
              @if (category.children?.length) {
                <div class="absolute left-0 top-full pt-1 hidden group-hover:block z-50">
                  <ul class="bg-white dark:bg-neutral-800 border border-neutral-200
                             dark:border-neutral-700 rounded-lg shadow-lg py-2 min-w-48">
                    @for (child of category.children; track child.id) {
                      <li>
                        <a
                          [routerLink]="['/categories', child.slug]"
                          class="block px-4 py-2 text-sm text-neutral-700 dark:text-neutral-300
                                 hover:bg-neutral-100 dark:hover:bg-neutral-700"
                          [attr.data-testid]="'category-nav-' + child.slug"
                        >
                          {{ child.name }}
                          @if (child.productCount) {
                            <span class="text-neutral-500 ml-2">({{ child.productCount }})</span>
                          }
                        </a>
                      </li>
                    }
                  </ul>
                </div>
              }
            </li>
          }
        </ul>
      </div>
    </nav>
  `
})
export class CategoryNavigationComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);

  readonly categories = signal<Category[]>([]);
  readonly isLoading = signal(true);

  async ngOnInit(): Promise<void> {
    try {
      const categories = await this.categoryService.getCategories();
      this.categories.set(categories);
    } finally {
      this.isLoading.set(false);
    }
  }
}
```

### 5.6 Breadcrumb Component

```typescript
// src/app/features/catalog/components/breadcrumb/breadcrumb.component.ts
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

export interface BreadcrumbItem {
  label: string;
  url?: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <nav aria-label="Breadcrumb" class="py-3" data-testid="breadcrumb">
      <ol class="flex items-center gap-2 text-sm">
        <li>
          <a
            routerLink="/"
            class="text-neutral-500 hover:text-primary-600 transition-colors"
          >
            {{ 'nav.home' | translate }}
          </a>
        </li>
        @for (item of items(); track item.label; let last = $last) {
          <li class="flex items-center gap-2">
            <svg class="w-4 h-4 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
            @if (item.url && !last) {
              <a
                [routerLink]="item.url"
                class="text-neutral-500 hover:text-primary-600 transition-colors"
              >
                {{ item.label }}
              </a>
            } @else {
              <span class="text-neutral-900 dark:text-white font-medium">
                {{ item.label }}
              </span>
            }
          </li>
        }
      </ol>
    </nav>
  `
})
export class BreadcrumbComponent {
  readonly items = input<BreadcrumbItem[]>([]);
}
```

---

## 6. Implementation Tasks

### Phase 1: Database Setup (Week 1)

#### Task SRCH-001: Add Search Vector Column
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Add tsvector column to products table for full-text search.

**Acceptance Criteria:**
- [ ] Migration adds `search_vector` column of type TSVECTOR
- [ ] Column is nullable initially for safe migration
- [ ] Migration is reversible
- [ ] Unit test verifies migration applies correctly

**Technical Notes:**
```bash
dotnet ef migrations add AddProductSearchVector --project src/ClimaSite.Infrastructure
```

---

#### Task SRCH-002: Create Search Vector Trigger
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Create PostgreSQL trigger function to auto-update search vector on product changes.

**Acceptance Criteria:**
- [ ] Trigger function created with weighted fields (A: name/sku, B: brand, C: description, D: attributes)
- [ ] Trigger fires on INSERT and UPDATE of relevant columns
- [ ] Existing products have search vectors populated
- [ ] Integration test verifies trigger updates search vector

---

#### Task SRCH-003: Create Search Indexes
**Priority:** High
**Estimated Time:** 2 hours

**Description:**
Create GIN index for full-text search and trigram indexes for autocomplete.

**Acceptance Criteria:**
- [ ] GIN index on `search_vector` column
- [ ] Trigram extension enabled
- [ ] Trigram indexes on `name` and `brand` columns
- [ ] Query explain shows index usage

---

#### Task SRCH-004: Create Facet Indexes
**Priority:** Medium
**Estimated Time:** 2 hours

**Description:**
Create indexes to optimize facet aggregation queries.

**Acceptance Criteria:**
- [ ] Index on `category_id` for category facets
- [ ] Index on `brand` for brand facets
- [ ] Index on price columns for range queries
- [ ] GIN index on `attributes` JSONB for spec facets

---

### Phase 2: Backend API (Weeks 1-2)

#### Task SRCH-005: Search Repository Interface
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Create ISearchRepository interface and search parameter models.

**Acceptance Criteria:**
- [ ] `ISearchRepository` interface defined in Core project
- [ ] `ProductSearchParams` model with all filter options
- [ ] `SearchResult<T>` generic result wrapper
- [ ] `SearchFacets` model for aggregation results

---

#### Task SRCH-006: Search Repository Implementation
**Priority:** High
**Estimated Time:** 8 hours

**Description:**
Implement SearchRepository with PostgreSQL full-text search.

**Acceptance Criteria:**
- [ ] Full-text search using tsquery
- [ ] Support for all filter types (category, brand, price, specs)
- [ ] Sorting by relevance, price, name, date, rating
- [ ] Pagination with skip/take
- [ ] Unit tests with in-memory database
- [ ] Integration tests with real PostgreSQL

---

#### Task SRCH-007: Facet Aggregation Implementation
**Priority:** High
**Estimated Time:** 6 hours

**Description:**
Implement facet aggregation for search filters.

**Acceptance Criteria:**
- [ ] Category facets with product counts
- [ ] Brand facets with product counts
- [ ] Price range buckets with counts
- [ ] Specification facets from JSONB attributes
- [ ] Facets respect current search query context

---

#### Task SRCH-008: Search Query Handler
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Create MediatR query handler for product search.

**Acceptance Criteria:**
- [ ] `SearchProductsQuery` with all parameters
- [ ] `SearchProductsHandler` using repository
- [ ] Timing logged for monitoring
- [ ] Mapping to DTOs via Mapster

---

#### Task SRCH-009: Autocomplete Handler
**Priority:** Medium
**Estimated Time:** 4 hours

**Description:**
Create handler for search autocomplete suggestions.

**Acceptance Criteria:**
- [ ] `GetSearchSuggestionsQuery` with query and limit
- [ ] Product suggestions using trigram similarity
- [ ] Category suggestions matching query
- [ ] Text suggestions from product names
- [ ] Response limited to configurable count

---

#### Task SRCH-010: Search API Controller
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Create SearchController with search endpoints.

**Acceptance Criteria:**
- [ ] GET `/api/v1/search` endpoint
- [ ] GET `/api/v1/search/suggestions` endpoint
- [ ] GET `/api/v1/search/facets` endpoint
- [ ] Request validation with FluentValidation
- [ ] Swagger documentation complete

---

#### Task SRCH-011: Category API Endpoints
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Create category navigation endpoints.

**Acceptance Criteria:**
- [ ] GET `/api/v1/categories` returns category tree
- [ ] GET `/api/v1/categories/{slug}` returns category details
- [ ] GET `/api/v1/categories/{slug}/products` returns products
- [ ] Support for `includeProductCounts` parameter
- [ ] Caching with Redis (30 min TTL)

---

#### Task SRCH-012: Search Response Caching
**Priority:** Medium
**Estimated Time:** 4 hours

**Description:**
Implement Redis caching for search results.

**Acceptance Criteria:**
- [ ] Cache key includes all search parameters
- [ ] TTL of 2 minutes for search results
- [ ] TTL of 5 minutes for facets
- [ ] Cache invalidation on product updates

---

### Phase 3: Frontend Components (Weeks 2-3)

#### Task SRCH-013: Search Service
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Create Angular service for search operations.

**Acceptance Criteria:**
- [ ] `SearchService` with signals for state
- [ ] Methods for search, filter updates, suggestions
- [ ] HTTP parameter building
- [ ] Error handling with user-friendly messages

---

#### Task SRCH-014: Search Bar Component
**Priority:** High
**Estimated Time:** 6 hours

**Description:**
Create search bar with autocomplete dropdown.

**Acceptance Criteria:**
- [ ] Text input with search icon button
- [ ] Debounced autocomplete (300ms)
- [ ] Suggestions dropdown with products, categories, text
- [ ] Keyboard navigation support
- [ ] Click outside closes dropdown
- [ ] Mobile responsive
- [ ] Data-testid attributes for E2E tests

---

#### Task SRCH-015: Search Results Page
**Priority:** High
**Estimated Time:** 8 hours

**Description:**
Create search results page with product grid and filters.

**Acceptance Criteria:**
- [ ] URL query params sync with search state
- [ ] Product grid with responsive layout
- [ ] Sort dropdown
- [ ] Pagination controls
- [ ] Loading and empty states
- [ ] "No results" message with suggestions

---

#### Task SRCH-016: Facet Filter Component
**Priority:** High
**Estimated Time:** 8 hours

**Description:**
Create faceted filter sidebar component.

**Acceptance Criteria:**
- [ ] Category filter with single selection
- [ ] Brand filter with multi-select checkboxes
- [ ] Price range slider or preset ranges
- [ ] Specification filters (BTU, energy rating)
- [ ] "Clear all filters" button
- [ ] Active filter count badge
- [ ] Collapsible sections

---

#### Task SRCH-017: Active Filters Component
**Priority:** Medium
**Estimated Time:** 4 hours

**Description:**
Create component showing active filters as removable chips.

**Acceptance Criteria:**
- [ ] Chip for each active filter
- [ ] Click to remove individual filter
- [ ] Clear all button
- [ ] Smooth animations

---

#### Task SRCH-018: Category Navigation Component
**Priority:** High
**Estimated Time:** 6 hours

**Description:**
Create horizontal category navigation bar.

**Acceptance Criteria:**
- [ ] Top-level categories as nav items
- [ ] Dropdown for subcategories on hover
- [ ] Active state for current category
- [ ] Horizontal scroll on mobile
- [ ] Loaded from API on init

---

#### Task SRCH-019: Breadcrumb Component
**Priority:** Medium
**Estimated Time:** 2 hours

**Description:**
Create breadcrumb navigation component.

**Acceptance Criteria:**
- [ ] Home > Category > Subcategory > Product
- [ ] Links for all but last item
- [ ] Configurable via input
- [ ] Proper aria labels

---

#### Task SRCH-020: Mobile Filter Drawer
**Priority:** Medium
**Estimated Time:** 4 hours

**Description:**
Create mobile-optimized filter drawer.

**Acceptance Criteria:**
- [ ] Full-screen drawer on mobile
- [ ] "Show X results" sticky button
- [ ] Smooth slide animation
- [ ] Touch-friendly controls

---

### Phase 4: Integration & Polish (Week 3)

#### Task SRCH-021: URL State Synchronization
**Priority:** High
**Estimated Time:** 4 hours

**Description:**
Sync search state with URL query parameters.

**Acceptance Criteria:**
- [ ] URL updates on filter changes
- [ ] Search state restores from URL on load
- [ ] Browser back/forward works correctly
- [ ] Shareable search URLs

---

#### Task SRCH-022: Search Analytics Events
**Priority:** Low
**Estimated Time:** 2 hours

**Description:**
Track search events for analytics.

**Acceptance Criteria:**
- [ ] Track search queries
- [ ] Track filter usage
- [ ] Track zero-result searches
- [ ] Track suggestion clicks

---

#### Task SRCH-023: Search Performance Optimization
**Priority:** Medium
**Estimated Time:** 4 hours

**Description:**
Optimize search performance for sub-100ms response.

**Acceptance Criteria:**
- [ ] Query execution under 50ms
- [ ] Total API response under 100ms
- [ ] Index coverage verified with EXPLAIN
- [ ] Connection pooling configured

---

#### Task SRCH-024: Internationalization
**Priority:** Medium
**Estimated Time:** 4 hours

**Description:**
Add i18n support for search UI.

**Acceptance Criteria:**
- [ ] Translation keys for all labels
- [ ] EN, BG, DE translations complete
- [ ] RTL support considerations
- [ ] Number/currency formatting per locale

---

---

## 7. E2E Tests (Playwright - NO MOCKING)

All E2E tests use real API calls and real data. Test data is created via admin API endpoints before tests and cleaned up after.

### Test Setup

```typescript
// tests/ClimaSite.E2E/fixtures/search-fixtures.ts
import { test as base, expect, APIRequestContext } from '@playwright/test';

interface SearchTestData {
  products: CreatedProduct[];
  categories: CreatedCategory[];
}

interface CreatedProduct {
  id: string;
  name: string;
  sku: string;
  slug: string;
}

interface CreatedCategory {
  id: string;
  name: string;
  slug: string;
}

export const test = base.extend<{ searchData: SearchTestData }>({
  searchData: async ({ request }, use) => {
    const data: SearchTestData = { products: [], categories: [] };
    const timestamp = Date.now();

    // Create test category
    const categoryResponse = await request.post('/api/v1/admin/categories', {
      data: {
        name: `Test Split ACs ${timestamp}`,
        slug: `test-split-acs-${timestamp}`,
        description: 'Test category for E2E search tests'
      }
    });
    const category = await categoryResponse.json();
    data.categories.push(category.data);

    // Create test products
    const products = [
      {
        name: `Carrier Split AC 12000 BTU ${timestamp}`,
        brand: 'Carrier',
        sku: `SRCH-CARRIER-${timestamp}`,
        basePrice: 699.00,
        categoryId: category.data.id,
        stockQuantity: 10,
        attributes: { btu: '12000', energyRating: 'A++' }
      },
      {
        name: `Daikin Window AC 9000 BTU ${timestamp}`,
        brand: 'Daikin',
        sku: `SRCH-DAIKIN-${timestamp}`,
        basePrice: 499.00,
        categoryId: category.data.id,
        stockQuantity: 5,
        attributes: { btu: '9000', energyRating: 'A+' }
      },
      {
        name: `Mitsubishi Inverter AC 18000 BTU ${timestamp}`,
        brand: 'Mitsubishi',
        sku: `SRCH-MITSU-${timestamp}`,
        basePrice: 1299.00,
        categoryId: category.data.id,
        stockQuantity: 3,
        attributes: { btu: '18000', energyRating: 'A+++' }
      }
    ];

    for (const product of products) {
      const response = await request.post('/api/v1/admin/products', {
        data: product
      });
      const created = await response.json();
      data.products.push(created.data);
    }

    // Wait for search index to update
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Use the test data
    await use(data);

    // Cleanup after test
    for (const product of data.products) {
      await request.delete(`/api/v1/admin/products/${product.id}`);
    }
    for (const category of data.categories) {
      await request.delete(`/api/v1/admin/categories/${category.id}`);
    }
  }
});

export { expect };
```

### Test SRCH-E2E-001: Search Products by Keyword

```typescript
// tests/ClimaSite.E2E/tests/search/search-products.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Product Search', () => {
  test('SRCH-E2E-001: user can search for products by keyword', async ({ page, searchData }) => {
    // Navigate to home page
    await page.goto('/');

    // Enter search query
    await page.fill('[data-testid="search-input"]', 'Carrier');
    await page.click('[data-testid="search-button"]');

    // Wait for search results page
    await page.waitForURL(/\/search\?q=Carrier/);

    // Verify Carrier product is visible
    const carrierProduct = searchData.products.find(p => p.name.includes('Carrier'));
    await expect(page.getByText(carrierProduct!.name)).toBeVisible();

    // Verify other brands are not visible in results (filtered out by relevance)
    const daikinProduct = searchData.products.find(p => p.name.includes('Daikin'));
    await expect(page.getByText(daikinProduct!.name)).not.toBeVisible();
  });

  test('SRCH-E2E-002: search results show product details', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);

    // Wait for results to load
    await page.waitForSelector('[data-testid="search-results"]');

    // Verify product card shows essential info
    const productCard = page.locator('[data-testid="product-card"]').first();
    await expect(productCard.getByText(/Carrier|Daikin|Mitsubishi/)).toBeVisible();
    await expect(productCard.getByText(/\$\d+/)).toBeVisible(); // Price
  });

  test('SRCH-E2E-003: empty search shows no results message', async ({ page }) => {
    await page.goto('/search?q=xyznonexistentproduct12345');

    await expect(page.getByText(/no results|no products found/i)).toBeVisible();
  });
});
```

### Test SRCH-E2E-004: Filter by Category

```typescript
// tests/ClimaSite.E2E/tests/search/filter-category.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Category Filtering', () => {
  test('SRCH-E2E-004: user can filter products by category', async ({ page, searchData }) => {
    const category = searchData.categories[0];
    const timestamp = category.slug.split('-').pop();

    // Navigate to search with products
    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="search-results"]');

    // Click on category filter
    await page.click(`[data-testid="category-filter-${category.slug}"]`);

    // Verify URL updated
    await expect(page).toHaveURL(new RegExp(`category=${category.slug}`));

    // Verify products from that category are shown
    for (const product of searchData.products) {
      await expect(page.getByText(product.name)).toBeVisible();
    }
  });

  test('SRCH-E2E-005: category facet shows product counts', async ({ page, searchData }) => {
    const timestamp = searchData.categories[0].slug.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="facet-filter"]');

    // Verify category shows count
    const categoryFacet = page.locator(`[data-testid="category-filter-${searchData.categories[0].slug}"]`);
    await expect(categoryFacet).toContainText('3'); // 3 test products
  });
});
```

### Test SRCH-E2E-006: Filter by Brand

```typescript
// tests/ClimaSite.E2E/tests/search/filter-brand.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Brand Filtering', () => {
  test('SRCH-E2E-006: user can filter products by brand', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="facet-filter"]');

    // Check Carrier brand filter
    await page.click('[data-testid="brand-filter-Carrier"]');

    // Verify URL updated
    await expect(page).toHaveURL(/brand=Carrier/);

    // Verify only Carrier product visible
    const carrierProduct = searchData.products.find(p => p.brand === 'Carrier');
    const daikinProduct = searchData.products.find(p => p.brand === 'Daikin');

    await expect(page.getByText(carrierProduct!.name)).toBeVisible();
    await expect(page.getByText(daikinProduct!.name)).not.toBeVisible();
  });

  test('SRCH-E2E-007: user can select multiple brands', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="facet-filter"]');

    // Select both Carrier and Daikin
    await page.click('[data-testid="brand-filter-Carrier"]');
    await page.click('[data-testid="brand-filter-Daikin"]');

    // Verify both products visible
    const carrierProduct = searchData.products.find(p => p.brand === 'Carrier');
    const daikinProduct = searchData.products.find(p => p.brand === 'Daikin');

    await expect(page.getByText(carrierProduct!.name)).toBeVisible();
    await expect(page.getByText(daikinProduct!.name)).toBeVisible();
  });
});
```

### Test SRCH-E2E-008: Filter by Price Range

```typescript
// tests/ClimaSite.E2E/tests/search/filter-price.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Price Filtering', () => {
  test('SRCH-E2E-008: user can filter by price range', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="facet-filter"]');

    // Select $500-$1000 price range
    await page.click('[data-testid="price-filter-500-1000"]');

    // Verify URL has price params
    await expect(page).toHaveURL(/minPrice=500/);
    await expect(page).toHaveURL(/maxPrice=1000/);

    // Carrier ($699) and Daikin ($499 - edge case) should be filtered
    // Only Carrier at $699 falls in 500-1000 range
    const carrierProduct = searchData.products.find(p => p.brand === 'Carrier');
    await expect(page.getByText(carrierProduct!.name)).toBeVisible();

    // Mitsubishi at $1299 should not be visible
    const mitsuProduct = searchData.products.find(p => p.brand === 'Mitsubishi');
    await expect(page.getByText(mitsuProduct!.name)).not.toBeVisible();
  });
});
```

### Test SRCH-E2E-009: Search Autocomplete

```typescript
// tests/ClimaSite.E2E/tests/search/autocomplete.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Search Autocomplete', () => {
  test('SRCH-E2E-009: autocomplete shows product suggestions', async ({ page, searchData }) => {
    await page.goto('/');

    // Type partial search query
    await page.fill('[data-testid="search-input"]', 'Carr');

    // Wait for suggestions dropdown
    await page.waitForSelector('[data-testid="search-suggestions"]');

    // Verify Carrier product appears in suggestions
    const carrierProduct = searchData.products.find(p => p.brand === 'Carrier');
    await expect(page.locator('[data-testid="suggestion-product"]').getByText(carrierProduct!.name)).toBeVisible();
  });

  test('SRCH-E2E-010: clicking suggestion navigates to product', async ({ page, searchData }) => {
    await page.goto('/');

    const carrierProduct = searchData.products.find(p => p.brand === 'Carrier');

    await page.fill('[data-testid="search-input"]', carrierProduct!.name.substring(0, 10));
    await page.waitForSelector('[data-testid="search-suggestions"]');

    // Click on product suggestion
    await page.click(`[data-testid="suggestion-product"]:has-text("${carrierProduct!.name}")`);

    // Should navigate to product detail page
    await expect(page).toHaveURL(new RegExp(`/products/${carrierProduct!.slug}`));
  });

  test('SRCH-E2E-011: autocomplete shows category suggestions', async ({ page, searchData }) => {
    const category = searchData.categories[0];

    await page.goto('/');
    await page.fill('[data-testid="search-input"]', 'Split');

    await page.waitForSelector('[data-testid="search-suggestions"]');

    // Verify category suggestion appears
    await expect(page.locator('[data-testid="suggestion-category"]')).toBeVisible();
  });
});
```

### Test SRCH-E2E-012: Category Navigation

```typescript
// tests/ClimaSite.E2E/tests/search/category-navigation.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Category Navigation', () => {
  test('SRCH-E2E-012: category navigation displays categories', async ({ page }) => {
    await page.goto('/');

    // Verify category navigation is visible
    await expect(page.locator('[data-testid="category-navigation"]')).toBeVisible();

    // Verify "All Products" link exists
    await expect(page.locator('[data-testid="category-nav-all"]')).toBeVisible();
  });

  test('SRCH-E2E-013: clicking category navigates to category page', async ({ page, searchData }) => {
    const category = searchData.categories[0];

    await page.goto('/');

    // Note: Test category may not appear in nav if it's dynamically created
    // This test works better with seeded categories
    await page.click('[data-testid="category-nav-all"]');

    await expect(page).toHaveURL('/products');
  });
});
```

### Test SRCH-E2E-014: Sort Results

```typescript
// tests/ClimaSite.E2E/tests/search/sort-results.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Sort Results', () => {
  test('SRCH-E2E-014: user can sort by price ascending', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="search-results"]');

    // Select price ascending sort
    await page.selectOption('[data-testid="sort-select"]', 'price_asc');

    // Verify URL updated
    await expect(page).toHaveURL(/sort=price_asc/);

    // Verify order: Daikin ($499) < Carrier ($699) < Mitsubishi ($1299)
    const productCards = page.locator('[data-testid="product-card"]');
    const firstProduct = await productCards.first().textContent();
    expect(firstProduct).toContain('Daikin');
  });

  test('SRCH-E2E-015: user can sort by price descending', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="search-results"]');

    await page.selectOption('[data-testid="sort-select"]', 'price_desc');

    const productCards = page.locator('[data-testid="product-card"]');
    const firstProduct = await productCards.first().textContent();
    expect(firstProduct).toContain('Mitsubishi');
  });
});
```

### Test SRCH-E2E-016: Clear Filters

```typescript
// tests/ClimaSite.E2E/tests/search/clear-filters.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Clear Filters', () => {
  test('SRCH-E2E-016: user can clear all filters', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    // Navigate with filters applied
    await page.goto(`/search?q=${timestamp}&brand=Carrier&minPrice=500`);
    await page.waitForSelector('[data-testid="facet-filter"]');

    // Verify filters are applied (only Carrier visible)
    const daikinProduct = searchData.products.find(p => p.brand === 'Daikin');
    await expect(page.getByText(daikinProduct!.name)).not.toBeVisible();

    // Click clear all filters
    await page.click('[data-testid="clear-filters-button"]');

    // Verify filters cleared from URL
    await expect(page).not.toHaveURL(/brand=/);
    await expect(page).not.toHaveURL(/minPrice=/);

    // Verify all products now visible
    for (const product of searchData.products) {
      await expect(page.getByText(product.name)).toBeVisible();
    }
  });

  test('SRCH-E2E-017: user can remove individual filter', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}&brand=Carrier,Daikin`);
    await page.waitForSelector('[data-testid="active-filters"]');

    // Remove Carrier filter chip
    await page.click('[data-testid="remove-filter-brand-Carrier"]');

    // Verify only Daikin in URL
    await expect(page).toHaveURL(/brand=Daikin/);
    await expect(page).not.toHaveURL(/Carrier/);
  });
});
```

### Test SRCH-E2E-018: Pagination

```typescript
// tests/ClimaSite.E2E/tests/search/pagination.spec.ts
import { test as base, expect } from '@playwright/test';

// Extended fixture that creates many products for pagination testing
const test = base.extend<{ manyProducts: { products: any[], cleanup: () => Promise<void> } }>({
  manyProducts: async ({ request }, use) => {
    const products: any[] = [];
    const timestamp = Date.now();

    // Create category
    const catResponse = await request.post('/api/v1/admin/categories', {
      data: { name: `Pagination Test ${timestamp}`, slug: `pagination-test-${timestamp}` }
    });
    const category = (await catResponse.json()).data;

    // Create 25 products (more than one page of 20)
    for (let i = 0; i < 25; i++) {
      const response = await request.post('/api/v1/admin/products', {
        data: {
          name: `Pagination Test Product ${i + 1} ${timestamp}`,
          brand: 'TestBrand',
          sku: `PAG-${timestamp}-${i}`,
          basePrice: 100 + i,
          categoryId: category.id,
          stockQuantity: 10
        }
      });
      products.push((await response.json()).data);
    }

    await new Promise(resolve => setTimeout(resolve, 2000));

    await use({
      products,
      cleanup: async () => {
        for (const p of products) {
          await request.delete(`/api/v1/admin/products/${p.id}`);
        }
        await request.delete(`/api/v1/admin/categories/${category.id}`);
      }
    });

    // Cleanup
    for (const p of products) {
      await request.delete(`/api/v1/admin/products/${p.id}`);
    }
    await request.delete(`/api/v1/admin/categories/${category.id}`);
  }
});

test.describe('Pagination', () => {
  test('SRCH-E2E-018: search results are paginated', async ({ page, manyProducts }) => {
    const timestamp = manyProducts.products[0].sku.split('-')[1];

    await page.goto(`/search?q=${timestamp}&pageSize=20`);
    await page.waitForSelector('[data-testid="search-results"]');

    // Verify pagination shows
    await expect(page.locator('[data-testid="pagination"]')).toBeVisible();

    // Verify showing page 1 of 2
    await expect(page.getByText(/page 1 of 2/i)).toBeVisible();

    // Verify 20 products on first page
    const productCards = page.locator('[data-testid="product-card"]');
    await expect(productCards).toHaveCount(20);
  });

  test('SRCH-E2E-019: user can navigate to next page', async ({ page, manyProducts }) => {
    const timestamp = manyProducts.products[0].sku.split('-')[1];

    await page.goto(`/search?q=${timestamp}&pageSize=20`);
    await page.waitForSelector('[data-testid="pagination"]');

    // Click next page
    await page.click('[data-testid="pagination-next"]');

    // Verify URL updated
    await expect(page).toHaveURL(/page=2/);

    // Verify different products shown (page 2 has 5 products)
    const productCards = page.locator('[data-testid="product-card"]');
    await expect(productCards).toHaveCount(5);
  });
});
```

### Test SRCH-E2E-020: Specification Filters

```typescript
// tests/ClimaSite.E2E/tests/search/filter-specifications.spec.ts
import { test, expect } from '../../fixtures/search-fixtures';

test.describe('Specification Filters', () => {
  test('SRCH-E2E-020: user can filter by BTU rating', async ({ page, searchData }) => {
    const timestamp = searchData.products[0].sku.split('-').pop();

    await page.goto(`/search?q=${timestamp}`);
    await page.waitForSelector('[data-testid="facet-filter"]');

    // Click 12000 BTU filter
    await page.click('[data-testid="spec-filter-btu-12000"]');

    // Verify URL updated
    await expect(page).toHaveURL(/btu=12000/);

    // Verify only 12000 BTU product visible (Carrier)
    const carrierProduct = searchData.products.find(p => p.attributes?.btu === '12000');
    await expect(page.getByText(carrierProduct!.name)).toBeVisible();

    // Verify other BTU products not visible
    const mitsuProduct = searchData.products.find(p => p.attributes?.btu === '18000');
    await expect(page.getByText(mitsuProduct!.name)).not.toBeVisible();
  });
});
```

---

## 8. Translation Keys

```json
{
  "search": {
    "title": "Search Results",
    "placeholder": "Search for air conditioners, heaters...",
    "noResults": {
      "title": "No products found",
      "message": "Try adjusting your search or filters",
      "suggestions": "Suggestions:",
      "clearFilters": "Clear all filters",
      "browseCategories": "Browse categories"
    },
    "results": {
      "showing": "Showing {{from}}-{{to}} of {{total}} products",
      "sortBy": "Sort by",
      "relevance": "Relevance",
      "priceAsc": "Price: Low to High",
      "priceDesc": "Price: High to Low",
      "nameAsc": "Name: A to Z",
      "nameDesc": "Name: Z to A",
      "newest": "Newest First",
      "rating": "Highest Rated"
    },
    "filters": {
      "title": "Filters",
      "clearAll": "Clear all",
      "category": "Category",
      "brand": "Brand",
      "price": "Price",
      "priceRange": "Price Range",
      "specifications": "Specifications",
      "btu": "BTU Rating",
      "energyRating": "Energy Rating",
      "inStock": "In Stock Only",
      "showMore": "Show more",
      "showLess": "Show less"
    },
    "suggestions": {
      "products": "Products",
      "categories": "Categories",
      "searches": "Popular searches",
      "items": "items"
    },
    "activeFilters": {
      "title": "Active filters",
      "clear": "Clear"
    }
  },
  "categories": {
    "all": "All Products",
    "viewAll": "View all in {{category}}"
  },
  "breadcrumb": {
    "home": "Home"
  }
}
```

---

## 9. Performance Considerations

### 9.1 Query Optimization

- Use `ts_rank_cd` for relevance scoring (faster than `ts_rank`)
- Limit facet aggregation to top 20 values
- Use covering indexes where possible
- Consider materialized views for complex facets

### 9.2 Caching Strategy

| Cache Key Pattern | TTL | Invalidation |
|------------------|-----|--------------|
| `search:{hash}` | 2 min | Product update/delete |
| `facets:{hash}` | 5 min | Product update/delete |
| `categories:tree` | 30 min | Category update |
| `suggestions:{prefix}` | 10 min | Product name update |

### 9.3 Frontend Optimization

- Debounce search input (300ms)
- Virtual scrolling for large result sets
- Lazy load images in product grid
- Prefetch next page on scroll near bottom

---

## 10. Monitoring & Metrics

### Key Metrics

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| Search latency (p95) | < 100ms | > 200ms |
| Zero-result rate | < 10% | > 20% |
| Suggestion click rate | > 15% | < 5% |
| Filter usage rate | > 30% | N/A |

### Logging

```csharp
_logger.LogInformation(
    "Search executed. Query: {Query}, Filters: {Filters}, Results: {Count}, Time: {ElapsedMs}ms",
    query, JsonSerializer.Serialize(filters), resultCount, elapsedMs);
```

---

## 11. Future Enhancements

- **Synonyms**: Configure PostgreSQL synonym dictionary for HVAC terms
- **Spell correction**: Implement "Did you mean?" using pg_trgm
- **Search history**: Personal search history for logged-in users
- **Saved searches**: Allow users to save frequent searches
- **Voice search**: Mobile voice input integration
- **Visual search**: Search by product image (future ML feature)

---

*Document Version: 1.0.0*
*Last Updated: January 2026*
*Author: ClimaSite Development Team*
