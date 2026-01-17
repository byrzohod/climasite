export interface Review {
  id: string;
  productId: string;
  userId: string;
  userName: string;
  rating: number;
  title: string | null;
  content: string | null;
  status: string;
  isVerifiedPurchase: boolean;
  helpfulCount: number;
  unhelpfulCount: number;
  adminResponse: string | null;
  adminRespondedAt: string | null;
  createdAt: string;
}

export interface ReviewSummary {
  productId: string;
  averageRating: number;
  totalReviews: number;
  ratingDistribution: Record<number, number>;
}

export interface CreateReviewRequest {
  productId: string;
  rating: number;
  title?: string;
  content?: string;
}

export interface PaginatedReviews {
  items: Review[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
