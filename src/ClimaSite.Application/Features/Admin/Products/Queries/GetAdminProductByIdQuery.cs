using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Admin.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Products.Queries;

public record GetAdminProductByIdQuery : IRequest<AdminProductDetailDto?>
{
    public Guid Id { get; init; }
}

public class GetAdminProductByIdQueryHandler : IRequestHandler<GetAdminProductByIdQuery, AdminProductDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAdminProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminProductDetailDto?> Handle(GetAdminProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            return null;
        }

        return new AdminProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Slug = product.Slug,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            BasePrice = product.BasePrice,
            CompareAtPrice = product.CompareAtPrice,
            CostPrice = product.CostPrice,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            Brand = product.Brand,
            Model = product.Model,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            RequiresInstallation = product.RequiresInstallation,
            WarrantyMonths = product.WarrantyMonths,
            WeightKg = product.WeightKg,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            Specifications = product.Specifications,
            Features = product.Features.Select(f => new ProductFeatureDto
            {
                Title = f.Title,
                Description = f.Description,
                Icon = f.Icon
            }).ToList(),
            Tags = product.Tags.ToList(),
            Images = product.Images.OrderBy(i => i.SortOrder).Select(i => new AdminProductImageDto
            {
                Id = i.Id,
                Url = i.Url,
                AltText = i.AltText,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).ToList(),
            Variants = product.Variants.OrderBy(v => v.SortOrder).Select(v => new AdminProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Name = v.Name,
                PriceAdjustment = v.PriceAdjustment,
                Attributes = v.Attributes,
                StockQuantity = v.StockQuantity,
                LowStockThreshold = v.LowStockThreshold,
                IsActive = v.IsActive,
                SortOrder = v.SortOrder
            }).ToList(),
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
