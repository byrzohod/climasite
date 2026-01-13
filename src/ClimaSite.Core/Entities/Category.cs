namespace ClimaSite.Core.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Navigation properties
    public virtual Category? Parent { get; private set; }
    public virtual ICollection<Category> Children { get; private set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();
    public virtual ICollection<CategoryTranslation> Translations { get; private set; } = new List<CategoryTranslation>();

    /// <summary>
    /// Gets the translated content for the specified language, or returns the default (English) content.
    /// </summary>
    public (string Name, string? Description, string? MetaTitle, string? MetaDescription) GetTranslatedContent(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return (Name, Description, MetaTitle, MetaDescription);
        }

        var translation = Translations.FirstOrDefault(t => t.LanguageCode.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        if (translation == null)
        {
            return (Name, Description, MetaTitle, MetaDescription);
        }

        return (
            translation.Name ?? Name,
            translation.Description ?? Description,
            translation.MetaTitle ?? MetaTitle,
            translation.MetaDescription ?? MetaDescription
        );
    }

    private Category() { }

    public Category(string name, string slug, string? description = null)
    {
        SetName(name);
        SetSlug(slug);
        Description = description;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));

        Name = name.Trim();
        SetUpdatedAt();
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category slug cannot be empty", nameof(slug));

        if (slug.Length > 100)
            throw new ArgumentException("Category slug cannot exceed 100 characters", nameof(slug));

        Slug = slug.Trim().ToLowerInvariant();
        SetUpdatedAt();
    }

    public void SetParent(Guid? parentId)
    {
        if (parentId == Id)
            throw new InvalidOperationException("A category cannot be its own parent");

        ParentId = parentId;
        SetUpdatedAt();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        SetUpdatedAt();
    }

    public void SetImageUrl(string? imageUrl)
    {
        if (imageUrl != null && imageUrl.Length > 500)
            throw new ArgumentException("Image URL cannot exceed 500 characters", nameof(imageUrl));

        ImageUrl = imageUrl;
        SetUpdatedAt();
    }

    public void SetIcon(string? icon)
    {
        if (icon != null && icon.Length > 50)
            throw new ArgumentException("Icon cannot exceed 50 characters", nameof(icon));

        Icon = icon;
        SetUpdatedAt();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        SetUpdatedAt();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        SetUpdatedAt();
    }

    public void SetMetaTitle(string? metaTitle)
    {
        if (metaTitle != null && metaTitle.Length > 200)
            throw new ArgumentException("Meta title cannot exceed 200 characters", nameof(metaTitle));

        MetaTitle = metaTitle?.Trim();
        SetUpdatedAt();
    }

    public void SetMetaDescription(string? metaDescription)
    {
        if (metaDescription != null && metaDescription.Length > 500)
            throw new ArgumentException("Meta description cannot exceed 500 characters", nameof(metaDescription));

        MetaDescription = metaDescription?.Trim();
        SetUpdatedAt();
    }

    public IEnumerable<Category> GetAncestors()
    {
        var ancestors = new List<Category>();
        var current = Parent;

        while (current != null)
        {
            ancestors.Add(current);
            current = current.Parent;
        }

        ancestors.Reverse();
        return ancestors;
    }

    public IEnumerable<Category> GetDescendants()
    {
        var descendants = new List<Category>();

        foreach (var child in Children)
        {
            descendants.Add(child);
            descendants.AddRange(child.GetDescendants());
        }

        return descendants;
    }

    public bool IsAncestorOf(Category category)
    {
        var current = category.Parent;

        while (current != null)
        {
            if (current.Id == Id)
                return true;
            current = current.Parent;
        }

        return false;
    }
}

public class CategoryTranslation
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public virtual Category? Category { get; set; }
}
