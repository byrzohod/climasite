import { Component, inject, signal, OnInit, OnDestroy, PLATFORM_ID, AfterViewInit, ElementRef, ViewChildren, QueryList } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Subscription } from 'rxjs';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ProductService } from '../../core/services/product.service';
import { ProductBrief } from '../../core/models/product.model';
import { ProductCardComponent } from '../products/product-card/product-card.component';
import { RevealDirective } from '../../shared/directives/reveal.directive';
import { TiltEffectDirective } from '../../shared/directives/tilt-effect.directive';
import { CountUpDirective } from '../../shared/directives/count-up.directive';
import { SkeletonProductCardComponent } from '../../shared/components/skeleton-product-card/skeleton-product-card.component';

interface Testimonial {
  id: number;
  name: string;
  location: string;
  rating: number;
  text: string;
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
    TiltEffectDirective,
    CountUpDirective,
    SkeletonProductCardComponent
  ],
  template: `
    <!-- ================================================================
         HERO - Full-screen immersive experience
         ================================================================ -->
    <section class="hero" data-testid="hero-section">
      <!-- Animated gradient background -->
      <div class="hero__bg">
        <div class="hero__gradient hero__gradient--1"></div>
        <div class="hero__gradient hero__gradient--2"></div>
        <div class="hero__gradient hero__gradient--3"></div>
        <div class="hero__noise"></div>
      </div>

      <!-- Content -->
      <div class="hero__content">
        <p class="hero__eyebrow">{{ 'home.hero.eyebrow' | translate }}</p>
        <h1 class="hero__title">
          <span class="hero__title-line">{{ 'home.hero.title1' | translate }}</span>
          <span class="hero__title-line hero__title-line--accent">{{ 'home.hero.title2' | translate }}</span>
        </h1>
        <p class="hero__subtitle">{{ 'home.hero.subtitle' | translate }}</p>
        <div class="hero__cta">
          <a routerLink="/products" class="btn btn--primary btn--large" data-testid="hero-cta">
            {{ 'home.hero.cta' | translate }}
            <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/></svg>
          </a>
        </div>
      </div>

      <!-- Scroll indicator -->
      <div class="hero__scroll" aria-hidden="true">
        <div class="hero__scroll-mouse">
          <div class="hero__scroll-wheel"></div>
        </div>
        <span class="hero__scroll-text">{{ 'home.hero.scroll' | translate }}</span>
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
         CATEGORIES - Visual navigation panels
         HOME-P02: Uses Intersection Observer for lazy loading background images
         ================================================================ -->
    <section class="categories" data-testid="categories-section">
      <div class="categories__grid">
        @for (cat of categories; track cat.slug) {
          <a
            #categoryPanel
            [routerLink]="['/products/category', cat.slug]"
            class="categories__panel"
            [attr.data-bg]="'url(' + cat.image + ')'"
            [attr.aria-label]="cat.name | translate"
            appReveal="scale-up" [delay]="$index * 75"
            appTiltEffect [maxTilt]="8" [glare]="true" [glareOpacity]="0.15"
            data-testid="category-card"
          >
            <div class="categories__panel-bg"></div>
            <div class="categories__panel-content">
              <h3 class="categories__panel-title">{{ cat.name | translate }}</h3>
              <span class="categories__panel-link">
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
            <div class="stats__item" appReveal="scale" [delay]="$index * 100">
              <span class="stats__value" [appCountUp]="stat.numericValue" [suffix]="stat.suffix" [decimals]="stat.decimals" [duration]="2000"></span>
              <span class="stats__label">{{ stat.label | translate }}</span>
            </div>
          }
        </div>
      </div>
    </section>

    <!-- ================================================================
         TESTIMONIALS - Single rotating quote
         ================================================================ -->
    <section class="testimonials" data-testid="testimonials-section">
      <div class="container">
        <div class="testimonials__content">
          <div class="testimonials__quote-mark">"</div>
          <blockquote class="testimonials__quote">
            {{ testimonials[activeTestimonial()].text }}
          </blockquote>
          <div class="testimonials__author">
            <div class="testimonials__avatar">
              {{ getInitials(testimonials[activeTestimonial()].name) }}
            </div>
            <div class="testimonials__info">
              <span class="testimonials__name">{{ testimonials[activeTestimonial()].name }}</span>
              <span class="testimonials__location">{{ testimonials[activeTestimonial()].location }}</span>
            </div>
          </div>
          <div class="testimonials__dots" role="tablist" [attr.aria-label]="'home.testimonials.title' | translate">
            @for (t of testimonials; track t.id) {
              <button
                type="button"
                role="tab"
                class="testimonials__dot"
                [class.active]="activeTestimonial() === $index"
                [attr.aria-selected]="activeTestimonial() === $index"
                (click)="setTestimonial($index)"
                [attr.aria-label]="'Testimonial ' + ($index + 1)"
              ></button>
            }
          </div>
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
       HERO SECTION
       ========================================================================== */
    .hero {
      position: relative;
      min-height: 100vh;
      min-height: 100dvh;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
    }

    .hero__bg {
      position: absolute;
      inset: 0;
      z-index: 0;
    }

    .hero__gradient {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.5;
      animation: float 20s ease-in-out infinite;

      &--1 {
        width: 60vw;
        height: 60vw;
        top: -20%;
        left: -10%;
        background: var(--color-primary);
        animation-delay: 0s;
      }

      &--2 {
        width: 50vw;
        height: 50vw;
        bottom: -20%;
        right: -10%;
        background: var(--color-accent);
        animation-delay: -7s;
      }

      &--3 {
        width: 40vw;
        height: 40vw;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background: var(--color-primary-light);
        animation-delay: -14s;
      }
    }

    .hero__noise {
      position: absolute;
      inset: 0;
      background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 400 400' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noiseFilter'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noiseFilter)'/%3E%3C/svg%3E");
      opacity: 0.03;
      pointer-events: none;
    }

    @keyframes float {
      0%, 100% { transform: translate(0, 0) scale(1); }
      25% { transform: translate(5%, 5%) scale(1.05); }
      50% { transform: translate(0, 10%) scale(1); }
      75% { transform: translate(-5%, 5%) scale(0.95); }
    }

    .hero__content {
      position: relative;
      z-index: 1;
      text-align: center;
      padding: 0 var(--container-padding);
      max-width: 900px;
      animation: heroFadeIn 1s ease-out;
    }

    @keyframes heroFadeIn {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .hero__eyebrow {
      display: inline-block;
      padding: 0.5rem 1rem;
      margin-bottom: 1.5rem;
      font-size: 0.875rem;
      font-weight: 600;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      color: var(--color-text-secondary);
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border-primary);
      border-radius: var(--radius-full);
      animation: heroFadeIn 1s ease-out 0.2s both;
    }

    .hero__title {
      font-size: clamp(3rem, 10vw, 6rem);
      font-weight: 800;
      line-height: 1;
      letter-spacing: -0.04em;
      margin: 0 0 1.5rem;
      color: var(--color-text-primary);
    }

    .hero__title-line {
      display: block;
      animation: heroFadeIn 1s ease-out 0.3s both;

      &--accent {
        background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-accent) 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
        animation-delay: 0.4s;
      }
    }

    .hero__subtitle {
      font-size: clamp(1.125rem, 2vw, 1.375rem);
      line-height: 1.6;
      color: var(--color-text-secondary);
      margin: 0 0 2.5rem;
      max-width: 600px;
      margin-left: auto;
      margin-right: auto;
      animation: heroFadeIn 1s ease-out 0.5s both;
    }

    .hero__cta {
      animation: heroFadeIn 1s ease-out 0.6s both;
    }

    .hero__scroll {
      position: absolute;
      bottom: 2rem;
      left: 50%;
      transform: translateX(-50%);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      animation: heroFadeIn 1s ease-out 1s both;
    }

    .hero__scroll-mouse {
      width: 24px;
      height: 40px;
      border: 2px solid var(--color-text-tertiary);
      border-radius: 12px;
      display: flex;
      justify-content: center;
      padding-top: 8px;
    }

    .hero__scroll-wheel {
      width: 4px;
      height: 8px;
      background: var(--color-text-tertiary);
      border-radius: 2px;
      animation: scrollWheel 2s ease-in-out infinite;
    }

    @keyframes scrollWheel {
      0%, 100% { opacity: 1; transform: translateY(0); }
      50% { opacity: 0.3; transform: translateY(8px); }
    }

    .hero__scroll-text {
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
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
      animation: scroll 30s linear infinite;
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
       CATEGORIES SECTION
       ========================================================================== */
    .categories {
      padding: var(--section-spacing) 0;
    }

    .categories__grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
      padding: 0 var(--container-padding);
      max-width: calc(var(--container-max) + var(--container-padding) * 2);
      margin: 0 auto;
    }

    .categories__panel {
      position: relative;
      height: 50vh;
      min-height: 400px;
      border-radius: var(--radius-lg);
      overflow: hidden;
      text-decoration: none;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;

      &:hover {
        .categories__panel-bg {
          transform: scale(1.1);
        }

        .categories__panel-content {
          transform: translateY(-8px);
        }

        .categories__panel-link svg {
          transform: translateX(4px);
        }
      }
    }

    .categories__panel-bg {
      position: absolute;
      inset: 0;
      /* HOME-P02: Background is set via CSS variable after lazy load */
      background: var(--color-bg-tertiary);
      transition: transform 0.8s cubic-bezier(0.4, 0, 0.2, 1), background-image 0.3s ease;

      &::after {
        content: '';
        position: absolute;
        inset: 0;
        background: linear-gradient(to top, var(--color-bg-overlay) 0%, rgba(0,0,0,0.3) 50%, rgba(0,0,0,0.1) 100%);
      }
    }

    /* HOME-P02: When loaded, show the background image */
    .categories__panel--loaded .categories__panel-bg {
      background: var(--panel-image) center/cover no-repeat;
    }

    .categories__panel-content {
      position: relative;
      z-index: 1;
      padding: 2rem;
      color: var(--color-text-inverse);
      transition: transform var(--transition);
    }

    .categories__panel-title {
      font-size: 1.5rem;
      font-weight: 700;
      margin: 0 0 0.75rem;
    }

    .categories__panel-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.9375rem;
      font-weight: 500;
      opacity: 0.9;

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
       TESTIMONIALS SECTION
       ========================================================================== */
    .testimonials {
      padding: var(--section-spacing) 0;
    }

    .testimonials__content {
      max-width: 800px;
      margin: 0 auto;
      text-align: center;
    }

    .testimonials__quote-mark {
      font-size: 6rem;
      line-height: 1;
      color: var(--color-primary);
      opacity: 0.2;
      font-family: Georgia, serif;
      margin-bottom: -2rem;
    }

    .testimonials__quote {
      font-size: clamp(1.25rem, 3vw, 1.75rem);
      font-weight: 500;
      line-height: 1.5;
      color: var(--color-text-primary);
      margin: 0 0 2rem;
    }

    .testimonials__author {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
    }

    .testimonials__avatar {
      width: 56px;
      height: 56px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, var(--color-primary), var(--color-accent));
      color: var(--color-text-inverse);
      font-weight: 700;
      border-radius: var(--radius-full);
    }

    .testimonials__info {
      text-align: left;
    }

    .testimonials__name {
      display: block;
      font-weight: 600;
      color: var(--color-text-primary);
    }

    .testimonials__location {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
    }

    .testimonials__dots {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
      margin-top: 2rem;
    }

    .testimonials__dot {
      width: 8px;
      height: 8px;
      padding: 0;
      background: var(--color-border-secondary);
      border: none;
      border-radius: var(--radius-full);
      cursor: pointer;
      transition: all var(--transition-fast);

      &:hover {
        background: var(--color-text-tertiary);
      }

      &:focus {
        outline: 2px solid var(--color-primary);
        outline-offset: 2px;
      }

      &.active {
        width: 24px;
        background: var(--color-primary);
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
      animation: ctaGradient 10s ease-in-out infinite alternate;
    }

    @keyframes ctaGradient {
      0% { opacity: 1; }
      100% { opacity: 0.7; }
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
      .categories__grid {
        grid-template-columns: repeat(2, 1fr);
      }

      .categories__panel {
        height: 40vh;
        min-height: 300px;
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
    }

    @media (max-width: 768px) {
      .hero__title {
        font-size: clamp(2.5rem, 12vw, 4rem);
      }

      .hero__scroll {
        display: none;
      }

      .values__container {
        gap: 2rem;
      }

      .values__item {
        flex: 0 0 calc(50% - 1rem);
        justify-content: center;
      }

      .categories__grid {
        grid-template-columns: 1fr;
        gap: 0.75rem;
      }

      .categories__panel {
        height: 30vh;
        min-height: 200px;
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
    }

    @media (max-width: 480px) {
      .values__item {
        flex: 0 0 100%;
      }

      .stats__grid {
        grid-template-columns: 1fr 1fr;
        gap: 1.5rem;
      }
    }

    /* Reduced motion */
    @media (prefers-reduced-motion: reduce) {
      .hero__gradient,
      .hero__scroll-wheel,
      .brands__track,
      .cta__gradient {
        animation: none;
      }

      .hero__content,
      .hero__eyebrow,
      .hero__title-line,
      .hero__subtitle,
      .hero__cta,
      .hero__scroll {
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

  private testimonialInterval: ReturnType<typeof setInterval> | null = null;
  // HOME-P03: Track subscriptions to prevent memory leaks
  private langChangeSubscription: Subscription | null = null;
  // HOME-P02: Intersection Observer for lazy loading background images
  private categoryObserver: IntersectionObserver | null = null;
  
  @ViewChildren('categoryPanel') categoryPanels!: QueryList<ElementRef<HTMLElement>>;

  // Signals
  featuredProducts = signal<ProductBrief[]>([]);
  loadingFeatured = signal(true);
  activeTestimonial = signal(0);
  newsletterEmail = '';
  newsletterLoading = signal(false);
  pauseMarquee = false;
  newsletterError = signal<string | null>(null);
  newsletterSubmitted = signal(false);

  // Data
  brandsList = ['Daikin', 'Mitsubishi Electric', 'LG', 'Samsung', 'Fujitsu', 'Gree', 'Midea', 'Toshiba', 'Aquaphor', 'BWT'];
  duplicatedBrands = [...this.brandsList, ...this.brandsList];

  valueProps = [
    { key: 'home.values.shipping', icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M8.25 18.75a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 01-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 01-3 0m3 0a1.5 1.5 0 00-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 00-3.213-9.193 2.056 2.056 0 00-1.58-.86H14.25m-2.25 0h-2.25m0 0v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 00-10.026 0 1.106 1.106 0 00-.987 1.106v4.964m12-4.006v4.006m0 0v3.75m-12-7.756v7.756m12-7.756h-2.25" /></svg>` },
    { key: 'home.values.warranty', icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" /></svg>` },
    { key: 'home.values.support', icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M20.25 8.511c.884.284 1.5 1.128 1.5 2.097v4.286c0 1.136-.847 2.1-1.98 2.193-.34.027-.68.052-1.02.072v3.091l-3-3c-1.354 0-2.694-.055-4.02-.163a2.115 2.115 0 01-.825-.242m9.345-8.334a2.126 2.126 0 00-.476-.095 48.64 48.64 0 00-8.048 0c-1.131.094-1.976 1.057-1.976 2.192v4.286c0 .837.46 1.58 1.155 1.951m9.345-8.334V6.637c0-1.621-1.152-3.026-2.76-3.235A48.455 48.455 0 0011.25 3c-2.115 0-4.198.137-6.24.402-1.608.209-2.76 1.614-2.76 3.235v6.226c0 1.621 1.152 3.026 2.76 3.235.577.075 1.157.14 1.74.194V21l4.155-4.155" /></svg>` },
    { key: 'home.values.installation', icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M11.42 15.17L17.25 21A2.652 2.652 0 0021 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 11-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 004.486-6.336l-3.276 3.277a3.004 3.004 0 01-2.25-2.25l3.276-3.276a4.5 4.5 0 00-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085m-1.745 1.437L5.909 7.5H4.5L2.25 3.75l1.5-1.5L7.5 4.5v1.409l4.26 4.26m-1.745 1.437l1.745-1.437m6.615 8.206L15.75 15.75M4.867 19.125h.008v.008h-.008v-.008z" /></svg>` }
  ];

  categories = [
    { slug: 'air-conditioners', name: 'categories.airConditioning', image: 'https://images.unsplash.com/photo-1625961332771-3f40b0e2bdcf?w=800&q=80' },
    { slug: 'water-purification', name: 'categories.waterPurification', image: 'https://images.unsplash.com/photo-1548839140-29a749e1cf4d?w=800&q=80' },
    { slug: 'heating-systems', name: 'categories.heatingSystems', image: 'https://images.unsplash.com/photo-1513694203232-719a280e022f?w=800&q=80' },
    { slug: 'ventilation', name: 'categories.ventilation', image: 'https://images.unsplash.com/photo-1585771724684-38269d6639fd?w=800&q=80' }
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
      'home.testimonials.items.2.name',
      'home.testimonials.items.2.location',
      'home.testimonials.items.2.text',
      'home.testimonials.items.3.name',
      'home.testimonials.items.3.location',
      'home.testimonials.items.3.text'
    ]).subscribe(translations => {
      this.testimonials = [
        {
          id: 1,
          name: translations['home.testimonials.items.1.name'],
          location: translations['home.testimonials.items.1.location'],
          rating: 5,
          text: translations['home.testimonials.items.1.text']
        },
        {
          id: 2,
          name: translations['home.testimonials.items.2.name'],
          location: translations['home.testimonials.items.2.location'],
          rating: 5,
          text: translations['home.testimonials.items.2.text']
        },
        {
          id: 3,
          name: translations['home.testimonials.items.3.name'],
          location: translations['home.testimonials.items.3.location'],
          rating: 5,
          text: translations['home.testimonials.items.3.text']
        }
      ];
    });
  }

  ngOnInit(): void {
    this.initTestimonials();
    this.loadFeaturedProducts();
    if (isPlatformBrowser(this.platformId)) {
      this.startTestimonialRotation();
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
    this.stopTestimonialRotation();
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

  // HOME-P02: Lazy load category background images using Intersection Observer
  private setupCategoryLazyLoading(): void {
    this.categoryObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const el = entry.target as HTMLElement;
            const bgUrl = el.getAttribute('data-bg');
            if (bgUrl) {
              el.style.setProperty('--panel-image', bgUrl);
              el.classList.add('categories__panel--loaded');
            }
            this.categoryObserver?.unobserve(el);
          }
        });
      },
      { rootMargin: '100px', threshold: 0.1 }
    );

    // Observe all category panels
    this.categoryPanels?.forEach((panel) => {
      this.categoryObserver?.observe(panel.nativeElement);
    });
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

  private startTestimonialRotation(): void {
    this.testimonialInterval = setInterval(() => {
      const next = (this.activeTestimonial() + 1) % this.testimonials.length;
      this.activeTestimonial.set(next);
    }, 6000);
  }

  private stopTestimonialRotation(): void {
    if (this.testimonialInterval) {
      clearInterval(this.testimonialInterval);
      this.testimonialInterval = null;
    }
  }

  setTestimonial(index: number): void {
    this.activeTestimonial.set(index);
    this.stopTestimonialRotation();
    if (isPlatformBrowser(this.platformId)) {
      this.startTestimonialRotation();
    }
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase();
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
