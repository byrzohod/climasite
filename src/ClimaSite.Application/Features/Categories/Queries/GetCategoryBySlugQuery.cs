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

    public string CacheKey => $"category_slug_{Slug}";
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
            .Include(c => c.Children.Where(ch => ch.IsActive).OrderBy(ch => ch.SortOrder))
            .FirstOrDefaultAsync(c => c.Slug == request.Slug.ToLowerInvariant() && c.IsActive, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", request.Slug);
        }

        var productCount = await _context.Products
            .CountAsync(p => p.CategoryId == category.Id && p.IsActive, cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ParentId = category.ParentId,
            ProductCount = productCount,
            Children = category.Children.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                ParentId = c.ParentId,
                ProductCount = 0,
                Children = new List<CategoryDto>()
            }).ToList()
        };
    }
}
