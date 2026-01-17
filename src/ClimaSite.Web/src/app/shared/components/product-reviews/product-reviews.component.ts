import { Component, inject, input, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ReviewService, ReviewSortBy } from '../../../core/services/review.service';
import { AuthService } from '../../../auth/services/auth.service';
import { Review, ReviewSummary, PaginatedReviews } from '../../../core/models/review.model';

@Component({
  selector: 'app-product-reviews',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslateModule],
  template: `
    <div class="product-reviews" data-testid="product-reviews">
      <!-- Summary Section -->
      <div class="reviews-summary" data-testid="reviews-summary">
        <div class="summary-rating">
          <div class="average-rating">
            <span class="rating-number">{{ summary()?.averageRating | number:'1.1-1' }}</span>
            <span class="rating-max">/ 5</span>
          </div>
          <div class="stars-display">
            @for (star of [1,2,3,4,5]; track star) {
              <span class="star" [class.filled]="star <= Math.floor(summary()?.averageRating || 0)">★</span>
            }
          </div>
          <div class="total-reviews">
            {{ summary()?.totalReviews || 0 }} {{ 'reviews.total' | translate }}
          </div>
        </div>

        <div class="rating-distribution">
          @for (rating of [5,4,3,2,1]; track rating) {
            <div class="distribution-row">
              <span class="rating-label">{{ rating }} ★</span>
              <div class="bar-container">
                <div
                  class="bar-fill"
                  [style.width.%]="getDistributionPercent(rating)"
                ></div>
              </div>
              <span class="rating-count">{{ getDistributionCount(rating) }}</span>
            </div>
          }
        </div>
      </div>

      <!-- Session Expired Error (shown outside form) -->
      @if (submitError() && !authService.isAuthenticated()) {
        <div class="session-error" data-testid="session-error">
          <p>{{ submitError() }}</p>
          <a routerLink="/login" class="login-link">{{ 'auth.login' | translate }}</a>
        </div>
      }

      <!-- Write Review Button -->
      @if (authService.isAuthenticated()) {
        <button
          class="btn-write-review"
          (click)="toggleReviewForm()"
          data-testid="write-review-btn"
        >
          @if (showReviewForm()) {
            {{ 'reviews.cancel' | translate }}
          } @else {
            {{ 'reviews.writeReview' | translate }}
          }
        </button>
      } @else if (!submitError()) {
        <div class="login-prompt" data-testid="reviews-login-prompt">
          <span class="prompt-icon">★</span>
          <span>{{ 'reviews.loginToReview' | translate }}</span>
          <a routerLink="/auth/login" class="login-link" data-testid="reviews-login-link">
            {{ 'reviews.loginNow' | translate }}
          </a>
        </div>
      }

      <!-- Review Form -->
      @if (showReviewForm()) {
        <div class="review-form" data-testid="review-form">
          <h4>{{ 'reviews.writeYourReview' | translate }}</h4>

          <!-- Star Rating Input -->
          <div class="form-group">
            <label>{{ 'reviews.yourRating' | translate }}</label>
            <div class="star-input">
              @for (star of [1,2,3,4,5]; track star) {
                <button
                  type="button"
                  class="star-btn"
                  [class.selected]="star <= newReviewRating()"
                  (click)="setRating(star)"
                  (mouseenter)="hoverRating.set(star)"
                  (mouseleave)="hoverRating.set(0)"
                >
                  ★
                </button>
              }
            </div>
          </div>

          <!-- Title -->
          <div class="form-group">
            <label for="review-title">{{ 'reviews.reviewTitle' | translate }}</label>
            <input
              id="review-title"
              type="text"
              [value]="newReviewTitle()"
              (input)="onTitleChange($event)"
              maxlength="200"
              placeholder="{{ 'reviews.titlePlaceholder' | translate }}"
            />
          </div>

          <!-- Content -->
          <div class="form-group">
            <label for="review-content">{{ 'reviews.reviewContent' | translate }}</label>
            <textarea
              id="review-content"
              [value]="newReviewContent()"
              (input)="onContentChange($event)"
              maxlength="5000"
              rows="4"
              placeholder="{{ 'reviews.contentPlaceholder' | translate }}"
            ></textarea>
          </div>

          @if (submitError()) {
            <p class="form-error">{{ submitError() }}</p>
          }

          <button
            class="btn-submit-review"
            [disabled]="isSubmitting() || newReviewRating() === 0"
            (click)="submitReview()"
            data-testid="submit-review-btn"
          >
            @if (isSubmitting()) {
              {{ 'common.loading' | translate }}
            } @else {
              {{ 'reviews.submitReview' | translate }}
            }
          </button>
        </div>
      }

      <!-- Sort and Filter -->
      <div class="reviews-controls">
        <label for="sort-by">{{ 'reviews.sortBy' | translate }}:</label>
        <select
          id="sort-by"
          [value]="currentSort()"
          (change)="onSortChange($event)"
        >
          <option value="newest">{{ 'reviews.sort.newest' | translate }}</option>
          <option value="oldest">{{ 'reviews.sort.oldest' | translate }}</option>
          <option value="helpful">{{ 'reviews.sort.helpful' | translate }}</option>
          <option value="rating_high">{{ 'reviews.sort.ratingHigh' | translate }}</option>
          <option value="rating_low">{{ 'reviews.sort.ratingLow' | translate }}</option>
        </select>
      </div>

      <!-- Reviews List -->
      @if (isLoading()) {
        <div class="loading">{{ 'common.loading' | translate }}</div>
      } @else if (reviews().length === 0) {
        <div class="no-reviews" data-testid="no-reviews">
          <p>{{ 'reviews.noReviews' | translate }}</p>
          <p class="no-reviews-cta">{{ 'reviews.beFirst' | translate }}</p>
        </div>
      } @else {
        <div class="reviews-list" data-testid="reviews-list">
          @for (review of reviews(); track review.id) {
            <div class="review-card" data-testid="review-card">
              <div class="review-header">
                <div class="review-rating">
                  @for (star of [1,2,3,4,5]; track star) {
                    <span class="star" [class.filled]="star <= review.rating">★</span>
                  }
                </div>
                @if (review.isVerifiedPurchase) {
                  <span class="verified-badge">
                    ✓ {{ 'reviews.verifiedPurchase' | translate }}
                  </span>
                }
              </div>

              @if (review.title) {
                <h4 class="review-title">{{ review.title }}</h4>
              }

              <p class="review-content">{{ review.content }}</p>

              <div class="review-meta">
                <span class="review-author">{{ review.userName }}</span>
                <span class="review-date">{{ review.createdAt | date:'mediumDate' }}</span>
              </div>

              @if (review.adminResponse) {
                <div class="admin-response">
                  <strong>{{ 'reviews.storeResponse' | translate }}:</strong>
                  <p>{{ review.adminResponse }}</p>
                </div>
              }

              <div class="review-actions">
                <span class="helpful-label">{{ 'reviews.wasHelpful' | translate }}</span>
                <button
                  class="btn-helpful"
                  (click)="markHelpful(review)"
                  [disabled]="!authService.isAuthenticated()"
                >
                  👍 {{ review.helpfulCount }}
                </button>
                <button
                  class="btn-helpful"
                  (click)="markUnhelpful(review)"
                  [disabled]="!authService.isAuthenticated()"
                >
                  👎 {{ review.unhelpfulCount }}
                </button>
              </div>
            </div>
          }
        </div>

        <!-- Pagination -->
        @if (totalPages() > 1) {
          <div class="pagination">
            <button
              class="btn-page"
              [disabled]="currentPage() === 1"
              (click)="goToPage(currentPage() - 1)"
            >
              {{ 'common.previous' | translate }}
            </button>

            <span class="page-info">
              {{ currentPage() }} / {{ totalPages() }}
            </span>

            <button
              class="btn-page"
              [disabled]="currentPage() >= totalPages()"
              (click)="goToPage(currentPage() + 1)"
            >
              {{ 'common.next' | translate }}
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .product-reviews {
      padding: 1rem 0;
    }

    .reviews-summary {
      display: grid;
      grid-template-columns: 1fr 2fr;
      gap: 2rem;
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      margin-bottom: 1.5rem;
    }

    .summary-rating {
      text-align: center;

      .average-rating {
        display: flex;
        align-items: baseline;
        justify-content: center;
        gap: 0.25rem;
      }

      .rating-number {
        font-size: 3rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }

      .rating-max {
        font-size: 1.25rem;
        color: var(--color-text-secondary);
      }

      .stars-display {
        margin: 0.5rem 0;
        font-size: 1.5rem;
      }

      .total-reviews {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .star {
      color: var(--color-border);
      &.filled {
        color: #ffc107;
      }
    }

    .rating-distribution {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .distribution-row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .rating-label {
      width: 40px;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .bar-container {
      flex: 1;
      height: 8px;
      background: var(--color-border);
      border-radius: 4px;
      overflow: hidden;
    }

    .bar-fill {
      height: 100%;
      background: #ffc107;
      transition: width 0.3s ease;
    }

    .rating-count {
      width: 30px;
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      text-align: right;
    }

    .btn-write-review {
      width: 100%;
      padding: 1rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      font-size: 1rem;
      cursor: pointer;
      transition: background-color 0.2s;
      margin-bottom: 1.5rem;

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    .login-prompt {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem 1.25rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
      color: var(--color-text-secondary);
      margin-bottom: 1.5rem;

      .prompt-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        background: var(--color-primary-light);
        color: var(--color-primary);
        border-radius: 50%;
        font-size: 1rem;
        flex-shrink: 0;
      }

      .login-link {
        margin-left: auto;
        padding: 0.5rem 1rem;
        background: var(--color-primary);
        color: white;
        border-radius: 6px;
        text-decoration: none;
        font-weight: 500;
        font-size: 0.875rem;
        transition: background 0.2s;

        &:hover {
          background: var(--color-primary-dark);
        }
      }
    }

    .session-error {
      text-align: center;
      padding: 1.5rem;
      background: var(--color-error-light, rgba(239, 68, 68, 0.1));
      border: 1px solid var(--color-error);
      border-radius: 8px;
      margin-bottom: 1.5rem;

      p {
        color: var(--color-error);
        margin: 0 0 1rem;
        font-weight: 500;
      }

      .login-link {
        display: inline-block;
        padding: 0.5rem 1.5rem;
        background: var(--color-primary);
        color: white;
        text-decoration: none;
        border-radius: 6px;
        font-weight: 600;
        transition: background-color 0.2s;

        &:hover {
          background: var(--color-primary-dark);
        }
      }
    }

    .review-form {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      margin-bottom: 1.5rem;

      h4 {
        margin: 0 0 1.5rem;
        color: var(--color-text-primary);
      }
    }

    .form-group {
      margin-bottom: 1rem;

      label {
        display: block;
        margin-bottom: 0.5rem;
        font-weight: 500;
        color: var(--color-text-secondary);
      }

      input, textarea {
        width: 100%;
        padding: 0.75rem;
        background: var(--color-bg-primary);
        border: 1px solid var(--color-border);
        border-radius: 8px;
        color: var(--color-text-primary);
        font-family: inherit;
        font-size: 1rem;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }
      }

      textarea {
        resize: vertical;
        min-height: 100px;
      }
    }

    .star-input {
      display: flex;
      gap: 0.5rem;

      .star-btn {
        font-size: 2rem;
        background: none;
        border: none;
        color: var(--color-border);
        cursor: pointer;
        padding: 0;
        transition: color 0.2s, transform 0.2s;

        &:hover, &.selected {
          color: #ffc107;
          transform: scale(1.1);
        }
      }
    }

    .form-error {
      color: var(--color-error);
      font-size: 0.875rem;
      margin-bottom: 1rem;
    }

    .btn-submit-review {
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }
    }

    .reviews-controls {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1.5rem;

      label {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }

      select {
        padding: 0.5rem 1rem;
        background: var(--color-bg-secondary);
        border: 1px solid var(--color-border);
        border-radius: 8px;
        color: var(--color-text-primary);
        font-size: 0.875rem;
        cursor: pointer;
      }
    }

    .loading {
      text-align: center;
      padding: 2rem;
      color: var(--color-text-secondary);
    }

    .no-reviews {
      text-align: center;
      padding: 3rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      color: var(--color-text-secondary);

      .no-reviews-cta {
        margin-top: 0.5rem;
        font-size: 0.875rem;
      }
    }

    .reviews-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .review-card {
      padding: 1.5rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
    }

    .review-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 0.75rem;
    }

    .review-rating {
      font-size: 1rem;
    }

    .verified-badge {
      font-size: 0.75rem;
      padding: 0.25rem 0.5rem;
      background: rgba(34, 197, 94, 0.1);
      color: var(--color-success, #22c55e);
      border-radius: 4px;
    }

    .review-title {
      margin: 0 0 0.5rem;
      color: var(--color-text-primary);
      font-size: 1rem;
    }

    .review-content {
      color: var(--color-text-secondary);
      line-height: 1.6;
      margin: 0 0 1rem;
    }

    .review-meta {
      display: flex;
      gap: 1rem;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      margin-bottom: 1rem;
    }

    .admin-response {
      padding: 1rem;
      background: var(--color-bg-primary);
      border-left: 3px solid var(--color-primary);
      margin-bottom: 1rem;
      border-radius: 0 8px 8px 0;

      strong {
        display: block;
        color: var(--color-primary);
        margin-bottom: 0.5rem;
        font-size: 0.875rem;
      }

      p {
        margin: 0;
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    .review-actions {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding-top: 0.75rem;
      border-top: 1px solid var(--color-border);

      .helpful-label {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      .btn-helpful {
        padding: 0.375rem 0.75rem;
        background: transparent;
        border: 1px solid var(--color-border);
        border-radius: 4px;
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        cursor: pointer;
        transition: all 0.2s;

        &:hover:not(:disabled) {
          background: var(--color-bg-primary);
          border-color: var(--color-primary);
        }

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      margin-top: 1.5rem;

      .btn-page {
        padding: 0.5rem 1rem;
        background: var(--color-bg-secondary);
        border: 1px solid var(--color-border);
        border-radius: 8px;
        color: var(--color-text-primary);
        cursor: pointer;

        &:hover:not(:disabled) {
          background: var(--color-primary);
          color: white;
          border-color: var(--color-primary);
        }

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }

      .page-info {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    @media (max-width: 768px) {
      .reviews-summary {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .summary-rating {
        order: -1;
      }
    }
  `]
})
export class ProductReviewsComponent implements OnInit {
  private readonly reviewService = inject(ReviewService);
  readonly authService = inject(AuthService);
  private readonly translate = inject(TranslateService);

