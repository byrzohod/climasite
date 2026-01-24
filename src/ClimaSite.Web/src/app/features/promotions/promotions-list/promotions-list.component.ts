import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { PromotionService } from '../../../core/services/promotion.service';
import { PromotionBrief, PromotionType } from '../../../core/models/promotion.model';

@Component({
  selector: 'app-promotions-list',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="promotions-page">
      <div class="page-header">
        <h1>{{ 'promotions.title' | translate }}</h1>
        <p class="subtitle">{{ 'promotions.subtitle' | translate }}</p>
      </div>

      @if (isLoading()) {
        <div class="loading">{{ 'common.loading' | translate }}</div>
      } @else if (promotions().length === 0) {
        <div class="empty-state">
          <div class="empty-icon">🎉</div>
          <p>{{ 'promotions.noPromotions' | translate }}</p>
        </div>
      } @else {
        <div class="promotions-grid">
          @for (promotion of promotions(); track promotion.id) {
            <a [routerLink]="['/promotions', promotion.slug]" class="promotion-card">
              <div class="promotion-image">
                @if (promotion.bannerImageUrl || promotion.thumbnailImageUrl) {
                  <img [src]="promotion.thumbnailImageUrl || promotion.bannerImageUrl" [alt]="promotion.name" loading="lazy" />
                } @else {
                  <div class="placeholder-image">
                    <span class="discount-badge">
                      {{ getDiscountText(promotion) }}
                    </span>
                  </div>
                }
                <div class="promo-badge" [class.featured]="promotion.productCount > 10">
                  {{ promotion.productCount }} {{ 'promotions.products' | translate }}
                </div>
              </div>
              <div class="promotion-content">
                <h3 class="promotion-name">{{ promotion.name }}</h3>
                @if (promotion.description) {
                  <p class="promotion-description">{{ promotion.description }}</p>
                }
                <div class="promotion-meta">
                  @if (promotion.code) {
                    <span class="promo-code">
                      {{ 'promotions.useCode' | translate }}: <strong>{{ promotion.code }}</strong>
                    </span>
                  }
                  <span class="validity">
                    {{ 'promotions.activeUntil' | translate }}: {{ promotion.endDate | date:'mediumDate' }}
                  </span>
                </div>
                <div class="cta">
                  <span class="btn-shop">{{ 'promotions.shopNow' | translate }}</span>
                </div>
              </div>
            </a>
          }
        </div>

        @if (hasMore()) {
          <div class="load-more">
            <button class="btn-load-more" (click)="loadMore()" [disabled]="isLoadingMore()">
              @if (isLoadingMore()) {
                {{ 'common.loading' | translate }}
              } @else {
                {{ 'common.showMore' | translate }}
              }
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .promotions-page {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .page-header {
      text-align: center;
      margin-bottom: 3rem;

      h1 {
        font-size: 2.5rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        font-size: 1.125rem;
      }
    }

    .loading, .empty-state {
      text-align: center;
      padding: 4rem 2rem;
      color: var(--color-text-secondary);
    }

    .empty-state {
      .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
      }
    }

    .promotions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 2rem;
    }

    .promotion-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 16px;
      overflow: hidden;
      text-decoration: none;
      color: inherit;
      transition: all 0.3s;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 12px 24px rgba(0, 0, 0, 0.1);
        border-color: var(--color-primary);
      }
    }

    .promotion-image {
      position: relative;
      aspect-ratio: 16 / 9;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      overflow: hidden;

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }

      .placeholder-image {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;

        .discount-badge {
          font-size: 2rem;
          font-weight: 700;
          color: white;
          text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
        }
      }

      .promo-badge {
        position: absolute;
        top: 1rem;
        right: 1rem;
        padding: 0.5rem 1rem;
        background: rgba(0, 0, 0, 0.7);
        color: white;
        font-size: 0.875rem;
        font-weight: 500;
        border-radius: 20px;

        &.featured {
          background: var(--color-warning, #f59e0b);
        }
      }
    }

    .promotion-content {
      padding: 1.5rem;
    }

    .promotion-name {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--color-text-primary);
      margin: 0 0 0.75rem;
    }

    .promotion-description {
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      line-height: 1.5;
      margin: 0 0 1rem;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .promotion-meta {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      margin-bottom: 1rem;
      font-size: 0.875rem;

      .promo-code {
        color: var(--color-primary);

        strong {
          background: var(--color-bg-secondary);
          padding: 0.25rem 0.5rem;
          border-radius: 4px;
          font-family: monospace;
        }
      }

      .validity {
        color: var(--color-text-secondary);
      }
    }

    .cta {
      .btn-shop {
        display: inline-block;
        padding: 0.75rem 1.5rem;
        background: var(--color-primary);
        color: white;
        border-radius: 8px;
        font-weight: 600;
        transition: background-color 0.2s;

        .promotion-card:hover & {
          background: var(--color-primary-dark);
        }
      }
    }

    .load-more {
      text-align: center;
      margin-top: 3rem;

      .btn-load-more {
        padding: 1rem 2rem;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border);
        border-radius: 8px;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;

        &:hover:not(:disabled) {
          background: var(--color-primary);
          color: white;
          border-color: var(--color-primary);
        }

        &:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }
      }
    }

    @media (max-width: 768px) {
      .promotions-grid {
        grid-template-columns: 1fr;
      }

      .page-header h1 {
        font-size: 1.75rem;
      }
    }
  `]
})
export class PromotionsListComponent implements OnInit {
  private readonly promotionService = inject(PromotionService);

  promotions = signal<PromotionBrief[]>([]);
  isLoading = signal(true);
  isLoadingMore = signal(false);
  hasMore = signal(false);
  private currentPage = 1;
  private pageSize = 12;

  ngOnInit(): void {
    this.loadPromotions();
  }

  private loadPromotions(): void {
    this.isLoading.set(true);
    this.promotionService.getActivePromotions(this.currentPage, this.pageSize).subscribe({
      next: (result) => {
        this.promotions.set(result.items);
        this.hasMore.set(this.currentPage < result.totalPages);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load promotions:', err);
        this.isLoading.set(false);
      }
    });
  }

  loadMore(): void {
    if (this.isLoadingMore()) return;

    this.isLoadingMore.set(true);
    this.currentPage++;

    this.promotionService.getActivePromotions(this.currentPage, this.pageSize).subscribe({
      next: (result) => {
        this.promotions.update(current => [...current, ...result.items]);
        this.hasMore.set(this.currentPage < result.totalPages);
        this.isLoadingMore.set(false);
      },
      error: (err) => {
        console.error('Failed to load more promotions:', err);
        this.isLoadingMore.set(false);
        this.currentPage--;
      }
    });
  }

  getDiscountText(promotion: PromotionBrief): string {
    switch (promotion.type) {
      case PromotionType.Percentage:
        return `-${promotion.discountValue}%`;
      case PromotionType.FixedAmount:
        return `-€${promotion.discountValue}`;
      case PromotionType.FreeShipping:
        return 'Free Shipping';
      case PromotionType.BuyOneGetOne:
        return 'BOGO';
      default:
        return 'SALE';
    }
  }
}
