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
        <div class="error">{{ error() }}</div>
      } @else {
        <div class="brands-grid">
          @for (brand of brands(); track brand.id) {
            <a [routerLink]="['/brands', brand.slug]" class="brand-card">
              <div class="brand-logo">
                @if (brand.logoUrl) {
                  <img [src]="brand.logoUrl" [alt]="brand.name" />
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

    .loading, .error {
      text-align: center;
      padding: 4rem 2rem;
      color: var(--color-text-secondary);
    }

    .error {
      color: var(--color-error);
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

  private loadBrands(): void {
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
