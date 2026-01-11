export interface Cart {
  id: string;
  sessionId?: string;
  userId?: string;
  items: CartItem[];
  subtotal: number;
  shipping: number;
  tax: number;
  total: number;
  itemCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CartItem {
  id: string;
  productId: string;
  variantId?: string;
  productName: string;
  productSlug: string;
  variantName?: string;
  sku: string;
  imageUrl?: string;
  unitPrice: number;
  salePrice?: number;
  quantity: number;
  subtotal: number;
  maxQuantity: number;
}

export interface AddToCartRequest {
  productId: string;
  variantId?: string;
  quantity: number;
  guestSessionId?: string;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

export interface CartSummary {
  itemCount: number;
  subtotal: number;
  total: number;
}
