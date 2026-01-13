using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Brands.DTOs;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Brands.Queries;

public record GetBrandBySlugQuery : IRequest<BrandDto?>, ICacheableQuery
{
    public string Slug { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }
    public int ProductPageNumber { get; init; } = 1;
    public int ProductPageSize { get; init; } = 12;

    public string CacheKey => $"brand_{Slug}_{LanguageCode ?? "en"}_{ProductPageNumber}_{ProductPageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public class GetBrandBySlugQueryHandler : IRequestHandler<GetBrandBySlugQuery, BrandDto?>
{
    private readonly IApplicationDbContext _context;

    public GetBrandBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BrandDto?> Handle(
        GetBrandBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var brand = await _context.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Slug == request.Slug.ToLowerInvariant() && b.IsActive, cancellationToken);

        if (brand == null)
            return null;

        // Get products for this brand
        var productsQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Translations)
            .Where(p => p.IsActive && p.Brand == brand.Name);

        var totalProductCount = await productsQuery.CountAsync(cancellationToken);

        var products = await productsQuery
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((request.ProductPageNumber - 1) * request.ProductPageSize)
            .Take(request.ProductPageSize)
            .ToListAsync(cancellationToken);

        var translated = brand.GetTranslatedContent(request.LanguageCode);

        return new BrandDto
        {
            Id = brand.Id,
            Name = translated.Name,
            Slug = brand.Slug,
            Description = translated.Description,
            LogoUrl = brand.LogoUrl,
            BannerImageUrl = brand.BannerImageUrl,
            WebsiteUrl = brand.WebsiteUrl,
            CountryOfOrigin = brand.CountryOfOrigin,
            FoundedYear = brand.FoundedYear,
            IsFeatured = brand.IsFeatured,
            MetaTitle = translated.MetaTitle,
            MetaDescription = translated.MetaDescription,
            ProductCount = totalProductCount,
            Products = products.Select(p =>
            {
                var productTranslated = p.GetTranslatedContent(request.LanguageCode);
                return new ProductBriefDto
                {
                    Id = p.Id,
                    Name = productTranslated.Name,
                    Slug = p.Slug,
                    ShortDescription = productTranslated.ShortDescription,
                    BasePrice = p.BasePrice,
                    SalePrice = p.IsOnSale ? p.BasePrice : null,
                    IsOnSale = p.IsOnSale,
                    DiscountPercentage = p.DiscountPercentage ?? 0,
                    Brand = p.Brand,
                    PrimaryImageUrl = p.PrimaryImage?.Url,
                    InStock = p.InStock
                };
            }).ToList()
        };
    }
}
