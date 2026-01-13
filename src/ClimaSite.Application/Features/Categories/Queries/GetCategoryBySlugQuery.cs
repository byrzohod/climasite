using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Exceptions;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Categories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Categories.Queries;

public record GetCategoryBySlugQuery : IRequest<CategoryDto>, ICacheableQuery
{
    public string Slug { get; init; } = string.Empty;
    public string? LanguageCode { get; init; }

    public string CacheKey => $"category_slug_{Slug}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}

public class GetCategoryBySlugQueryHandler : IRequestHandler<GetCategoryBySlugQuery, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Translations)
            .Include(c => c.Children.Where(ch => ch.IsActive).OrderBy(ch => ch.SortOrder))
                .ThenInclude(ch => ch.Translations)
            .FirstOrDefaultAsync(c => c.Slug == request.Slug.ToLowerInvariant() && c.IsActive, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", request.Slug);
        }

        var productCount = await _context.Products
            .CountAsync(p => p.CategoryId == category.Id && p.IsActive, cancellationToken);

        var (name, description, metaTitle, metaDescription) = category.GetTranslatedContent(request.LanguageCode);

        return new CategoryDto
        {
            Id = category.Id,
            Name = name,
            Slug = category.Slug,
            Description = description,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ParentId = category.ParentId,
            ProductCount = productCount,
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            Children = category.Children.Select(c =>
            {
                var (childName, childDesc, _, _) = c.GetTranslatedContent(request.LanguageCode);
                return new CategoryDto
                {
                    Id = c.Id,
                    Name = childName,
                    Slug = c.Slug,
                    Description = childDesc,
                    ImageUrl = c.ImageUrl,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive,
                    ParentId = c.ParentId,
                    ProductCount = 0,
                    Children = new List<CategoryDto>()
                };
            }).ToList()
        };
    }
}
