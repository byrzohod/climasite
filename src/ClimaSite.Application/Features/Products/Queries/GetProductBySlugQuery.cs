using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

public record GetProductBySlugQuery : IRequest<ProductDto>, ICacheableQuery
{
    public string Slug { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"product_slug_{Slug}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(
        GetProductBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Translations)
            .Where(p => p.Slug == request.Slug && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Slug);
        }

        // Get translated content based on the requested language
        var (name, shortDescription, description, metaTitle, metaDescription) =
            product.GetTranslatedContent(request.LanguageCode);

        return new ProductDto
        {
            Id = product.Id,
            Name = name,
            Slug = product.Slug,
            Description = description,
            ShortDescription = shortDescription,
            BasePrice = product.BasePrice,
            SalePrice = product.CompareAtPrice,
            IsOnSale = product.CompareAtPrice.HasValue && product.CompareAtPrice > product.BasePrice,
            DiscountPercentage = product.CompareAtPrice.HasValue && product.CompareAtPrice > product.BasePrice
                ? Math.Round((product.CompareAtPrice.Value - product.BasePrice) / product.CompareAtPrice.Value * 100, 0)
                : 0,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Brand = product.Brand,
            Model = product.Model,
            AverageRating = 0, // Will be calculated from reviews
            ReviewCount = 0, // Will be calculated from reviews
            Specifications = product.Specifications,
            Features = product.Features?.ToDictionary(f => f.Title, f => (object)f.Description),
            CreatedAt = product.CreatedAt,
            Category = product.Category != null
                ? new CategoryBriefDto
                {
                    Id = product.Category.Id,
                    Name = product.Category.Name,
                    Slug = product.Category.Slug
                }
                : null,
            Images = product.Images.Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = i.Url,
                AltText = i.AltText,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Name = v.Name,
                Price = product.BasePrice + v.PriceAdjustment,
                SalePrice = null,
                StockQuantity = v.StockQuantity,
                ReservedQuantity = 0,
                AvailableQuantity = v.StockQuantity,
                IsActive = v.IsActive,
                Attributes = v.Attributes?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty)
            }).ToList()
        };
    }
}
