# Wishlist Feature Plan

## 1. Overview

The Wishlist feature allows ClimaSite users to save HVAC products they are interested in for future purchase consideration. Users can add products with a heart icon, manage their wishlist, move items to cart, and optionally share their wishlist via a public link.

### Key Features
- **Save products for later** - Heart icon toggle on product cards and detail pages
- **Manage wishlist** - View, organize, and remove saved items
- **Move to cart** - One-click transfer from wishlist to shopping cart
- **Share wishlist** - Generate public shareable link for gift registries or recommendations

### User Stories
- As a customer, I want to save products I'm interested in so I can purchase them later
- As a customer, I want to quickly move wishlist items to my cart when ready to buy
- As a customer, I want to share my wishlist with family for gift suggestions
- As a visitor, I want to view a shared wishlist without logging in

---

## 2. Database Schema

### 2.1 Tables

```sql
-- Wishlist table (one per user)
CREATE TABLE wishlists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE UNIQUE,
    name VARCHAR(100) DEFAULT 'My Wishlist',
    is_public BOOLEAN DEFAULT FALSE,
    share_token VARCHAR(50) UNIQUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Wishlist items
CREATE TABLE wishlist_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    wishlist_id UUID REFERENCES wishlists(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id) ON DELETE CASCADE,
    variant_id UUID REFERENCES product_variants(id) ON DELETE SET NULL,
    added_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    notes VARCHAR(500),
    priority INTEGER DEFAULT 0,
    notify_on_sale BOOLEAN DEFAULT FALSE,
    UNIQUE(wishlist_id, product_id, variant_id)
);

-- Indexes for performance
CREATE INDEX idx_wishlists_user_id ON wishlists(user_id);
CREATE INDEX idx_wishlists_share_token ON wishlists(share_token) WHERE share_token IS NOT NULL;
CREATE INDEX idx_wishlist_items_wishlist_id ON wishlist_items(wishlist_id);
CREATE INDEX idx_wishlist_items_product_id ON wishlist_items(product_id);
```

### 2.2 EF Core Entities

```csharp
// ClimaSite.Core/Entities/Wishlist.cs
public class Wishlist : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = "My Wishlist";
    public bool IsPublic { get; set; }
    public string? ShareToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}

// ClimaSite.Core/Entities/WishlistItem.cs
public class WishlistItem : BaseEntity
{
    public Guid WishlistId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public DateTime AddedAt { get; set; }
    public string? Notes { get; set; }
    public int Priority { get; set; }
    public bool NotifyOnSale { get; set; }

    public Wishlist Wishlist { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
```

### 2.3 EF Core Configuration

```csharp
// ClimaSite.Infrastructure/Data/Configurations/WishlistConfiguration.cs
public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("wishlists");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(w => w.Name).HasColumnName("name").HasMaxLength(100);
        builder.Property(w => w.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
        builder.Property(w => w.ShareToken).HasColumnName("share_token").HasMaxLength(50);
        builder.Property(w => w.CreatedAt).HasColumnName("created_at");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(w => w.UserId).IsUnique();
        builder.HasIndex(w => w.ShareToken).IsUnique().HasFilter("share_token IS NOT NULL");

        builder.HasOne(w => w.User)
            .WithOne(u => u.Wishlist)
            .HasForeignKey<Wishlist>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ClimaSite.Infrastructure/Data/Configurations/WishlistItemConfiguration.cs
public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("wishlist_items");

        builder.HasKey(wi => wi.Id);
        builder.Property(wi => wi.Id).HasColumnName("id");
        builder.Property(wi => wi.WishlistId).HasColumnName("wishlist_id").IsRequired();
        builder.Property(wi => wi.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(wi => wi.VariantId).HasColumnName("variant_id");
        builder.Property(wi => wi.AddedAt).HasColumnName("added_at");
        builder.Property(wi => wi.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(wi => wi.Priority).HasColumnName("priority").HasDefaultValue(0);
        builder.Property(wi => wi.NotifyOnSale).HasColumnName("notify_on_sale").HasDefaultValue(false);

        builder.HasIndex(wi => new { wi.WishlistId, wi.ProductId, wi.VariantId }).IsUnique();

        builder.HasOne(wi => wi.Wishlist)
            .WithMany(w => w.Items)
            .HasForeignKey(wi => wi.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wi => wi.Product)
            .WithMany()
            .HasForeignKey(wi => wi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wi => wi.Variant)
            .WithMany()
            .HasForeignKey(wi => wi.VariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## 3. API Endpoints

### 3.1 Endpoint Summary

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/wishlist` | Get user's wishlist with items | Required |
| POST | `/api/v1/wishlist/items` | Add item to wishlist | Required |
| DELETE | `/api/v1/wishlist/items/{id}` | Remove item from wishlist | Required |
| PUT | `/api/v1/wishlist/items/{id}` | Update item (notes, priority) | Required |
| POST | `/api/v1/wishlist/items/{id}/move-to-cart` | Move item to cart | Required |
| POST | `/api/v1/wishlist/items/move-all-to-cart` | Move all items to cart | Required |
| PUT | `/api/v1/wishlist/share` | Enable/disable sharing | Required |
| GET | `/api/v1/wishlist/shared/{token}` | Get shared wishlist | Public |
| GET | `/api/v1/wishlist/check/{productId}` | Check if product in wishlist | Required |

### 3.2 Request/Response DTOs

```csharp
// DTOs/Wishlist/WishlistDto.cs
public record WishlistDto(
    Guid Id,
    string Name,
    bool IsPublic,
    string? ShareUrl,
    int ItemCount,
    List<WishlistItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// DTOs/Wishlist/WishlistItemDto.cs
public record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ProductImageUrl,
    decimal Price,
    decimal? SalePrice,
    bool InStock,
    Guid? VariantId,
    string? VariantName,
    string? Notes,
    int Priority,
    bool NotifyOnSale,
    DateTime AddedAt
);

// DTOs/Wishlist/AddWishlistItemRequest.cs
public record AddWishlistItemRequest(
    Guid ProductId,
    Guid? VariantId = null,
    string? Notes = null
);

// DTOs/Wishlist/UpdateWishlistItemRequest.cs
public record UpdateWishlistItemRequest(
    string? Notes,
    int? Priority,
    bool? NotifyOnSale
);

// DTOs/Wishlist/UpdateWishlistSharingRequest.cs
public record UpdateWishlistSharingRequest(
    bool IsPublic
);

// DTOs/Wishlist/SharedWishlistDto.cs
public record SharedWishlistDto(
    string Name,
    string OwnerName,
    int ItemCount,
    List<SharedWishlistItemDto> Items
);

public record SharedWishlistItemDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ProductImageUrl,
    decimal Price,
    decimal? SalePrice,
    bool InStock
);
```

### 3.3 Controller Implementation

