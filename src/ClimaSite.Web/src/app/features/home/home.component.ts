import { Component, inject, signal, OnInit, OnDestroy, PLATFORM_ID, AfterViewInit, ElementRef, ViewChildren, QueryList } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Subscription } from 'rxjs';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ProductService } from '../../core/services/product.service';
import { ProductBrief } from '../../core/models/product.model';
import { ProductCardComponent } from '../products/product-card/product-card.component';
import { RevealDirective } from '../../shared/directives/reveal.directive';
import { CountUpDirective } from '../../shared/directives/count-up.directive';
import { SkeletonProductCardComponent } from '../../shared/components/skeleton-product-card/skeleton-product-card.component';

interface Testimonial {
  id: number;
  name: string;
  location: string;
  rating: number;
  text: string;
  verified: boolean;
  date: string;
  role?: string;
  photoUrl?: string;
}

interface PromoBanner {
  id: string;
  messageKey: string;
  ctaKey: string;
  link: string;
  active: boolean;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    TranslateModule,
    ProductCardComponent,
    RevealDirective,
    CountUpDirective,
    SkeletonProductCardComponent
  ],
  template: `
    <!-- ================================================================
         PROMO BANNER - Seasonal/promotional banner (TASK-21A-003)
         ================================================================ -->
    @if (promoBanner().active && !promoBannerDismissed()) {
      <div class="promo-banner" data-testid="promo-banner">
        <div class="promo-banner__content">
          <span class="promo-banner__message">{{ promoBanner().messageKey | translate }}</span>
          <a [routerLink]="promoBanner().link" class="promo-banner__cta">
            {{ promoBanner().ctaKey | translate }}
            <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
          </a>
        </div>
        <button 
          type="button" 
          class="promo-banner__dismiss" 
          (click)="dismissPromoBanner()"
          [attr.aria-label]="'common.dismiss' | translate"
        >
          <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
        </button>
      </div>
    }

    <!-- ================================================================
         HERO - Product-focused split layout (TASK-21A-001, 002, 005, 006, 007)
         ================================================================ -->
    <section class="hero" data-testid="hero-section">
      <!-- Clean gradient background - no blobs -->
      <div class="hero__bg">
        <div class="hero__gradient-subtle"></div>
      </div>

      <div class="hero__container">
        <div class="hero__grid">
          <!-- Left: Content -->
          <div class="hero__content">
            <p class="hero__eyebrow">{{ 'home.hero.eyebrow' | translate }}</p>
            <h1 class="hero__title">
              <span class="hero__title-line">{{ 'home.hero.title1' | translate }}</span>
              <span class="hero__title-line hero__title-line--accent">{{ 'home.hero.title2' | translate }}</span>
            </h1>
            <p class="hero__subtitle">{{ 'home.hero.subtitle' | translate }}</p>

            <!-- Search Bar (TASK-21A-006) -->
            <form class="hero__search" (submit)="onHeroSearch($event)" data-testid="hero-search">
              <div class="hero__search-input-wrap">
                <svg class="hero__search-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd"/>
                </svg>
                <input
                  type="search"
                  [(ngModel)]="heroSearchQuery"
                  name="heroSearch"
                  class="hero__search-input"
                  [placeholder]="'home.hero.searchPlaceholder' | translate"
                  [attr.aria-label]="'common.search' | translate"
                />
                <button type="submit" class="hero__search-btn" [attr.aria-label]="'common.search' | translate">
                  {{ 'common.search' | translate }}
                </button>
              </div>
            </form>

            <!-- Dual CTAs (TASK-21A-007) -->
            <div class="hero__ctas">
              <a routerLink="/products" class="btn btn--primary btn--large" data-testid="hero-cta-primary">
                {{ 'home.hero.cta.primary' | translate }}
                <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
              </a>
              <a routerLink="/contact" class="btn btn--ghost btn--large" data-testid="hero-cta-secondary">
                {{ 'home.hero.cta.secondary' | translate }}
                <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
              </a>
            </div>

            <!-- Shop by Category quick links -->
            <nav class="hero__categories" aria-label="Shop by category">
              <span class="hero__categories-label">{{ 'home.hero.shopBy' | translate }}</span>
              <div class="hero__categories-links">
                @for (cat of categories; track cat.slug) {
                  <a [routerLink]="['/products/category', cat.slug]" class="hero__category-link">
                    {{ cat.name | translate }}
                  </a>
                }
              </div>
            </nav>
          </div>

          <!-- Right: Product Image (TASK-21A-002) -->
          <div class="hero__product">
            <div class="hero__product-card">
              <div class="hero__product-badge">{{ 'home.hero.featuredBadge' | translate }}</div>
              <img 
                src="https://images.unsplash.com/photo-1625961332771-3f40b0e2bdcf?w=600&h=600&fit=crop&q=80"
                alt="Featured HVAC unit"
                class="hero__product-image"
                loading="eager"
                width="600"
                height="600"
              />
              <div class="hero__product-info">
                <span class="hero__product-name">{{ 'home.hero.featuredProduct' | translate }}</span>
                <span class="hero__product-tagline">{{ 'home.hero.featuredTagline' | translate }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ================================================================
         VALUE STRIP - Above the fold (TASK-21A-004)
         ================================================================ -->
    <section class="values values--compact" data-testid="values-section">
      <div class="values__container">
        @for (value of valueProps; track value.key) {
          <div class="values__item">
            <div class="values__icon" [innerHTML]="value.icon"></div>
            <div class="values__text">
              <span class="values__label">{{ value.key | translate }}</span>
            </div>
          </div>
        }
      </div>
    </section>

    <!-- ================================================================
         BRANDS - Infinite scrolling ticker
         ================================================================ -->
    <section class="brands" data-testid="brands-section">
      <div class="brands__track" (mouseenter)="pauseMarquee = true" (mouseleave)="pauseMarquee = false" [class.paused]="pauseMarquee">
        @for (brand of duplicatedBrands; track $index) {
          <span class="brands__item">{{ brand }}</span>
        }
      </div>
    </section>

    <!-- ================================================================
         VALUE STRIP - Minimal benefit icons
         ================================================================ -->
    <section class="values" data-testid="values-section">
      <div class="values__container">
        @for (value of valueProps; track value.key) {
          <div class="values__item" appReveal="fade-up" [delay]="$index * 100">
            <div class="values__icon" [innerHTML]="value.icon"></div>
            <span class="values__label">{{ value.key | translate }}</span>
          </div>
        }
      </div>
    </section>

    <!-- ================================================================
         CATEGORIES - Bento Grid Layout (TASK-21A-009 to 21A-015)
         Asymmetric grid with gradient backgrounds and icons
         ================================================================ -->
    <section class="categories" data-testid="categories-section">
      <div class="container">
        <header class="section-header">
          <h2 class="section-header__title">{{ 'home.categories.title' | translate }}</h2>
        </header>
      </div>
      <div class="categories__bento">
        @for (cat of categories; track cat.slug; let i = $index) {
          <a
            #categoryPanel
            [routerLink]="['/products/category', cat.slug]"
            class="categories__card"
            [class.categories__card--large]="i === 0"
            [class.categories__card--medium]="i === 3"
            [attr.aria-label]="cat.name | translate"
            [style.--card-gradient]="cat.gradient"
            [style.--card-accent]="cat.accent"
            appReveal="fade-up" [delay]="i * 75"
            data-testid="category-card"
          >
            <div class="categories__card-bg"></div>
            <div class="categories__card-icon" [innerHTML]="cat.icon"></div>
            <div class="categories__card-content">
              <h3 class="categories__card-title">{{ cat.name | translate }}</h3>
              <p class="categories__card-tagline">{{ cat.tagline | translate }}</p>
              <span class="categories__card-link">
                {{ 'home.categories.explore' | translate }}
                <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
              </span>
            </div>
          </a>
        }
      </div>
    </section>

    <!-- ================================================================
         PROCESS - How it works timeline
         ================================================================ -->
    <section class="process" data-testid="process-section">
      <div class="container">
        <header class="section-header">
          <h2 class="section-header__title">{{ 'home.process.title' | translate }}</h2>
        </header>

        <div class="process__timeline">
          <div class="process__line" #processLine></div>
          @for (step of processSteps; track step.num) {
            <div class="process__step" appReveal="fade-up" [delay]="$index * 150">
              <div class="process__step-number">{{ step.num }}</div>
              <div class="process__step-content">
                <h3 class="process__step-title">{{ step.title | translate }}</h3>
                <p class="process__step-desc">{{ step.desc | translate }}</p>
              </div>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ================================================================
         FEATURED PRODUCTS - Clean product grid
         ================================================================ -->
    <section class="products" data-testid="featured-products">
      <div class="container">
        <header class="section-header section-header--row">
          <h2 class="section-header__title">{{ 'home.products.title' | translate }}</h2>
          <a routerLink="/products" class="section-header__link" data-testid="view-all-products">
            {{ 'home.products.viewAll' | translate }}
            <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
          </a>
        </header>

        @if (loadingFeatured()) {
          <div class="products__grid">
            @for (i of [1,2,3,4]; track i) {
              <app-skeleton-product-card />
            }
          </div>
        } @else if (featuredProducts().length === 0) {
          <p class="products__empty">{{ 'products.noProducts' | translate }}</p>
        } @else {
          <div class="products__grid">
            @for (product of featuredProducts().slice(0, 4); track product.id) {
              <div class="products__item" appReveal="fade-up" [delay]="$index * 100">
                <app-product-card [product]="product" />
              </div>
            }
          </div>
        }
      </div>
    </section>

    <!-- ================================================================
         STATS - Large numbers on dark background
         ================================================================ -->
    <section class="stats" data-testid="stats-section" appReveal="fade">
      <div class="container">
        <div class="stats__grid">
          @for (stat of stats; track stat.label) {
            <div class="stats__item" appReveal="fade-up" [delay]="$index * 100">
              <span class="stats__value" [appCountUp]="stat.numericValue" [suffix]="stat.suffix" [decimals]="stat.decimals" [duration]="2000"></span>
              <span class="stats__label">{{ stat.label | translate }}</span>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ================================================================
         TESTIMONIALS - 3-card grid layout (TASK-21A-028 to 21A-034)
         ================================================================ -->
    <section class="testimonials" data-testid="testimonials-section">
      <div class="container">
        <header class="section-header">
          <h2 class="section-header__title">{{ 'home.testimonials.title' | translate }}</h2>
          <p class="section-header__subtitle">{{ 'home.testimonials.subtitle' | translate }}</p>
        </header>

        <div class="testimonials__grid">
          @for (testimonial of testimonials; track testimonial.id) {
            <article class="testimonial-card" appReveal="fade-up" [delay]="$index * 100">
              <!-- Star Rating -->
              <div class="testimonial-card__rating" [attr.aria-label]="testimonial.rating + ' out of 5 stars'">
                @for (star of [1,2,3,4,5]; track star) {
                  <svg 
                    class="testimonial-card__star" 
                    [class.filled]="star <= testimonial.rating"
                    viewBox="0 0 20 20" 
                    fill="currentColor"
                    aria-hidden="true"
                  >
                    <path fill-rule="evenodd" d="M10.868 2.884c-.321-.772-1.415-.772-1.736 0l-1.83 4.401-4.753.381c-.833.067-1.171 1.107-.536 1.651l3.62 3.102-1.106 4.637c-.194.813.691 1.456 1.405 1.02L10 15.591l4.069 2.485c.713.436 1.598-.207 1.404-1.02l-1.106-4.637 3.62-3.102c.635-.544.297-1.584-.536-1.65l-4.752-.382-1.831-4.401Z" clip-rule="evenodd"/>
                  </svg>
                }
              </div>

              <!-- Quote -->
              <blockquote class="testimonial-card__quote">
                "{{ testimonial.text }}"
              </blockquote>

              <!-- Author -->
              <div class="testimonial-card__author">
                <div class="testimonial-card__avatar" [class.has-photo]="testimonial.photoUrl">
                  @if (testimonial.photoUrl) {
                    <img [src]="testimonial.photoUrl" [alt]="testimonial.name" loading="lazy" />
                  } @else {
                    {{ getInitials(testimonial.name) }}
                  }
                </div>
                <div class="testimonial-card__info">
                  <span class="testimonial-card__name">{{ testimonial.name }}</span>
                  @if (testimonial.role) {
                    <span class="testimonial-card__role">{{ testimonial.role }}</span>
                  }
                  <span class="testimonial-card__meta">
                    <span class="testimonial-card__location">{{ testimonial.location }}</span>
                    <span class="testimonial-card__date">{{ testimonial.date }}</span>
                  </span>
                </div>
              </div>

              <!-- Verified Purchase Badge -->
              @if (testimonial.verified) {
                <div class="testimonial-card__verified">
                  <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fill-rule="evenodd" d="M16.403 12.652a3 3 0 0 0 0-5.304 3 3 0 0 0-3.75-3.751 3 3 0 0 0-5.305 0 3 3 0 0 0-3.751 3.75 3 3 0 0 0 0 5.305 3 3 0 0 0 3.75 3.751 3 3 0 0 0 5.305 0 3 3 0 0 0 3.751-3.75Zm-2.546-4.46a.75.75 0 0 0-1.214-.883l-3.483 4.79-1.88-1.88a.75.75 0 1 0-1.06 1.061l2.5 2.5a.75.75 0 0 0 1.137-.089l4-5.5Z" clip-rule="evenodd"/>
                  </svg>
                  <span>{{ 'home.testimonials.verified' | translate }}</span>
                </div>
              }
            </article>
          }
        </div>
      </div>
    </section>

    <!-- ================================================================
         NEWSLETTER - Clean signup
         ================================================================ -->
    <section class="newsletter" data-testid="newsletter-section">
      <div class="container">
        <div class="newsletter__content">
          <div class="newsletter__text">
            <h2 class="newsletter__title">{{ 'home.newsletter.title' | translate }}</h2>
            <p class="newsletter__subtitle">{{ 'home.newsletter.subtitle' | translate }}</p>
          </div>
          @if (!newsletterSubmitted()) {
            <form class="newsletter__form" (submit)="submitNewsletter($event)">
              <div class="newsletter__input-wrap">
                <input
                  type="email"
                  [(ngModel)]="newsletterEmail"
                  name="email"
                  class="newsletter__input"
                  [placeholder]="'home.newsletter.placeholder' | translate"
                  [attr.aria-label]="'home.newsletter.placeholder' | translate"
                  required
                />
                <button type="submit" class="newsletter__btn" [disabled]="newsletterLoading()" [attr.aria-label]="'home.newsletter.subscribe' | translate">
                  @if (newsletterLoading()) {
                    <span class="spinner"></span>
                  } @else {
                    <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
                  }
                </button>
              </div>
              @if (newsletterError()) {
                <p class="newsletter__error">{{ newsletterError() }}</p>
              }
            </form>
          } @else {
            <div class="newsletter__success">
              <svg viewBox="0 0 24 24" fill="currentColor"><path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd"/></svg>
              <span>{{ 'home.newsletter.success' | translate }}</span>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ================================================================
         FINAL CTA - Bold call to action
         ================================================================ -->
    <section class="cta">
      <div class="cta__bg">
        <div class="cta__gradient"></div>
      </div>
      <div class="container">
        <div class="cta__content">
          <h2 class="cta__title">{{ 'home.cta.title' | translate }}</h2>
          <a routerLink="/products" class="cta__btn" [attr.aria-label]="'home.cta.button' | translate">
            {{ 'home.cta.button' | translate }}
            <svg viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
          </a>
        </div>
      </div>
    </section>
  `,
  styles: [`
    /* ==========================================================================
       DESIGN TOKENS
       ========================================================================== */
    :host {
      --section-spacing: clamp(5rem, 10vw, 8rem);
      --container-max: 1200px;
      --container-padding: clamp(1.5rem, 5vw, 3rem);
      --radius-sm: 8px;
      --radius-md: 12px;
      --radius-lg: 20px;
      --radius-xl: 28px;
      --radius-full: 9999px;
      --transition-fast: 0.2s cubic-bezier(0.4, 0, 0.2, 1);
      --transition: 0.4s cubic-bezier(0.4, 0, 0.2, 1);
      --transition-slow: 0.6s cubic-bezier(0.4, 0, 0.2, 1);
    }

    .container {
      max-width: var(--container-max);
      margin: 0 auto;
      padding: 0 var(--container-padding);
    }

    /* ==========================================================================
       PROMO BANNER (TASK-21A-003)
       ========================================================================== */
    .promo-banner {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0.75rem 3rem 0.75rem 1.5rem;
      background: linear-gradient(135deg, var(--color-ember-500) 0%, var(--color-ember-600) 100%);
      color: var(--color-text-inverse);
    }

    .promo-banner__content {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
      justify-content: center;
    }

    .promo-banner__message {
      font-size: 0.9375rem;
      font-weight: 500;
    }

    .promo-banner__cta {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-inverse);
      text-decoration: none;
      padding: 0.375rem 0.75rem;
      background: rgba(255, 255, 255, 0.15);
      border-radius: var(--radius-full);
      transition: background var(--transition-fast);

      svg {
        width: 14px;
        height: 14px;
        transition: transform var(--transition-fast);
      }

      &:hover {
        background: rgba(255, 255, 255, 0.25);

        svg {
          transform: translateX(2px);
        }
      }
    }

    .promo-banner__dismiss {
      position: absolute;
      right: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      padding: 0.375rem;
      background: transparent;
      border: none;
      color: var(--color-text-inverse);
      cursor: pointer;
      opacity: 0.7;
      transition: opacity var(--transition-fast);

      svg {
        width: 18px;
        height: 18px;
      }

      &:hover {
        opacity: 1;
      }
    }

    /* ==========================================================================
       BUTTONS
       ========================================================================== */
    .btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.875rem 1.75rem;
      font-size: 1rem;
      font-weight: 600;
      text-decoration: none;
      border-radius: var(--radius-full);
      border: none;
      cursor: pointer;
      transition: all var(--transition);

      svg {
        width: 18px;
        height: 18px;
        transition: transform var(--transition-fast);
      }

      &:hover svg {
        transform: translateX(4px);
      }
    }

    .btn--primary {
      background: var(--color-text-primary);
      color: var(--color-bg-primary);

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 10px 40px -10px var(--shadow-color-lg);
      }
    }

    .btn--ghost {
      background: transparent;
      color: var(--color-text-primary);
      border: 2px solid var(--color-border-secondary);

      &:hover {
        background: var(--color-bg-hover);
        border-color: var(--color-text-primary);
      }
    }

    .btn--large {
      padding: 1rem 2rem;
      font-size: 1.125rem;
    }

    /* ==========================================================================
       SECTION HEADERS
       ========================================================================== */
    .section-header {
      margin-bottom: 3rem;

      &--row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
      }
    }

    .section-header__title {
      font-size: clamp(1.75rem, 4vw, 2.5rem);
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0;
      letter-spacing: -0.03em;
    }

    .section-header__link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: 600;
      color: var(--color-text-secondary);
      text-decoration: none;
      transition: color var(--transition-fast), gap var(--transition-fast);

      svg {
        width: 16px;
        height: 16px;
      }

      &:hover {
        color: var(--color-text-primary);
        gap: 0.75rem;
      }
    }

    /* ==========================================================================
       HERO SECTION - Redesigned (TASK-21A-001, 002, 005)
       ========================================================================== */
    .hero {
      position: relative;
      min-height: 85vh;
      display: flex;
      align-items: center;
      overflow: hidden;
      padding: 4rem 0 2rem;
    }

    .hero__bg {
      position: absolute;
      inset: 0;
      z-index: 0;
    }

    /* Subtle gradient background - no blobs (TASK-21A-001) */
    .hero__gradient-subtle {
      position: absolute;
      inset: 0;
      background: 
        radial-gradient(ellipse 80% 50% at 20% 40%, var(--color-primary-100) 0%, transparent 50%),
        radial-gradient(ellipse 60% 40% at 80% 60%, var(--color-accent-100) 0%, transparent 50%);
      opacity: 0.6;
    }

    [data-theme="dark"] .hero__gradient-subtle {
      background: 
        radial-gradient(ellipse 80% 50% at 20% 40%, rgba(14, 165, 233, 0.1) 0%, transparent 50%),
        radial-gradient(ellipse 60% 40% at 80% 60%, rgba(6, 182, 212, 0.1) 0%, transparent 50%);
      opacity: 0.8;
    }

    .hero__container {
      position: relative;
      z-index: 1;
      width: 100%;
      max-width: var(--container-max);
      margin: 0 auto;
      padding: 0 var(--container-padding);
    }

    .hero__grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 4rem;
      align-items: center;
      animation: heroFadeIn 0.8s ease-out;
    }

    @keyframes heroFadeIn {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .hero__content {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .hero__eyebrow {
      display: inline-flex;
      align-self: flex-start;
      padding: 0.5rem 1rem;
      font-size: 0.8125rem;
      font-weight: 600;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      color: var(--color-primary);
      background: var(--color-primary-100);
      border-radius: var(--radius-full);
      margin: 0;
    }

    [data-theme="dark"] .hero__eyebrow {
      background: rgba(14, 165, 233, 0.15);
    }

    .hero__title {
      font-size: clamp(2.5rem, 5vw, 4rem);
      font-weight: 800;
      line-height: 1.1;
      letter-spacing: -0.03em;
      margin: 0;
      color: var(--color-text-primary);
    }

    .hero__title-line {
      display: block;

      &--accent {
        background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-accent) 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }
    }

    .hero__subtitle {
      font-size: 1.125rem;
      line-height: 1.6;
      color: var(--color-text-secondary);
      margin: 0;
      max-width: 480px;
    }

    /* Hero Search (TASK-21A-006) */
    .hero__search {
      margin-top: 0.5rem;
    }

    .hero__search-input-wrap {
      display: flex;
      align-items: center;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border-primary);
      border-radius: var(--radius-full);
      padding: 0.25rem;
      box-shadow: 0 4px 20px var(--shadow-color);
      transition: border-color var(--transition-fast), box-shadow var(--transition-fast);

      &:focus-within {
        border-color: var(--color-primary);
        box-shadow: 0 4px 20px var(--shadow-color), 0 0 0 3px var(--color-primary-100);
      }
    }

    .hero__search-icon {
      width: 20px;
      height: 20px;
      margin-left: 1rem;
      color: var(--color-text-tertiary);
      flex-shrink: 0;
    }

    .hero__search-input {
      flex: 1;
      padding: 0.875rem 1rem;
      border: none;
      background: transparent;
      color: var(--color-text-primary);
      font-size: 1rem;
      outline: none;
      min-width: 0;

      &::placeholder {
        color: var(--color-text-tertiary);
      }
    }

    .hero__search-btn {
      padding: 0.75rem 1.5rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      border-radius: var(--radius-full);
      font-size: 0.9375rem;
      font-weight: 600;
      cursor: pointer;
      transition: background var(--transition-fast);
      white-space: nowrap;

      &:hover {
        background: var(--color-primary-hover);
      }
    }

    /* Dual CTAs (TASK-21A-007) */
    .hero__ctas {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
    }

    /* Shop by category links */
    .hero__categories {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex-wrap: wrap;
      margin-top: 0.5rem;
    }

    .hero__categories-label {
      font-size: 0.875rem;
      color: var(--color-text-tertiary);
    }

    .hero__categories-links {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .hero__category-link {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--color-text-secondary);
      text-decoration: none;
      padding: 0.375rem 0.75rem;
      background: var(--color-bg-secondary);
      border-radius: var(--radius-full);
      transition: all var(--transition-fast);

      &:hover {
        color: var(--color-primary);
        background: var(--color-primary-100);
      }
    }

    [data-theme="dark"] .hero__category-link:hover {
      background: rgba(14, 165, 233, 0.15);
    }

    /* Hero Product Image (TASK-21A-002) */
    .hero__product {
      display: flex;
      justify-content: center;
      align-items: center;
    }

    .hero__product-card {
      position: relative;
      background: var(--color-bg-card);
      border-radius: var(--radius-xl);
      padding: 1.5rem;
      box-shadow: 0 20px 60px var(--shadow-color-lg);
      border: 1px solid var(--color-border-primary);
      max-width: 400px;
      width: 100%;
    }

    .hero__product-badge {
      position: absolute;
      top: 1rem;
      left: 1rem;
      padding: 0.375rem 0.75rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      border-radius: var(--radius-full);
    }

    .hero__product-image {
      width: 100%;
      height: auto;
      aspect-ratio: 1;
      object-fit: cover;
      border-radius: var(--radius-lg);
      background: var(--color-bg-tertiary);
    }

    .hero__product-info {
      margin-top: 1rem;
      text-align: center;
    }

    .hero__product-name {
      display: block;
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .hero__product-tagline {
      display: block;
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      margin-top: 0.25rem;
    }

    /* ==========================================================================
       VALUES SECTION - Compact above-fold version (TASK-21A-004)
       ========================================================================== */
    .values--compact {
      padding: 2rem 0;
      background: var(--color-bg-secondary);
      border-top: 1px solid var(--color-border-primary);
      border-bottom: 1px solid var(--color-border-primary);
    }

    .values--compact .values__container {
      max-width: var(--container-max);
      margin: 0 auto;
      padding: 0 var(--container-padding);
      display: flex;
      justify-content: center;
      gap: 3rem;
      flex-wrap: wrap;
    }

    .values--compact .values__item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .values--compact .values__icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-primary);
      background: var(--color-primary-100);
      border-radius: var(--radius-md);

      :deep(svg) {
        width: 20px;
        height: 20px;
      }
    }

    [data-theme="dark"] .values--compact .values__icon {
      background: rgba(14, 165, 233, 0.15);
    }

    .values--compact .values__text {
      display: flex;
      flex-direction: column;
    }

    .values--compact .values__label {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .values--compact .values__detail {
      font-size: 0.8125rem;
      color: var(--color-text-tertiary);
    }

    /* ==========================================================================
       BRANDS SECTION
       ========================================================================== */
    .brands {
      padding: 2rem 0;
      overflow: hidden;
      background: var(--color-bg-secondary);
      border-top: 1px solid var(--color-border-primary);
      border-bottom: 1px solid var(--color-border-primary);
    }

    .brands__track {
      display: flex;
      animation: scroll 45s linear infinite;
      width: max-content;

      &.paused {
        animation-play-state: paused;
      }
    }

    .brands__item {
      flex-shrink: 0;
      padding: 0 3rem;
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--color-text-tertiary);
      white-space: nowrap;
      opacity: 0.5;
      transition: opacity var(--transition-fast);

      &:hover {
        opacity: 1;
      }
    }

    @keyframes scroll {
      0% { transform: translateX(0); }
      100% { transform: translateX(-50%); }
    }

    /* ==========================================================================
       VALUES SECTION
       ========================================================================== */
    .values {
      padding: 4rem 0;
    }

    .values__container {
      max-width: var(--container-max);
      margin: 0 auto;
      padding: 0 var(--container-padding);
      display: flex;
      justify-content: center;
      gap: 4rem;
      flex-wrap: wrap;
    }

    .values__item {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .values__icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--color-primary);

      :deep(svg) {
        width: 24px;
        height: 24px;
      }
    }

    .values__label {
      font-weight: 600;
      color: var(--color-text-primary);
    }

    /* ==========================================================================
       CATEGORIES SECTION - Bento Grid (TASK-21A-009 to 21A-015)
       ========================================================================== */
    .categories {
      padding: var(--section-spacing) 0;
    }

    .categories__bento {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      grid-template-rows: repeat(2, 200px);
      gap: 1rem;
      padding: 0 var(--container-padding);
      max-width: calc(var(--container-max) + var(--container-padding) * 2);
      margin: 0 auto;
    }

    .categories__card {
      position: relative;
      border-radius: var(--radius-xl);
      overflow: hidden;
      text-decoration: none;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
      transition: transform var(--transition), box-shadow var(--transition);

      /* Default: 1x1 (small) */
      grid-column: span 1;
      grid-row: span 1;

      /* Hover: subtle scale + shadow increase (TASK-21A-013) */
      &:hover {
        transform: scale(1.02);
        box-shadow: 0 20px 40px var(--shadow-color-lg);

        .categories__card-link svg {
          transform: translateX(4px);
        }

        .categories__card-icon {
          transform: scale(1.1);
        }
      }

      &:focus-visible {
        outline: 2px solid var(--color-primary);
        outline-offset: 2px;
      }
    }

    /* Large card: 2x2 - Featured category (TASK-21A-011) */
    .categories__card--large {
      grid-column: span 2;
      grid-row: span 2;

      .categories__card-icon {
        width: 80px;
        height: 80px;

        :deep(svg) {
          width: 48px;
          height: 48px;
        }
      }

      .categories__card-title {
        font-size: 2rem;
      }

      .categories__card-tagline {
        font-size: 1rem;
        max-width: 280px;
      }
    }

    /* Medium card: 2x1 - Secondary highlight (TASK-21A-011) */
    .categories__card--medium {
      grid-column: span 2;

      .categories__card-content {
        flex-direction: row;
        align-items: center;
        justify-content: space-between;
      }

      .categories__card-icon {
        position: relative;
        top: auto;
        right: auto;
        margin-bottom: 0;
      }
    }

    /* Gradient background (TASK-21A-012) */
    .categories__card-bg {
      position: absolute;
      inset: 0;
      background: var(--card-gradient, linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-accent-500) 100%));
      transition: opacity var(--transition);

      /* Subtle pattern overlay */
      &::before {
        content: '';
        position: absolute;
        inset: 0;
        background: 
          radial-gradient(circle at 30% 20%, rgba(255, 255, 255, 0.15) 0%, transparent 50%),
          radial-gradient(circle at 80% 80%, rgba(0, 0, 0, 0.1) 0%, transparent 40%);
      }

      /* Bottom gradient for text readability */
      &::after {
        content: '';
        position: absolute;
        inset: 0;
        background: linear-gradient(to top, rgba(0, 0, 0, 0.4) 0%, transparent 60%);
      }
    }

    /* Category icon (TASK-21A-012) */
    .categories__card-icon {
      position: absolute;
      top: 1.5rem;
      right: 1.5rem;
      width: 56px;
      height: 56px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(255, 255, 255, 0.2);
      backdrop-filter: blur(8px);
      border-radius: var(--radius-lg);
      color: var(--color-text-inverse);
      transition: transform var(--transition);

      :deep(svg) {
        width: 28px;
        height: 28px;
      }
    }

    .categories__card-content {
      position: relative;
      z-index: 1;
      padding: 1.5rem;
      color: var(--color-text-inverse);
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .categories__card-title {
      font-size: 1.25rem;
      font-weight: 700;
      margin: 0;
      letter-spacing: -0.02em;
    }

    .categories__card-tagline {
      font-size: 0.875rem;
      margin: 0;
      opacity: 0.85;
      line-height: 1.4;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .categories__card-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      font-weight: 600;
      margin-top: 0.5rem;

      svg {
        width: 16px;
        height: 16px;
        transition: transform var(--transition-fast);
      }
    }

    /* ==========================================================================
       PROCESS SECTION
       ========================================================================== */
    .process {
      padding: var(--section-spacing) 0;
      background: var(--color-bg-secondary);
    }

    .process__timeline {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 2rem;
      position: relative;
    }

    .process__line {
      display: none;
    }

    .process__step {
      text-align: center;
    }

    .process__step-number {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 64px;
      height: 64px;
      margin-bottom: 1.25rem;
      font-size: 1.5rem;
      font-weight: 800;
      color: var(--color-primary);
      background: var(--color-primary-100, rgba(59, 130, 246, 0.1));
      border-radius: var(--radius-full);
    }

    [data-theme="dark"] .process__step-number {
      background: var(--color-primary-900, rgba(59, 130, 246, 0.2));
    }

    .process__step-title {
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--color-text-primary);
      margin: 0 0 0.5rem;
    }

    .process__step-desc {
      font-size: 0.9375rem;
      color: var(--color-text-secondary);
      margin: 0;
      line-height: 1.5;
    }

    /* ==========================================================================
       PRODUCTS SECTION
       ========================================================================== */
    .products {
      padding: var(--section-spacing) 0;
    }

    .products__grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
    }

    .products__item {
      transition: transform var(--transition);

      &:hover {
        transform: translateY(-4px);
      }
    }

    .products__empty {
      text-align: center;
      color: var(--color-text-secondary);
      padding: 3rem;
      grid-column: 1 / -1;
    }

    /* ==========================================================================
       STATS SECTION
       ========================================================================== */
    .stats {
      padding: var(--section-spacing) 0;
      background: var(--color-text-primary);
      color: var(--color-bg-primary);
    }

    .stats__grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 2rem;
      text-align: center;
    }

    .stats__value {
      display: block;
      font-size: clamp(2.5rem, 5vw, 4rem);
      font-weight: 800;
      letter-spacing: -0.02em;
      line-height: 1;
    }

    .stats__label {
      display: block;
      margin-top: 0.5rem;
      font-size: 0.875rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      opacity: 0.7;
    }

    /* ==========================================================================
       TESTIMONIALS SECTION - 3-card grid (TASK-21A-028 to 21A-034)
       ========================================================================== */
    .testimonials {
      padding: var(--section-spacing) 0;
      background: var(--color-bg-secondary);
    }

    .testimonials .section-header {
      text-align: center;
      margin-bottom: 3rem;
    }

    .testimonials .section-header__subtitle {
      font-size: 1.125rem;
      color: var(--color-text-secondary);
      margin-top: 0.5rem;
    }

    .testimonials__grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1.5rem;
    }

    .testimonial-card {
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: var(--radius-xl);
      padding: 2rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      position: relative;
      transition: transform var(--transition), box-shadow var(--transition);

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 12px 40px var(--shadow-color-lg);
      }
    }

    .testimonial-card__rating {
      display: flex;
      gap: 0.25rem;
    }

    .testimonial-card__star {
      width: 18px;
      height: 18px;
      color: var(--color-border-secondary);
      transition: color var(--transition-fast);

      &.filled {
        color: var(--color-rating-star);
      }
    }

    .testimonial-card__quote {
      font-size: 1rem;
      line-height: 1.6;
      color: var(--color-text-primary);
      margin: 0;
      flex: 1;
    }

    .testimonial-card__author {
      display: flex;
      align-items: center;
      gap: 0.875rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border-primary);
    }

    .testimonial-card__avatar {
      width: 48px;
      height: 48px;
      min-width: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, var(--color-primary), var(--color-accent));
      color: var(--color-text-inverse);
      font-weight: 700;
      font-size: 0.875rem;
      border-radius: var(--radius-full);
      overflow: hidden;

      &.has-photo {
        background: none;
      }

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
    }

    .testimonial-card__info {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
      min-width: 0;
    }

    .testimonial-card__name {
      font-weight: 600;
      color: var(--color-text-primary);
      font-size: 0.9375rem;
    }

    .testimonial-card__role {
      font-size: 0.8125rem;
      color: var(--color-text-secondary);
    }

    .testimonial-card__meta {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8125rem;
      color: var(--color-text-tertiary);
    }

    .testimonial-card__location {
      &::after {
        content: '·';
        margin-left: 0.5rem;
      }
    }

    .testimonial-card__verified {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      background: var(--color-success-light);
      color: var(--color-success);
      font-size: 0.75rem;
      font-weight: 600;
      border-radius: var(--radius-full);
      position: absolute;
      top: 1.5rem;
      right: 1.5rem;

      svg {
        width: 14px;
        height: 14px;
      }
    }

    /* ==========================================================================
       NEWSLETTER SECTION
       ========================================================================== */
    .newsletter {
      padding: var(--section-spacing) 0;
      background: var(--color-bg-secondary);
    }

    .newsletter__content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 3rem;
      flex-wrap: wrap;
    }

    .newsletter__text {
      flex: 1;
      min-width: 280px;
    }

    .newsletter__title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--color-text-primary);
      margin: 0 0 0.5rem;
    }

    .newsletter__subtitle {
      color: var(--color-text-secondary);
      margin: 0;
    }

    .newsletter__form {
      flex: 1;
      min-width: 320px;
      max-width: 400px;
    }

    .newsletter__input-wrap {
      display: flex;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border-primary);
      border-radius: var(--radius-full);
      overflow: hidden;
      transition: border-color var(--transition-fast);

      &:focus-within {
        border-color: var(--color-primary);
      }
    }

    .newsletter__input {
      flex: 1;
      padding: 1rem 1.5rem;
      border: none;
      background: transparent;
      color: var(--color-text-primary);
      font-size: 1rem;
      outline: none;

      &::placeholder {
        color: var(--color-text-tertiary);
      }
    }

    .newsletter__btn {
      padding: 1rem 1.5rem;
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border: none;
      cursor: pointer;
      transition: background var(--transition-fast);

      svg {
        width: 20px;
        height: 20px;
      }

      &:hover:not(:disabled) {
        background: var(--color-primary-hover);
      }

      &:disabled {
        opacity: 0.7;
        cursor: not-allowed;
      }
    }

    .newsletter__error {
      font-size: 0.875rem;
      color: var(--color-error);
      margin: 0.5rem 0 0 1.5rem;
    }

    .newsletter__success {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1rem 1.5rem;
      background: var(--color-success-light);
      color: var(--color-success);
      border-radius: var(--radius-full);
      font-weight: 500;

      svg {
        width: 24px;
        height: 24px;
      }
    }

    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid var(--glass-border);
      border-top-color: var(--color-text-inverse);
      border-radius: var(--radius-full);
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    /* ==========================================================================
       CTA SECTION
       ========================================================================== */
    .cta {
      position: relative;
      padding: var(--section-spacing) 0;
      overflow: hidden;
    }

    .cta__bg {
      position: absolute;
      inset: 0;
      background: var(--color-primary);
    }

    .cta__gradient {
      position: absolute;
      inset: 0;
      background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-accent) 100%);
      /* No animation - static gradient */
    }

    .cta__content {
      position: relative;
      z-index: 1;
      text-align: center;
    }

    .cta__title {
      font-size: clamp(1.75rem, 4vw, 2.5rem);
      font-weight: 700;
      color: var(--color-text-inverse);
      margin: 0 0 2rem;
      letter-spacing: -0.02em;
    }

    .cta__btn {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1rem 2rem;
      font-size: 1.125rem;
      font-weight: 600;
      background: var(--color-bg-primary);
      color: var(--color-primary);
      border-radius: var(--radius-full);
      text-decoration: none;
      transition: all var(--transition);

      svg {
        width: 20px;
        height: 20px;
        transition: transform var(--transition-fast);
      }

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 10px 40px var(--shadow-color-lg);

        svg {
          transform: translateX(4px);
        }
      }
    }

    /* ==========================================================================
       RESPONSIVE
       ========================================================================== */
    @media (max-width: 1024px) {
      .hero__grid {
        grid-template-columns: 1fr;
        gap: 3rem;
        text-align: center;
      }

      .hero__content {
        align-items: center;
      }

      .hero__eyebrow {
        align-self: center;
      }

      .hero__subtitle {
        max-width: 100%;
      }

      .hero__ctas {
        justify-content: center;
      }

      .hero__categories {
        justify-content: center;
      }

      .hero__product {
        order: -1;
      }

      .hero__product-card {
        max-width: 320px;
      }

      /* Bento grid responsive (TASK-21A-015) */
      .categories__bento {
        grid-template-columns: repeat(2, 1fr);
        grid-template-rows: repeat(3, 180px);
      }

      .categories__card--large {
        grid-column: span 2;
        grid-row: span 2;
      }

      .categories__card--medium {
        grid-column: span 2;
      }

      .process__timeline {
        grid-template-columns: repeat(2, 1fr);
      }

      .products__grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .stats__grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .values--compact .values__container {
        gap: 2rem;
      }

      /* Testimonials grid responsive - 2 columns on tablet */
      .testimonials__grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .testimonials__grid .testimonial-card:last-child {
        grid-column: span 2;
        max-width: 50%;
        justify-self: center;
      }
    }

    @media (max-width: 768px) {
      .hero {
        min-height: auto;
        padding: 2rem 0;
      }

      .hero__title {
        font-size: clamp(2rem, 8vw, 3rem);
      }

      .hero__search-input-wrap {
        flex-wrap: wrap;
      }

      .hero__search-btn {
        width: 100%;
        margin-top: 0.5rem;
      }

      .hero__product-card {
        max-width: 280px;
        padding: 1rem;
      }

      .values--compact .values__container {
        flex-direction: column;
        align-items: center;
        gap: 1.25rem;
      }

      .values--compact .values__item {
        width: 100%;
        max-width: 280px;
      }

      /* Testimonials grid responsive - 1 column on mobile */
      .testimonials__grid {
        grid-template-columns: 1fr;
      }

      .testimonials__grid .testimonial-card:last-child {
        grid-column: span 1;
        max-width: 100%;
      }

      .testimonial-card {
        padding: 1.5rem;
      }

      .testimonial-card__verified {
        position: static;
        align-self: flex-start;
        margin-top: -0.5rem;
      }

      /* Bento grid mobile (TASK-21A-015) */
      .categories__bento {
        grid-template-columns: 1fr;
        grid-template-rows: auto;
        gap: 0.75rem;
      }

      .categories__card {
        min-height: 160px;
      }

      .categories__card--large,
      .categories__card--medium {
        grid-column: span 1;
        grid-row: span 1;
      }

      .categories__card--large {
        min-height: 220px;

        .categories__card-icon {
          width: 56px;
          height: 56px;

          :deep(svg) {
            width: 28px;
            height: 28px;
          }
        }

        .categories__card-title {
          font-size: 1.5rem;
        }

        .categories__card-tagline {
          font-size: 0.875rem;
        }
      }

      .categories__card--medium .categories__card-content {
        flex-direction: column;
        align-items: flex-start;
      }

      .process__timeline {
        grid-template-columns: 1fr;
        gap: 2.5rem;
      }

      .products__grid {
        grid-template-columns: 1fr;
      }

      .stats__grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .newsletter__content {
        flex-direction: column;
        text-align: center;
      }

      .newsletter__form {
        width: 100%;
        max-width: none;
      }

      .promo-banner__content {
        flex-direction: column;
        gap: 0.5rem;
      }
    }

    @media (max-width: 480px) {
      .hero__ctas {
        flex-direction: column;
        width: 100%;
      }

      .hero__ctas .btn {
        width: 100%;
        justify-content: center;
      }

      .hero__categories-links {
        justify-content: center;
      }

      .stats__grid {
        grid-template-columns: 1fr 1fr;
        gap: 1.5rem;
      }
    }

    /* Reduced motion */
    @media (prefers-reduced-motion: reduce) {
      .brands__track {
        animation: none;
      }

      .hero__grid {
        animation: none;
      }
    }
  `]
})
export class HomeComponent implements OnInit, OnDestroy, AfterViewInit {
  private readonly productService = inject(ProductService);
  private readonly translateService = inject(TranslateService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly elementRef = inject(ElementRef);
  private readonly router = inject(Router);

  // Testimonial rotation removed - now using static 3-card grid (TASK-21A-028)
  // HOME-P03: Track subscriptions to prevent memory leaks
  private langChangeSubscription: Subscription | null = null;
  // HOME-P02: Intersection Observer for lazy loading background images
  private categoryObserver: IntersectionObserver | null = null;
  
  @ViewChildren('categoryPanel') categoryPanels!: QueryList<ElementRef<HTMLElement>>;

  // Signals
  featuredProducts = signal<ProductBrief[]>([]);
  loadingFeatured = signal(true);
  newsletterEmail = '';
  newsletterLoading = signal(false);
  pauseMarquee = false;
  newsletterError = signal<string | null>(null);
  newsletterSubmitted = signal(false);

  // Hero section signals (TASK-21A-003, 21A-006, 21A-008)
  heroSearchQuery = '';
  promoBannerDismissed = signal(false);
  promoBanner = signal<PromoBanner>({
    id: 'winter-2026',
    messageKey: 'home.promo.banner.winter',
    ctaKey: 'home.promo.banner.cta',
    link: '/products/category/heating-systems',
    active: true
  });

  // Hero categories for quick links
  heroCategories = [
    { slug: 'air-conditioners', name: 'categories.airConditioning' },
    { slug: 'heating-systems', name: 'categories.heatingSystems' },
    { slug: 'ventilation', name: 'categories.ventilation' },
    { slug: 'water-purification', name: 'categories.waterPurification' }
  ];

  // Data
  brandsList = ['Daikin', 'Mitsubishi Electric', 'LG', 'Samsung', 'Fujitsu', 'Gree', 'Midea', 'Toshiba', 'Aquaphor', 'BWT'];
  duplicatedBrands = [...this.brandsList, ...this.brandsList];

  valueProps = [
    { key: 'home.values.shipping', detail: 'home.values.shippingDetail', icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M8.25 18.75a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 01-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 00-3.213-9.193 2.056 2.056 0 00-1.58-.86H14.25m-2.25 0h-2.25m0 0v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 00-10.026 0 1.106 1.106 0 00-.987 1.106v4.964m12-4.006v4.006m0 0v3.75m-12-7.756v7.756m12-7.756h-2.25" /></svg>` },
    { key: 'home.values.warranty', detail: null, icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>` },
    { key: 'home.values.support', detail: null, icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M20.25 8.511c.884.284 1.5 1.128 1.5 2.097v4.286c0 1.136-.847 2.1-1.98 2.193-.34.027-.68.052-1.02.072v3.091l-3-3c-1.354 0-2.694-.055-4.02-.163a2.115 2.115 0 01-.825-.242m9.345-8.334a2.126 2.126 0 00-.476-.095 48.64 48.64 0 00-8.048 0c-1.131.094-1.976 1.057-1.976 2.192v4.286c0 .837.46 1.58 1.155 1.951m9.345-8.334V6.637c0-1.621-1.152-3.026-2.76-3.235A48.455 48.455 0 0011.25 3c-2.115 0-4.198.137-6.24.402-1.608.209-2.76 1.614-2.76 3.235v6.226c0 1.621 1.152 3.026 2.76 3.235.577.075 1.157.14 1.74.194V21l4.155-4.155" /></svg>` },
    { key: 'home.values.installation', detail: null, icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 11-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 004.486-6.336l-3.276 3.277a3.004 3.004 0 01-2.25-2.25l3.276-3.276a4.5 4.5 0 00-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085m-1.745 1.437L5.909 7.5H4.5L2.25 3.75l1.5-1.5L7.5 4.5v1.409l4.26 4.26m-1.745 1.437l1.745-1.437m6.615 8.206L15.75 15.75M4.867 19.125h.008v.008h-.008v-.008z" /></svg>` }
  ];

  // Categories with gradient backgrounds and icons (TASK-21A-012)
  categories = [
    { 
      slug: 'air-conditioners', 
      name: 'categories.airConditioning', 
      tagline: 'home.categories.taglines.airConditioning',
      gradient: 'linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-accent-500) 100%)',
      accent: 'var(--color-primary-200)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z" /></svg>`
    },
    { 
      slug: 'heating-systems', 
      name: 'categories.heatingSystems', 
      tagline: 'home.categories.taglines.heating',
      gradient: 'linear-gradient(135deg, var(--color-ember-500) 0%, var(--color-warm-500) 100%)',
      accent: 'var(--color-ember-200)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15.362 5.214A8.252 8.252 0 0 1 12 21 8.25 8.25 0 0 1 6.038 7.047 8.287 8.287 0 0 0 9 9.601a8.983 8.983 0 0 1 3.361-6.867 8.21 8.21 0 0 0 3 2.48Z" /><path stroke-linecap="round" stroke-linejoin="round" d="M12 18a3.75 3.75 0 0 0 .495-7.468 5.99 5.99 0 0 0-1.925 3.547 5.975 5.975 0 0 1-2.133-1.001A3.75 3.75 0 0 0 12 18Z" /></svg>`
    },
    { 
      slug: 'ventilation', 
      name: 'categories.ventilation', 
      tagline: 'home.categories.taglines.ventilation',
      gradient: 'linear-gradient(135deg, var(--color-aurora-500) 0%, var(--color-accent-400) 100%)',
      accent: 'var(--color-aurora-200)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" /></svg>`
    },
    { 
      slug: 'water-purification', 
      name: 'categories.waterPurification', 
      tagline: 'home.categories.taglines.water',
      gradient: 'linear-gradient(135deg, var(--color-accent-500) 0%, var(--color-primary-400) 100%)',
      accent: 'var(--color-accent-200)',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 0 1-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 0 1 4.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0 1 12 15a9.065 9.065 0 0 1-6.23.693L5 15.5m14.8-.2-.8 3.2a1.5 1.5 0 0 1-1.4 1H6.4a1.5 1.5 0 0 1-1.4-1l-.8-3.2" /></svg>`
    }
  ];

  processSteps = [
    { num: '01', title: 'home.process.step1.title', desc: 'home.process.step1.desc' },
    { num: '02', title: 'home.process.step2.title', desc: 'home.process.step2.desc' },
    { num: '03', title: 'home.process.step3.title', desc: 'home.process.step3.desc' },
    { num: '04', title: 'home.process.step4.title', desc: 'home.process.step4.desc' }
  ];

  stats = [
    { numericValue: 10000, suffix: '+', decimals: 0, label: 'home.stats.customers' },
    { numericValue: 500, suffix: '+', decimals: 0, label: 'home.stats.products' },
    { numericValue: 15, suffix: '+', decimals: 0, label: 'home.stats.years' },
    { numericValue: 4.9, suffix: '', decimals: 1, label: 'home.stats.rating' }
  ];

  testimonials: Testimonial[] = [];
  
  private initTestimonials(): void {
    // Subscribe to language changes to update testimonials
    this.translateService.get([
      'home.testimonials.items.1.name',
      'home.testimonials.items.1.location',
      'home.testimonials.items.1.text',
      'home.testimonials.items.1.role',
      'home.testimonials.items.1.date',
      'home.testimonials.items.2.name',
      'home.testimonials.items.2.location',
      'home.testimonials.items.2.text',
      'home.testimonials.items.2.role',
      'home.testimonials.items.2.date',
      'home.testimonials.items.3.name',
      'home.testimonials.items.3.location',
      'home.testimonials.items.3.text',
      'home.testimonials.items.3.role',
      'home.testimonials.items.3.date'
    ]).subscribe(translations => {
      this.testimonials = [
        {
          id: 1,
          name: translations['home.testimonials.items.1.name'],
          location: translations['home.testimonials.items.1.location'],
          rating: 5,
          text: translations['home.testimonials.items.1.text'],
          role: translations['home.testimonials.items.1.role'],
          date: translations['home.testimonials.items.1.date'],
          verified: true,
          photoUrl: undefined
        },
        {
          id: 2,
          name: translations['home.testimonials.items.2.name'],
          location: translations['home.testimonials.items.2.location'],
          rating: 5,
          text: translations['home.testimonials.items.2.text'],
          role: translations['home.testimonials.items.2.role'],
          date: translations['home.testimonials.items.2.date'],
          verified: true,
          photoUrl: undefined
        },
        {
          id: 3,
          name: translations['home.testimonials.items.3.name'],
          location: translations['home.testimonials.items.3.location'],
          rating: 5,
          text: translations['home.testimonials.items.3.text'],
          role: translations['home.testimonials.items.3.role'],
          date: translations['home.testimonials.items.3.date'],
          verified: false,
          photoUrl: undefined
        }
      ];
    });
  }

  ngOnInit(): void {
    this.initTestimonials();
    this.loadFeaturedProducts();
    if (isPlatformBrowser(this.platformId)) {
      // Check if promo banner was previously dismissed
      const dismissed = localStorage.getItem('promoBannerDismissed_' + this.promoBanner().id);
      if (dismissed === 'true') {
        this.promoBannerDismissed.set(true);
      }
    }
    // HOME-P03: Store subscription for cleanup to prevent memory leak
    this.langChangeSubscription = this.translateService.onLangChange.subscribe(() => {
      this.initTestimonials();
    });
  }

  ngAfterViewInit(): void {
    // HOME-P02: Set up Intersection Observer for lazy loading category images
    if (isPlatformBrowser(this.platformId)) {
      this.setupCategoryLazyLoading();
    }
  }

  ngOnDestroy(): void {
    // HOME-P03: Clean up language change subscription to prevent memory leak
    if (this.langChangeSubscription) {
      this.langChangeSubscription.unsubscribe();
      this.langChangeSubscription = null;
    }
    // HOME-P02: Clean up Intersection Observer
    if (this.categoryObserver) {
      this.categoryObserver.disconnect();
      this.categoryObserver = null;
    }
  }

  // HOME-P02: Category panels now use CSS gradients, no lazy loading needed
  // Keeping the observer setup for potential future use with product images
  private setupCategoryLazyLoading(): void {
    // Categories now use gradient backgrounds instead of images (TASK-21A-012)
    // No lazy loading needed for gradients
  }

  private loadFeaturedProducts(): void {
    this.loadingFeatured.set(true);
    this.productService.getFeaturedProducts(4).subscribe({
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

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase();
  }

  // Hero section methods (TASK-21A-003, 21A-006)
  dismissPromoBanner(): void {
    this.promoBannerDismissed.set(true);
    // Optionally persist to localStorage
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('promoBannerDismissed_' + this.promoBanner().id, 'true');
    }
  }

  onHeroSearch(event: Event): void {
    event.preventDefault();
    const query = this.heroSearchQuery.trim();
    if (query) {
      this.router.navigate(['/products'], { queryParams: { search: query } });
    }
  }

  submitNewsletter(event: Event): void {
    event.preventDefault();
    this.newsletterError.set(null);

    const email = this.newsletterEmail.trim();
    if (!email) {
      this.translateService.get('home.newsletter.errorRequired').subscribe(msg => {
        this.newsletterError.set(msg);
      });
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      this.translateService.get('home.newsletter.errorInvalid').subscribe(msg => {
        this.newsletterError.set(msg);
      });
      return;
    }

    this.newsletterLoading.set(true);
    setTimeout(() => {
      this.newsletterLoading.set(false);
      this.newsletterSubmitted.set(true);
      this.newsletterEmail = '';
    }, 1000);
  }
}
