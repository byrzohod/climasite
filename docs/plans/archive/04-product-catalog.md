# Product Catalog Implementation Plan

## 1. Overview

The Product Catalog is the core module of ClimaSite, providing comprehensive HVAC product management with categories, variants, specifications, and advanced search capabilities. This plan covers the complete implementation from database schema to E2E testing.

### Goals

- Support complex HVAC product hierarchy with categories and subcategories
- Flexible product variants (sizes, capacities, colors)
- HVAC-specific attributes stored in JSONB (BTU, energy ratings, dimensions)
- PostgreSQL full-text search for product discovery
- Real E2E tests with Playwright (no mocking)

### Tech Stack

| Component | Technology |
|-----------|------------|
| Backend | ASP.NET Core .NET 10 |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL 16+ with JSONB |
| Search | PostgreSQL Full-Text Search |
| Frontend | Angular 19+ (standalone components) |
| State | Angular Signals |
| E2E Tests | Playwright |

---

## 2. Database Schema

### 2.1 Categories Table

```sql
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    description TEXT,
    image_url VARCHAR(500),
    icon VARCHAR(50),
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    meta_title VARCHAR(200),
    meta_description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_categories_parent_id ON categories(parent_id);
CREATE INDEX idx_categories_slug ON categories(slug);
CREATE INDEX idx_categories_is_active ON categories(is_active);
CREATE INDEX idx_categories_sort_order ON categories(sort_order);
```

### 2.2 Products Table

```sql
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    short_description VARCHAR(500),
    description TEXT,
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    brand VARCHAR(100),
    model VARCHAR(100),
    base_price DECIMAL(10,2) NOT NULL,
    compare_at_price DECIMAL(10,2),
    cost_price DECIMAL(10,2),
    specifications JSONB DEFAULT '{}',
    features JSONB DEFAULT '[]',
    images JSONB DEFAULT '[]',
    tags TEXT[] DEFAULT '{}',
    search_vector TSVECTOR,
    is_active BOOLEAN DEFAULT TRUE,
    is_featured BOOLEAN DEFAULT FALSE,
    requires_installation BOOLEAN DEFAULT FALSE,
    warranty_months INT DEFAULT 12,
    weight_kg DECIMAL(8,2),
    meta_title VARCHAR(200),
    meta_description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_products_search ON products USING GIN(search_vector);
CREATE INDEX idx_products_category_id ON products(category_id);
CREATE INDEX idx_products_brand ON products(brand);
CREATE INDEX idx_products_slug ON products(slug);
CREATE INDEX idx_products_sku ON products(sku);
CREATE INDEX idx_products_is_active ON products(is_active);
CREATE INDEX idx_products_is_featured ON products(is_featured);
CREATE INDEX idx_products_base_price ON products(base_price);
CREATE INDEX idx_products_tags ON products USING GIN(tags);
CREATE INDEX idx_products_specifications ON products USING GIN(specifications);
CREATE INDEX idx_products_created_at ON products(created_at DESC);

-- Full-text search trigger
CREATE OR REPLACE FUNCTION products_search_vector_update() RETURNS trigger AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('english', COALESCE(NEW.name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.brand, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.model, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.short_description, '')), 'C') ||
        setweight(to_tsvector('english', COALESCE(NEW.description, '')), 'D');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER products_search_vector_trigger
    BEFORE INSERT OR UPDATE ON products
    FOR EACH ROW EXECUTE FUNCTION products_search_vector_update();
```

### 2.3 Product Variants Table

```sql
CREATE TABLE product_variants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    price_adjustment DECIMAL(10,2) DEFAULT 0,
    attributes JSONB DEFAULT '{}',
    stock_quantity INT DEFAULT 0,
    low_stock_threshold INT DEFAULT 5,
    is_active BOOLEAN DEFAULT TRUE,
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_product_variants_product_id ON product_variants(product_id);
CREATE INDEX idx_product_variants_sku ON product_variants(sku);
CREATE INDEX idx_product_variants_is_active ON product_variants(is_active);
CREATE INDEX idx_product_variants_stock ON product_variants(stock_quantity);
CREATE INDEX idx_product_variants_attributes ON product_variants USING GIN(attributes);
```

### 2.4 Product Images Table

```sql
CREATE TABLE product_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id UUID REFERENCES product_variants(id) ON DELETE CASCADE,
    url VARCHAR(500) NOT NULL,
    alt_text VARCHAR(255),
    sort_order INT DEFAULT 0,
    is_primary BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_product_images_product_id ON product_images(product_id);
CREATE INDEX idx_product_images_variant_id ON product_images(variant_id);
CREATE INDEX idx_product_images_is_primary ON product_images(is_primary);
```

### 2.5 Related Products Table

```sql
CREATE TABLE related_products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    related_product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    relation_type VARCHAR(50) NOT NULL, -- 'similar', 'accessory', 'upgrade', 'bundle'
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(product_id, related_product_id, relation_type)
);

-- Indexes
CREATE INDEX idx_related_products_product_id ON related_products(product_id);
CREATE INDEX idx_related_products_related_id ON related_products(related_product_id);
CREATE INDEX idx_related_products_type ON related_products(relation_type);
```

---

## 3. HVAC Category Hierarchy

```
Air Conditioners
├── Split Systems
│   ├── Single Zone
│   └── Multi Zone
├── Window Units
├── Portable AC
├── Central Air
└── Mini Split

Heating Systems
├── Furnaces
│   ├── Gas Furnaces
│   └── Electric Furnaces
├── Heat Pumps
│   ├── Air Source
│   └── Ground Source
├── Boilers
├── Space Heaters
└── Radiant Heating

Ventilation
├── Exhaust Fans
├── Ceiling Fans
├── Air Purifiers
├── Dehumidifiers
├── Humidifiers
└── HRV/ERV Systems

Thermostats & Controls
├── Smart Thermostats
├── Programmable Thermostats
├── Non-Programmable
└── Zone Controllers

Parts & Accessories
├── Filters
├── Refrigerants
├── Ductwork
├── Insulation
├── Tools
└── Mounting Hardware
```

---

## 4. JSONB Specification Examples

### 4.1 Air Conditioner Specifications

```json
{
  "btu": 12000,
  "coolingCapacity": "12000 BTU/h",
  "heatingCapacity": "13000 BTU/h",
  "energyRating": "A++",
  "seer": 21,
  "eer": 12.5,
  "hspf": 10,
  "voltage": "220V",
  "amperage": 15,
  "refrigerantType": "R-32",
  "refrigerantCharge": "1.2 kg",
  "noiseLevel": {
    "indoor": 22,
    "outdoor": 52,
    "unit": "dB"
  },
  "dimensions": {
    "indoor": {
      "width": 998,
      "height": 290,
      "depth": 225,
      "unit": "mm"
    },
    "outdoor": {
      "width": 800,
      "height": 554,
      "depth": 285,
      "unit": "mm"
    }
  },
  "weight": {
    "indoor": 10.5,
    "outdoor": 32,
    "unit": "kg"
  },
  "coverageArea": {
    "min": 30,
    "max": 45,
    "unit": "sqm"
  },
  "features": [
    "Inverter Technology",
    "WiFi Control",
    "Sleep Mode",
    "Turbo Mode",
    "Self-Cleaning",
    "Dehumidification"
  ],
  "certifications": ["Energy Star", "CE", "UL Listed"],
  "operatingTemperature": {
    "cooling": { "min": 18, "max": 43, "unit": "C" },
    "heating": { "min": -15, "max": 24, "unit": "C" }
  }
}
```

### 4.2 Furnace Specifications