```csharp
// Controllers/WishlistController.cs
[ApiController]
[Route("api/v1/wishlist")]
[Produces("application/json")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    /// <summary>Get user's wishlist</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(WishlistDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishlistDto>> GetWishlist()
    {
        var userId = User.GetUserId();
        var wishlist = await _wishlistService.GetOrCreateWishlistAsync(userId);
        return Ok(wishlist);
    }

    /// <summary>Add item to wishlist</summary>
    [HttpPost("items")]
    [Authorize]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WishlistItemDto>> AddItem([FromBody] AddWishlistItemRequest request)
    {
        var userId = User.GetUserId();
        var item = await _wishlistService.AddItemAsync(userId, request);
        return CreatedAtAction(nameof(GetWishlist), item);
    }

    /// <summary>Remove item from wishlist</summary>
    [HttpDelete("items/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id)
    {
        var userId = User.GetUserId();
        await _wishlistService.RemoveItemAsync(userId, id);
        return NoContent();
    }

    /// <summary>Update wishlist item</summary>
    [HttpPut("items/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishlistItemDto>> UpdateItem(
        Guid id,
        [FromBody] UpdateWishlistItemRequest request)
    {
        var userId = User.GetUserId();
        var item = await _wishlistService.UpdateItemAsync(userId, id, request);
        return Ok(item);
    }

    /// <summary>Move item to cart</summary>
    [HttpPost("items/{id:guid}/move-to-cart")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveToCart(Guid id)
    {
        var userId = User.GetUserId();
        await _wishlistService.MoveItemToCartAsync(userId, id);
        return Ok(new { message = "Item moved to cart" });
    }

    /// <summary>Move all items to cart</summary>
    [HttpPost("items/move-all-to-cart")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveAllToCart()
    {
        var userId = User.GetUserId();
        var count = await _wishlistService.MoveAllItemsToCartAsync(userId);
        return Ok(new { message = $"{count} items moved to cart", count });
    }

    /// <summary>Update sharing settings</summary>
    [HttpPut("share")]
    [Authorize]
    [ProducesResponseType(typeof(WishlistDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishlistDto>> UpdateSharing(
        [FromBody] UpdateWishlistSharingRequest request)
    {
        var userId = User.GetUserId();
        var wishlist = await _wishlistService.UpdateSharingAsync(userId, request.IsPublic);
        return Ok(wishlist);
    }

    /// <summary>Get shared wishlist (public)</summary>
    [HttpGet("shared/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SharedWishlistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SharedWishlistDto>> GetSharedWishlist(string token)
    {
        var wishlist = await _wishlistService.GetSharedWishlistAsync(token);
        return Ok(wishlist);
    }

    /// <summary>Check if product is in wishlist</summary>
    [HttpGet("check/{productId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(WishlistCheckResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WishlistCheckResponse>> CheckProduct(
        Guid productId,
        [FromQuery] Guid? variantId = null)
    {
        var userId = User.GetUserId();
        var isInWishlist = await _wishlistService.IsInWishlistAsync(userId, productId, variantId);
        return Ok(new WishlistCheckResponse(isInWishlist));
    }
}

public record WishlistCheckResponse(bool IsInWishlist);
```

---

## 4. Backend Implementation Tasks

### Task WISH-001: Database Migration

**Description:** Create EF Core migration for wishlist tables.

**Files to Create/Modify:**
- `src/ClimaSite.Infrastructure/Migrations/YYYYMMDDHHMMSS_AddWishlistTables.cs`

**Acceptance Criteria:**
- [ ] `wishlists` table created with all columns
- [ ] `wishlist_items` table created with all columns
- [ ] Foreign key constraints properly configured
- [ ] Unique constraints on `user_id` and `(wishlist_id, product_id, variant_id)`
- [ ] Indexes created for performance
- [ ] Migration can be rolled back cleanly

**Commands:**
```bash
dotnet ef migrations add AddWishlistTables --project src/ClimaSite.Infrastructure --startup-project src/ClimaSite.Api
dotnet ef database update --project src/ClimaSite.Infrastructure --startup-project src/ClimaSite.Api
```

---

### Task WISH-002: Domain Entities

**Description:** Create Wishlist and WishlistItem entities in Core project.

**Files to Create:**
- `src/ClimaSite.Core/Entities/Wishlist.cs`
- `src/ClimaSite.Core/Entities/WishlistItem.cs`

**Acceptance Criteria:**
- [ ] Wishlist entity with all properties
- [ ] WishlistItem entity with all properties
- [ ] Navigation properties configured
- [ ] User entity updated with Wishlist navigation property

---

### Task WISH-003: EF Core Configurations

**Description:** Create entity configurations for PostgreSQL mapping.

**Files to Create:**
- `src/ClimaSite.Infrastructure/Data/Configurations/WishlistConfiguration.cs`
- `src/ClimaSite.Infrastructure/Data/Configurations/WishlistItemConfiguration.cs`

**Acceptance Criteria:**
- [ ] Snake_case column naming applied
- [ ] All constraints configured (unique, foreign keys)
- [ ] Indexes defined
- [ ] Delete behaviors set correctly
- [ ] DbContext updated to include DbSets

---

### Task WISH-004: Wishlist Repository

**Description:** Implement repository for wishlist data access.

**Files to Create:**
- `src/ClimaSite.Core/Interfaces/IWishlistRepository.cs`
- `src/ClimaSite.Infrastructure/Repositories/WishlistRepository.cs`

**Implementation:**
```csharp
public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(Guid userId, bool includeItems = true);
    Task<Wishlist?> GetByShareTokenAsync(string token, bool includeItems = true);
    Task<Wishlist> CreateAsync(Wishlist wishlist);
    Task<Wishlist> UpdateAsync(Wishlist wishlist);
    Task<WishlistItem?> GetItemByIdAsync(Guid itemId);
    Task<WishlistItem> AddItemAsync(WishlistItem item);
    Task RemoveItemAsync(WishlistItem item);
    Task<bool> ItemExistsAsync(Guid wishlistId, Guid productId, Guid? variantId);
}
```

**Acceptance Criteria:**
- [ ] All interface methods implemented
- [ ] Eager loading for items with products
- [ ] Efficient queries with proper includes
- [ ] Repository registered in DI container

---

### Task WISH-005: Wishlist Service

**Description:** Implement business logic service for wishlist operations.

**Files to Create:**
- `src/ClimaSite.Core/Interfaces/IWishlistService.cs`
- `src/ClimaSite.Core/Services/WishlistService.cs`

**Implementation:**
```csharp
public interface IWishlistService
{
    Task<WishlistDto> GetOrCreateWishlistAsync(Guid userId);
    Task<WishlistItemDto> AddItemAsync(Guid userId, AddWishlistItemRequest request);
    Task RemoveItemAsync(Guid userId, Guid itemId);
    Task<WishlistItemDto> UpdateItemAsync(Guid userId, Guid itemId, UpdateWishlistItemRequest request);
    Task MoveItemToCartAsync(Guid userId, Guid itemId);
    Task<int> MoveAllItemsToCartAsync(Guid userId);
    Task<WishlistDto> UpdateSharingAsync(Guid userId, bool isPublic);
    Task<SharedWishlistDto> GetSharedWishlistAsync(string token);
    Task<bool> IsInWishlistAsync(Guid userId, Guid productId, Guid? variantId);
}

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly ICartService _cartService;
    private readonly IProductRepository _productRepository;

    public async Task<WishlistDto> GetOrCreateWishlistAsync(Guid userId)
    {
        var wishlist = await _wishlistRepository.GetByUserIdAsync(userId);

        if (wishlist is null)
        {
            wishlist = await _wishlistRepository.CreateAsync(new Wishlist
            {
                UserId = userId,
                Name = "My Wishlist",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return MapToDto(wishlist);
    }

    public async Task<WishlistItemDto> AddItemAsync(Guid userId, AddWishlistItemRequest request)
    {
        var wishlist = await GetOrCreateWishlistEntityAsync(userId);

        // Check for duplicate
        if (await _wishlistRepository.ItemExistsAsync(wishlist.Id, request.ProductId, request.VariantId))
        {
            throw new ConflictException("Item already in wishlist");
        }

        // Validate product exists
        var product = await _productRepository.GetByIdAsync(request.ProductId)
            ?? throw new NotFoundException("Product not found");

        var item = new WishlistItem
        {
            WishlistId = wishlist.Id,
            ProductId = request.ProductId,
            VariantId = request.VariantId,
            Notes = request.Notes,
            AddedAt = DateTime.UtcNow
        };

        await _wishlistRepository.AddItemAsync(item);
        wishlist.UpdatedAt = DateTime.UtcNow;
        await _wishlistRepository.UpdateAsync(wishlist);

        return MapItemToDto(item, product);
    }

    public async Task MoveItemToCartAsync(Guid userId, Guid itemId)
    {
        var wishlist = await _wishlistRepository.GetByUserIdAsync(userId)
            ?? throw new NotFoundException("Wishlist not found");

        var item = wishlist.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Wishlist item not found");

        // Add to cart
        await _cartService.AddItemAsync(userId, new AddCartItemRequest(
            item.ProductId,
            item.VariantId,
            Quantity: 1
        ));

        // Remove from wishlist
        await _wishlistRepository.RemoveItemAsync(item);
    }

    public async Task<WishlistDto> UpdateSharingAsync(Guid userId, bool isPublic)
    {
        var wishlist = await GetOrCreateWishlistEntityAsync(userId);

        wishlist.IsPublic = isPublic;

        if (isPublic && string.IsNullOrEmpty(wishlist.ShareToken))
        {
            wishlist.ShareToken = GenerateShareToken();
        }

        wishlist.UpdatedAt = DateTime.UtcNow;
        await _wishlistRepository.UpdateAsync(wishlist);

        return MapToDto(wishlist);
    }

    private static string GenerateShareToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
```

