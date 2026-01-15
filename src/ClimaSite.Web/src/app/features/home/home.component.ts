import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ProductService } from '../../core/services/product.service';
import { ProductBrief } from '../../core/models/product.model';
import { ProductCardComponent } from '../products/product-card/product-card.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, ProductCardComponent],
  template: `
    <div class="home-container">
      <!-- HOME-001: Enhanced hero section -->
      <section class="hero" data-testid="hero-section">
        <div class="hero-content">
          <h1>{{ 'common.tagline' | translate }}</h1>
          <p>{{ 'home.hero.subtitle' | translate }}</p>
          <a routerLink="/products" class="cta-button" data-testid="hero-cta">{{ 'home.hero.cta' | translate }}</a>
        </div>
      </section>

      <!-- HOME-001: Benefits section -->
      <section class="benefits-section" data-testid="benefits-section">
        <div class="benefits-grid">
          <div class="benefit-card">
            <div class="benefit-icon">🚚</div>
            <h3>{{ 'home.benefits.freeShipping' | translate }}</h3>
            <p>{{ 'home.benefits.freeShippingDesc' | translate }}</p>
          </div>
          <div class="benefit-card">
            <div class="benefit-icon">🛡️</div>
            <h3>{{ 'home.benefits.warranty' | translate }}</h3>
            <p>{{ 'home.benefits.warrantyDesc' | translate }}</p>
          </div>
          <div class="benefit-card">
            <div class="benefit-icon">💬</div>
            <h3>{{ 'home.benefits.support' | translate }}</h3>
            <p>{{ 'home.benefits.supportDesc' | translate }}</p>
          </div>
          <div class="benefit-card">
            <div class="benefit-icon">🔧</div>
            <h3>{{ 'home.benefits.installation' | translate }}</h3>
            <p>{{ 'home.benefits.installationDesc' | translate }}</p>
          </div>
        </div>
      </section>

      <!-- NAV-001 FIX: Use route-based navigation instead of query params -->
      <section class="categories-section">
        <h2>{{ 'home.categories.title' | translate }}</h2>
        <div class="categories-grid">
          <a [routerLink]="['/products/category', 'air-conditioning']" class="category-card" data-testid="category-card">
            <div class="category-icon">❄️</div>
            <h3>{{ 'categories.airConditioning' | translate }}</h3>
          </a>
          <a [routerLink]="['/products/category', 'heating']" class="category-card" data-testid="category-card">
            <div class="category-icon">🔥</div>
            <h3>{{ 'categories.heatingSystems' | translate }}</h3>
          </a>
          <a [routerLink]="['/products/category', 'ventilation']" class="category-card" data-testid="category-card">
            <div class="category-icon">💨</div>
            <h3>{{ 'categories.ventilation' | translate }}</h3>
          </a>
          <a [routerLink]="['/products/category', 'accessories']" class="category-card" data-testid="category-card">
            <div class="category-icon">🔧</div>
            <h3>{{ 'categories.accessories' | translate }}</h3>
          </a>
        </div>
      </section>

      <!-- HOME-001: Featured products with actual loading -->
      <section class="featured-section" data-testid="featured-products">
        <h2>{{ 'home.featured.title' | translate }}</h2>
        @if (loadingFeatured()) {
          <div class="featured-loading">
            <p>{{ 'common.loading' | translate }}</p>
          </div>
        } @else if (featuredProducts().length === 0) {
          <div class="featured-empty">
            <p>{{ 'products.noProducts' | translate }}</p>
          </div>
        } @else {
          <div class="featured-grid">
            @for (product of featuredProducts(); track product.id) {
              <app-product-card [product]="product" />
            }
          </div>
        }
        <a routerLink="/products" class="view-all-link" data-testid="view-all-products">{{ 'home.featured.viewAll' | translate }} →</a>
      </section>
    </div>
  `,
  styles: [`
    .home-container {
      padding: 0;
    }

    .hero {
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      color: white;
      padding: 4rem 2rem;
      text-align: center;
      min-height: 400px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .hero-content {
      max-width: 800px;

      h1 {
        font-size: 3rem;
        margin-bottom: 1rem;
        font-weight: 700;
      }

      p {
        font-size: 1.25rem;
        margin-bottom: 2rem;
        opacity: 0.9;
      }
    }

    .cta-button {
      display: inline-block;
      background: white;
      color: var(--color-primary);
      padding: 1rem 2.5rem;
      border-radius: 8px;
      text-decoration: none;
      font-weight: 600;
      font-size: 1.125rem;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      }
    }

    .categories-section {
      padding: 4rem 2rem;
      max-width: 1200px;
      margin: 0 auto;

      h2 {
        text-align: center;
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    .categories-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .category-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 2rem;
      text-align: center;
      text-decoration: none;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
      }

      .category-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
      }

      h3 {
        color: var(--color-text-primary);
        font-size: 1.25rem;
        font-weight: 600;
      }
    }

    .featured-section {
      padding: 4rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
      text-align: center;

      h2 {
        font-size: 2rem;
        margin-bottom: 2rem;
        color: var(--color-text-primary);
      }
    }

    /* HOME-001: Benefits section styles */
    .benefits-section {
      background: var(--color-bg-secondary);
      padding: 3rem 2rem;
    }

    .benefits-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .benefit-card {
      background: var(--color-bg-primary);
      border-radius: 12px;
      padding: 1.5rem;
      text-align: center;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);

      .benefit-icon {
        font-size: 2.5rem;
        margin-bottom: 1rem;
      }

      h3 {
        color: var(--color-text-primary);
        font-size: 1rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        margin: 0;
      }
    }

    /* HOME-001: Featured products grid */
    .featured-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
      text-align: left;
    }

    .featured-loading,
    .featured-empty {
      padding: 3rem;
      text-align: center;
      color: var(--color-text-secondary);
    }

    .view-all-link {
      display: inline-block;
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 600;

      &:hover {
        text-decoration: underline;
      }
    }

    @media (max-width: 768px) {
      .hero-content h1 {
        font-size: 2rem;
      }

      .benefits-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    @media (max-width: 480px) {
      .benefits-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class HomeComponent implements OnInit {
  private readonly productService = inject(ProductService);

  featuredProducts = signal<ProductBrief[]>([]);
  loadingFeatured = signal(true);

  ngOnInit(): void {
    this.loadFeaturedProducts();
  }

  private loadFeaturedProducts(): void {
    this.loadingFeatured.set(true);
    this.productService.getFeaturedProducts(8).subscribe({
      next: (products) => {
        this.featuredProducts.set(products);
        this.loadingFeatured.set(false);
      },
      error: () => {
        this.featuredProducts.set([]);
        this.loadingFeatured.set(false);
      }
    });
  }
}
