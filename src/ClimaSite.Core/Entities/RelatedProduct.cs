namespace ClimaSite.Core.Entities;

public class RelatedProduct : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid RelatedProductId { get; private set; }
    public RelationType RelationType { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation properties
    public virtual Product Product { get; private set; } = null!;
    public virtual Product Related { get; private set; } = null!;

    private RelatedProduct() { }

    public RelatedProduct(Guid productId, Guid relatedProductId, RelationType relationType)
    {
        if (productId == relatedProductId)
            throw new ArgumentException("A product cannot be related to itself");

        ProductId = productId;
        RelatedProductId = relatedProductId;
        RelationType = relationType;
    }

    public void SetRelationType(RelationType relationType)
    {
        RelationType = relationType;
        SetUpdatedAt();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        SetUpdatedAt();
    }
}

public enum RelationType
{
    Similar,
    Accessory,
    Upgrade,
    Bundle,
    FrequentlyBoughtTogether
}