**Acceptance Criteria:**
- [ ] Get or create wishlist for user
- [ ] Add item with duplicate prevention
- [ ] Remove item with ownership validation
- [ ] Update item notes and priority
- [ ] Move single item to cart
- [ ] Move all items to cart
- [ ] Generate secure share token
- [ ] Enable/disable public sharing
- [ ] Retrieve shared wishlist by token
- [ ] Check if product is in wishlist

---

### Task WISH-006: DTOs and Mapping

**Description:** Create DTOs and AutoMapper profiles for wishlist.

**Files to Create:**
- `src/ClimaSite.Api/DTOs/Wishlist/WishlistDto.cs`
- `src/ClimaSite.Api/DTOs/Wishlist/WishlistItemDto.cs`
- `src/ClimaSite.Api/DTOs/Wishlist/AddWishlistItemRequest.cs`
- `src/ClimaSite.Api/DTOs/Wishlist/UpdateWishlistItemRequest.cs`
- `src/ClimaSite.Api/DTOs/Wishlist/SharedWishlistDto.cs`
- `src/ClimaSite.Api/Mappings/WishlistMappingProfile.cs`

**Acceptance Criteria:**
- [ ] All DTOs created with proper validation attributes
- [ ] AutoMapper profile configured
- [ ] Share URL generation includes base URL

---

### Task WISH-007: Wishlist Controller

**Description:** Implement API controller with all endpoints.

**Files to Create:**
- `src/ClimaSite.Api/Controllers/WishlistController.cs`

**Acceptance Criteria:**
- [ ] All endpoints implemented per specification
- [ ] Proper authorization attributes
- [ ] OpenAPI documentation attributes
- [ ] Consistent error responses using ProblemDetails
- [ ] Input validation with FluentValidation

---

### Task WISH-008: FluentValidation Validators

**Description:** Create request validators.

**Files to Create:**
- `src/ClimaSite.Api/Validators/AddWishlistItemRequestValidator.cs`
- `src/ClimaSite.Api/Validators/UpdateWishlistItemRequestValidator.cs`

**Implementation:**
```csharp
public class AddWishlistItemRequestValidator : AbstractValidator<AddWishlistItemRequest>
{
    public AddWishlistItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);
    }
}
```

**Acceptance Criteria:**
- [ ] ProductId required and valid GUID
- [ ] Notes max length 500 characters
- [ ] Priority between 0 and 10 if provided
- [ ] Validators registered in DI

---

### Task WISH-009: Unit Tests - Service Layer

**Description:** Write unit tests for WishlistService.

**Files to Create:**
- `tests/ClimaSite.Core.Tests/Services/WishlistServiceTests.cs`

**Test Cases:**
```csharp
public class WishlistServiceTests
{
    [Fact]
    public async Task GetOrCreateWishlist_NewUser_CreatesWishlist() { }

    [Fact]
    public async Task GetOrCreateWishlist_ExistingUser_ReturnsExisting() { }

    [Fact]
    public async Task AddItem_NewItem_AddsSuccessfully() { }

    [Fact]
    public async Task AddItem_DuplicateItem_ThrowsConflict() { }

    [Fact]
    public async Task AddItem_InvalidProduct_ThrowsNotFound() { }

    [Fact]
    public async Task RemoveItem_ValidItem_RemovesSuccessfully() { }

    [Fact]
    public async Task RemoveItem_WrongUser_ThrowsNotFound() { }

    [Fact]
    public async Task MoveToCart_ValidItem_AddsToCartAndRemoves() { }

    [Fact]
    public async Task UpdateSharing_EnablePublic_GeneratesToken() { }

    [Fact]
    public async Task UpdateSharing_DisablePublic_KeepsToken() { }

    [Fact]
    public async Task GetSharedWishlist_PrivateWishlist_ThrowsNotFound() { }
}
```

**Acceptance Criteria:**
- [ ] All service methods have test coverage
- [ ] Edge cases tested (duplicates, not found, unauthorized)
- [ ] Mocks used for repository dependencies
- [ ] Tests pass in CI/CD pipeline

---

### Task WISH-010: Integration Tests - API

**Description:** Write integration tests for wishlist API endpoints.

**Files to Create:**
- `tests/ClimaSite.Api.Tests/Controllers/WishlistControllerTests.cs`

**Acceptance Criteria:**
- [ ] All endpoints tested
- [ ] Authentication scenarios tested
- [ ] Error responses validated
- [ ] Uses WebApplicationFactory
- [ ] Test database seeding

---

## 5. Frontend Implementation Tasks

### Task WISH-011: Wishlist Service

**Description:** Create Angular service for wishlist state management.

**Files to Create:**
- `src/ClimaSite.Web/src/app/features/wishlist/services/wishlist.service.ts`
- `src/ClimaSite.Web/src/app/features/wishlist/models/wishlist.models.ts`

**Implementation:**
```typescript
// wishlist.models.ts
export interface Wishlist {
  id: string;
  name: string;
  isPublic: boolean;
  shareUrl?: string;
  itemCount: number;
  items: WishlistItem[];
  createdAt: Date;
  updatedAt: Date;
}

export interface WishlistItem {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  productImageUrl?: string;
  price: number;
  salePrice?: number;
  inStock: boolean;
  variantId?: string;
  variantName?: string;
  notes?: string;
  priority: number;
  notifyOnSale: boolean;
  addedAt: Date;
}

// wishlist.service.ts
@Injectable({ providedIn: 'root' })
export class WishlistService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  // State
  private wishlistState = signal<Wishlist | null>(null);
  private loadingState = signal<boolean>(false);
  private errorState = signal<string | null>(null);

  // Public selectors
  readonly wishlist = this.wishlistState.asReadonly();
  readonly items = computed(() => this.wishlistState()?.items ?? []);
  readonly itemCount = computed(() => this.wishlistState()?.itemCount ?? 0);
  readonly loading = this.loadingState.asReadonly();
  readonly error = this.errorState.asReadonly();

  constructor() {
    // Load wishlist when user logs in
    effect(() => {
      if (this.authService.isAuthenticated()) {
        this.loadWishlist();
      } else {
        this.wishlistState.set(null);
      }
    });
  }

  async loadWishlist(): Promise<void> {
    this.loadingState.set(true);
    this.errorState.set(null);

    try {
      const wishlist = await firstValueFrom(
        this.http.get<Wishlist>('/api/v1/wishlist')
      );
      this.wishlistState.set(wishlist);
    } catch (error) {
      this.errorState.set('Failed to load wishlist');
      console.error('Wishlist load error:', error);
    } finally {
      this.loadingState.set(false);
    }
  }

  async addItem(productId: string, variantId?: string, notes?: string): Promise<void> {
    try {
      const item = await firstValueFrom(
        this.http.post<WishlistItem>('/api/v1/wishlist/items', {
          productId,
          variantId,
          notes
        })
      );

      this.wishlistState.update(wishlist => {
        if (!wishlist) return wishlist;
        return {
          ...wishlist,
          items: [...wishlist.items, item],
          itemCount: wishlist.itemCount + 1,
          updatedAt: new Date()
        };
      });
    } catch (error: any) {
      if (error.status === 409) {
        // Already in wishlist, ignore
        return;
      }
      throw error;
    }
  }

  async removeItem(itemId: string): Promise<void> {
    await firstValueFrom(
      this.http.delete(`/api/v1/wishlist/items/${itemId}`)
    );

    this.wishlistState.update(wishlist => {
      if (!wishlist) return wishlist;
      return {
        ...wishlist,
        items: wishlist.items.filter(i => i.id !== itemId),
        itemCount: wishlist.itemCount - 1,
        updatedAt: new Date()
      };
    });
  }

  async toggleItem(productId: string, variantId?: string): Promise<void> {
    const existing = this.findItem(productId, variantId);

    if (existing) {
      await this.removeItem(existing.id);
    } else {
      await this.addItem(productId, variantId);
    }
  }

  async moveToCart(itemId: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`/api/v1/wishlist/items/${itemId}/move-to-cart`, {})
    );

    this.wishlistState.update(wishlist => {
      if (!wishlist) return wishlist;
      return {
        ...wishlist,
        items: wishlist.items.filter(i => i.id !== itemId),
        itemCount: wishlist.itemCount - 1,
        updatedAt: new Date()
      };
    });
  }

  async moveAllToCart(): Promise<number> {
    const response = await firstValueFrom(
      this.http.post<{ count: number }>('/api/v1/wishlist/items/move-all-to-cart', {})
    );

    this.wishlistState.update(wishlist => {
      if (!wishlist) return wishlist;
      return {
        ...wishlist,
        items: [],
        itemCount: 0,
        updatedAt: new Date()
      };
    });

    return response.count;
  }

  async updateSharing(isPublic: boolean): Promise<void> {
    const wishlist = await firstValueFrom(
      this.http.put<Wishlist>('/api/v1/wishlist/share', { isPublic })
    );
    this.wishlistState.set(wishlist);
  }

  isInWishlist(productId: string, variantId?: string): boolean {
    return !!this.findItem(productId, variantId);
  }

  private findItem(productId: string, variantId?: string): WishlistItem | undefined {
    return this.items().find(item =>
      item.productId === productId &&
      (variantId ? item.variantId === variantId : !item.variantId)
    );
  }

  // Shared wishlist (public)
  getSharedWishlist(token: string): Observable<SharedWishlist> {
    return this.http.get<SharedWishlist>(`/api/v1/wishlist/shared/${token}`);
  }
}
```

