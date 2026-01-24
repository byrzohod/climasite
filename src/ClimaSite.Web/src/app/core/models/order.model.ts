export interface Order {
  id: string;
  orderNumber: string;
  userId?: string;
  customerEmail: string;
  customerPhone?: string;
  status: OrderStatus;
  shippingAddress: Address;
  billingAddress?: Address;
  items: OrderItem[];
  subtotal: number;
  shippingCost: number;
  taxAmount: number;
  discountAmount: number;
  total: number;
  currency: string;
  paymentMethod?: string;
  paymentIntentId?: string;
  shippingMethod?: string;
  trackingNumber?: string;
  paidAt?: string;
  shippedAt?: string;
  deliveredAt?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  notes?: string;
  events: OrderEvent[];
  createdAt: string;
  updatedAt: string;
}

export interface OrderBrief {
  id: string;
  orderNumber: string;
  status: string;
  total: number;
  itemCount: number;
  createdAt: string;
  items: OrderItemBrief[];
}

export interface OrderItemBrief {
  id: string;
  productName: string;
  imageUrl?: string;
  quantity: number;
}

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productSlug?: string;
  variantId?: string;
  variantName?: string;
  sku: string;
  imageUrl?: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderEvent {
  id: string;
  orderId: string;
  status: string;
  description?: string;
  notes?: string;
  createdAt: string;
}

export interface Address {
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
  phone?: string;
}

export interface CreateOrderRequest {
  customerEmail: string;
  customerPhone?: string;
  shippingAddress: Address;
  billingAddress?: Address;
  shippingMethod: string;
  paymentMethod?: string;
  paymentIntentId?: string;
  guestSessionId?: string;
  notes?: string;
}

export interface OrdersFilterParams {
  pageNumber?: number;
  pageSize?: number;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  search?: string;
  sortBy?: 'date' | 'total' | 'status';
  sortDirection?: 'asc' | 'desc';
}

export interface PaginatedOrders {
  items: OrderBrief[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export type OrderStatus =
  | 'Pending'
  | 'Paid'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Refunded'
  | 'Returned';

// Status badge colors now use CSS custom properties for proper dark mode support
// Components should use these CSS variable names in their styles
export const ORDER_STATUS_CONFIG: Record<OrderStatus, { label: string; colorVar: string; bgColorVar: string }> = {
  Pending: { label: 'Pending', colorVar: 'var(--color-status-pending-text)', bgColorVar: 'var(--color-status-pending-bg)' },
  Paid: { label: 'Paid', colorVar: 'var(--color-status-success-text)', bgColorVar: 'var(--color-status-success-bg)' },
  Processing: { label: 'Processing', colorVar: 'var(--color-status-processing-text)', bgColorVar: 'var(--color-status-processing-bg)' },
  Shipped: { label: 'Shipped', colorVar: 'var(--color-status-shipped-text)', bgColorVar: 'var(--color-status-shipped-bg)' },
  Delivered: { label: 'Delivered', colorVar: 'var(--color-status-success-text)', bgColorVar: 'var(--color-status-success-bg)' },
  Cancelled: { label: 'Cancelled', colorVar: 'var(--color-status-error-text)', bgColorVar: 'var(--color-status-error-bg)' },
  Refunded: { label: 'Refunded', colorVar: 'var(--color-status-refunded-text)', bgColorVar: 'var(--color-status-refunded-bg)' },
  Returned: { label: 'Returned', colorVar: 'var(--color-status-returned-text)', bgColorVar: 'var(--color-status-returned-bg)' }
};

export interface ReorderResult {
  cart: Cart;
  itemsAdded: number;
  itemsSkipped: number;
  skippedReasons: string[];
}

export interface Cart {
  id: string;
  userId?: string;
  guestSessionId?: string;
  items: CartItem[];
  subtotal: number;
  tax: number;
  total: number;
  itemCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CartItem {
  id: string;
  productId: string;
  variantId: string;
  productName: string;
  productSlug?: string;
  variantName?: string;
  sku?: string;
  imageUrl?: string;
  unitPrice: number;
  salePrice?: number;
  effectivePrice: number;
  quantity: number;
  lineTotal: number;
  availableStock: number;
  isAvailable: boolean;
}
