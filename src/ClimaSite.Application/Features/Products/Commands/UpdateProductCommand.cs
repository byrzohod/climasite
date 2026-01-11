using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Products.Commands;

public record UpdateProductCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public decimal? BasePrice { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public decimal? CostPrice { get; init; }
    public Dictionary<string, object>? Specifications { get; init; }
    public List<ProductFeatureRequest>? Features { get; init; }
    public List<string>? Tags { get; init; }
    public bool? RequiresInstallation { get; init; }
    public int? WarrantyMonths { get; init; }
    public decimal? WeightKg { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsFeatured { get; init; }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Name)
            .MaximumLength(255).WithMessage("Product name cannot exceed 255 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be non-negative")
            .When(x => x.BasePrice.HasValue);

        RuleFor(x => x.CompareAtPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Compare at price must be non-negative")
            .When(x => x.CompareAtPrice.HasValue);

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
            .GreaterThanOrEqualTo(0).WithMessage("Warranty months must be non-negative")
            .When(x => x.WarrantyMonths.HasValue);

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("Weight must be non-negative")
            .When(x => x.WeightKg.HasValue);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync(new object[] { request.Id }, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        if (request.CategoryId.HasValue && request.CategoryId != product.CategoryId)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId.Value, cancellationToken);
            if (!categoryExists)
            {
                return Result<bool>.Failure($"Category with ID '{request.CategoryId}' not found");
            }
        }

        if (request.Name != null)
        {
            product.SetName(request.Name);
        }

        if (request.ShortDescription != null)
        {
            product.SetShortDescription(request.ShortDescription);
        }

        if (request.Description != null)
        {
            product.SetDescription(request.Description);
        }

        if (request.CategoryId.HasValue)
        {
            product.SetCategory(request.CategoryId);
        }

        if (request.Brand != null)
        {
            product.SetBrand(request.Brand);
        }

        if (request.Model != null)
        {
            product.SetModel(request.Model);
        }

        if (request.BasePrice.HasValue)
        {
            product.SetBasePrice(request.BasePrice.Value);
        }

        if (request.CompareAtPrice.HasValue)
        {
            product.SetCompareAtPrice(request.CompareAtPrice);
        }

        if (request.CostPrice.HasValue)
        {
            product.SetCostPrice(request.CostPrice);
        }

        if (request.Specifications != null)
        {
            product.SetSpecifications(request.Specifications);
        }

        if (request.Features != null)
        {
            var features = request.Features.Select(f => new Core.Entities.ProductFeature(f.Title, f.Description, f.Icon)).ToList();
            product.SetFeatures(features);
        }

        if (request.Tags != null)
        {
            product.SetTags(request.Tags);
        }

        if (request.RequiresInstallation.HasValue)
        {
            product.SetRequiresInstallation(request.RequiresInstallation.Value);
        }

        if (request.WarrantyMonths.HasValue)
        {
            product.SetWarrantyMonths(request.WarrantyMonths.Value);
        }

        if (request.WeightKg.HasValue)
        {
            product.SetWeightKg(request.WeightKg);
        }

        if (request.MetaTitle != null)
        {
            product.SetMetaTitle(request.MetaTitle);
        }

        if (request.MetaDescription != null)
        {
            product.SetMetaDescription(request.MetaDescription);
        }

        if (request.IsActive.HasValue)
        {
            product.SetActive(request.IsActive.Value);
        }

        if (request.IsFeatured.HasValue)
        {
            product.SetFeatured(request.IsFeatured.Value);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
