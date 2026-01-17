import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton-product-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-card" data-testid="skeleton-product-card">
      <div class="skeleton-image shimmer"></div>
      <div class="skeleton-content">
        <div class="skeleton-category shimmer"></div>
        <div class="skeleton-title shimmer"></div>
        <div class="skeleton-title-short shimmer"></div>
        <div class="skeleton-rating shimmer"></div>
        <div class="skeleton-price shimmer"></div>
      </div>
    </div>
  `,
  styles: [`
    .skeleton-card {
      background: var(--color-bg-primary);
      border-radius: 12px;
      overflow: hidden;
      border: 1px solid var(--color-border);
    }

    .skeleton-image {
      width: 100%;
      height: 200px;
      background: var(--color-bg-secondary);
    }

    .skeleton-content {
      padding: 1rem;
    }

    .skeleton-category {
      width: 60%;
      height: 12px;
      border-radius: 4px;
      margin-bottom: 0.75rem;
      background: var(--color-bg-secondary);
    }

    .skeleton-title {
      width: 100%;
      height: 16px;
      border-radius: 4px;
      margin-bottom: 0.5rem;
      background: var(--color-bg-secondary);
    }

    .skeleton-title-short {
      width: 70%;
      height: 16px;
      border-radius: 4px;
      margin-bottom: 1rem;
      background: var(--color-bg-secondary);
    }

    .skeleton-rating {
      width: 40%;
      height: 14px;
      border-radius: 4px;
      margin-bottom: 1rem;
      background: var(--color-bg-secondary);
    }

    .skeleton-price {
      width: 50%;
      height: 24px;
      border-radius: 4px;
      background: var(--color-bg-secondary);
    }

    .shimmer {
      position: relative;
      overflow: hidden;

      &::after {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: linear-gradient(
          90deg,
          transparent,
          rgba(255, 255, 255, 0.2),
          transparent
        );
        animation: shimmer 1.5s infinite;
      }
    }

    @keyframes shimmer {
      0% {
        transform: translateX(-100%);
      }
      100% {
        transform: translateX(100%);
      }
    }

    @media (prefers-reduced-motion: reduce) {
      .shimmer::after {
        animation: none;
      }
    }
  `]
})
export class SkeletonProductCardComponent {}
