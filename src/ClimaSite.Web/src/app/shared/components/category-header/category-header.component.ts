import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

export interface CategoryInfo {
  id: string;
  name: string;
  slug: string;
  description?: string;
  icon?: string;
  imageUrl?: string;
  productCount?: number;
  parentCategory?: {
    name: string;
    slug: string;
  };
}

@Component({
  selector: 'app-category-header',
  standalone: true,
imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="category-header" [class.has-image]="category()?.imageUrl" data-testid="category-header">
<!-- Background Image Overlay -->
      @if (category()?.imageUrl) {
        <div
          class="header-background"
          [style.backgroundImage]="'url(' + category()!.imageUrl + ')'"
        ></div>
      }

      <div class="header-content">
        <!-- Breadcrumb -->
        <nav class="breadcrumb" [attr.aria-label]="'common.aria.breadcrumb' | translate" data-testid="category-breadcrumb">
          <a routerLink="/" class="breadcrumb-link">{{ 'nav.home' | translate }}</a>
          <span class="separator">/</span>
          <a routerLink="/products" class="breadcrumb-link">{{ 'nav.products' | translate }}</a>
          @if (category()?.parentCategory; as parentCategory) {
            <span class="separator">/</span>
            <a [routerLink]="['/products/category', parentCategory.slug]" class="breadcrumb-link">
              {{ parentCategory.name }}
            </a>
          }
          <span class="separator">/</span>
          <span class="breadcrumb-current">{{ category()?.name }}</span>
        </nav>

        <!-- Category Icon -->
        @if (category()?.icon && !category()?.imageUrl) {
          <div class="category-icon">
            <span>{{ getIconEmoji(category()!.icon!) }}</span>
          </div>
        }

        <!-- Category Title -->
        <h1 class="category-title" data-testid="category-title">{{ category()?.name }}</h1>

        <!-- Category Description -->
        @if (category()?.description) {
          <p class="category-description" data-testid="category-description">
            {{ category()?.description }}
          </p>
        }

        <!-- Product Count -->
        @if (category()?.productCount !== undefined) {
          <div class="product-count" data-testid="product-count">
            {{ 'products.itemsFound' | translate: { count: category()!.productCount } }}
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .category-header {
      position: relative;
      padding: 2rem;
      margin-bottom: 2rem;
      background: var(--color-bg-secondary);
      border-radius: 12px;
      overflow: hidden;

      &.has-image {
        min-height: 200px;
        color: white;

        .breadcrumb-link,
        .breadcrumb-current,
        .separator {
          color: rgba(255, 255, 255, 0.9);
        }

        .breadcrumb-link:hover {
          color: white;
        }

        .category-title,
        .category-description,
        .product-count {
          color: white;
        }
      }
    }

    .header-background {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-size: cover;
      background-position: center;
      z-index: 0;

      &::after {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: linear-gradient(135deg, rgba(0, 0, 0, 0.7) 0%, rgba(0, 0, 0, 0.4) 100%);
      }
    }

    .header-content {
      position: relative;
      z-index: 1;
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1rem;
      font-size: 0.875rem;
      flex-wrap: wrap;
    }

    .breadcrumb-link {
      color: var(--color-text-secondary);
      text-decoration: none;
      transition: color 0.2s;

      &:hover {
        color: var(--color-primary);
      }
    }

    .separator {
      color: var(--color-text-tertiary);
    }

    .breadcrumb-current {
      color: var(--color-text-primary);
      font-weight: 500;
    }

    .category-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 64px;
      height: 64px;
      margin-bottom: 1rem;
      background: var(--color-primary-light);
      border-radius: 16px;
      font-size: 2rem;
    }

    .category-title {
      font-size: 2rem;
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0 0 0.5rem;

      @media (max-width: 768px) {
        font-size: 1.5rem;
      }
    }

    .category-description {
      font-size: 1rem;
      color: var(--color-text-secondary);
      margin: 0 0 1rem;
      max-width: 600px;
      line-height: 1.6;
    }

    .product-count {
      font-size: 0.875rem;
      color: var(--color-text-tertiary);
      font-weight: 500;
    }

    @media (max-width: 768px) {
      .category-header {
        padding: 1.5rem;
      }

      .category-icon {
        width: 48px;
        height: 48px;
        font-size: 1.5rem;
      }
    }
  `]
})
export class CategoryHeaderComponent {
  category = input<CategoryInfo | null>(null);

  getIconEmoji(icon: string): string {
    const iconMap: Record<string, string> = {
      'snowflake': '❄️',
      'fire': '🔥',
      'wind': '💨',
      'tools': '🔧',
      'thermometer': '🌡️',
      'fan': '🌀',
      'home': '🏠',
      'leaf': '🍃',
      'sun': '☀️',
      'plug': '🔌'
    };
    return iconMap[icon] || '📦';
  }
}
