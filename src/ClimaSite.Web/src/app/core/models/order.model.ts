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

export const ORDER_STATUS_CONFIG: Record<OrderStatus, { label: string; color: string; bgColor: string }> = {
  Pending: { label: 'Pending', color: '#92400e', bgColor: '#fef3c7' },
  Paid: { label: 'Paid', color: '#166534', bgColor: '#dcfce7' },
  Processing: { label: 'Processing', color: '#1e40af', bgColor: '#dbeafe' },
  Shipped: { label: 'Shipped', color: '#3730a3', bgColor: '#e0e7ff' },
  Delivered: { label: 'Delivered', color: '#166534', bgColor: '#dcfce7' },
  Cancelled: { label: 'Cancelled', color: '#991b1b', bgColor: '#fee2e2' },
  Refunded: { label: 'Refunded', color: '#6b21a8', bgColor: '#f3e8ff' },
  Returned: { label: 'Returned', color: '#0369a1', bgColor: '#e0f2fe' }
};