```json
{
  "fuelType": "Natural Gas",
  "btuInput": 80000,
  "btuOutput": 76000,
  "afue": 95,
  "stages": 2,
  "voltage": "120V",
  "dimensions": {
    "width": 445,
    "height": 838,
    "depth": 711,
    "unit": "mm"
  },
  "weight": {
    "value": 54,
    "unit": "kg"
  },
  "blowerMotor": "Variable Speed ECM",
  "heatExchanger": "Stainless Steel",
  "ventType": "Direct Vent",
  "filterSize": "16x25x4",
  "certifications": ["Energy Star", "CSA", "AHRI"],
  "coverageArea": {
    "min": 150,
    "max": 200,
    "unit": "sqm"
  }
}
```

### 4.3 Thermostat Specifications

```json
{
  "type": "Smart",
  "display": "Color Touchscreen",
  "displaySize": "3.5 inches",
  "connectivity": ["WiFi", "Bluetooth", "Zigbee"],
  "compatibility": [
    "Single Stage",
    "Multi Stage",
    "Heat Pump",
    "Dual Fuel"
  ],
  "voltage": "24V",
  "sensors": ["Temperature", "Humidity", "Occupancy", "Proximity"],
  "features": [
    "Learning Algorithm",
    "Geofencing",
    "Voice Control",
    "Energy Reports",
    "Remote Sensors Support",
    "Schedule Programming"
  ],
  "voiceAssistants": ["Alexa", "Google Assistant", "Apple HomeKit"],
  "dimensions": {
    "width": 109,
    "height": 109,
    "depth": 25,
    "unit": "mm"
  },
  "batteryBackup": true,
  "c-wireRequired": false
}
```

### 4.4 Product Variant Attributes

```json
{
  "capacity": "12000 BTU",
  "color": "White",
  "refrigerantType": "R-32",
  "installationType": "Wall Mount"
}
```

```json
{
  "capacity": "18000 BTU",
  "color": "Silver",
  "refrigerantType": "R-32",
  "installationType": "Wall Mount"
}
```

### 4.5 Product Features Array

```json
[
  {
    "title": "Inverter Technology",
    "description": "Variable speed compressor for precise temperature control and energy savings",
    "icon": "inverter"
  },
  {
    "title": "WiFi Enabled",
    "description": "Control your AC from anywhere using our mobile app",
    "icon": "wifi"
  },
  {
    "title": "Self-Cleaning",
    "description": "Automatic cleaning function to maintain air quality",
    "icon": "clean"
  }
]
```

### 4.6 Product Images Array

```json
[
  {
    "url": "/images/products/ac-12000-front.webp",
    "alt": "Split AC 12000 BTU Front View",
    "isPrimary": true,
    "sortOrder": 0
  },
  {
    "url": "/images/products/ac-12000-side.webp",
    "alt": "Split AC 12000 BTU Side View",
    "isPrimary": false,
    "sortOrder": 1
  },
  {
    "url": "/images/products/ac-12000-outdoor.webp",
    "alt": "Split AC 12000 BTU Outdoor Unit",
    "isPrimary": false,
    "sortOrder": 2
  }
]
```

---

## 5. API Endpoints

### 5.1 Public Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/products` | List products with filtering, sorting, pagination |
| GET | `/api/v1/products/{slug}` | Get product details by slug |
| GET | `/api/v1/products/{id}/variants` | Get product variants |
| GET | `/api/v1/products/{id}/related` | Get related products |
| GET | `/api/v1/products/search` | Full-text search with filters |
| GET | `/api/v1/products/featured` | Get featured products |
| GET | `/api/v1/categories` | Get category tree |
| GET | `/api/v1/categories/{slug}` | Get category details |
| GET | `/api/v1/categories/{slug}/products` | Get products by category |
| GET | `/api/v1/brands` | List all brands |
| GET | `/api/v1/filters` | Get available filter options |

### 5.2 Admin Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/admin/products` | Create product |
| PUT | `/api/v1/admin/products/{id}` | Update product |
| DELETE | `/api/v1/admin/products/{id}` | Delete product (soft) |
| PATCH | `/api/v1/admin/products/{id}/status` | Toggle product status |
| POST | `/api/v1/admin/products/{id}/variants` | Add variant |
| PUT | `/api/v1/admin/products/{id}/variants/{variantId}` | Update variant |
| DELETE | `/api/v1/admin/products/{id}/variants/{variantId}` | Delete variant |
| POST | `/api/v1/admin/products/{id}/images` | Upload images |
| DELETE | `/api/v1/admin/products/{id}/images/{imageId}` | Delete image |
| POST | `/api/v1/admin/categories` | Create category |
| PUT | `/api/v1/admin/categories/{id}` | Update category |
| DELETE | `/api/v1/admin/categories/{id}` | Delete category |
| PATCH | `/api/v1/admin/categories/reorder` | Reorder categories |
| POST | `/api/v1/admin/products/bulk-import` | Bulk import products |
| GET | `/api/v1/admin/products/export` | Export products |

### 5.3 Query Parameters

#### GET /api/v1/products

```
?page=1
&pageSize=20
&sort=price_asc|price_desc|name_asc|name_desc|newest|popular
&category=air-conditioners
&brand=daikin,mitsubishi
&minPrice=500
&maxPrice=2000
&inStock=true
&specs[btu]=12000,18000
&specs[energyRating]=A++,A+
&tags=inverter,wifi
```

#### GET /api/v1/products/search

```
?q=split air conditioner 12000 btu
&page=1
&pageSize=20
&category=air-conditioners
&brand=daikin
&minPrice=500
&maxPrice=2000
```

---

## 6. Implementation Tasks

### Phase 1: Database & Core Entities

#### Task CAT-001: Database Schema & Migrations

**Priority:** Critical
**Estimated Hours:** 8
**Dependencies:** None

**Description:**
Create EF Core migrations for all product catalog tables with proper indexes and constraints.

**Acceptance Criteria:**
- [ ] Categories table with self-referencing parent relationship
- [ ] Products table with JSONB columns for specifications, features, images
- [ ] ProductVariants table with JSONB attributes
- [ ] ProductImages table for additional images
- [ ] RelatedProducts table for cross-selling
- [ ] All indexes created for performance
- [ ] Full-text search trigger implemented
- [ ] Migration runs successfully on clean database
- [ ] Rollback migration works correctly

**Technical Notes:**
```csharp
// Example EF Core configuration
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Specifications)
            .HasColumnName("specifications")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions.Default)
            );

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Sku).IsUnique();
    }
}
```

---

#### Task CAT-002: Domain Entities

**Priority:** Critical
**Estimated Hours:** 6
**Dependencies:** None

**Description:**
Create domain entities in ClimaSite.Core with proper encapsulation and validation.

**Acceptance Criteria:**
- [ ] Category entity with tree navigation methods
- [ ] Product entity with specification accessors
- [ ] ProductVariant entity with price calculation
- [ ] ProductImage entity
- [ ] RelatedProduct entity with relation types
- [ ] Value objects for Money, Dimensions, Weight
- [ ] Unit tests for domain logic

**Files to Create:**
- `src/ClimaSite.Core/Entities/Category.cs`
- `src/ClimaSite.Core/Entities/Product.cs`
- `src/ClimaSite.Core/Entities/ProductVariant.cs`
- `src/ClimaSite.Core/Entities/ProductImage.cs`
- `src/ClimaSite.Core/Entities/RelatedProduct.cs`
- `src/ClimaSite.Core/ValueObjects/Money.cs`
- `src/ClimaSite.Core/ValueObjects/Dimensions.cs`

---

#### Task CAT-003: EF Core Configurations

**Priority:** Critical
**Estimated Hours:** 6
**Dependencies:** CAT-001, CAT-002

**Description:**
Create Fluent API configurations for all entities with proper JSONB handling.

**Acceptance Criteria:**
- [ ] CategoryConfiguration with self-join
- [ ] ProductConfiguration with JSONB columns
- [ ] ProductVariantConfiguration with cascade delete
- [ ] ProductImageConfiguration
- [ ] RelatedProductConfiguration with composite unique constraint
- [ ] Proper snake_case naming convention
- [ ] JSONB serialization/deserialization configured

