import { ProductBrief } from './product.model';

export interface PromotionBrief {
  id: string;
  name: string;
  slug: string;
  description?: string;
  code?: string;
  type: PromotionType;
  discountValue: number;
  startDate: string;
  endDate: string;
  bannerImageUrl?: string;
  thumbnailImageUrl?: string;
  productCount: number;
}

export interface Promotion {
  id: string;
  name: string;
  slug: string;
  description?: string;
  code?: string;
  type: PromotionType;
  discountValue: number;
  minimumOrderAmount?: number;
  startDate: string;
  endDate: string;
  bannerImageUrl?: string;
  thumbnailImageUrl?: string;
  isActive: boolean;
  isFeatured: boolean;
  termsAndConditions?: string;
  products: ProductBrief[];
}

export enum PromotionType {
  Percentage = 'Percentage',
  FixedAmount = 'FixedAmount',
  BuyOneGetOne = 'BuyOneGetOne',
  FreeShipping = 'FreeShipping'
}
