using ClimaSite.Core.Entities;

namespace ClimaSite.Core.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetTreeAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetDescendantsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<int> GetProductCountAsync(Guid categoryId, bool includeChildren = true, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task ReorderAsync(IEnumerable<CategoryOrderItem> items, CancellationToken cancellationToken = default);
}

public record CategoryOrderItem(Guid Id, int SortOrder);