**Acceptance Criteria:**
- [ ] Signal-based state management
- [ ] Computed properties for derived state
- [ ] Auto-load on authentication
- [ ] Optimistic updates for better UX
- [ ] Error handling with user-friendly messages
- [ ] Toggle functionality for add/remove

---

### Task WISH-012: Wishlist Button Component

**Description:** Create heart icon button for adding/removing products from wishlist.

**Files to Create:**
- `src/ClimaSite.Web/src/app/shared/components/wishlist-button/wishlist-button.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/wishlist-button/wishlist-button.component.html`
- `src/ClimaSite.Web/src/app/shared/components/wishlist-button/wishlist-button.component.scss`

**Implementation:**
```typescript
@Component({
  selector: 'app-wishlist-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './wishlist-button.component.html',
  styleUrl: './wishlist-button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WishlistButtonComponent {
  @Input({ required: true }) productId!: string;
  @Input() variantId?: string;
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() showLabel = false;

  @Output() toggled = new EventEmitter<boolean>();

  private wishlistService = inject(WishlistService);
  private authService = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);

  isInWishlist = computed(() =>
    this.wishlistService.isInWishlist(this.productId, this.variantId)
  );

  label = computed(() =>
    this.isInWishlist() ? 'Remove from wishlist' : 'Add to wishlist'
  );

  async toggleWishlist(event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();

    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth/login'], {
        queryParams: { returnUrl: this.router.url }
      });
      return;
    }

    this.loading.set(true);

    try {
      await this.wishlistService.toggleItem(this.productId, this.variantId);
      this.toggled.emit(this.isInWishlist());
    } finally {
      this.loading.set(false);
    }
  }
}
```

```html
<!-- wishlist-button.component.html -->
<button
  type="button"
  class="wishlist-button"
  [class.active]="isInWishlist()"
  [class.loading]="loading()"
  [class.size-small]="size === 'small'"
  [class.size-large]="size === 'large'"
  [attr.aria-label]="label()"
  [attr.aria-pressed]="isInWishlist()"
  [disabled]="loading()"
  (click)="toggleWishlist($event)"
  data-testid="wishlist-button">

  <svg
    class="heart-icon"
    [class.filled]="isInWishlist()"
    viewBox="0 0 24 24"
    aria-hidden="true">
    <path
      d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"
      [attr.fill]="isInWishlist() ? 'currentColor' : 'none'"
      stroke="currentColor"
      stroke-width="2"/>
  </svg>

  @if (showLabel) {
    <span class="label">{{ label() }}</span>
  }

  @if (loading()) {
    <span class="loading-spinner" aria-hidden="true"></span>
  }
</button>
```

```scss
// wishlist-button.component.scss
.wishlist-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.5rem;
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--color-text-secondary);
  transition: color 0.2s ease, transform 0.2s ease;

  &:hover:not(:disabled) {
    color: var(--color-danger);
    transform: scale(1.1);
  }

  &:focus-visible {
    outline: 2px solid var(--color-focus);
    outline-offset: 2px;
    border-radius: 4px;
  }

  &.active {
    color: var(--color-danger);

    .heart-icon.filled {
      animation: heartbeat 0.3s ease-in-out;
    }
  }

  &.loading {
    pointer-events: none;
    opacity: 0.7;
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.5;
  }

  // Sizes
  &.size-small {
    padding: 0.25rem;
    .heart-icon { width: 16px; height: 16px; }
  }

  &.size-large {
    padding: 0.75rem;
    .heart-icon { width: 28px; height: 28px; }
  }
}

.heart-icon {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
}

.label {
  font-size: 0.875rem;
}

.loading-spinner {
  position: absolute;
  width: 16px;
  height: 16px;
  border: 2px solid transparent;
  border-top-color: currentColor;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes heartbeat {
  0% { transform: scale(1); }
  50% { transform: scale(1.2); }
  100% { transform: scale(1); }
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
```

**Acceptance Criteria:**
- [ ] Heart icon toggles filled/outline state
- [ ] Loading state during API call
- [ ] Redirects to login if not authenticated
- [ ] Accessible with proper ARIA attributes
- [ ] Animation on state change
- [ ] Multiple size variants
- [ ] Prevents event bubbling (for use in clickable cards)

---

### Task WISH-013: Wishlist Page Component

**Description:** Create full wishlist management page.

**Files to Create:**
- `src/ClimaSite.Web/src/app/features/wishlist/pages/wishlist-page/wishlist-page.component.ts`
- `src/ClimaSite.Web/src/app/features/wishlist/pages/wishlist-page/wishlist-page.component.html`
- `src/ClimaSite.Web/src/app/features/wishlist/pages/wishlist-page/wishlist-page.component.scss`

**Implementation:**
```typescript
@Component({
  selector: 'app-wishlist-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    WishlistItemComponent,
    ShareWishlistComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './wishlist-page.component.html',
  styleUrl: './wishlist-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WishlistPageComponent {
  private wishlistService = inject(WishlistService);
  private cartService = inject(CartService);
  private notificationService = inject(NotificationService);

  wishlist = this.wishlistService.wishlist;
  items = this.wishlistService.items;
  loading = this.wishlistService.loading;
  error = this.wishlistService.error;

  movingAllToCart = signal(false);
  showShareDialog = signal(false);

  async removeItem(itemId: string): Promise<void> {
    await this.wishlistService.removeItem(itemId);
    this.notificationService.success('Item removed from wishlist');
  }

  async moveToCart(itemId: string): Promise<void> {
    await this.wishlistService.moveToCart(itemId);
    this.notificationService.success('Item moved to cart');
  }

  async moveAllToCart(): Promise<void> {
    if (this.items().length === 0) return;

    this.movingAllToCart.set(true);

    try {
      const count = await this.wishlistService.moveAllToCart();
      this.notificationService.success(`${count} items moved to cart`);
    } finally {
      this.movingAllToCart.set(false);
    }
  }

  openShareDialog(): void {
    this.showShareDialog.set(true);
  }

  closeShareDialog(): void {
    this.showShareDialog.set(false);
  }
}
```