---

### Phase 2: Repository & Services

#### Task CAT-004: Product Repository

**Priority:** Critical
**Estimated Hours:** 10
**Dependencies:** CAT-003

**Description:**
Implement repository pattern for product data access with full-text search.

**Acceptance Criteria:**
- [ ] IProductRepository interface in Core
- [ ] ProductRepository implementation in Infrastructure
- [ ] Full-text search with ranking
- [ ] Filtering by category (including children)
- [ ] Filtering by brand, price range, specifications
- [ ] Sorting by price, name, date, popularity
- [ ] Pagination with total count
- [ ] Include variants and images options
- [ ] Unit tests with in-memory database

**Repository Interface:**
```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResult<Product>> GetPagedAsync(ProductFilterRequest filter, CancellationToken ct = default);
    Task<PagedResult<Product>> SearchAsync(ProductSearchRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, bool includeChildren, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetRelatedAsync(Guid productId, string relationType, CancellationToken ct = default);
    Task<Product> AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

---

#### Task CAT-005: Category Repository

**Priority:** High
**Estimated Hours:** 6
**Dependencies:** CAT-003

**Description:**
Implement repository for category management with tree operations.

**Acceptance Criteria:**
- [ ] ICategoryRepository interface
- [ ] Get full category tree
- [ ] Get category by slug with ancestors
- [ ] Get category children (recursive)
- [ ] Get category product count
- [ ] Reorder categories
- [ ] Prevent circular references
- [ ] Unit tests

---

#### Task CAT-006: Product Service

**Priority:** High
**Estimated Hours:** 8
**Dependencies:** CAT-004, CAT-005

**Description:**
Business logic layer for product operations.

**Acceptance Criteria:**
- [ ] IProductService interface
- [ ] Get product with full details
- [ ] List products with filters
- [ ] Search products with highlighting
- [ ] Get filter options (brands, specs, price ranges)
- [ ] Validate product data
- [ ] Generate unique slugs
- [ ] Handle variant pricing
- [ ] Unit tests with mocked repository

---

#### Task CAT-007: Category Service

**Priority:** High
**Estimated Hours:** 4
**Dependencies:** CAT-005

**Description:**
Business logic for category management.

**Acceptance Criteria:**
- [ ] ICategoryService interface
- [ ] Build category tree for navigation
- [ ] Get breadcrumb path
- [ ] Validate category hierarchy
- [ ] Generate slugs
- [ ] Unit tests

---

### Phase 3: API Endpoints

#### Task CAT-008: Products Controller (Public)

**Priority:** Critical
**Estimated Hours:** 8
**Dependencies:** CAT-006

**Description:**
Implement public API endpoints for product browsing.

**Acceptance Criteria:**
- [ ] GET /api/v1/products with all filter parameters
- [ ] GET /api/v1/products/{slug} with full details
- [ ] GET /api/v1/products/{id}/variants
- [ ] GET /api/v1/products/{id}/related
- [ ] GET /api/v1/products/featured
- [ ] GET /api/v1/products/search
- [ ] Response caching headers
- [ ] OpenAPI documentation
- [ ] Integration tests

**Response DTOs:**
```csharp
public record ProductListItemDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? ShortDescription,
    string Brand,
    decimal BasePrice,
    decimal? CompareAtPrice,
    string PrimaryImageUrl,
    bool InStock,
    double? AverageRating,
    int ReviewCount
);

public record ProductDetailDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    CategoryBreadcrumbDto Category,
    string Brand,
    string? Model,
    decimal BasePrice,
    decimal? CompareAtPrice,
    Dictionary<string, object> Specifications,
    List<ProductFeatureDto> Features,
    List<ProductImageDto> Images,
    List<ProductVariantDto> Variants,
    List<string> Tags,
    bool RequiresInstallation,
    int WarrantyMonths,
    DateTime CreatedAt
);
```

---

#### Task CAT-009: Categories Controller (Public)

**Priority:** Critical
**Estimated Hours:** 4
**Dependencies:** CAT-007

**Description:**
Implement public API endpoints for category navigation.

**Acceptance Criteria:**
- [ ] GET /api/v1/categories (tree structure)
- [ ] GET /api/v1/categories/{slug} (single category with children)
- [ ] GET /api/v1/categories/{slug}/products
- [ ] Response caching
- [ ] OpenAPI documentation
- [ ] Integration tests

---

#### Task CAT-010: Admin Products Controller

**Priority:** High
**Estimated Hours:** 10
**Dependencies:** CAT-008

**Description:**
Implement admin API endpoints for product management.

**Acceptance Criteria:**
- [ ] POST /api/v1/admin/products
- [ ] PUT /api/v1/admin/products/{id}
- [ ] DELETE /api/v1/admin/products/{id}
- [ ] PATCH /api/v1/admin/products/{id}/status
- [ ] Variant CRUD endpoints
- [ ] Image management endpoints
- [ ] Authorization (Admin role required)
- [ ] FluentValidation for requests
- [ ] Integration tests

**Request DTOs:**
```csharp
public record CreateProductRequest(
    string Sku,
    string Name,
    string? ShortDescription,
    string? Description,
    Guid? CategoryId,
    string Brand,
    string? Model,
    decimal BasePrice,
    decimal? CompareAtPrice,
    decimal? CostPrice,
    Dictionary<string, object>? Specifications,
    List<ProductFeatureRequest>? Features,
    List<string>? Tags,
    bool RequiresInstallation,
    int WarrantyMonths,
    decimal? WeightKg
);

