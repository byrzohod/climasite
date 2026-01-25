# CQRS FEATURES

20 feature folders. MediatR handlers, FluentValidation.

## FOLDER STRUCTURE

```
Features/
├── Products/
│   ├── Commands/
│   │   ├── CreateProductCommand.cs
│   │   └── CreateProductCommandValidator.cs
│   ├── Queries/
│   │   ├── GetProductQuery.cs
│   │   └── GetProductQueryHandler.cs
│   └── DTOs/
│       └── ProductDto.cs
├── Orders/
├── Cart/
├── Reviews/
├── Admin/
│   ├── Products/
│   ├── Orders/
│   └── Dashboard/
└── ...
```

## COMMAND PATTERN

```csharp
public record CreateProductCommand(
    string Name,
    decimal Price
) : IRequest<Guid>;

public class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, Guid>
{
    // Implementation
}
```

## FEATURES LIST

Products, Orders, Cart, Auth, Reviews, Questions, Categories, Brands, Wishlist, Notifications, Inventory, Payments, Addresses, Promotions, PriceHistory, Gdpr, Installation, Users

## ADMIN FEATURES

Nested under `Features/Admin/`: Products, Orders, Customers, Dashboard, Translations, RelatedProducts

## CONVENTIONS

- Commands mutate state, Queries read
- ALL commands need validators
- Return DTOs, not entities
- Use Mapster for entity→DTO mapping

## KNOWN ISSUE

Auth exists in TWO locations: `Application/Auth/` AND `Application/Features/Auth/`. Use `Features/Auth/` (canonical).