```html
<!-- wishlist-page.component.html -->
<div class="wishlist-page">
  <header class="wishlist-header">
    <h1>My Wishlist</h1>

    @if (items().length > 0) {
      <div class="header-actions">
        <button
          class="btn btn-outline"
          (click)="openShareDialog()"
          data-testid="share-wishlist">
          <svg class="icon"><!-- share icon --></svg>
          Share Wishlist
        </button>

        <button
          class="btn btn-primary"
          [disabled]="movingAllToCart()"
          (click)="moveAllToCart()"
          data-testid="move-all-to-cart">
          @if (movingAllToCart()) {
            <span class="spinner"></span>
          }
          Add All to Cart
        </button>
      </div>
    }
  </header>

  @if (loading()) {
    <app-loading-spinner />
  } @else if (error()) {
    <div class="error-state">
      <p>{{ error() }}</p>
      <button class="btn btn-primary" (click)="wishlistService.loadWishlist()">
        Try Again
      </button>
    </div>
  } @else if (items().length === 0) {
    <app-empty-state
      title="Your wishlist is empty"
      description="Save products you love by clicking the heart icon"
      actionLabel="Browse Products"
      actionLink="/products"
      icon="heart" />
  } @else {
    <div class="wishlist-summary">
      <span>{{ items().length }} items in your wishlist</span>
    </div>

    <ul class="wishlist-items" role="list">
      @for (item of items(); track item.id) {
        <li>
          <app-wishlist-item
            [item]="item"
            (remove)="removeItem(item.id)"
            (moveToCart)="moveToCart(item.id)" />
        </li>
      }
    </ul>
  }

  @if (showShareDialog()) {
    <app-share-wishlist
      [wishlist]="wishlist()!"
      (close)="closeShareDialog()" />
  }
</div>
```

**Acceptance Criteria:**
- [ ] Displays all wishlist items
- [ ] Empty state with CTA to browse products
- [ ] Remove item from wishlist
- [ ] Move individual item to cart
- [ ] Move all items to cart
- [ ] Share wishlist dialog
- [ ] Loading and error states
- [ ] Responsive layout (grid on desktop, list on mobile)

---

### Task WISH-014: Wishlist Item Component

**Description:** Create individual wishlist item display component.

**Files to Create:**
- `src/ClimaSite.Web/src/app/features/wishlist/components/wishlist-item/wishlist-item.component.ts`
- `src/ClimaSite.Web/src/app/features/wishlist/components/wishlist-item/wishlist-item.component.html`
- `src/ClimaSite.Web/src/app/features/wishlist/components/wishlist-item/wishlist-item.component.scss`

**Implementation:**
```typescript
@Component({
  selector: 'app-wishlist-item',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './wishlist-item.component.html',
  styleUrl: './wishlist-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WishlistItemComponent {
  @Input({ required: true }) item!: WishlistItem;

  @Output() remove = new EventEmitter<void>();
  @Output() moveToCart = new EventEmitter<void>();

  removing = signal(false);
  movingToCart = signal(false);

  get hasDiscount(): boolean {
    return !!this.item.salePrice && this.item.salePrice < this.item.price;
  }

  get discountPercent(): number {
    if (!this.hasDiscount) return 0;
    return Math.round((1 - this.item.salePrice! / this.item.price) * 100);
  }

  async onRemove(): Promise<void> {
    this.removing.set(true);
    this.remove.emit();
  }

  async onMoveToCart(): Promise<void> {
    this.movingToCart.set(true);
    this.moveToCart.emit();
  }
}
```

```html
<!-- wishlist-item.component.html -->
<article class="wishlist-item" [class.out-of-stock]="!item.inStock">
  <a [routerLink]="['/products', item.productSlug]" class="item-image">
    @if (item.productImageUrl) {
      <img [src]="item.productImageUrl" [alt]="item.productName" loading="lazy" />
    } @else {
      <div class="placeholder-image">
        <svg><!-- placeholder icon --></svg>
      </div>
    }

    @if (!item.inStock) {
      <span class="out-of-stock-badge">Out of Stock</span>
    }

    @if (hasDiscount) {
      <span class="discount-badge">-{{ discountPercent }}%</span>
    }
  </a>

  <div class="item-details">
    <a [routerLink]="['/products', item.productSlug]" class="item-name">
      {{ item.productName }}
    </a>

    @if (item.variantName) {
      <span class="item-variant">{{ item.variantName }}</span>
    }

    <div class="item-price">
      @if (hasDiscount) {
        <span class="sale-price">{{ item.salePrice | currency }}</span>
        <span class="original-price">{{ item.price | currency }}</span>
      } @else {
        <span class="price">{{ item.price | currency }}</span>
      }
    </div>

    <span class="added-date">Added {{ item.addedAt | date:'mediumDate' }}</span>

    @if (item.notes) {
      <p class="item-notes">{{ item.notes }}</p>
    }
  </div>

  <div class="item-actions">
    <button
      class="btn btn-primary"
      [disabled]="!item.inStock || movingToCart()"
      (click)="onMoveToCart()"
      data-testid="move-to-cart">
      @if (movingToCart()) {
        <span class="spinner"></span>
      } @else {
        Add to Cart
      }
    </button>

    <button
      class="btn btn-ghost btn-icon"
      [disabled]="removing()"
      (click)="onRemove()"
      aria-label="Remove from wishlist"
      data-testid="remove-from-wishlist">
      <svg class="icon"><!-- trash icon --></svg>
    </button>
  </div>
</article>
```

**Acceptance Criteria:**
- [ ] Product image with lazy loading
- [ ] Product name links to detail page
- [ ] Variant name displayed if applicable
- [ ] Price with sale price support
- [ ] Discount percentage badge
- [ ] Out of stock indicator
- [ ] Date added display
- [ ] Notes display if present
- [ ] Add to Cart button (disabled if out of stock)
- [ ] Remove button

---

### Task WISH-015: Share Wishlist Component

**Description:** Create dialog for sharing wishlist settings.

**Files to Create:**
- `src/ClimaSite.Web/src/app/features/wishlist/components/share-wishlist/share-wishlist.component.ts`
- `src/ClimaSite.Web/src/app/features/wishlist/components/share-wishlist/share-wishlist.component.html`
- `src/ClimaSite.Web/src/app/features/wishlist/components/share-wishlist/share-wishlist.component.scss`

**Implementation:**
```typescript
@Component({
  selector: 'app-share-wishlist',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogComponent],
  templateUrl: './share-wishlist.component.html',
  styleUrl: './share-wishlist.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShareWishlistComponent {
  @Input({ required: true }) wishlist!: Wishlist;
  @Output() close = new EventEmitter<void>();

  private wishlistService = inject(WishlistService);
  private clipboard = inject(Clipboard);
  private notificationService = inject(NotificationService);

  updating = signal(false);
  copied = signal(false);

  get shareUrl(): string {
    return this.wishlist.shareUrl ?? '';
  }

  async toggleSharing(): Promise<void> {
    this.updating.set(true);

    try {
      await this.wishlistService.updateSharing(!this.wishlist.isPublic);
    } finally {
      this.updating.set(false);
    }
  }

  async copyLink(): Promise<void> {
    if (!this.shareUrl) return;

    this.clipboard.copy(this.shareUrl);
    this.copied.set(true);
    this.notificationService.success('Link copied to clipboard');

    setTimeout(() => this.copied.set(false), 2000);
  }

  shareNative(): void {
    if (navigator.share && this.shareUrl) {
      navigator.share({
        title: `${this.wishlist.name} - ClimaSite Wishlist`,
        url: this.shareUrl
      });
    }
  }
}
```