public record CreateVariantRequest(
    string Sku,
    string Name,
    decimal PriceAdjustment,
    Dictionary<string, object> Attributes,
    int StockQuantity,
    int LowStockThreshold
);
```

---

#### Task CAT-011: Admin Categories Controller

**Priority:** High
**Estimated Hours:** 6
**Dependencies:** CAT-009

**Description:**
Implement admin API endpoints for category management.

**Acceptance Criteria:**
- [ ] POST /api/v1/admin/categories
- [ ] PUT /api/v1/admin/categories/{id}
- [ ] DELETE /api/v1/admin/categories/{id}
- [ ] PATCH /api/v1/admin/categories/reorder
- [ ] Prevent deletion of categories with products
- [ ] Authorization
- [ ] Integration tests

---

#### Task CAT-012: Filters Endpoint

**Priority:** Medium
**Estimated Hours:** 4
**Dependencies:** CAT-004

**Description:**
Implement endpoint to get available filter options.

**Acceptance Criteria:**
- [ ] GET /api/v1/filters
- [ ] Return available brands
- [ ] Return price range (min/max)
- [ ] Return specification options (BTU ranges, energy ratings)
- [ ] Filter options based on current category
- [ ] Caching strategy

**Response:**
```json
{
  "brands": [
    { "name": "Daikin", "count": 45 },
    { "name": "Mitsubishi", "count": 38 }
  ],
  "priceRange": { "min": 299.99, "max": 4999.99 },
  "specifications": {
    "btu": [
      { "value": "9000", "label": "9,000 BTU", "count": 12 },
      { "value": "12000", "label": "12,000 BTU", "count": 28 }
    ],
    "energyRating": [
      { "value": "A++", "count": 35 },
      { "value": "A+", "count": 42 }
    ]
  },
  "features": [
    { "name": "WiFi Control", "count": 56 },
    { "name": "Inverter", "count": 72 }
  ]
}
```

---

#### Task CAT-013: Bulk Import/Export

**Priority:** Low
**Estimated Hours:** 8
**Dependencies:** CAT-010

**Description:**
Implement bulk product import and export functionality.

**Acceptance Criteria:**
- [ ] POST /api/v1/admin/products/bulk-import (CSV/JSON)
- [ ] GET /api/v1/admin/products/export
- [ ] Validate all products before import
- [ ] Return detailed error report
- [ ] Background processing for large files
- [ ] Progress tracking

---

### Phase 4: Angular Frontend

#### Task CAT-014: Product Models & Services

**Priority:** Critical
**Estimated Hours:** 6
**Dependencies:** CAT-008, CAT-009

**Description:**
Create TypeScript models and API services for product catalog.

**Acceptance Criteria:**
- [ ] Product, Category, Variant interfaces
- [ ] ProductService with all API calls
- [ ] CategoryService
- [ ] Proper error handling
- [ ] Request/response type safety
- [ ] Unit tests

**Files to Create:**
- `src/ClimaSite.Web/src/app/core/models/product.model.ts`
- `src/ClimaSite.Web/src/app/core/models/category.model.ts`
- `src/ClimaSite.Web/src/app/core/services/product.service.ts`
- `src/ClimaSite.Web/src/app/core/services/category.service.ts`

---

#### Task CAT-015: Product List Component

**Priority:** Critical
**Estimated Hours:** 10
**Dependencies:** CAT-014

**Description:**
Create product listing component with filtering and pagination.

**Acceptance Criteria:**
- [ ] Standalone component
- [ ] Grid/List view toggle
- [ ] Responsive product cards
- [ ] Price display with compare price
- [ ] Stock status indicator
- [ ] Quick add to cart button
- [ ] Infinite scroll or pagination
- [ ] Loading skeletons
- [ ] Empty state
- [ ] Unit tests

---

#### Task CAT-016: Product Filter Component

**Priority:** Critical
**Estimated Hours:** 8
**Dependencies:** CAT-014

**Description:**
Create filter sidebar/panel for product filtering.

**Acceptance Criteria:**
- [ ] Category filter (tree navigation)
- [ ] Brand filter (checkboxes)
- [ ] Price range slider
- [ ] Specification filters (BTU, energy rating)
- [ ] Active filters display
- [ ] Clear all filters
- [ ] Mobile-friendly (drawer)
- [ ] URL query parameter sync
- [ ] Unit tests

---

#### Task CAT-017: Product Detail Component

**Priority:** Critical
**Estimated Hours:** 12
**Dependencies:** CAT-014

**Description:**
Create comprehensive product detail page.

**Acceptance Criteria:**
- [ ] Image gallery with zoom
- [ ] Variant selector
- [ ] Dynamic price update
- [ ] Specifications table
- [ ] Features list with icons
- [ ] Add to cart with quantity
- [ ] Stock status
- [ ] Delivery information
- [ ] Related products carousel
- [ ] Breadcrumb navigation
- [ ] SEO meta tags
- [ ] Structured data (JSON-LD)
- [ ] Unit tests

---

#### Task CAT-018: Category Navigation Component

**Priority:** High
**Estimated Hours:** 6
**Dependencies:** CAT-014

**Description:**
Create category navigation components.

**Acceptance Criteria:**
- [ ] Mega menu for desktop
- [ ] Collapsible tree for mobile
- [ ] Category icons
- [ ] Product count badges
- [ ] Active state indication
- [ ] Keyboard navigation
- [ ] Unit tests

---

#### Task CAT-019: Search Component

**Priority:** High
**Estimated Hours:** 8
**Dependencies:** CAT-014

**Description:**
Create search functionality with autocomplete.

**Acceptance Criteria:**
- [ ] Search input with icon
- [ ] Debounced API calls
- [ ] Autocomplete dropdown
- [ ] Recent searches (localStorage)
- [ ] Search results page
- [ ] Highlighting matched terms
- [ ] No results state
- [ ] Keyboard navigation
- [ ] Unit tests

---

#### Task CAT-020: Product Card Component

**Priority:** High
**Estimated Hours:** 4
**Dependencies:** None

**Description:**
Create reusable product card component.

**Acceptance Criteria:**
- [ ] Product image with hover effect
- [ ] Product name and brand
- [ ] Price with sale indicator
- [ ] Rating stars
- [ ] Quick view button
- [ ] Add to cart button
- [ ] Wishlist toggle
- [ ] Responsive design
- [ ] Unit tests

---

#### Task CAT-021: Product Comparison Component

**Priority:** Medium
**Estimated Hours:** 8
**Dependencies:** CAT-017

**Description:**
Create product comparison feature.

**Acceptance Criteria:**
- [ ] Add to compare button on cards
- [ ] Compare bar at bottom
- [ ] Side-by-side comparison table
- [ ] Highlight differences
- [ ] Maximum 4 products
- [ ] Persist in localStorage
- [ ] Remove from comparison
- [ ] Unit tests

---

### Phase 5: Admin Frontend

#### Task CAT-022: Admin Product List Component

**Priority:** High
**Estimated Hours:** 8
**Dependencies:** CAT-014

**Description:**
Create admin product management list.

**Acceptance Criteria:**
- [ ] Data table with sorting
- [ ] Search and filters
- [ ] Bulk selection
- [ ] Bulk actions (activate, deactivate, delete)
- [ ] Quick edit inline
- [ ] Status toggle
- [ ] Export button
- [ ] Unit tests

---

#### Task CAT-023: Admin Product Form Component

**Priority:** High
**Estimated Hours:** 12
**Dependencies:** CAT-022

**Description:**
Create product create/edit form.

**Acceptance Criteria:**
- [ ] Multi-step form or tabs
- [ ] Basic info section
- [ ] Pricing section
- [ ] Specifications builder (dynamic fields)
- [ ] Features builder
- [ ] Image upload with drag-drop
- [ ] Variant management
- [ ] SEO section
- [ ] Form validation
- [ ] Autosave draft
- [ ] Preview mode
- [ ] Unit tests

---

#### Task CAT-024: Admin Category Manager Component

**Priority:** High
**Estimated Hours:** 8
**Dependencies:** CAT-014

**Description:**
Create category management interface.

**Acceptance Criteria:**
- [ ] Tree view with drag-drop reorder
- [ ] Inline editing
- [ ] Add child category
- [ ] Delete with confirmation
- [ ] Image upload
- [ ] Product count display
- [ ] Unit tests

---

#### Task CAT-025: Specification Builder Component

**Priority:** Medium
**Estimated Hours:** 6
**Dependencies:** CAT-023

**Description:**
Create dynamic specification builder for admin.

**Acceptance Criteria:**
- [ ] Template selection by category
- [ ] Add custom fields
- [ ] Field types (text, number, select, range)
- [ ] Nested objects (dimensions)
- [ ] Array fields (features)
- [ ] Validation rules
- [ ] Unit tests

---

### Phase 6: Testing & Performance

#### Task CAT-026: Unit Tests - Backend

**Priority:** High
**Estimated Hours:** 12
**Dependencies:** CAT-001 through CAT-013

**Description:**
Comprehensive unit tests for backend components.

**Acceptance Criteria:**
- [ ] Domain entity tests
- [ ] Repository tests (in-memory DB)
- [ ] Service tests (mocked dependencies)
- [ ] Controller tests
- [ ] Validation tests
- [ ] Minimum 80% code coverage

---

#### Task CAT-027: Integration Tests - API

**Priority:** High
**Estimated Hours:** 10
**Dependencies:** CAT-026

**Description:**
API integration tests with test database.

**Acceptance Criteria:**
- [ ] Test all public endpoints
- [ ] Test all admin endpoints
- [ ] Test authentication/authorization
- [ ] Test error responses
- [ ] Test pagination
- [ ] Test search functionality
- [ ] Use WebApplicationFactory

---

#### Task CAT-028: Unit Tests - Frontend

**Priority:** High
**Estimated Hours:** 10
**Dependencies:** CAT-014 through CAT-025

**Description:**
Unit tests for Angular components and services.

**Acceptance Criteria:**
- [ ] Service tests with HttpClientTestingModule
- [ ] Component tests with TestBed
- [ ] Store/signal tests
- [ ] Pipe tests
- [ ] Minimum 70% code coverage

---

#### Task CAT-029: Performance Optimization

**Priority:** Medium
**Estimated Hours:** 8
**Dependencies:** CAT-027

**Description:**
Optimize product catalog performance.

**Acceptance Criteria:**
- [ ] Query optimization with EXPLAIN ANALYZE
- [ ] Response caching strategy
- [ ] Image lazy loading
- [ ] Virtual scrolling for large lists
- [ ] Bundle size optimization
- [ ] Database index tuning
- [ ] Load testing results

---

#### Task CAT-030: Search Optimization

**Priority:** Medium
**Estimated Hours:** 6
**Dependencies:** CAT-004

**Description:**
Optimize full-text search performance and relevance.

**Acceptance Criteria:**
- [ ] Custom text search configuration
- [ ] Synonym support
- [ ] Search ranking tuning
- [ ] Facet count optimization
- [ ] Search analytics logging

---

## 7. E2E Tests (Playwright - NO MOCKING)

All E2E tests create REAL data through the API and verify through the UI.

### Test CAT-E2E-001: Browse Products by Category

```typescript
import { test, expect } from '@playwright/test';
import { createTestCategory, createTestProduct, cleanupTestData } from './helpers/test-data';

