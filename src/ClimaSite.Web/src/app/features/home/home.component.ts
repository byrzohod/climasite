import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="home-container">
      <section class="hero">
        <div class="hero-content">
          <h1>{{ 'common.tagline' | translate }}</h1>
          <p>Quality HVAC products for your home and business</p>
          <a routerLink="/products" class="cta-button">{{ 'common.viewAll' | translate }} {{ 'nav.products' | translate }}</a>
        </div>
      </section>

      <section class="categories-section">
        <h2>{{ 'nav.categories' | translate }}</h2>
        <div class="categories-grid">
          <a routerLink="/products" [queryParams]="{category: 'air-conditioners'}" class="category-card" data-testid="category-card">
            <div class="category-icon">❄️</div>
            <h3>{{ 'nav.airConditioners' | translate }}</h3>
          </a>
          <a routerLink="/products" [queryParams]="{category: 'heating'}" class="category-card" data-testid="category-card">
            <div class="category-icon">🔥</div>
            <h3>{{ 'nav.heatingSystems' | translate }}</h3>
          </a>
          <a routerLink="/products" [queryParams]="{category: 'ventilation'}" class="category-card" data-testid="category-card">
            <div class="category-icon">💨</div>
            <h3>{{ 'nav.ventilation' | translate }}</h3>
          </a>
          <a routerLink="/products" [queryParams]="{category: 'accessories'}" class="category-card" data-testid="category-card">
            <div class="category-icon">🔧</div>
            <h3>{{ 'nav.accessories' | translate }}</h3>
          </a>
        </div>
      </section>

      <section class="featured-section" data-testid="featured-products">
        <h2>{{ 'home.featured.title' | translate }}</h2>
        <div class="featured-grid">
          <p class="coming-soon">{{ 'products.featured' | translate }} - {{ 'common.loading' | translate }}</p>
        </div>
        <a routerLink="/products" class="view-all-link">{{ 'home.featured.viewAll' | translate }} →</a>
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

    .featured-grid {
      margin-bottom: 2rem;
    }

    .coming-soon {
      color: var(--color-text-secondary);
      font-size: 1rem;
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
    }
  `]
})
export class HomeComponent {}