```html
<!-- share-wishlist.component.html -->
<app-dialog (closeDialog)="close.emit()">
  <h2 slot="header">Share Your Wishlist</h2>

  <div class="share-content">
    <div class="share-toggle">
      <label class="toggle">
        <input
          type="checkbox"
          [checked]="wishlist.isPublic"
          [disabled]="updating()"
          (change)="toggleSharing()"
          data-testid="enable-sharing" />
        <span class="toggle-slider"></span>
      </label>
      <div class="toggle-label">
        <span class="toggle-title">Public Wishlist</span>
        <span class="toggle-description">
          Anyone with the link can view your wishlist
        </span>
      </div>
    </div>

    @if (wishlist.isPublic && shareUrl) {
      <div class="share-link-section">
        <label for="share-link">Share Link</label>
        <div class="share-link-input">
          <input
            id="share-link"
            type="text"
            [value]="shareUrl"
            readonly
            data-testid="share-link" />
          <button
            class="btn btn-icon"
            (click)="copyLink()"
            [attr.aria-label]="copied() ? 'Copied!' : 'Copy link'">
            @if (copied()) {
              <svg><!-- check icon --></svg>
            } @else {
              <svg><!-- copy icon --></svg>
            }
          </button>
        </div>

        @if ('share' in navigator) {
          <button class="btn btn-outline btn-full" (click)="shareNative()">
            <svg><!-- share icon --></svg>
            Share via...
          </button>
        }
      </div>
    }

    <p class="share-note">
      @if (wishlist.isPublic) {
        Your wishlist is visible to anyone with the link. They can view products but not modify your list.
      } @else {
        Enable sharing to generate a link you can share with friends and family.
      }
    </p>
  </div>
</app-dialog>
```

**Acceptance Criteria:**
- [ ] Toggle to enable/disable public sharing
- [ ] Share URL displayed when public
- [ ] Copy to clipboard functionality
- [ ] Native share API integration (mobile)
- [ ] Clear explanation of sharing behavior
- [ ] Loading state during toggle

---

### Task WISH-016: Shared Wishlist Page

**Description:** Create public page for viewing shared wishlists.

**Files to Create:**
- `src/ClimaSite.Web/src/app/features/wishlist/pages/shared-wishlist-page/shared-wishlist-page.component.ts`
- `src/ClimaSite.Web/src/app/features/wishlist/pages/shared-wishlist-page/shared-wishlist-page.component.html`

**Implementation:**
```typescript
@Component({
  selector: 'app-shared-wishlist-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent, LoadingSpinnerComponent],
  templateUrl: './shared-wishlist-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SharedWishlistPageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private wishlistService = inject(WishlistService);

  wishlist = signal<SharedWishlist | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');

    if (!token) {
      this.error.set('Invalid wishlist link');
      this.loading.set(false);
      return;
    }

    this.wishlistService.getSharedWishlist(token).subscribe({
      next: (wishlist) => {
        this.wishlist.set(wishlist);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          err.status === 404
            ? 'This wishlist is no longer available'
            : 'Failed to load wishlist'
        );
        this.loading.set(false);
      }
    });
  }
}
```

**Acceptance Criteria:**
- [ ] Loads wishlist by share token
- [ ] Displays owner name and wishlist name
- [ ] Shows all products in grid layout
- [ ] Handles not found (private or deleted)
- [ ] No login required
- [ ] Products link to detail pages

---

### Task WISH-017: Wishlist Routes

**Description:** Configure routing for wishlist feature.

**Files to Modify:**
- `src/ClimaSite.Web/src/app/app.routes.ts`
- `src/ClimaSite.Web/src/app/features/wishlist/wishlist.routes.ts`

**Implementation:**
```typescript
// wishlist.routes.ts
export const WISHLIST_ROUTES: Routes = [
  {
    path: '',
    component: WishlistPageComponent,
    canActivate: [authGuard],
    title: 'My Wishlist - ClimaSite'
  }
];

// app.routes.ts (add to existing routes)
{
  path: 'account/wishlist',
  loadChildren: () => import('./features/wishlist/wishlist.routes')
    .then(m => m.WISHLIST_ROUTES)
},
{
  path: 'wishlist/shared/:token',
  loadComponent: () => import('./features/wishlist/pages/shared-wishlist-page/shared-wishlist-page.component')
    .then(m => m.SharedWishlistPageComponent),
  title: 'Shared Wishlist - ClimaSite'
}
```

**Acceptance Criteria:**
- [ ] `/account/wishlist` route protected by auth guard
- [ ] `/wishlist/shared/:token` route publicly accessible
- [ ] Lazy loading configured
- [ ] Page titles set

---

### Task WISH-018: Integrate Wishlist Button

**Description:** Add wishlist button to product cards and detail pages.

**Files to Modify:**
- `src/ClimaSite.Web/src/app/shared/components/product-card/product-card.component.html`
- `src/ClimaSite.Web/src/app/features/products/pages/product-detail/product-detail.component.html`

**Acceptance Criteria:**
- [ ] Heart icon on product cards (top right corner)
- [ ] Heart icon on product detail page
- [ ] Consistent behavior across all locations
- [ ] Proper z-index for overlapping elements

---

### Task WISH-019: Header Wishlist Icon

**Description:** Add wishlist icon with count to site header.

**Files to Modify:**
- `src/ClimaSite.Web/src/app/core/components/header/header.component.html`
- `src/ClimaSite.Web/src/app/core/components/header/header.component.ts`

**Implementation:**
```html
<!-- In header navigation -->
<a
  routerLink="/account/wishlist"
  class="header-icon-link"
  aria-label="Wishlist"
  data-testid="header-wishlist">
  <svg class="icon"><!-- heart icon --></svg>
  @if (wishlistCount() > 0) {
    <span class="badge">{{ wishlistCount() }}</span>
  }
</a>
```

**Acceptance Criteria:**
- [ ] Heart icon in header
- [ ] Badge showing item count (when > 0)
- [ ] Links to wishlist page
- [ ] Only visible when logged in

---

## 6. E2E Tests (Playwright - NO MOCKING)

### Test File Structure

```
tests/
└── e2e/
    └── wishlist/
        ├── wishlist.spec.ts
        ├── wishlist-sharing.spec.ts
        └── fixtures/
            └── wishlist.fixtures.ts
```

### Test WISH-E2E-001: Add Product to Wishlist

**File:** `tests/e2e/wishlist/wishlist.spec.ts`

```typescript
import { test, expect } from '@playwright/test';
import { factory } from '../fixtures/factory';
import { loginAs } from '../helpers/auth';

test.describe('Wishlist - Add Items', () => {
  test('user can add product to wishlist from product page', async ({ page }) => {
    // Setup: Create user and product in database
    const user = await factory.createUser({
      email: 'wishlist-test@example.com',
      password: 'TestPass123!'
    });
    const product = await factory.createProduct({
      name: 'Wishlist Test AC Unit',
      slug: 'wishlist-test-ac-unit',
      price: 1299.99
    });

    // Login
    await loginAs(page, user.email, 'TestPass123!');

    // Navigate to product page
    await page.goto(`/products/${product.slug}`);

    // Verify wishlist button exists and is not active
    const wishlistButton = page.getByTestId('wishlist-button');
    await expect(wishlistButton).toBeVisible();
    await expect(wishlistButton).not.toHaveClass(/active/);

    // Click wishlist button
    await wishlistButton.click();

    // Verify button is now active (filled heart)
    await expect(wishlistButton).toHaveClass(/active/);

    // Navigate to wishlist page
    await page.goto('/account/wishlist');

    // Verify product appears in wishlist
    await expect(page.getByText('Wishlist Test AC Unit')).toBeVisible();
    await expect(page.getByText('$1,299.99')).toBeVisible();

    // Cleanup
    await factory.cleanup();
  });

  test('user can add product to wishlist from product card', async ({ page }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'Card Wishlist Test',
      slug: 'card-wishlist-test'
    });

    await loginAs(page, user.email, 'TestPass123!');
    await page.goto('/products');

    // Find the product card and its wishlist button
    const productCard = page.locator(`[data-product-id="${product.id}"]`);
    const wishlistButton = productCard.getByTestId('wishlist-button');

    // Add to wishlist
    await wishlistButton.click();
    await expect(wishlistButton).toHaveClass(/active/);

    // Verify in wishlist
    await page.goto('/account/wishlist');
    await expect(page.getByText('Card Wishlist Test')).toBeVisible();

    await factory.cleanup();
  });

  test('unauthenticated user is redirected to login', async ({ page }) => {
    const product = await factory.createProduct({
      slug: 'auth-test-product'
    });

    await page.goto(`/products/${product.slug}`);

    // Click wishlist without being logged in
    await page.getByTestId('wishlist-button').click();

    // Should redirect to login with return URL
    await expect(page).toHaveURL(/\/auth\/login\?returnUrl=/);

    await factory.cleanup();
  });

  test('wishlist persists across sessions', async ({ page, context }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Persistent Item' });

    // First session: add to wishlist
    await loginAs(page, user.email, 'TestPass123!');
    await page.goto(`/products/${product.slug}`);
    await page.getByTestId('wishlist-button').click();
    await expect(page.getByTestId('wishlist-button')).toHaveClass(/active/);

    // Clear session (simulate closing browser)
    await context.clearCookies();

    // New session: login again
    await loginAs(page, user.email, 'TestPass123!');
    await page.goto(`/products/${product.slug}`);

    // Item should still be in wishlist
    await expect(page.getByTestId('wishlist-button')).toHaveClass(/active/);

    await factory.cleanup();
  });
});
```