test.describe('Product Catalog - Browse by Category', () => {
  let testCategory: { id: string; slug: string };
  let testProduct: { id: string; name: string };

  test.beforeEach(async ({ request }) => {
    // Create real test data via API
    testCategory = await createTestCategory(request, {
      name: `Test AC Units ${Date.now()}`,
      slug: `test-ac-units-${Date.now()}`,
      description: 'Air conditioning units for testing'
    });

    testProduct = await createTestProduct(request, {
      name: 'Test Split AC 12000 BTU',
      sku: `SKU-${Date.now()}`,
      categoryId: testCategory.id,
      brand: 'TestBrand',
      basePrice: 599.99,
      specifications: {
        btu: 12000,
        energyRating: 'A++',
        seer: 21
      }
    });
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request, {
      productIds: [testProduct.id],
      categoryIds: [testCategory.id]
    });
  });

  test('user can browse products by category', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Verify category page loaded
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Test AC Units');

    // Verify product is displayed
    await expect(page.getByText('Test Split AC 12000 BTU')).toBeVisible();
    await expect(page.getByText('$599.99')).toBeVisible();
    await expect(page.getByText('TestBrand')).toBeVisible();
  });

  test('product card shows correct information', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    const productCard = page.locator('[data-testid="product-card"]').first();

    await expect(productCard.getByText('Test Split AC 12000 BTU')).toBeVisible();
    await expect(productCard.getByText('$599.99')).toBeVisible();
    await expect(productCard.locator('[data-testid="energy-rating"]')).toContainText('A++');
  });
});
```

### Test CAT-E2E-002: View Product Details

```typescript
import { test, expect } from '@playwright/test';
import { createTestCategory, createTestProduct, createTestVariant, cleanupTestData } from './helpers/test-data';

test.describe('Product Catalog - Product Details', () => {
  let testCategory: { id: string; slug: string };
  let testProduct: { id: string; slug: string };
  let testVariants: { id: string }[];

  test.beforeEach(async ({ request }) => {
    testCategory = await createTestCategory(request, {
      name: `Test Category ${Date.now()}`,
      slug: `test-cat-${Date.now()}`
    });

    testProduct = await createTestProduct(request, {
      name: 'Daikin Inverter Split AC',
      slug: `daikin-inverter-${Date.now()}`,
      sku: `DAI-${Date.now()}`,
      categoryId: testCategory.id,
      brand: 'Daikin',
      model: 'FTXM35R',
      basePrice: 899.99,
      compareAtPrice: 1099.99,
      shortDescription: 'Energy-efficient inverter air conditioner',
      description: 'Premium split system with advanced inverter technology',
      specifications: {
        btu: 12000,
        energyRating: 'A+++',
        seer: 23,
        noiseLevel: { indoor: 19, outdoor: 48, unit: 'dB' },
        dimensions: {
          indoor: { width: 998, height: 290, depth: 225, unit: 'mm' }
        }
      },
      features: [
        { title: 'Inverter Technology', description: 'Variable speed compressor' },
        { title: 'WiFi Control', description: 'Control via smartphone app' }
      ],
      warrantyMonths: 24,
      requiresInstallation: true
    });

    // Create variants
    testVariants = [
      await createTestVariant(request, testProduct.id, {
        sku: `DAI-12K-${Date.now()}`,
        name: '12000 BTU',
        priceAdjustment: 0,
        attributes: { capacity: '12000 BTU', color: 'White' },
        stockQuantity: 15
      }),
      await createTestVariant(request, testProduct.id, {
        sku: `DAI-18K-${Date.now()}`,
        name: '18000 BTU',
        priceAdjustment: 200,
        attributes: { capacity: '18000 BTU', color: 'White' },
        stockQuantity: 8
      })
    ];
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request, {
      productIds: [testProduct.id],
      categoryIds: [testCategory.id]
    });
  });

  test('displays full product details', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Basic info
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Daikin Inverter Split AC');
    await expect(page.getByText('Daikin')).toBeVisible();
    await expect(page.getByText('FTXM35R')).toBeVisible();

    // Pricing
    await expect(page.getByTestId('current-price')).toContainText('$899.99');
    await expect(page.getByTestId('compare-price')).toContainText('$1,099.99');

    // Description
    await expect(page.getByText('Energy-efficient inverter air conditioner')).toBeVisible();
  });

  test('displays specifications correctly', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Click specs tab if tabbed layout
    const specsTab = page.getByRole('tab', { name: /specifications/i });
    if (await specsTab.isVisible()) {
      await specsTab.click();
    }

    await expect(page.getByText('12,000 BTU')).toBeVisible();
    await expect(page.getByText('A+++')).toBeVisible();
    await expect(page.getByText('SEER 23')).toBeVisible();
    await expect(page.getByText('19 dB')).toBeVisible();
  });

  test('variant selection updates price', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    // Select 18000 BTU variant
    await page.getByRole('button', { name: '18000 BTU' }).click();

    // Price should update
    await expect(page.getByTestId('current-price')).toContainText('$1,099.99');
  });

  test('displays features list', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    await expect(page.getByText('Inverter Technology')).toBeVisible();
    await expect(page.getByText('Variable speed compressor')).toBeVisible();
    await expect(page.getByText('WiFi Control')).toBeVisible();
  });

  test('shows warranty and installation info', async ({ page }) => {
    await page.goto(`/products/${testProduct.slug}`);

    await expect(page.getByText('24 months warranty')).toBeVisible();
    await expect(page.getByText(/installation required/i)).toBeVisible();
  });
});
```

### Test CAT-E2E-003: Search Products

```typescript
import { test, expect } from '@playwright/test';
import { createTestCategory, createTestProduct, cleanupTestData } from './helpers/test-data';

