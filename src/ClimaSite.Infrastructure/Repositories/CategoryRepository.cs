using ClimaSite.Core.Entities;
using ClimaSite.Core.Interfaces;
using ClimaSite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Infrastructure.Repositories;

public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Parent)
            .Include(c => c.Children.Where(ch => ch.IsActive).OrderBy(ch => ch.SortOrder))
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetTreeAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(c => c.Children)
            .Where(c => c.ParentId == null);

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var rootCategories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        foreach (var root in rootCategories)
        {
            await LoadChildrenRecursivelyAsync(root, activeOnly, cancellationToken);
        }

        return rootCategories;
    }

    private async Task LoadChildrenRecursivelyAsync(Category parent, bool activeOnly, CancellationToken cancellationToken)
    {
        var childrenQuery = DbSet.Where(c => c.ParentId == parent.Id);

        if (activeOnly)
        {
            childrenQuery = childrenQuery.Where(c => c.IsActive);
        }

        var children = await childrenQuery
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            await LoadChildrenRecursivelyAsync(child, activeOnly, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var ancestors = new List<Category>();
        var currentCategory = await DbSet
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (currentCategory == null)
            return ancestors;

        var current = currentCategory.Parent;
        while (current != null)
        {
            ancestors.Add(current);
            current = await DbSet
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == current.ParentId, cancellationToken);

            if (current != null)
            {
                var loadedParent = await DbSet.FirstOrDefaultAsync(c => c.Id == current.ParentId, cancellationToken);
                if (loadedParent != null)
                {
                    current = await DbSet.Include(c => c.Parent).FirstOrDefaultAsync(c => c.Id == loadedParent.Id, cancellationToken);
                }
            }
        }

        ancestors.Reverse();
        return ancestors;
    }

    public async Task<IReadOnlyList<Category>> GetDescendantsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var descendants = new List<Category>();
        await GetDescendantsRecursivelyAsync(categoryId, descendants, cancellationToken);
        return descendants;
    }

    private async Task GetDescendantsRecursivelyAsync(Guid parentId, List<Category> descendants, CancellationToken cancellationToken)
    {
        var children = await DbSet
            .Where(c => c.ParentId == parentId)
            .ToListAsync(cancellationToken);

        foreach (var child in children)
        {
            descendants.Add(child);
            await GetDescendantsRecursivelyAsync(child.Id, descendants, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();
        await GetDescendantIdsRecursivelyAsync(categoryId, ids, cancellationToken);
        return ids;
    }

    private async Task GetDescendantIdsRecursivelyAsync(Guid parentId, List<Guid> ids, CancellationToken cancellationToken)
    {
        var childIds = await DbSet
            .Where(c => c.ParentId == parentId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        ids.AddRange(childIds);

        foreach (var childId in childIds)
        {
            await GetDescendantIdsRecursivelyAsync(childId, ids, cancellationToken);
        }
    }

    public async Task<int> GetProductCountAsync(Guid categoryId, bool includeChildren = true, CancellationToken cancellationToken = default)
    {
        var categoryIds = new List<Guid> { categoryId };

        if (includeChildren)
        {
            var descendantIds = await GetDescendantIdsAsync(categoryId, cancellationToken);
            categoryIds.AddRange(descendantIds);
        }

        return await Context.Products
            .Where(p => p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value) && p.IsActive)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(c => c.ParentId == categoryId, cancellationToken);
    }

    public async Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await Context.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.ToLowerInvariant();
        var query = DbSet.Where(c => c.Slug == normalizedSlug);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task ReorderAsync(IEnumerable<CategoryOrderItem> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            var category = await DbSet.FindAsync(new object[] { item.Id }, cancellationToken);
            if (category != null)
            {
                category.SetSortOrder(item.SortOrder);
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
