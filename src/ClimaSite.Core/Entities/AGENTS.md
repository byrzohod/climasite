# DOMAIN ENTITIES

25 rich domain entities. Private setters, validation in Set* methods.

## BASE PATTERN

```csharp
public class Entity : BaseEntity
{
    public string Name { get; private set; }  // Private setter
    
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required");
        Name = name.Trim();
        SetUpdatedAt();  // ALWAYS call this
    }
}
```

## KEY ENTITIES

| Entity | Lines | Complexity |
|--------|-------|------------|
| Product | 312 | JSONB specs, translations, 30+ setters |
| Order | 237 | Status state machine, event tracking |
| Category | 196 | Hierarchical tree (GetAncestors/Descendants) |
| Cart | 150 | Session mgmt, item operations |
| Review | 123 | Voting, admin responses |

## TRANSLATION SUPPORT

Product and Category have translation entities. Use `GetTranslatedContent(languageCode)`:

```csharp
var (name, desc, _, _) = product.GetTranslatedContent("bg");
```

## CONVENTIONS

- Inherit `BaseEntity` (Id, CreatedAt, UpdatedAt)
- Private setters + public `Set*` methods
- Call `SetUpdatedAt()` in EVERY setter
- Throw `ArgumentException` for validation
- Navigation properties are `virtual`

## JSONB USAGE

Product.Specifications stores flexible attributes:

```csharp
product.SetSpecification("btu", 12000);
var btu = product.GetSpecification<int>("btu");
```

## ORDER STATE MACHINE

Valid transitions enforced by `ValidateStatusTransition()`. States: Pending → Confirmed → Processing → Shipped → Delivered (or Cancelled/Refunded).