test.describe('Product Catalog - Search', () => {
  let testCategory: { id: string };
  let testProducts: { id: string; name: string }[];

  test.beforeEach(async ({ request }) => {
    testCategory = await createTestCategory(request, {
      name: `Search Test Category ${Date.now()}`,
      slug: `search-test-${Date.now()}`
    });

    testProducts = [
      await createTestProduct(request, {
        name: 'Mitsubishi Electric MSZ-LN25',
        sku: `MIT-LN25-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'Mitsubishi Electric',
        basePrice: 1299.99,
        specifications: { btu: 9000, energyRating: 'A+++' }
      }),
      await createTestProduct(request, {
        name: 'Mitsubishi Heavy SRK35ZS',
        sku: `MIT-SRK35-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'Mitsubishi Heavy',
        basePrice: 899.99,
        specifications: { btu: 12000, energyRating: 'A++' }
      }),
      await createTestProduct(request, {
        name: 'LG DualCool Inverter',
        sku: `LG-DC-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'LG',
        basePrice: 749.99,
        specifications: { btu: 12000, energyRating: 'A+' }
      })
    ];
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request, {
      productIds: testProducts.map(p => p.id),
      categoryIds: [testCategory.id]
    });
  });

  test('search returns matching products', async ({ page }) => {
    await page.goto('/');

    // Perform search
    await page.getByRole('searchbox').fill('Mitsubishi');
    await page.getByRole('searchbox').press('Enter');

    // Should show Mitsubishi products
    await expect(page.getByText('Mitsubishi Electric MSZ-LN25')).toBeVisible();
    await expect(page.getByText('Mitsubishi Heavy SRK35ZS')).toBeVisible();

    // Should NOT show LG product
    await expect(page.getByText('LG DualCool Inverter')).not.toBeVisible();
  });

  test('search by BTU returns correct products', async ({ page }) => {
    await page.goto('/');

    await page.getByRole('searchbox').fill('12000 BTU');
    await page.getByRole('searchbox').press('Enter');

    // Should show 12000 BTU products
    await expect(page.getByText('Mitsubishi Heavy SRK35ZS')).toBeVisible();
    await expect(page.getByText('LG DualCool Inverter')).toBeVisible();

    // Should NOT show 9000 BTU product
    await expect(page.getByText('Mitsubishi Electric MSZ-LN25')).not.toBeVisible();
  });

  test('autocomplete shows suggestions', async ({ page }) => {
    await page.goto('/');

    await page.getByRole('searchbox').fill('Mitsub');

    // Wait for autocomplete dropdown
    const autocomplete = page.getByTestId('search-autocomplete');
    await expect(autocomplete).toBeVisible();

    await expect(autocomplete.getByText('Mitsubishi Electric MSZ-LN25')).toBeVisible();
    await expect(autocomplete.getByText('Mitsubishi Heavy SRK35ZS')).toBeVisible();
  });

  test('no results shows appropriate message', async ({ page }) => {
    await page.goto('/');

    await page.getByRole('searchbox').fill('NonExistentProduct12345');
    await page.getByRole('searchbox').press('Enter');

    await expect(page.getByText(/no products found/i)).toBeVisible();
  });
});
```

### Test CAT-E2E-004: Filter Products

```typescript
import { test, expect } from '@playwright/test';
import { createTestCategory, createTestProduct, cleanupTestData } from './helpers/test-data';

test.describe('Product Catalog - Filters', () => {
  let testCategory: { id: string; slug: string };
  let testProducts: { id: string }[];

  test.beforeEach(async ({ request }) => {
    testCategory = await createTestCategory(request, {
      name: `Filter Test Category ${Date.now()}`,
      slug: `filter-test-${Date.now()}`
    });

    testProducts = [
      await createTestProduct(request, {
        name: 'Budget AC Unit',
        sku: `BUD-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'BudgetBrand',
        basePrice: 299.99,
        specifications: { btu: 9000, energyRating: 'A' }
      }),
      await createTestProduct(request, {
        name: 'Mid-Range AC Unit',
        sku: `MID-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'MidBrand',
        basePrice: 599.99,
        specifications: { btu: 12000, energyRating: 'A+' }
      }),
      await createTestProduct(request, {
        name: 'Premium AC Unit',
        sku: `PRE-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'PremiumBrand',
        basePrice: 1299.99,
        specifications: { btu: 18000, energyRating: 'A+++' }
      })
    ];
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request, {
      productIds: testProducts.map(p => p.id),
      categoryIds: [testCategory.id]
    });
  });

  test('filter by price range', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Set price filter
    await page.getByLabel('Min Price').fill('400');
    await page.getByLabel('Max Price').fill('800');
    await page.getByRole('button', { name: /apply/i }).click();

    // Should only show mid-range product
    await expect(page.getByText('Mid-Range AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();
    await expect(page.getByText('Premium AC Unit')).not.toBeVisible();
  });

  test('filter by brand', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Check brand filter
    await page.getByLabel('PremiumBrand').check();

    // Should only show premium product
    await expect(page.getByText('Premium AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();
    await expect(page.getByText('Mid-Range AC Unit')).not.toBeVisible();
  });

  test('filter by energy rating', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Filter by A+ or higher
    await page.getByLabel('A+').check();
    await page.getByLabel('A+++').check();

    // Should show mid and premium
    await expect(page.getByText('Mid-Range AC Unit')).toBeVisible();
    await expect(page.getByText('Premium AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();
  });

  test('filter by BTU range', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Filter by 12000+ BTU
    await page.getByLabel('12000 BTU').check();
    await page.getByLabel('18000 BTU').check();

    await expect(page.getByText('Mid-Range AC Unit')).toBeVisible();
    await expect(page.getByText('Premium AC Unit')).toBeVisible();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();
  });

  test('clear all filters', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Apply filters
    await page.getByLabel('PremiumBrand').check();
    await expect(page.getByText('Budget AC Unit')).not.toBeVisible();

    // Clear filters
    await page.getByRole('button', { name: /clear all/i }).click();

    // All products should be visible
    await expect(page.getByText('Budget AC Unit')).toBeVisible();
    await expect(page.getByText('Mid-Range AC Unit')).toBeVisible();
    await expect(page.getByText('Premium AC Unit')).toBeVisible();
  });

  test('filters persist in URL', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    // Apply brand filter
    await page.getByLabel('PremiumBrand').check();

    // URL should contain filter
    await expect(page).toHaveURL(/brand=PremiumBrand/);

    // Reload page
    await page.reload();

    // Filter should still be applied
    await expect(page.getByLabel('PremiumBrand')).toBeChecked();
    await expect(page.getByText('Premium AC Unit')).toBeVisible();
  });
});
```

### Test CAT-E2E-005: Sort Products

```typescript
import { test, expect } from '@playwright/test';
import { createTestCategory, createTestProduct, cleanupTestData } from './helpers/test-data';

test.describe('Product Catalog - Sorting', () => {
  let testCategory: { id: string; slug: string };
  let testProducts: { id: string }[];

  test.beforeEach(async ({ request }) => {
    testCategory = await createTestCategory(request, {
      name: `Sort Test Category ${Date.now()}`,
      slug: `sort-test-${Date.now()}`
    });

    testProducts = [
      await createTestProduct(request, {
        name: 'Alpha Product',
        sku: `ALPHA-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'TestBrand',
        basePrice: 500.00
      }),
      await createTestProduct(request, {
        name: 'Beta Product',
        sku: `BETA-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'TestBrand',
        basePrice: 300.00
      }),
      await createTestProduct(request, {
        name: 'Gamma Product',
        sku: `GAMMA-${Date.now()}`,
        categoryId: testCategory.id,
        brand: 'TestBrand',
        basePrice: 800.00
      })
    ];
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request, {
      productIds: testProducts.map(p => p.id),
      categoryIds: [testCategory.id]
    });
  });

  test('sort by price low to high', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    await page.getByRole('combobox', { name: /sort/i }).selectOption('price_asc');

    const productCards = page.locator('[data-testid="product-card"]');
    const prices = await productCards.locator('[data-testid="product-price"]').allTextContents();

    // Verify order: 300, 500, 800
    expect(parseFloat(prices[0].replace('$', ''))).toBe(300.00);
    expect(parseFloat(prices[1].replace('$', ''))).toBe(500.00);
    expect(parseFloat(prices[2].replace('$', ''))).toBe(800.00);
  });

  test('sort by price high to low', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    await page.getByRole('combobox', { name: /sort/i }).selectOption('price_desc');

    const productCards = page.locator('[data-testid="product-card"]');
    const prices = await productCards.locator('[data-testid="product-price"]').allTextContents();

    // Verify order: 800, 500, 300
    expect(parseFloat(prices[0].replace('$', ''))).toBe(800.00);
    expect(parseFloat(prices[1].replace('$', ''))).toBe(500.00);
    expect(parseFloat(prices[2].replace('$', ''))).toBe(300.00);
  });

  test('sort by name A-Z', async ({ page }) => {
    await page.goto(`/categories/${testCategory.slug}`);

    await page.getByRole('combobox', { name: /sort/i }).selectOption('name_asc');

    const productCards = page.locator('[data-testid="product-card"]');
    const names = await productCards.locator('[data-testid="product-name"]').allTextContents();

    expect(names[0]).toContain('Alpha');
    expect(names[1]).toContain('Beta');
    expect(names[2]).toContain('Gamma');
  });
});
```

### Test CAT-E2E-006: Admin Create Product

```typescript
import { test, expect } from '@playwright/test';
import { createTestCategory, cleanupTestData, loginAsAdmin } from './helpers/test-data';

