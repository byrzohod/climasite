using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Common.Models;
using ClimaSite.Core.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Categories.Commands;

public record CreateCategoryCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentId { get; init; }
    public string? ImageUrl { get; init; }
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public bool IsActive { get; init; } = true;
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters")
            .When(x => x.ImageUrl != null);

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("Icon cannot exceed 50 characters")
            .When(x => x.Icon != null);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta title cannot exceed 200 characters")
            .When(x => x.MetaTitle != null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description cannot exceed 500 characters")
            .When(x => x.MetaDescription != null);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = GenerateSlug(request.Name);
        var slugSuffix = 1;
        var originalSlug = slug;

        while (await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken))
        {
            slug = $"{originalSlug}-{slugSuffix++}";
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.Categories.AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken);
            if (!parentExists)
            {
                return Result<Guid>.Failure($"Parent category with ID '{request.ParentId}' not found");
            }
        }

        var category = new Category(request.Name, slug, request.Description);

        category.SetParent(request.ParentId);
        category.SetImageUrl(request.ImageUrl);
        category.SetIcon(request.Icon);
        category.SetSortOrder(request.SortOrder);
        category.SetMetaTitle(request.MetaTitle);
        category.SetMetaDescription(request.MetaDescription);
        category.SetActive(request.IsActive);

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
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
