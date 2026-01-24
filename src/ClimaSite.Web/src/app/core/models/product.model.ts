export interface Product {
  id: string;
  sku: string;
  name: string;
  slug: string;
  shortDescription?: string;
  description?: string;
  brand?: string;
  model?: string;
  basePrice: number;
  salePrice?: number;
  isOnSale: boolean;
  discountPercentage: number;
  isActive: boolean;
  isFeatured: boolean;
  averageRating: number;
  reviewCount: number;
  category?: CategoryBrief;
  images: ProductImage[];
  variants: ProductVariant[];
  specifications?: Record<string, unknown>;
  features?: Record<string, string>;
  warrantyMonths: number;
  requiresInstallation: boolean;
  createdAt: string;
}

export interface ProductBrief {
  id: string;
  name: string;
  slug: string;
  shortDescription?: string;
  basePrice: number;
  salePrice?: number;
  isOnSale: boolean;
  discountPercentage: number;
  brand?: string;
  averageRating: number;
  reviewCount: number;
  primaryImageUrl?: string;
  inStock: boolean;
  // HVAC-specific quick specs
  energyRating?: EnergyRatingLevel;
  btuCapacity?: number;
  noiseLevel?: number;        // in dB
  roomSizeMin?: number;       // in m²
  roomSizeMax?: number;       // in m²
  hasWifi?: boolean;
  hasInverter?: boolean;
  isHeatPump?: boolean;
}

// EU Energy Efficiency Rating levels
export type EnergyRatingLevel = 'A+++' | 'A++' | 'A+' | 'A' | 'B' | 'C' | 'D' | 'E' | 'F' | 'G';

export interface ProductImage {
  id: string;
  url: string;
  altText?: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface ProductVariant {
  id: string;
  sku: string;
  name?: string;
  price: number;
  salePrice?: number;
  stockQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  isActive: boolean;
  attributes?: Record<string, string>;
}

export interface CategoryBrief {
  id: string;
  name: string;
  slug: string;
}

export interface PaginatedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface FilterOptions {
  brands: BrandOption[];
  priceRange: PriceRange;
  specifications: Record<string, SpecificationOption[]>;
  tags: TagOption[];
}

export interface BrandOption {
  name: string;
  count: number;
}

export interface PriceRange {
  min: number;
  max: number;
}

export interface SpecificationOption {
  value: string;
  label: string;
  count: number;
}

export interface TagOption {
  name: string;
  count: number;
}

export interface ProductFilter {
  pageNumber?: number;
  pageSize?: number;
  categoryId?: string;
  searchTerm?: string;
  brand?: string;
  minPrice?: number;
  maxPrice?: number;
  inStock?: boolean;
  onSale?: boolean;
  isFeatured?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  // HVAC-specific filters
  energyRatings?: EnergyRatingLevel[];
  btuMin?: number;
  btuMax?: number;
  noiseLevelMax?: number;
  roomSize?: number;
  features?: ProductFeature[];
}

export type ProductFeature = 'wifi' | 'inverter' | 'heat_pump' | 'smart_home';

export interface ProductSearchParams {
  q: string;
  pageNumber?: number;
  pageSize?: number;
  categorySlug?: string;
  brands?: string[];
  minPrice?: number;
  maxPrice?: number;
}
