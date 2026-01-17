import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Review, ReviewSummary, CreateReviewRequest, PaginatedReviews } from '../models/review.model';

export type ReviewSortBy = 'newest' | 'oldest' | 'helpful' | 'rating_high' | 'rating_low';

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  /**
   * Get paginated reviews for a product
   */
  getProductReviews(
    productId: string,
    pageNumber: number = 1,
    pageSize: number = 10,
    sortBy: ReviewSortBy = 'newest'
  ): Observable<PaginatedReviews> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString())
      .set('sortBy', sortBy);

    return this.http.get<PaginatedReviews>(
      `${this.apiUrl}/api/reviews/product/${productId}`,
      { params }
    );
  }

  /**
   * Get review summary (rating distribution) for a product
   */
  getProductReviewSummary(productId: string): Observable<ReviewSummary> {
    return this.http.get<ReviewSummary>(
      `${this.apiUrl}/api/reviews/product/${productId}/summary`
    );
  }

  /**
   * Create a new review (requires authentication)
   */
  createReview(request: CreateReviewRequest): Observable<Review> {
    return this.http.post<Review>(`${this.apiUrl}/api/reviews`, request);
  }

  /**
   * Mark a review as helpful
   */
  markHelpful(reviewId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/api/reviews/${reviewId}/helpful`, {});
  }

  /**
   * Mark a review as unhelpful
   */
  markUnhelpful(reviewId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/api/reviews/${reviewId}/unhelpful`, {});
  }
}