test.describe('Admin - Create Product', () => {
  let testCategory: { id: string; name: string };
  let createdProductId: string | null = null;

  test.beforeEach(async ({ page, request }) => {
    await loginAsAdmin(page);

    testCategory = await createTestCategory(request, {
      name: `Admin Test Category ${Date.now()}`,
      slug: `admin-test-${Date.now()}`
    });
  });

  test.afterEach(async ({ request }) => {
    const cleanupIds: string[] = [];
    if (createdProductId) cleanupIds.push(createdProductId);

    await cleanupTestData(request, {
      productIds: cleanupIds,
      categoryIds: [testCategory.id]
    });
  });

  test('admin can create a new product', async ({ page, request }) => {
    await page.goto('/admin/products/new');

    // Fill basic info
    await page.getByLabel('SKU').fill(`NEW-PROD-${Date.now()}`);
    await page.getByLabel('Name').fill('New Test Air Conditioner');
    await page.getByLabel('Brand').fill('TestBrand');
    await page.getByLabel('Model').fill('TEST-2024');
    await page.getByLabel('Category').selectOption(testCategory.name);

    // Fill pricing
    await page.getByLabel('Base Price').fill('999.99');
    await page.getByLabel('Compare At Price').fill('1199.99');

    // Fill specifications
    await page.getByRole('tab', { name: /specifications/i }).click();
    await page.getByLabel('BTU').fill('15000');
    await page.getByLabel('Energy Rating').selectOption('A++');
    await page.getByLabel('SEER').fill('20');

    // Add feature
    await page.getByRole('button', { name: /add feature/i }).click();
    await page.getByLabel('Feature Title').fill('Smart Control');
    await page.getByLabel('Feature Description').fill('Control via app');

    // Submit
    await page.getByRole('button', { name: /save product/i }).click();

    // Verify success
    await expect(page.getByText(/product created successfully/i)).toBeVisible();

    // Verify product appears in list
    await page.goto('/admin/products');
    await expect(page.getByText('New Test Air Conditioner')).toBeVisible();

    // Get created product ID for cleanup
    const response = await request.get('/api/v1/products?search=New Test Air Conditioner');
    const data = await response.json();
    createdProductId = data.items[0]?.id;
  });

  test('validation prevents invalid product creation', async ({ page }) => {
    await page.goto('/admin/products/new');

    // Try to submit empty form
    await page.getByRole('button', { name: /save product/i }).click();

    // Should show validation errors
    await expect(page.getByText(/sku is required/i)).toBeVisible();
    await expect(page.getByText(/name is required/i)).toBeVisible();
    await expect(page.getByText(/base price is required/i)).toBeVisible();
  });

  test('admin can add product variants', async ({ page, request }) => {
    // First create a product
    const product = await createTestProduct(request, {
      name: 'Variant Test Product',
      sku: `VAR-TEST-${Date.now()}`,
      categoryId: testCategory.id,
      brand: 'TestBrand',
      basePrice: 599.99
    });
    createdProductId = product.id;

    await page.goto(`/admin/products/${product.id}/edit`);

    // Go to variants tab
    await page.getByRole('tab', { name: /variants/i }).click();

    // Add variant
    await page.getByRole('button', { name: /add variant/i }).click();
    await page.getByLabel('Variant SKU').fill(`VAR-12K-${Date.now()}`);
    await page.getByLabel('Variant Name').fill('12000 BTU');
    await page.getByLabel('Price Adjustment').fill('0');
    await page.getByLabel('Stock Quantity').fill('25');

    // Add variant attributes
    await page.getByLabel('Capacity').fill('12000 BTU');
    await page.getByLabel('Color').selectOption('White');

    await page.getByRole('button', { name: /save variant/i }).click();

    // Verify variant added
    await expect(page.getByText('12000 BTU')).toBeVisible();
    await expect(page.getByText('25 in stock')).toBeVisible();
  });
});
```

### Test CAT-E2E-007: Admin Manage Categories

```typescript
import { test, expect } from '@playwright/test';
import { loginAsAdmin, cleanupTestData } from './helpers/test-data';

test.describe('Admin - Category Management', () => {
  let createdCategoryIds: string[] = [];

  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request, { categoryIds: createdCategoryIds });
    createdCategoryIds = [];
  });

  test('admin can create a category', async ({ page, request }) => {
    await page.goto('/admin/categories');

    await page.getByRole('button', { name: /add category/i }).click();

    await page.getByLabel('Name').fill('New Test Category');
    await page.getByLabel('Description').fill('A test category for E2E testing');

    await page.getByRole('button', { name: /save/i }).click();

    await expect(page.getByText(/category created/i)).toBeVisible();
    await expect(page.getByText('New Test Category')).toBeVisible();

    // Get ID for cleanup
    const response = await request.get('/api/v1/categories');
    const categories = await response.json();
    const created = categories.find((c: any) => c.name === 'New Test Category');
    if (created) createdCategoryIds.push(created.id);
  });

  test('admin can create subcategory', async ({ page, request }) => {
    // Create parent first
    const parentResponse = await request.post('/api/v1/admin/categories', {
      data: { name: `Parent Category ${Date.now()}`, slug: `parent-${Date.now()}` }
    });
    const parent = await parentResponse.json();
    createdCategoryIds.push(parent.id);

    await page.goto('/admin/categories');

    // Find parent and add child
    await page.locator(`[data-category-id="${parent.id}"]`).getByRole('button', { name: /add child/i }).click();

    await page.getByLabel('Name').fill('Child Category');
    await page.getByRole('button', { name: /save/i }).click();

    await expect(page.getByText('Child Category')).toBeVisible();

    // Verify hierarchy
    const childElement = page.getByText('Child Category');
    await expect(childElement.locator('xpath=ancestor::*[@data-parent-id]')).toHaveAttribute('data-parent-id', parent.id);
  });

  test('admin can reorder categories', async ({ page, request }) => {
    // Create categories
    const cat1 = await request.post('/api/v1/admin/categories', {
      data: { name: 'Category A', slug: `cat-a-${Date.now()}`, sortOrder: 1 }
    }).then(r => r.json());

    const cat2 = await request.post('/api/v1/admin/categories', {
      data: { name: 'Category B', slug: `cat-b-${Date.now()}`, sortOrder: 2 }
    }).then(r => r.json());

    createdCategoryIds.push(cat1.id, cat2.id);

    await page.goto('/admin/categories');

    // Drag Category B above Category A
    const catBHandle = page.locator(`[data-category-id="${cat2.id}"] [data-drag-handle]`);
    const catATarget = page.locator(`[data-category-id="${cat1.id}"]`);

    await catBHandle.dragTo(catATarget);

    // Verify new order persisted
    await page.reload();

    const categories = page.locator('[data-category-id]');
    const firstCatId = await categories.first().getAttribute('data-category-id');
    expect(firstCatId).toBe(cat2.id);
  });
});
```

### Test Helper Functions

```typescript
// tests/e2e/helpers/test-data.ts

