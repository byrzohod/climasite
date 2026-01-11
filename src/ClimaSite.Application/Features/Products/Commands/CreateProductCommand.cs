using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Commands;

public record CreateProductCommand : IRequest<Result<Guid>>
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? CostPrice { get; init; }
    public int StockQuantity { get; init; } = 50;
    public Dictionary<string, object>? Specifications { get; init; }
    public List<ProductFeatureRequest>? Features { get; init; }
    public List<string>? Tags { get; init; }
    public bool RequiresInstallation { get; init; }
    public int WarrantyMonths { get; init; } = 12;
    public decimal? WeightKg { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsFeatured { get; init; }
}

public record ProductFeatureRequest(string Title, string Description, string? Icon = null);

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be non-negative");

        RuleFor(x => x.CompareAtPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Compare at price must be non-negative")
            .When(x => x.CompareAtPrice.HasValue);

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Cost price must be non-negative")
            .When(x => x.CostPrice.HasValue);

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500).WithMessage("Short description cannot exceed 500 characters")
            .When(x => x.ShortDescription != null);

        RuleFor(x => x.Brand)
            .MaximumLength(100).WithMessage("Brand cannot exceed 100 characters")
            .When(x => x.Brand != null);

        RuleFor(x => x.Model)
            .MaximumLength(100).WithMessage("Model cannot exceed 100 characters")
            .When(x => x.Model != null);

        RuleFor(x => x.WarrantyMonths)
            .GreaterThanOrEqualTo(0).WithMessage("Warranty months must be non-negative");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("Weight must be non-negative")
            .When(x => x.WeightKg.HasValue);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta title cannot exceed 200 characters")
            .When(x => x.MetaTitle != null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description cannot exceed 500 characters")
            .When(x => x.MetaDescription != null);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku.ToUpperInvariant(), cancellationToken))
        {
            return Result<Guid>.Failure($"A product with SKU '{request.Sku}' already exists");
        }

        var slug = GenerateSlug(request.Name);
        var slugSuffix = 1;
        var originalSlug = slug;

        while (await _context.Products.AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            slug = $"{originalSlug}-{slugSuffix++}";
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                return Result<Guid>.Failure($"Category with ID '{request.CategoryId}' not found");
            }
        }

        var product = new Product(request.Sku, request.Name, slug, request.BasePrice);

        product.SetShortDescription(request.ShortDescription);
        product.SetDescription(request.Description);
        product.SetCategory(request.CategoryId);
        product.SetBrand(request.Brand);
        product.SetModel(request.Model);
        product.SetCompareAtPrice(request.CompareAtPrice);
        product.SetCostPrice(request.CostPrice);
        product.SetSpecifications(request.Specifications);
        product.SetTags(request.Tags);
        product.SetRequiresInstallation(request.RequiresInstallation);
        product.SetWarrantyMonths(request.WarrantyMonths);
        product.SetWeightKg(request.WeightKg);
        product.SetMetaTitle(request.MetaTitle);
        product.SetMetaDescription(request.MetaDescription);
        product.SetActive(request.IsActive);
        product.SetFeatured(request.IsFeatured);

        if (request.Features != null)
        {
            foreach (var feature in request.Features)
            {
                product.AddFeature(feature.Title, feature.Description, feature.Icon);
            }
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // Create a default variant with stock
        var defaultVariant = new ProductVariant(product.Id, $"{request.Sku}-DEFAULT", "Default");
        defaultVariant.SetStockQuantity(request.StockQuantity > 0 ? request.StockQuantity : 50);
        _context.ProductVariants.Add(defaultVariant);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        var chars = slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
        slug = new string(chars);

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }
}
