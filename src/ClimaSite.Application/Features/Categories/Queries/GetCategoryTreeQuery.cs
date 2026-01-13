using ClimaSite.Application.Common.Behaviors;
using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Categories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Application.Features.Categories.Queries;

public record GetCategoryTreeQuery : IRequest<List<CategoryTreeDto>>, ICacheableQuery
{
    public string? NameFilter { get; init; }
    public string? LanguageCode { get; init; }
    public string CacheKey => string.IsNullOrEmpty(NameFilter)
        ? $"category_tree_{LanguageCode ?? "en"}"
        : $"category_tree_{NameFilter}_{LanguageCode ?? "en"}";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}

public class GetCategoryTreeQueryHandler : IRequestHandler<GetCategoryTreeQuery, List<CategoryTreeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryTreeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryTreeDto>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Include(c => c.Translations)
            .Where(c => c.IsActive);

        // Filter by name if provided
        if (!string.IsNullOrEmpty(request.NameFilter))
        {
            query = query.Where(c => c.Name.Contains(request.NameFilter));
        }

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        // If filtering by name, return flat list
        if (!string.IsNullOrEmpty(request.NameFilter))
        {
            return categories.Select(c =>
            {
                var (name, _, _, _) = c.GetTranslatedContent(request.LanguageCode);
                return new CategoryTreeDto
                {
                    Id = c.Id,
                    Name = name,
                    Slug = c.Slug,
                    ImageUrl = c.ImageUrl,
                    Children = new List<CategoryTreeDto>()
                };
            }).ToList();
        }

        // Build tree structure
        var rootCategories = categories
            .Where(c => c.ParentId == null)
            .Select(c => BuildCategoryTree(c, categories, request.LanguageCode))
            .ToList();

        return rootCategories;
    }

    private static CategoryTreeDto BuildCategoryTree(
        Core.Entities.Category category,
        List<Core.Entities.Category> allCategories,
        string? languageCode)
    {
        var children = allCategories
            .Where(c => c.ParentId == category.Id)
            .Select(c => BuildCategoryTree(c, allCategories, languageCode))
            .ToList();

        var (name, _, _, _) = category.GetTranslatedContent(languageCode);

        return new CategoryTreeDto
        {
            Id = category.Id,
            Name = name,
            Slug = category.Slug,
            ImageUrl = category.ImageUrl,
            Children = children
        };
    }
}
