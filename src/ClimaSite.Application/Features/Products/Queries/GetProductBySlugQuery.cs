using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Pricing;
using ClimaSite.Application.Features.Products.DTOs;
using ClimaSite.Application.Features.Products.Specifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Queries;

// INV-01 A3: deliberately NOT ICacheableQuery. The PDP DTO now embeds volatile reservation state
// (ReservedQuantity / AvailableQuantity = stock − reserved), so response-caching it would serve stale
// availability (and stale Product JSON-LD) for up to the cache TTL after a hold is taken/released —
// defeating honest availability. The PDP is not a hot-enough path to need the cache.
public record GetProductBySlugQuery : IRequest<ProductDto>
{
    public string Slug { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }
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
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            BasePrice = product.BasePrice,
            SalePrice = ProductPricing.GetSalePrice(product.BasePrice, product.CompareAtPrice),
            IsOnSale = ProductPricing.IsOnSale(product.BasePrice, product.CompareAtPrice),
            DiscountPercentage = ProductPricing.GetDiscountPercentage(product.BasePrice, product.CompareAtPrice),
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Brand = product.Brand,
            Model = product.Model,
            AverageRating = 0, // Will be calculated from reviews
            ReviewCount = 0, // Will be calculated from reviews
            // Hide machine-only canonical HVAC keys (scoring inputs, not marketing specs) from the public PDP.
            Specifications = product.Specifications
                .Where(kvp => !HvacSpecResolver.IsMachineOnlyKey(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
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
            // INV-01 A3: deterministic order (SortOrder, Id) so the PDP's default-variant availability matches
            // add-to-cart's default-variant selection (both take the first active variant in this same order).
            Variants = product.Variants.OrderBy(v => v.SortOrder).ThenBy(v => v.Id).Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Name = v.Name,
                Price = product.BasePrice + v.PriceAdjustment,
                SalePrice = null,
                StockQuantity = v.StockQuantity,
                // INV-01 A3: honest availability on the PDP — subtract units held by Active checkout
                // reservations so shoppers don't see/add stock another checkout already holds. Computed
                // in-memory on the materialized variant (this Select runs after FirstOrDefaultAsync).
                ReservedQuantity = v.ReservedQuantity,
                AvailableQuantity = Math.Max(v.StockQuantity - v.ReservedQuantity, 0),
                IsActive = v.IsActive,
                Attributes = v.Attributes?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty)
            }).ToList()
        };
    }
}