  productId = input.required<string>();

  Math = Math;

  // State signals
  summary = signal<ReviewSummary | null>(null);
  reviews = signal<Review[]>([]);
  isLoading = signal(true);
  currentPage = signal(1);
  totalPages = signal(1);
  currentSort = signal<ReviewSortBy>('newest');

  // Review form state
  showReviewForm = signal(false);
  newReviewRating = signal(0);
  newReviewTitle = signal('');
  newReviewContent = signal('');
  hoverRating = signal(0);
  isSubmitting = signal(false);
  submitError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadReviews();
    this.loadSummary();
  }

  private loadSummary(): void {
    this.reviewService.getProductReviewSummary(this.productId()).subscribe({
      next: (summary) => {
        this.summary.set(summary);
      },
      error: (err) => {
        console.error('Failed to load review summary:', err);
      }
    });
  }

  private loadReviews(): void {
    this.isLoading.set(true);
    this.reviewService.getProductReviews(
      this.productId(),
      this.currentPage(),
      10,
      this.currentSort()
    ).subscribe({
      next: (result: PaginatedReviews) => {
        this.reviews.set(result.items);
        this.totalPages.set(result.totalPages);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load reviews:', err);
        this.isLoading.set(false);
      }
    });
  }

  getDistributionPercent(rating: number): number {
    const summ = this.summary();
    if (!summ || summ.totalReviews === 0) return 0;
    const count = summ.ratingDistribution[rating] || 0;
    return (count / summ.totalReviews) * 100;
  }

  getDistributionCount(rating: number): number {
    return this.summary()?.ratingDistribution[rating] || 0;
  }

  toggleReviewForm(): void {
    this.showReviewForm.update(v => !v);
    if (!this.showReviewForm()) {
      this.resetForm();
    }
  }

  private resetForm(): void {
    this.newReviewRating.set(0);
    this.newReviewTitle.set('');
    this.newReviewContent.set('');
    this.submitError.set(null);
  }

  setRating(rating: number): void {
    this.newReviewRating.set(rating);
  }

  onTitleChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.newReviewTitle.set(input.value);
  }

  onContentChange(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.newReviewContent.set(textarea.value);
  }

  submitReview(): void {
    if (this.newReviewRating() === 0 || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.reviewService.createReview({
      productId: this.productId(),
      rating: this.newReviewRating(),
      title: this.newReviewTitle() || undefined,
      content: this.newReviewContent() || undefined
    }).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showReviewForm.set(false);
        this.resetForm();
        // Reload reviews and summary
        this.currentPage.set(1);
        this.loadReviews();
        this.loadSummary();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        // Check if it's an authentication error (session expired)
        if (err.status === 401) {
          this.submitError.set(this.translate.instant('reviews.sessionExpired') || 'Your session has expired. Please log in again.');
          // Hide the form since user is no longer authenticated
          this.showReviewForm.set(false);
        } else {
          const message = err.error?.message || this.translate.instant('reviews.submitError') || 'Failed to submit review';
          this.submitError.set(message);
        }
      }
    });
  }

  onSortChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.currentSort.set(select.value as ReviewSortBy);
    this.currentPage.set(1);
    this.loadReviews();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadReviews();
  }

  markHelpful(review: Review): void {
    this.reviewService.markHelpful(review.id).subscribe({
      next: () => {
        // Update local state
        this.reviews.update(reviews =>
          reviews.map(r =>
            r.id === review.id ? { ...r, helpfulCount: r.helpfulCount + 1 } : r
          )
        );
      }
    });
  }

  markUnhelpful(review: Review): void {
    this.reviewService.markUnhelpful(review.id).subscribe({
      next: () => {
        // Update local state
        this.reviews.update(reviews =>
          reviews.map(r =>
            r.id === review.id ? { ...r, unhelpfulCount: r.unhelpfulCount + 1 } : r
          )
        );
      }
    });
  }
}
