using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Admin.Products.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Products.Commands;

public record UpdateProductCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? CostPrice { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool RequiresInstallation { get; init; }
    public int WarrantyMonths { get; init; }
    public decimal? WeightKg { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Dictionary<string, object>? Specifications { get; init; }
    public List<ProductFeatureDto>? Features { get; init; }
    public List<string>? Tags { get; init; }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters")
            .MustAsync(BeUniqueSkuForProduct).WithMessage("SKU already exists");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(255).WithMessage("Slug cannot exceed 255 characters");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price cannot be negative");

        RuleFor(x => x.CompareAtPrice)
            .GreaterThanOrEqualTo(0).When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare at price cannot be negative");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).When(x => x.CostPrice.HasValue)
            .WithMessage("Cost price cannot be negative");

        RuleFor(x => x.CategoryId)
            .MustAsync(CategoryExists).When(x => x.CategoryId.HasValue)
            .WithMessage("Category not found");

        RuleFor(x => x.WarrantyMonths)
            .GreaterThanOrEqualTo(0).WithMessage("Warranty months cannot be negative");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0).When(x => x.WeightKg.HasValue)
            .WithMessage("Weight cannot be negative");
    }

    private async Task<bool> BeUniqueSkuForProduct(UpdateProductCommand command, string sku, CancellationToken cancellationToken)
    {
        return !await _context.Products.AnyAsync(
            p => p.Sku == sku.ToUpperInvariant() && p.Id != command.Id,
            cancellationToken);
    }

    private async Task<bool> CategoryExists(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return true;
        return await _context.Categories.AnyAsync(c => c.Id == categoryId.Value, cancellationToken);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            return Result.Failure("Product not found");
        }

        // Check if price changed and record history
        var priceChanged = product.BasePrice != request.BasePrice || product.CompareAtPrice != request.CompareAtPrice;
        if (priceChanged)
        {
            var priceHistory = Core.Entities.ProductPriceHistory.Create(
                product.Id,
                request.BasePrice,
                request.CompareAtPrice,
                Core.Entities.PriceChangeReason.PriceChange,
                $"Price changed from {product.BasePrice:C} to {request.BasePrice:C}");
            _context.ProductPriceHistory.Add(priceHistory);
        }

        product.SetName(request.Name);
        product.SetSku(request.Sku);
        product.SetSlug(request.Slug);
        product.SetShortDescription(request.ShortDescription);
        product.SetDescription(request.Description);
        product.SetBasePrice(request.BasePrice);
        product.SetCompareAtPrice(request.CompareAtPrice);
        product.SetCostPrice(request.CostPrice);
        product.SetCategory(request.CategoryId);
        product.SetBrand(request.Brand);
        product.SetModel(request.Model);
        product.SetActive(request.IsActive);
        product.SetFeatured(request.IsFeatured);
        product.SetRequiresInstallation(request.RequiresInstallation);
        product.SetWarrantyMonths(request.WarrantyMonths);
        product.SetWeightKg(request.WeightKg);
        product.SetMetaTitle(request.MetaTitle);
        product.SetMetaDescription(request.MetaDescription);

        if (request.Specifications != null)
        {
            product.SetSpecifications(request.Specifications);
        }

        if (request.Features != null)
        {
            product.SetFeatures(request.Features.Select(f =>
                new Core.Entities.ProductFeature(f.Title, f.Description, f.Icon)).ToList());
        }

        if (request.Tags != null)
        {
            product.SetTags(request.Tags);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
