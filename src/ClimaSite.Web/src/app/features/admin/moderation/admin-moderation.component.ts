import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ModerationService, AdminQuestion, AdminAnswer, PendingModeration, AdminReview, PendingReviewModeration } from '../../../core/services/moderation.service';
import { forkJoin } from 'rxjs';

type TabType = 'questions' | 'answers' | 'reviews';

@Component({
  selector: 'app-admin-moderation',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="moderation-container">
      <div class="moderation-header">
        <h1>{{ 'admin.moderation.title' | translate }}</h1>
        <p class="subtitle">{{ 'admin.moderation.subtitle' | translate }}</p>
      </div>

      <!-- Stats -->
      <div class="stats-row" *ngIf="data()">
        <div class="stat-card pending">
          <span class="stat-value">{{ data()!.pendingQuestions }}</span>
          <span class="stat-label">{{ 'admin.moderation.pendingQuestions' | translate }}</span>
        </div>
        <div class="stat-card pending">
          <span class="stat-value">{{ data()!.pendingAnswers }}</span>
          <span class="stat-label">{{ 'admin.moderation.pendingAnswers' | translate }}</span>
        </div>
        <div class="stat-card pending">
          <span class="stat-value">{{ reviewData()?.pendingReviews || 0 }}</span>
          <span class="stat-label">{{ 'admin.moderation.pendingReviews' | translate }}</span>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs">
        <button
          class="tab"
          [class.active]="activeTab() === 'questions'"
          (click)="setTab('questions')"
          data-testid="questions-tab">
          {{ 'admin.moderation.questionsTab' | translate }}
          <span class="badge" *ngIf="data()?.pendingQuestions">{{ data()!.pendingQuestions }}</span>
        </button>
        <button
          class="tab"
          [class.active]="activeTab() === 'answers'"
          (click)="setTab('answers')"
          data-testid="answers-tab">
          {{ 'admin.moderation.answersTab' | translate }}
          <span class="badge" *ngIf="data()?.pendingAnswers">{{ data()!.pendingAnswers }}</span>
        </button>
        <button
          class="tab"
          [class.active]="activeTab() === 'reviews'"
          (click)="setTab('reviews')"
          data-testid="reviews-tab">
          {{ 'admin.moderation.reviewsTab' | translate }}
          <span class="badge" *ngIf="reviewData()?.pendingReviews">{{ reviewData()!.pendingReviews }}</span>
        </button>
      </div>

      <!-- Loading State -->
      <div class="loading" *ngIf="loading()">
        <div class="spinner"></div>
        <span>{{ 'common.loading' | translate }}</span>
      </div>

      <!-- Questions List -->
      <div class="items-list" *ngIf="!loading() && activeTab() === 'questions'">
        <div class="empty-state" *ngIf="data()?.questions?.length === 0">
          <span class="icon">&#10003;</span>
          <p>{{ 'admin.moderation.noQuestions' | translate }}</p>
        </div>

        <div class="moderation-item" *ngFor="let question of data()?.questions" data-testid="question-item">
          <div class="item-header">
            <span class="status-badge" [class]="question.status.toLowerCase()">
              {{ question.status }}
            </span>
            <a [routerLink]="['/products', question.productSlug]" class="product-link">
              {{ question.productName }}
            </a>
            <span class="date">{{ question.createdAt | date:'medium' }}</span>
          </div>

          <div class="item-content">
            <p class="question-text">{{ question.questionText }}</p>
            <div class="meta">
              <span *ngIf="question.askerName">
                <strong>{{ 'admin.moderation.askedBy' | translate }}:</strong> {{ question.askerName }}
              </span>
              <span *ngIf="question.askerEmail">
                ({{ question.askerEmail }})
              </span>
            </div>
          </div>

          <div class="item-actions">
            <button
              class="action-btn approve"
              (click)="approveQuestion(question)"
              [disabled]="processingId() === question.id"
              data-testid="approve-question">
              <span *ngIf="processingId() !== question.id">{{ 'admin.moderation.approve' | translate }}</span>
              <span *ngIf="processingId() === question.id" class="spinner small"></span>
            </button>
            <button
              class="action-btn reject"
              (click)="rejectQuestion(question)"
              [disabled]="processingId() === question.id"
              data-testid="reject-question">
              {{ 'admin.moderation.reject' | translate }}
            </button>
          </div>
        </div>
      </div>

      <!-- Answers List -->
      <div class="items-list" *ngIf="!loading() && activeTab() === 'answers'">
        <div class="empty-state" *ngIf="data()?.answers?.length === 0">
          <span class="icon">&#10003;</span>
          <p>{{ 'admin.moderation.noAnswers' | translate }}</p>
        </div>

        <div class="moderation-item" *ngFor="let answer of data()?.answers" data-testid="answer-item">
          <div class="item-header">
            <span class="status-badge" [class]="answer.status.toLowerCase()">
              {{ answer.status }}
            </span>
            <a [routerLink]="['/products', answer.productSlug]" class="product-link">
              {{ answer.productName }}
            </a>
            <span class="date">{{ answer.createdAt | date:'medium' }}</span>
          </div>

          <div class="item-content">
            <p class="context-text">
              <strong>{{ 'admin.moderation.question' | translate }}:</strong> {{ answer.questionText }}
            </p>
            <p class="answer-text">{{ answer.answerText }}</p>
            <div class="meta">
              <span *ngIf="answer.answererName">
                <strong>{{ 'admin.moderation.answeredBy' | translate }}:</strong> {{ answer.answererName }}
              </span>
              <span class="official-badge" *ngIf="answer.isOfficial">{{ 'admin.moderation.official' | translate }}</span>
            </div>
          </div>

          <div class="item-actions">
            <button
              class="action-btn approve"
              (click)="approveAnswer(answer, false)"
              [disabled]="processingId() === answer.id"
              data-testid="approve-answer">
              <span *ngIf="processingId() !== answer.id">{{ 'admin.moderation.approve' | translate }}</span>
              <span *ngIf="processingId() === answer.id" class="spinner small"></span>
            </button>
            <button
              class="action-btn approve-official"
              (click)="approveAnswer(answer, true)"
              [disabled]="processingId() === answer.id"
              data-testid="approve-official">
              {{ 'admin.moderation.approveAsOfficial' | translate }}
            </button>
            <button
              class="action-btn reject"
              (click)="rejectAnswer(answer)"
              [disabled]="processingId() === answer.id"
              data-testid="reject-answer">
              {{ 'admin.moderation.reject' | translate }}
            </button>
          </div>
        </div>
      </div>

      <!-- Reviews List -->
      <div class="items-list" *ngIf="!loading() && activeTab() === 'reviews'">
        <div class="empty-state" *ngIf="reviewData()?.reviews?.length === 0">
          <span class="icon">&#10003;</span>
          <p>{{ 'admin.moderation.noReviews' | translate }}</p>
        </div>

        <div class="moderation-item" *ngFor="let review of reviewData()?.reviews" data-testid="review-item">
          <div class="item-header">
            <span class="status-badge" [class]="review.status.toLowerCase()">
              {{ review.status }}
            </span>
            <div class="rating">
              <span class="stars">{{ '★'.repeat(review.rating) }}{{ '☆'.repeat(5 - review.rating) }}</span>
            </div>
            <a [routerLink]="['/products', review.productSlug]" class="product-link">
              {{ review.productName }}
            </a>
            <span class="date">{{ review.createdAt | date:'medium' }}</span>
          </div>

          <div class="item-content">
            <p class="review-title" *ngIf="review.title"><strong>{{ review.title }}</strong></p>
            <p class="review-text">{{ review.content }}</p>
            <div class="meta">
              <span *ngIf="review.reviewerName">
                <strong>By:</strong> {{ review.reviewerName }}
              </span>
              <span class="verified-badge" *ngIf="review.isVerifiedPurchase">Verified Purchase</span>
            </div>
          </div>

          <div class="item-actions">
            <button
              class="action-btn approve"
              (click)="approveReview(review)"
              [disabled]="processingId() === review.id"
              data-testid="approve-review">
              <span *ngIf="processingId() !== review.id">{{ 'admin.moderation.approve' | translate }}</span>
              <span *ngIf="processingId() === review.id" class="spinner small"></span>
            </button>
            <button
              class="action-btn reject"
              (click)="rejectReview(review)"
              [disabled]="processingId() === review.id"
              data-testid="reject-review">
              {{ 'admin.moderation.reject' | translate }}
            </button>
          </div>
        </div>
      </div>

      <!-- Error Message -->
      <div class="error-message" *ngIf="error()">
        <span>{{ error() }}</span>
        <button (click)="loadData()">{{ 'common.retry' | translate }}</button>
      </div>
    </div>
  `,
  styles: [`
    .moderation-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .moderation-header {
      margin-bottom: 2rem;

      h1 {
        font-size: 2rem;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .stats-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }

    .stat-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 1.5rem;
      background: var(--color-bg-card);
      border-radius: 8px;
      border: 1px solid var(--color-border-primary);

      &.pending .stat-value {
        color: var(--color-warning);
      }

      .stat-value {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-text-primary);
      }

      .stat-label {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        margin-top: 0.25rem;
      }
    }

    .tabs {
      display: flex;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border-primary);
      padding-bottom: 0.5rem;
    }

    .tab {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      background: none;
      border: none;
      border-radius: 8px 8px 0 0;
      color: var(--color-text-secondary);
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        color: var(--color-text-primary);
        background: var(--color-bg-hover);
      }

      &.active {
        color: var(--color-primary);
        background: var(--color-primary-light);
      }

      .badge {
        padding: 0.125rem 0.5rem;
        background: var(--color-warning);
        color: white;
        font-size: 0.75rem;
        border-radius: 10px;
      }
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 3rem;
      color: var(--color-text-secondary);
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid var(--color-border-primary);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;

      &.small {
        width: 16px;
        height: 16px;
        border-width: 2px;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem;
      background: var(--color-bg-card);
      border-radius: 8px;
      color: var(--color-text-secondary);

      .icon {
        font-size: 3rem;
        color: var(--color-success);
        margin-bottom: 1rem;
      }
    }

    .items-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .moderation-item {
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
      padding: 1.5rem;
    }

    .item-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 1rem;
      flex-wrap: wrap;

      .status-badge {
        padding: 0.25rem 0.75rem;
        border-radius: 4px;
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;

        &.pending {
          background: var(--color-warning-light);
          color: var(--color-warning-dark);
        }

        &.flagged {
          background: var(--color-error-light);
          color: var(--color-error-dark);
        }

        &.approved {
          background: var(--color-success-light);
          color: var(--color-success-dark);
        }

        &.rejected {
          background: var(--color-error-light);
          color: var(--color-error-dark);
        }
      }

      .product-link {
        color: var(--color-primary);
        text-decoration: none;
        font-weight: 500;

        &:hover {
          text-decoration: underline;
        }
      }

      .date {
        margin-left: auto;
        font-size: 0.875rem;
        color: var(--color-text-tertiary);
      }
    }

    .item-content {
      margin-bottom: 1rem;

      .question-text,
      .answer-text {
        color: var(--color-text-primary);
        line-height: 1.6;
        margin: 0 0 0.5rem;
      }

      .context-text {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        margin: 0 0 0.5rem;
        padding: 0.5rem;
        background: var(--color-bg-secondary);
        border-radius: 4px;
      }

      .meta {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        display: flex;
        align-items: center;
        gap: 0.5rem;
        flex-wrap: wrap;
      }

      .official-badge,
      .verified-badge {
        padding: 0.125rem 0.5rem;
        background: var(--color-primary);
        color: white;
        font-size: 0.75rem;
        border-radius: 4px;
      }

      .verified-badge {
        background: var(--color-success);
      }

      .review-title {
        margin: 0 0 0.5rem;
        color: var(--color-text-primary);
      }

      .review-text {
        color: var(--color-text-primary);
        line-height: 1.6;
        margin: 0 0 0.5rem;
      }
    }

    .rating {
      .stars {
        color: var(--color-warning);
        font-size: 1rem;
      }
    }

    .item-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .action-btn {
      padding: 0.5rem 1rem;
      border: none;
      border-radius: 6px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 80px;

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      &.approve {
        background: var(--color-success);
        color: white;

        &:hover:not(:disabled) {
          background: var(--color-success-dark);
        }
      }

      &.approve-official {
        background: var(--color-primary);
        color: white;

        &:hover:not(:disabled) {
          background: var(--color-primary-dark);
        }
      }

      &.reject {
        background: var(--color-error);
        color: white;

        &:hover:not(:disabled) {
          background: var(--color-error-dark);
        }
      }
    }

    .error-message {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      padding: 1rem;
      background: var(--color-error-light);
      color: var(--color-error-dark);
      border-radius: 8px;
      margin-top: 1rem;

      button {
        padding: 0.5rem 1rem;
        background: var(--color-error);
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;

        &:hover {
          background: var(--color-error-dark);
        }
      }
    }

    @media (max-width: 768px) {
      .moderation-container {
        padding: 1rem;
      }

      .item-actions {
        flex-direction: column;
      }

      .action-btn {
        width: 100%;
      }
    }
  `]
})
export class AdminModerationComponent implements OnInit {
  private readonly moderationService = inject(ModerationService);

  data = signal<PendingModeration | null>(null);
  reviewData = signal<PendingReviewModeration | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  activeTab = signal<TabType>('questions');
  processingId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadData();
  }

  setTab(tab: TabType): void {
    this.activeTab.set(tab);
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      qa: this.moderationService.getPendingQA(),
      reviews: this.moderationService.getPendingReviews()
    }).subscribe({
      next: ({ qa, reviews }) => {
        this.data.set(qa);
        this.reviewData.set(reviews);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Failed to load moderation data');
        this.loading.set(false);
      }
    });
  }

  approveQuestion(question: AdminQuestion): void {
    this.processingId.set(question.id);
    this.moderationService.approveQuestion(question.id).subscribe({
      next: () => {
        this.processingId.set(null);
        this.loadData();
      },
      error: () => {
        this.processingId.set(null);
      }
    });
  }

  rejectQuestion(question: AdminQuestion): void {
    this.processingId.set(question.id);
    this.moderationService.rejectQuestion(question.id).subscribe({
      next: () => {
        this.processingId.set(null);
        this.loadData();
      },
      error: () => {
        this.processingId.set(null);
      }
    });
  }

  approveAnswer(answer: AdminAnswer, markAsOfficial: boolean): void {
    this.processingId.set(answer.id);
    this.moderationService.approveAnswer(answer.id, markAsOfficial).subscribe({
      next: () => {
        this.processingId.set(null);
        this.loadData();
      },
      error: () => {
        this.processingId.set(null);
      }
    });
  }

  rejectAnswer(answer: AdminAnswer): void {
    this.processingId.set(answer.id);
    this.moderationService.rejectAnswer(answer.id).subscribe({
      next: () => {
        this.processingId.set(null);
        this.loadData();
      },
      error: () => {
        this.processingId.set(null);
      }
    });
  }

  approveReview(review: AdminReview): void {
    this.processingId.set(review.id);
    this.moderationService.approveReview(review.id).subscribe({
      next: () => {
        this.processingId.set(null);
        this.loadData();
      },
      error: () => {
        this.processingId.set(null);
      }
    });
  }

  rejectReview(review: AdminReview): void {
    this.processingId.set(review.id);
    this.moderationService.rejectReview(review.id).subscribe({
      next: () => {
        this.processingId.set(null);
        this.loadData();
      },
      error: () => {
        this.processingId.set(null);
      }
    });
  }
}
