import { ProductBrief } from './product.model';

export interface BrandBrief {
  id: string;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  countryOfOrigin?: string;
  isFeatured: boolean;
  productCount: number;
}

export interface Brand {
  id: string;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  bannerImageUrl?: string;
  websiteUrl?: string;
  countryOfOrigin?: string;
  foundedYear: number;
  isFeatured: boolean;
  metaTitle?: string;
  metaDescription?: string;
  productCount: number;
  products: ProductBrief[];
}

export interface BrandListResponse {
  items: BrandBrief[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