import { APIRequestContext, Page } from '@playwright/test';

const API_BASE = process.env.API_URL || 'http://localhost:5000';

export async function loginAsAdmin(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Email').fill(process.env.ADMIN_EMAIL || 'admin@climasite.test');
  await page.getByLabel('Password').fill(process.env.ADMIN_PASSWORD || 'AdminPassword123!');
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL('/admin/**');
}

export async function createTestCategory(
  request: APIRequestContext,
  data: {
    name: string;
    slug: string;
    description?: string;
    parentId?: string;
  }
): Promise<{ id: string; slug: string; name: string }> {
  const response = await request.post(`${API_BASE}/api/v1/admin/categories`, {
    data,
    headers: await getAdminHeaders()
  });

  if (!response.ok()) {
    throw new Error(`Failed to create category: ${await response.text()}`);
  }

  return response.json();
}

export async function createTestProduct(
  request: APIRequestContext,
  data: {
    name: string;
    sku: string;
    categoryId: string;
    brand: string;
    basePrice: number;
    slug?: string;
    model?: string;
    shortDescription?: string;
    description?: string;
    compareAtPrice?: number;
    specifications?: Record<string, any>;
    features?: Array<{ title: string; description: string }>;
    warrantyMonths?: number;
    requiresInstallation?: boolean;
  }
): Promise<{ id: string; slug: string; name: string }> {
  const slug = data.slug || `${data.name.toLowerCase().replace(/\s+/g, '-')}-${Date.now()}`;

  const response = await request.post(`${API_BASE}/api/v1/admin/products`, {
    data: { ...data, slug },
    headers: await getAdminHeaders()
  });

  if (!response.ok()) {
    throw new Error(`Failed to create product: ${await response.text()}`);
  }

  return response.json();
}

export async function createTestVariant(
  request: APIRequestContext,
  productId: string,
  data: {
    sku: string;
    name: string;
    priceAdjustment: number;
    attributes: Record<string, any>;
    stockQuantity: number;
  }
): Promise<{ id: string }> {
  const response = await request.post(
    `${API_BASE}/api/v1/admin/products/${productId}/variants`,
    {
      data,
      headers: await getAdminHeaders()
    }
  );

  if (!response.ok()) {
    throw new Error(`Failed to create variant: ${await response.text()}`);
  }

  return response.json();
}

export async function cleanupTestData(
  request: APIRequestContext,
  data: {
    productIds?: string[];
    categoryIds?: string[];
  }
): Promise<void> {
  const headers = await getAdminHeaders();

  // Delete products first (due to foreign key constraints)
  for (const productId of data.productIds || []) {
    await request.delete(`${API_BASE}/api/v1/admin/products/${productId}`, { headers });
  }

  // Delete categories
  for (const categoryId of data.categoryIds || []) {
    await request.delete(`${API_BASE}/api/v1/admin/categories/${categoryId}`, { headers });
  }
}

async function getAdminHeaders(): Promise<Record<string, string>> {
  // In a real implementation, this would authenticate and return a JWT token
  // For testing, we might use a test-specific auth mechanism
  return {
    'Authorization': `Bearer ${process.env.ADMIN_TEST_TOKEN || 'test-admin-token'}`,
    'Content-Type': 'application/json'
  };
}
```

---

## 8. Playwright Configuration

```typescript
// playwright.config.ts

import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ['html'],
    ['json', { outputFile: 'test-results/results.json' }]
  ],

  use: {
    baseURL: process.env.BASE_URL || 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'mobile-chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'mobile-safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  webServer: [
    {
      command: 'dotnet run --project src/ClimaSite.Api',
      url: 'http://localhost:5000/health',
      reuseExistingServer: !process.env.CI,
      timeout: 120000,
    },
    {
      command: 'cd src/ClimaSite.Web && ng serve',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
      timeout: 120000,
    },
  ],
});
```

---

## 9. Task Dependencies

```
Phase 1: Database & Core
CAT-001 (Schema) ──┬── CAT-002 (Entities) ──── CAT-003 (EF Config)
                   │
Phase 2: Repository & Services
                   └── CAT-004 (Product Repo) ──┬── CAT-006 (Product Service)
                       CAT-005 (Category Repo) ─┴── CAT-007 (Category Service)
                                                        │
Phase 3: API                                            │
                       CAT-008 (Products API) ──────────┘
                       CAT-009 (Categories API)
                       CAT-010 (Admin Products API)
                       CAT-011 (Admin Categories API)
                       CAT-012 (Filters API)
                       CAT-013 (Bulk Import)

Phase 4: Angular Frontend
CAT-014 (Models/Services) ──┬── CAT-015 (Product List)
                            ├── CAT-016 (Filters)
                            ├── CAT-017 (Product Detail)
                            ├── CAT-018 (Category Nav)
                            ├── CAT-019 (Search)
                            └── CAT-020 (Product Card)
                                    │
                            CAT-021 (Compare) ────────┘

Phase 5: Admin Frontend
CAT-022 (Admin List) ──── CAT-023 (Admin Form) ──── CAT-025 (Spec Builder)
CAT-024 (Category Manager)

Phase 6: Testing
CAT-026 (Backend Unit Tests)
CAT-027 (API Integration Tests)
CAT-028 (Frontend Unit Tests)
CAT-029 (Performance)
CAT-030 (Search Optimization)
```

---

## 10. Acceptance Criteria Summary

| Task | Critical Criteria |
|------|-------------------|
| CAT-001 | All tables created, indexes working, FTS trigger functional |
| CAT-002 | Entities have proper encapsulation and validation |
| CAT-003 | JSONB columns serialize/deserialize correctly |
| CAT-004 | Full-text search returns ranked results, all filters work |
| CAT-005 | Category tree loads in single query, no circular refs |
| CAT-006 | Service handles all business logic, proper validation |
| CAT-008 | All public endpoints respond < 200ms, proper caching |
| CAT-010 | Admin can CRUD products with proper authorization |
| CAT-015 | Product list renders < 3s, infinite scroll works |
| CAT-017 | Product detail SEO optimized, variants work correctly |
| E2E Tests | All tests pass with REAL data, no mocking |

---

## 11. Estimated Timeline

| Phase | Tasks | Estimated Hours | Duration |
|-------|-------|-----------------|----------|
| Phase 1: Database | CAT-001 to CAT-003 | 20 | 3 days |
| Phase 2: Services | CAT-004 to CAT-007 | 28 | 4 days |
| Phase 3: API | CAT-008 to CAT-013 | 40 | 5 days |
| Phase 4: Frontend | CAT-014 to CAT-021 | 62 | 8 days |
| Phase 5: Admin | CAT-022 to CAT-025 | 34 | 5 days |
| Phase 6: Testing | CAT-026 to CAT-030 | 46 | 6 days |
| **Total** | **30 tasks** | **230 hours** | **~31 days** |

---

## 12. Risk Mitigation

| Risk | Mitigation Strategy |
|------|---------------------|
| JSONB query performance | Create GIN indexes, test with realistic data volumes |
| Full-text search relevance | Tune search weights, add synonyms, monitor search analytics |
| Category tree complexity | Use CTE queries, implement caching for navigation |
| Variant complexity | Start simple, iterate based on actual HVAC product needs |
| E2E test flakiness | Use unique identifiers, proper cleanup, retry mechanisms |
| Frontend bundle size | Implement lazy loading, tree shaking, analyze with webpack |

---

## 13. Definition of Done

A task is considered complete when:

1. Code is written and follows project conventions
2. Unit tests pass with > 80% coverage (backend) / > 70% (frontend)
3. Integration tests pass
4. Code is reviewed and approved
5. Documentation is updated
6. No critical SonarQube issues
7. Feature works in development environment
8. E2E tests pass (for UI features)