---

### Test WISH-E2E-002: Remove Product from Wishlist

```typescript
test.describe('Wishlist - Remove Items', () => {
  test('user can remove product from wishlist page', async ({ page, request }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Remove Test AC' });

    await loginAs(page, user.email, 'TestPass123!');

    // Add to wishlist via API
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    // Go to wishlist page
    await page.goto('/account/wishlist');
    await expect(page.getByText('Remove Test AC')).toBeVisible();

    // Remove item
    await page.getByTestId('remove-from-wishlist').click();

    // Verify removed
    await expect(page.getByText('Remove Test AC')).not.toBeVisible();
    await expect(page.getByText('Your wishlist is empty')).toBeVisible();

    await factory.cleanup();
  });

  test('user can toggle wishlist from product page', async ({ page, request }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Toggle Test' });

    await loginAs(page, user.email, 'TestPass123!');

    // Add via API first
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    await page.goto(`/products/${product.slug}`);

    // Button should be active
    const wishlistButton = page.getByTestId('wishlist-button');
    await expect(wishlistButton).toHaveClass(/active/);

    // Toggle off
    await wishlistButton.click();
    await expect(wishlistButton).not.toHaveClass(/active/);

    // Verify removed from wishlist
    await page.goto('/account/wishlist');
    await expect(page.getByText('Toggle Test')).not.toBeVisible();

    await factory.cleanup();
  });
});
```

---

### Test WISH-E2E-003: Move to Cart

```typescript
test.describe('Wishlist - Move to Cart', () => {
  test('user can move single item from wishlist to cart', async ({ page, request }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'Move Test AC',
      price: 899.99,
      stock: 10
    });

    await loginAs(page, user.email, 'TestPass123!');

    // Add to wishlist via API
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    // Go to wishlist
    await page.goto('/account/wishlist');
    await expect(page.getByText('Move Test AC')).toBeVisible();

    // Move to cart
    await page.getByTestId('move-to-cart').click();

    // Verify removed from wishlist
    await expect(page.getByText('Move Test AC')).not.toBeVisible();

    // Verify in cart
    await page.goto('/cart');
    await expect(page.getByText('Move Test AC')).toBeVisible();
    await expect(page.getByText('$899.99')).toBeVisible();

    await factory.cleanup();
  });

  test('user can move all items from wishlist to cart', async ({ page, request }) => {
    const user = await factory.createUser();
    const products = await Promise.all([
      factory.createProduct({ name: 'Batch Move 1', stock: 5 }),
      factory.createProduct({ name: 'Batch Move 2', stock: 5 }),
      factory.createProduct({ name: 'Batch Move 3', stock: 5 })
    ]);

    await loginAs(page, user.email, 'TestPass123!');

    // Add all to wishlist
    for (const product of products) {
      await request.post('/api/v1/wishlist/items', {
        headers: { Authorization: `Bearer ${user.token}` },
        data: { productId: product.id }
      });
    }

    await page.goto('/account/wishlist');

    // Verify all items present
    for (const product of products) {
      await expect(page.getByText(product.name)).toBeVisible();
    }

    // Move all to cart
    await page.getByTestId('move-all-to-cart').click();

    // Verify wishlist empty
    await expect(page.getByText('Your wishlist is empty')).toBeVisible();

    // Verify all in cart
    await page.goto('/cart');
    for (const product of products) {
      await expect(page.getByText(product.name)).toBeVisible();
    }

    await factory.cleanup();
  });

  test('out of stock items cannot be moved to cart', async ({ page, request }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'Out of Stock AC',
      stock: 0
    });

    await loginAs(page, user.email, 'TestPass123!');

    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    await page.goto('/account/wishlist');

    // Move to cart button should be disabled
    const moveButton = page.getByTestId('move-to-cart');
    await expect(moveButton).toBeDisabled();

    // Out of stock badge should be visible
    await expect(page.getByText('Out of Stock')).toBeVisible();

    await factory.cleanup();
  });
});
```

---

### Test WISH-E2E-004: Share Wishlist

```typescript
test.describe('Wishlist - Sharing', () => {
  test('user can enable wishlist sharing and get link', async ({ page, request }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Share Test Item' });

    await loginAs(page, user.email, 'TestPass123!');

    // Add item to wishlist
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    await page.goto('/account/wishlist');

    // Open share dialog
    await page.getByTestId('share-wishlist').click();

    // Enable sharing
    const sharingToggle = page.getByTestId('enable-sharing');
    await expect(sharingToggle).not.toBeChecked();
    await sharingToggle.click();

    // Wait for share link to appear
    const shareLinkInput = page.getByTestId('share-link');
    await expect(shareLinkInput).toBeVisible();

    // Verify link format
    const shareLink = await shareLinkInput.inputValue();
    expect(shareLink).toMatch(/\/wishlist\/shared\/[A-Za-z0-9_-]+/);

    await factory.cleanup();
  });

  test('shared wishlist is viewable without login', async ({ page, request, context }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Shared Item AC' });

    await loginAs(page, user.email, 'TestPass123!');

    // Add item and enable sharing via API
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    const shareResponse = await request.put('/api/v1/wishlist/share', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { isPublic: true }
    });
    const { shareUrl } = await shareResponse.json();

    // Open new incognito context (no session)
    const incognitoContext = await context.browser()!.newContext();
    const newPage = await incognitoContext.newPage();

    // Visit share link without being logged in
    await newPage.goto(shareUrl);

    // Verify shared wishlist content
    await expect(newPage.getByText('Shared Item AC')).toBeVisible();
    await expect(newPage.getByText(user.name)).toBeVisible(); // Owner name

    // No edit buttons should be visible
    await expect(newPage.getByTestId('remove-from-wishlist')).not.toBeVisible();
    await expect(newPage.getByTestId('move-to-cart')).not.toBeVisible();

    await incognitoContext.close();
    await factory.cleanup();
  });

  test('disabling sharing makes wishlist private again', async ({ page, request, context }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Privacy Test Item' });

    await loginAs(page, user.email, 'TestPass123!');

    // Setup: Add item and enable sharing
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    const shareResponse = await request.put('/api/v1/wishlist/share', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { isPublic: true }
    });
    const { shareUrl } = await shareResponse.json();

    // Verify link works initially
    const incognitoContext = await context.browser()!.newContext();
    const guestPage = await incognitoContext.newPage();
    await guestPage.goto(shareUrl);
    await expect(guestPage.getByText('Privacy Test Item')).toBeVisible();

    // Disable sharing
    await page.goto('/account/wishlist');
    await page.getByTestId('share-wishlist').click();
    await page.getByTestId('enable-sharing').click(); // Toggle off

    // Verify link no longer works
    await guestPage.reload();
    await expect(guestPage.getByText('This wishlist is no longer available')).toBeVisible();

    await incognitoContext.close();
    await factory.cleanup();
  });

  test('copy share link to clipboard', async ({ page, request, context }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Copy Link Test' });

    await loginAs(page, user.email, 'TestPass123!');

    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    await page.goto('/account/wishlist');
    await page.getByTestId('share-wishlist').click();
    await page.getByTestId('enable-sharing').click();

    // Grant clipboard permissions
    await context.grantPermissions(['clipboard-write', 'clipboard-read']);

    // Copy link
    await page.getByRole('button', { name: /copy/i }).click();

    // Verify clipboard content
    const clipboardContent = await page.evaluate(() => navigator.clipboard.readText());
    expect(clipboardContent).toMatch(/\/wishlist\/shared\/[A-Za-z0-9_-]+/);

    await factory.cleanup();
  });
});
```

