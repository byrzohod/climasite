import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { BrandService } from '../../../core/services/brand.service';
import { BrandBrief } from '../../../core/models/brand.model';

@Component({
  selector: 'app-brands-list',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="brands-page">
      <div class="page-header">
        <h1>{{ 'brands.title' | translate }}</h1>
        <p class="subtitle">{{ 'brands.subtitle' | translate }}</p>
      </div>

      @if (isLoading() && brands().length === 0) {
        <div class="loading">{{ 'common.loading' | translate }}</div>
      } @else if (error()) {
        <div class="error-container">
          <div class="error-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
            </svg>
          </div>
          <p class="error-message">{{ error() }}</p>
          <button class="retry-btn" (click)="loadBrands()">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path fill-rule="evenodd" d="M4.755 10.059a7.5 7.5 0 0112.548-3.364l1.903 1.903h-3.183a.75.75 0 100 1.5h4.992a.75.75 0 00.75-.75V4.356a.75.75 0 00-1.5 0v3.18l-1.9-1.9A9 9 0 003.306 9.67a.75.75 0 101.45.388zm15.408 3.352a.75.75 0 00-.919.53 7.5 7.5 0 01-12.548 3.364l-1.902-1.903h3.183a.75.75 0 000-1.5H2.984a.75.75 0 00-.75.75v4.992a.75.75 0 001.5 0v-3.18l1.9 1.9a9 9 0 0015.059-4.035.75.75 0 00-.53-.918z" clip-rule="evenodd" />
            </svg>
            {{ 'common.retry' | translate }}
          </button>
        </div>
      } @else {
        <div class="brands-grid">
          @for (brand of brands(); track brand.id) {
            <a [routerLink]="['/brands', brand.slug]" class="brand-card">
              <div class="brand-logo">
                @if (brand.logoUrl) {
                  <img [src]="brand.logoUrl" [alt]="brand.name" loading="lazy" />
                } @else {
                  <div class="logo-placeholder">{{ brand.name.charAt(0) }}</div>
                }
              </div>
              <div class="brand-info">
                <h3>{{ brand.name }}</h3>
                @if (brand.countryOfOrigin) {
                  <span class="country">{{ brand.countryOfOrigin }}</span>
                }
                @if (brand.description) {
                  <p class="description">{{ brand.description }}</p>
                }
                <span class="product-count">
                  {{ brand.productCount }} {{ 'brands.products' | translate }}
                </span>
              </div>
              @if (brand.isFeatured) {
                <span class="featured-badge">{{ 'brands.featured' | translate }}</span>
              }
            </a>
          }
        </div>

        @if (brands().length === 0 && !isLoading()) {
          <div class="no-brands">
            <p>{{ 'brands.noBrands' | translate }}</p>
          </div>
        }

        @if (hasNextPage()) {
          <div class="load-more">
            <button
              class="btn-load-more"
              (click)="loadMore()"
              [disabled]="isLoading()">
              @if (isLoading()) {
                <span class="spinner"></span>
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
    .brands-page {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }

    .page-header {
      text-align: center;
      margin-bottom: 3rem;

      h1 {
        font-size: 2.5rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .subtitle {
        font-size: 1.125rem;
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .loading {
      text-align: center;
      padding: 4rem 2rem;
      color: var(--color-text-secondary);
    }

    .error-container {
      text-align: center;
      padding: 4rem 2rem;
      background: var(--color-bg-secondary);
      border-radius: 16px;
      max-width: 400px;
      margin: 2rem auto;
    }

    .error-icon {
      color: var(--color-error);
      margin-bottom: 1rem;

      svg {
        width: 48px;
        height: 48px;
      }
    }

    .error-message {
      color: var(--color-text-secondary);
      margin: 0 0 1.5rem;
      font-size: 1rem;
    }

    .retry-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;

      svg {
        width: 18px;
        height: 18px;
      }

      &:hover {
        background: var(--color-primary-dark);
      }
    }

    .brands-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
    }

    .brand-card {
      position: relative;
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 2rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 16px;
      text-decoration: none;
      transition: all 0.3s ease;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 12px 24px rgba(0, 0, 0, 0.1);
        border-color: var(--color-primary);
      }
    }

    .brand-logo {
      width: 120px;
      height: 80px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 1.5rem;

      img {
        max-width: 100%;
        max-height: 100%;
        object-fit: contain;
      }

      .logo-placeholder {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: linear-gradient(135deg, var(--color-primary), var(--color-primary-dark));
        color: white;
        font-size: 2rem;
        font-weight: 700;
        display: flex;
        align-items: center;
        justify-content: center;
      }
    }

    .brand-info {
      text-align: center;

      h3 {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin: 0 0 0.25rem;
      }

      .country {
        display: block;
        font-size: 0.75rem;
        color: var(--color-text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-bottom: 0.75rem;
      }

      .description {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
        line-height: 1.5;
        margin: 0 0 0.75rem;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
      }

      .product-count {
        font-size: 0.875rem;
        color: var(--color-primary);
        font-weight: 500;
      }
    }

    .featured-badge {
      position: absolute;
      top: 1rem;
      right: 1rem;
      padding: 0.25rem 0.75rem;
      background: var(--color-primary);
      color: white;
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: 20px;
    }

    .no-brands {
      text-align: center;
      padding: 4rem 2rem;
      color: var(--color-text-secondary);
    }

    .load-more {
      display: flex;
      justify-content: center;
      margin-top: 3rem;
    }

    .btn-load-more {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 2rem;
      background: var(--color-bg-primary);
      color: var(--color-text-primary);
      border: 2px solid var(--color-border);
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        border-color: var(--color-primary);
        color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .spinner {
        width: 16px;
        height: 16px;
        border: 2px solid transparent;
        border-top-color: currentColor;
        border-radius: 50%;
        animation: spin 0.6s linear infinite;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
      .page-header h1 {
        font-size: 1.75rem;
      }

      .brands-grid {
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
      }

      .brand-card {
        padding: 1.5rem 1rem;
      }

      .brand-logo {
        width: 80px;
        height: 60px;

        .logo-placeholder {
          width: 60px;
          height: 60px;
          font-size: 1.5rem;
        }
      }
    }
  `]
})
export class BrandsListComponent implements OnInit {
  private readonly brandService = inject(BrandService);

  brands = signal<BrandBrief[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  currentPage = signal(1);
  hasNextPage = signal(false);

  private readonly pageSize = 24;

  ngOnInit(): void {
    this.loadBrands();
  }

  loadBrands(): void {
    this.isLoading.set(true);
    this.brandService.getBrands(this.currentPage(), this.pageSize).subscribe({
      next: (response) => {
        if (this.currentPage() === 1) {
          this.brands.set(response.items);
        } else {
          this.brands.update(current => [...current, ...response.items]);
        }
        this.hasNextPage.set(response.hasNextPage);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load brands:', err);
        this.error.set('Failed to load brands');
        this.isLoading.set(false);
      }
    });
  }

  loadMore(): void {
    this.currentPage.update(page => page + 1);
    this.loadBrands();
  }
}
