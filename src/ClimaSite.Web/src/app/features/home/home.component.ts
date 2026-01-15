import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ProductService } from '../../core/services/product.service';
import { ProductBrief } from '../../core/models/product.model';
import { ProductCardComponent } from '../products/product-card/product-card.component';

interface HeroSlide {
  title: string;
  subtitle: string;
  cta: string;
  link: string;
  gradient: string;
}

interface PromoCard {
  icon: string;
  title: string;
  description: string;
  link: string;
  color: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, ProductCardComponent],
  template: `
    <div class="home-container">
      <!-- HOME-001: Enhanced Hero Slider -->
      <section class="hero-slider" data-testid="hero-section">
        <div class="hero-slides">
          @for (slide of heroSlides; track slide.title; let i = $index) {
            <div
              class="hero-slide"
              [class.active]="currentSlide() === i"
              [style.background]="slide.gradient"
            >
              <div class="hero-content">
                <h1>{{ slide.title | translate }}</h1>
                <p>{{ slide.subtitle | translate }}</p>
                <a [routerLink]="slide.link" class="cta-button" data-testid="hero-cta">
                  {{ slide.cta | translate }}
                </a>
              </div>
            </div>
          }
        </div>
        <div class="slide-indicators">
          @for (slide of heroSlides; track slide.title; let i = $index) {
            <button
              class="indicator"
              [class.active]="currentSlide() === i"
              (click)="goToSlide(i)"
              [attr.aria-label]="'Go to slide ' + (i + 1)"
            ></button>
          }
        </div>
        <button class="slide-nav prev" (click)="prevSlide()" aria-label="Previous slide">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path d="M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z"/>
          </svg>
        </button>
        <button class="slide-nav next" (click)="nextSlide()" aria-label="Next slide">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
            <path d="M8.59 16.59L10 18l6-6-6-6-1.41 1.41L13.17 12z"/>
          </svg>
        </button>
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

      <!-- HOME-001: Promotional Banners -->
      <section class="promo-section" data-testid="promo-section">
        <div class="promo-grid">
          @for (promo of promoCards; track promo.title) {
            <a [routerLink]="promo.link" class="promo-card" [style.--promo-color]="promo.color">
              <div class="promo-icon">{{ promo.icon }}</div>
              <div class="promo-content">
                <h3>{{ promo.title | translate }}</h3>
                <p>{{ promo.description | translate }}</p>
              </div>
              <div class="promo-arrow">→</div>
            </a>
          }
        </div>
      </section>

      <!-- NAV-001 FIX: Use route-based navigation with correct database slugs -->
      <section class="categories-section">
        <h2>{{ 'home.categories.title' | translate }}</h2>
        <div class="categories-grid">
          <a [routerLink]="['/products/category', 'air-conditioners']" class="category-card" data-testid="category-card">
            <div class="category-icon">❄️</div>
            <h3>{{ 'categories.airConditioning' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
          <a [routerLink]="['/products/category', 'heating-systems']" class="category-card" data-testid="category-card">
            <div class="category-icon">🔥</div>
            <h3>{{ 'categories.heatingSystems' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
          <a [routerLink]="['/products/category', 'ventilation']" class="category-card" data-testid="category-card">
            <div class="category-icon">💨</div>
            <h3>{{ 'categories.ventilation' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
          <a [routerLink]="['/products/category', 'accessories']" class="category-card" data-testid="category-card">
            <div class="category-icon">🔧</div>
            <h3>{{ 'categories.accessories' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
        </div>
      </section>

      <!-- HOME-001: Featured products with actual loading -->
      <section class="featured-section" data-testid="featured-products">
        <div class="section-header">
          <h2>{{ 'home.featured.title' | translate }}</h2>
          <a routerLink="/products" class="view-all-link" data-testid="view-all-products">
            {{ 'home.featured.viewAll' | translate }} →
          </a>
        </div>
        @if (loadingFeatured()) {
          <div class="featured-loading">
            <div class="loading-spinner"></div>
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
      </section>

      <!-- HOME-001: Newsletter Section -->
      <section class="newsletter-section" data-testid="newsletter-section">
        <div class="newsletter-content">
          <h2>{{ 'home.newsletter.title' | translate }}</h2>
          <p>{{ 'home.newsletter.subtitle' | translate }}</p>
          <form class="newsletter-form" (submit)="subscribeNewsletter($event)">
            <input
              type="email"
              [placeholder]="'home.newsletter.placeholder' | translate"
              class="newsletter-input"
              required
            />
            <button type="submit" class="newsletter-button">
              {{ 'home.newsletter.subscribe' | translate }}
            </button>
          </form>
        </div>
      </section>

      <!-- HOME-001: Trust Badges -->
      <section class="trust-section">
        <div class="trust-grid">
          <div class="trust-badge">
            <span class="trust-number">10K+</span>
            <span class="trust-label">{{ 'home.trust.customers' | translate }}</span>
          </div>
          <div class="trust-badge">
            <span class="trust-number">500+</span>
            <span class="trust-label">{{ 'home.trust.products' | translate }}</span>
          </div>
          <div class="trust-badge">
            <span class="trust-number">15+</span>
            <span class="trust-label">{{ 'home.trust.years' | translate }}</span>
          </div>
          <div class="trust-badge">
            <span class="trust-number">4.8★</span>
            <span class="trust-label">{{ 'home.trust.rating' | translate }}</span>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .home-container {
      padding: 0;
    }

    /* Hero Slider */
    .hero-slider {
      position: relative;
      height: 500px;
      overflow: hidden;
    }

    .hero-slides {
      position: relative;
      height: 100%;
    }

    .hero-slide {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      opacity: 0;
      transition: opacity 0.5s ease-in-out;
      color: white;

      &.active {
        opacity: 1;
        z-index: 1;
      }
    }

    .hero-content {
      max-width: 800px;
      text-align: center;
      padding: 2rem;

      h1 {
        font-size: 3.5rem;
        margin-bottom: 1rem;
        font-weight: 700;
        text-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
      }

      p {
        font-size: 1.5rem;
        margin-bottom: 2rem;
        opacity: 0.95;
      }
    }

    .cta-button {
      display: inline-block;
      background: white;
      color: var(--color-primary);
      padding: 1rem 2.5rem;
      border-radius: 50px;
      text-decoration: none;
      font-weight: 600;
      font-size: 1.125rem;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
      }
    }

    .slide-indicators {
      position: absolute;
      bottom: 2rem;
      left: 50%;
      transform: translateX(-50%);
      display: flex;
      gap: 0.75rem;
      z-index: 10;
    }

    .indicator {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.5);
      border: none;
      cursor: pointer;
      transition: all 0.3s;

      &.active {
        background: white;
        transform: scale(1.2);
      }

      &:hover {
        background: rgba(255, 255, 255, 0.8);
      }
    }

    .slide-nav {
      position: absolute;
      top: 50%;
      transform: translateY(-50%);
      width: 50px;
      height: 50px;
      border-radius: 50%;
      background: rgba(255, 255, 255, 0.2);
      border: none;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10;
      transition: background 0.3s;

      svg {
        width: 24px;
        height: 24px;
        fill: white;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.4);
      }

      &.prev { left: 2rem; }
      &.next { right: 2rem; }
    }

    /* Benefits Section */
    .benefits-section {
      background: var(--color-bg-secondary);
      padding: 3rem 2rem;
    }

    .benefits-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
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
      transition: transform 0.2s;

      &:hover {
        transform: translateY(-4px);
      }

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

    /* Promo Section */
    .promo-section {
      padding: 3rem 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .promo-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1.5rem;
    }

    .promo-card {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1.5rem;
      background: var(--color-bg-primary);
      border-radius: 12px;
      border-left: 4px solid var(--promo-color, var(--color-primary));
      text-decoration: none;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateX(4px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }

      .promo-icon {
        font-size: 2rem;
      }

      .promo-content {
        flex: 1;

        h3 {
          color: var(--color-text-primary);
          font-size: 1rem;
          font-weight: 600;
          margin-bottom: 0.25rem;
        }

        p {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
          margin: 0;
        }
      }

      .promo-arrow {
        color: var(--promo-color, var(--color-primary));
        font-size: 1.5rem;
        font-weight: bold;
      }
    }

    /* Categories Section */
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
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
    }

    .category-card {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 16px;
      padding: 2rem;
      text-align: center;
      text-decoration: none;
      transition: all 0.3s;

      &:hover {
        transform: translateY(-8px);
        box-shadow: 0 12px 32px rgba(0, 0, 0, 0.12);
        border-color: var(--color-primary);
      }

      .category-icon {
        font-size: 3.5rem;
        margin-bottom: 1rem;
      }

      h3 {
        color: var(--color-text-primary);
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
      }

      .category-count {
        color: var(--color-primary);
        font-size: 0.875rem;
        font-weight: 500;
      }
    }

    /* Featured Section */
    .featured-section {
      padding: 4rem 2rem;
      max-width: 1400px;
      margin: 0 auto;
      background: var(--color-bg-secondary);
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;

      h2 {
        font-size: 2rem;
        color: var(--color-text-primary);
        margin: 0;
      }
    }

    .view-all-link {
      color: var(--color-primary);
      text-decoration: none;
      font-weight: 600;
      transition: color 0.2s;

      &:hover {
        color: var(--color-primary-dark);
      }
    }

    .featured-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
    }

    .featured-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 4rem;
      gap: 1rem;
    }

    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 3px solid var(--color-border);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .featured-empty {
      padding: 3rem;
      text-align: center;
      color: var(--color-text-secondary);
    }

    /* Newsletter Section */
    .newsletter-section {
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
      padding: 4rem 2rem;
      color: white;
    }

    .newsletter-content {
      max-width: 600px;
      margin: 0 auto;
      text-align: center;

      h2 {
        font-size: 2rem;
        margin-bottom: 0.5rem;
      }

      p {
        opacity: 0.9;
        margin-bottom: 2rem;
      }
    }

    .newsletter-form {
      display: flex;
      gap: 0.5rem;
      max-width: 500px;
      margin: 0 auto;
    }

    .newsletter-input {
      flex: 1;
      padding: 1rem 1.5rem;
      border: none;
      border-radius: 50px;
      font-size: 1rem;
      outline: none;
    }

    .newsletter-button {
      padding: 1rem 2rem;
      background: white;
      color: var(--color-primary);
      border: none;
      border-radius: 50px;
      font-weight: 600;
      cursor: pointer;
      transition: transform 0.2s;

      &:hover {
        transform: scale(1.05);
      }
    }

    /* Trust Section */
    .trust-section {
      padding: 3rem 2rem;
      background: var(--color-bg-primary);
    }

    .trust-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 2rem;
      max-width: 1000px;
      margin: 0 auto;
      text-align: center;
    }

    .trust-badge {
      .trust-number {
        display: block;
        font-size: 2.5rem;
        font-weight: 700;
        color: var(--color-primary);
        margin-bottom: 0.5rem;
      }

      .trust-label {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
      }
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .hero-content h1 { font-size: 2.5rem; }
      .benefits-grid { grid-template-columns: repeat(2, 1fr); }
      .promo-grid { grid-template-columns: 1fr; }
      .categories-grid { grid-template-columns: repeat(2, 1fr); }
      .featured-grid { grid-template-columns: repeat(2, 1fr); }
      .trust-grid { grid-template-columns: repeat(2, 1fr); }
    }

    @media (max-width: 768px) {
      .hero-slider { height: 400px; }
      .hero-content h1 { font-size: 2rem; }
      .hero-content p { font-size: 1.125rem; }
      .slide-nav { display: none; }
      .section-header { flex-direction: column; gap: 1rem; }
    }

    @media (max-width: 480px) {
      .hero-slider { height: 350px; }
      .benefits-grid { grid-template-columns: 1fr; }
      .categories-grid { grid-template-columns: 1fr; }
      .featured-grid { grid-template-columns: 1fr; }
      .trust-grid { grid-template-columns: repeat(2, 1fr); }
      .newsletter-form { flex-direction: column; }
    }
  `]
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly productService = inject(ProductService);
  private slideInterval: ReturnType<typeof setInterval> | null = null;

  featuredProducts = signal<ProductBrief[]>([]);
  loadingFeatured = signal(true);
  currentSlide = signal(0);

  heroSlides: HeroSlide[] = [
    {
      title: 'common.tagline',
      subtitle: 'home.hero.subtitle',
      cta: 'home.hero.cta',
      link: '/products',
      gradient: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)'
    },
    {
      title: 'home.slides.summer.title',
      subtitle: 'home.slides.summer.subtitle',
      cta: 'home.slides.summer.cta',
      link: '/products/category/air-conditioners',
      gradient: 'linear-gradient(135deg, #06b6d4 0%, #0891b2 100%)'
    },
    {
      title: 'home.slides.winter.title',
      subtitle: 'home.slides.winter.subtitle',
      cta: 'home.slides.winter.cta',
      link: '/products/category/heating-systems',
      gradient: 'linear-gradient(135deg, #f97316 0%, #ea580c 100%)'
    }
  ];

  promoCards: PromoCard[] = [
    {
      icon: '🏷️',
      title: 'home.promo.sale.title',
      description: 'home.promo.sale.description',
      link: '/promotions',
      color: '#ef4444'
    },
    {
      icon: '⚡',
      title: 'home.promo.new.title',
      description: 'home.promo.new.description',
      link: '/products',
      color: '#8b5cf6'
    },
    {
      icon: '🎁',
      title: 'home.promo.bundle.title',
      description: 'home.promo.bundle.description',
      link: '/products',
      color: '#10b981'
    }
  ];

  ngOnInit(): void {
    this.loadFeaturedProducts();
    this.startSlideshow();
  }

  ngOnDestroy(): void {
    this.stopSlideshow();
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

  private startSlideshow(): void {
    this.slideInterval = setInterval(() => {
      this.nextSlide();
    }, 5000);
  }

  private stopSlideshow(): void {
    if (this.slideInterval) {
      clearInterval(this.slideInterval);
      this.slideInterval = null;
    }
  }

  nextSlide(): void {
    const next = (this.currentSlide() + 1) % this.heroSlides.length;
    this.currentSlide.set(next);
  }

  prevSlide(): void {
    const prev = (this.currentSlide() - 1 + this.heroSlides.length) % this.heroSlides.length;
    this.currentSlide.set(prev);
  }

  goToSlide(index: number): void {
    this.currentSlide.set(index);
    this.stopSlideshow();
    this.startSlideshow();
  }

  subscribeNewsletter(event: Event): void {
    event.preventDefault();
    // Newsletter subscription logic would go here
    alert('Thank you for subscribing!');
  }
}
