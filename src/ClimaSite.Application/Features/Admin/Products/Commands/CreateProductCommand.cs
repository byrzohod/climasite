using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Application.Features.Admin.Products.DTOs;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Admin.Products.Commands;

public record CreateProductCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? CostPrice { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsFeatured { get; init; }
    public bool RequiresInstallation { get; init; }
    public int WarrantyMonths { get; init; } = 12;
    public decimal? WeightKg { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Dictionary<string, object>? Specifications { get; init; }
    public List<ProductFeatureDto>? Features { get; init; }
    public List<string>? Tags { get; init; }
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters")
            .MustAsync(BeUniqueSku).WithMessage("SKU already exists");

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

    private async Task<bool> BeUniqueSku(string sku, CancellationToken cancellationToken)
    {
        return !await _context.Products.AnyAsync(p => p.Sku == sku.ToUpperInvariant(), cancellationToken);
    }

    private async Task<bool> CategoryExists(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return true;
        return await _context.Categories.AnyAsync(c => c.Id == categoryId.Value, cancellationToken);
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
        var slug = GenerateSlug(request.Name);
        var product = new Product(request.Sku, request.Name, slug, request.BasePrice);

        product.SetShortDescription(request.ShortDescription);
        product.SetDescription(request.Description);
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
            foreach (var feature in request.Features)
            {
                product.AddFeature(feature.Title, feature.Description, feature.Icon);
            }
        }

        if (request.Tags != null)
        {
            product.SetTags(request.Tags);
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // Create a default variant with stock
        var defaultVariant = new ProductVariant(product.Id, $"{request.Sku}-DEFAULT", "Default");
        defaultVariant.SetStockQuantity(50);
        _context.ProductVariants.Add(defaultVariant);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("'", "")
            .Replace("\"", "");
    }
}