---

### Test WISH-E2E-005: Wishlist Header Badge

```typescript
test.describe('Wishlist - Header Badge', () => {
  test('header shows correct wishlist count', async ({ page, request }) => {
    const user = await factory.createUser();
    const products = await Promise.all([
      factory.createProduct({ name: 'Badge Test 1' }),
      factory.createProduct({ name: 'Badge Test 2' })
    ]);

    await loginAs(page, user.email, 'TestPass123!');

    // Initially no badge (empty wishlist)
    const headerWishlist = page.getByTestId('header-wishlist');
    await expect(headerWishlist.locator('.badge')).not.toBeVisible();

    // Add first item
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: products[0].id }
    });

    await page.reload();
    await expect(headerWishlist.locator('.badge')).toHaveText('1');

    // Add second item
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: products[1].id }
    });

    await page.reload();
    await expect(headerWishlist.locator('.badge')).toHaveText('2');

    await factory.cleanup();
  });

  test('header badge updates when item removed', async ({ page, request }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct({ name: 'Badge Remove Test' });

    await loginAs(page, user.email, 'TestPass123!');

    // Add item
    await request.post('/api/v1/wishlist/items', {
      headers: { Authorization: `Bearer ${user.token}` },
      data: { productId: product.id }
    });

    await page.goto('/account/wishlist');
    await expect(page.getByTestId('header-wishlist').locator('.badge')).toHaveText('1');

    // Remove item
    await page.getByTestId('remove-from-wishlist').click();

    // Badge should disappear
    await expect(page.getByTestId('header-wishlist').locator('.badge')).not.toBeVisible();

    await factory.cleanup();
  });
});
```

---

### Test Fixtures and Helpers

**File:** `tests/e2e/wishlist/fixtures/wishlist.fixtures.ts`

```typescript
import { prisma } from '../../../lib/prisma';
import { hash } from 'bcrypt';
import { v4 as uuid } from 'uuid';

export const factory = {
  createdEntities: {
    users: [] as string[],
    products: [] as string[],
    wishlists: [] as string[]
  },

  async createUser(data?: Partial<{
    email: string;
    password: string;
    name: string;
  }>) {
    const id = uuid();
    const email = data?.email ?? `test-${id}@example.com`;
    const passwordHash = await hash(data?.password ?? 'TestPass123!', 10);

    const user = await prisma.user.create({
      data: {
        id,
        email,
        passwordHash,
        name: data?.name ?? 'Test User',
        emailVerified: true
      }
    });

    this.createdEntities.users.push(id);

    // Generate auth token for API requests
    const token = await generateTestToken(user);

    return { ...user, token };
  },

  async createProduct(data?: Partial<{
    name: string;
    slug: string;
    price: number;
    stock: number;
  }>) {
    const id = uuid();
    const name = data?.name ?? `Test Product ${id.slice(0, 8)}`;

    const product = await prisma.product.create({
      data: {
        id,
        name,
        slug: data?.slug ?? name.toLowerCase().replace(/\s+/g, '-'),
        price: data?.price ?? 999.99,
        stock: data?.stock ?? 100,
        isActive: true,
        categoryId: await this.getOrCreateCategory()
      }
    });

    this.createdEntities.products.push(id);

    return product;
  },

  async getOrCreateCategory() {
    let category = await prisma.category.findFirst({ where: { slug: 'test-category' } });

    if (!category) {
      category = await prisma.category.create({
        data: {
          id: uuid(),
          name: 'Test Category',
          slug: 'test-category'
        }
      });
    }

    return category.id;
  },

  async cleanup() {
    // Delete in reverse order of dependencies
    if (this.createdEntities.wishlists.length > 0) {
      await prisma.wishlistItem.deleteMany({
        where: { wishlistId: { in: this.createdEntities.wishlists } }
      });
      await prisma.wishlist.deleteMany({
        where: { id: { in: this.createdEntities.wishlists } }
      });
    }

    if (this.createdEntities.products.length > 0) {
      await prisma.product.deleteMany({
        where: { id: { in: this.createdEntities.products } }
      });
    }

    if (this.createdEntities.users.length > 0) {
      await prisma.user.deleteMany({
        where: { id: { in: this.createdEntities.users } }
      });
    }

    // Reset tracking
    this.createdEntities = { users: [], products: [], wishlists: [] };
  }
};

async function generateTestToken(user: { id: string; email: string }): Promise<string> {
  // Implementation depends on your auth setup
  // This should generate a valid JWT for test requests
}
```

**File:** `tests/e2e/helpers/auth.ts`

```typescript
import { Page } from '@playwright/test';

export async function loginAs(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/auth/login');
  await page.fill('[data-testid="email-input"]', email);
  await page.fill('[data-testid="password-input"]', password);
  await page.click('[data-testid="login-button"]');

  // Wait for redirect to complete
  await page.waitForURL(url => !url.pathname.includes('/auth/'));
}
```

---

## 7. Test Execution Commands

```bash
# Run all wishlist E2E tests
npx playwright test tests/e2e/wishlist/

# Run specific test file
npx playwright test tests/e2e/wishlist/wishlist.spec.ts

# Run with UI mode for debugging
npx playwright test tests/e2e/wishlist/ --ui

# Run with headed browser
npx playwright test tests/e2e/wishlist/ --headed

# Run specific test by name
npx playwright test -g "user can add product to wishlist"

# Generate HTML report
npx playwright test tests/e2e/wishlist/ --reporter=html
```

---

## 8. Implementation Order

### Phase 1: Backend Foundation (Tasks WISH-001 to WISH-005)
1. WISH-001: Database Migration
2. WISH-002: Domain Entities
3. WISH-003: EF Core Configurations
4. WISH-004: Repository Implementation
5. WISH-005: Wishlist Service

### Phase 2: Backend API (Tasks WISH-006 to WISH-010)
6. WISH-006: DTOs and Mapping
7. WISH-007: Controller Implementation
8. WISH-008: Validators
9. WISH-009: Unit Tests
10. WISH-010: Integration Tests

### Phase 3: Frontend Core (Tasks WISH-011 to WISH-014)
11. WISH-011: Wishlist Service
12. WISH-012: Wishlist Button Component
13. WISH-013: Wishlist Page Component
14. WISH-014: Wishlist Item Component

### Phase 4: Frontend Features (Tasks WISH-015 to WISH-019)
15. WISH-015: Share Wishlist Component
16. WISH-016: Shared Wishlist Page
17. WISH-017: Routes Configuration
18. WISH-018: Product Integration
19. WISH-019: Header Integration

### Phase 5: E2E Testing
20. WISH-E2E-001: Add to Wishlist Tests
21. WISH-E2E-002: Remove from Wishlist Tests
22. WISH-E2E-003: Move to Cart Tests
23. WISH-E2E-004: Share Wishlist Tests
24. WISH-E2E-005: Header Badge Tests

---

## 9. Definition of Done

- [ ] All database migrations applied successfully
- [ ] All API endpoints functional with proper authorization
- [ ] All frontend components implemented with proper styling
- [ ] Wishlist button visible on all product cards and detail pages
- [ ] Move to cart functionality working correctly
- [ ] Share wishlist with public link working
- [ ] Header wishlist icon with live count
- [ ] All unit tests passing (>80% coverage)
- [ ] All integration tests passing
- [ ] All E2E tests passing (NO MOCKING)
- [ ] Responsive design verified on mobile/tablet/desktop
- [ ] Accessibility audit passed (WCAG 2.1 AA)
- [ ] Performance metrics within acceptable range
- [ ] Code reviewed and approved
- [ ] Documentation updated
