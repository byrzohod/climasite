namespace ClimaSite.Application.Features.Categories.DTOs;

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public string? ImageUrl { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentId { get; init; }
    public ParentCategoryInfo? ParentCategory { get; init; }
    public List<CategoryDto> Children { get; init; } = new();
    public int ProductCount { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
}

public record ParentCategoryInfo
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public record CategoryTreeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public List<CategoryTreeDto> Children { get; init; } = new();
}
