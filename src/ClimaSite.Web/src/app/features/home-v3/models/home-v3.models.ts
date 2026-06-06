export type RoomType = 'living' | 'bedroom' | 'office' | 'commercial';
export type ClimateZone = 'A' | 'B' | 'C';

export interface WizardState {
  area: number;        // m²
  roomType: RoomType;
  zone: ClimateZone;
}

export interface RecommendedProduct {
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
  score: number;
  matchReason: string;
  btuCapacity?: number;
  isInverter: boolean;
  noiseLevel?: number;
}
