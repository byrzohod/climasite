import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
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


@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, TranslateModule, ProductCardComponent],
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

      <!-- HOME-001: Benefits section with professional SVG icons -->
      <section class="benefits-section" data-testid="benefits-section">
        <div class="benefits-grid">
          <div class="benefit-card">
            <div class="benefit-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M3.375 4.5C2.339 4.5 1.5 5.34 1.5 6.375V13.5h12V6.375c0-1.036-.84-1.875-1.875-1.875h-8.25zM13.5 15h-12v2.625c0 1.035.84 1.875 1.875 1.875h.375a3 3 0 116 0h3a.75.75 0 00.75-.75V15z" />
                <path d="M8.25 19.5a1.5 1.5 0 10-3 0 1.5 1.5 0 003 0zM15.75 6.75a.75.75 0 00-.75.75v11.25c0 .087.015.17.042.248a3 3 0 015.958.464c.853-.175 1.522-.935 1.464-1.883a18.659 18.659 0 00-3.732-10.104 1.837 1.837 0 00-1.47-.725H15.75z" />
                <path d="M19.5 19.5a1.5 1.5 0 10-3 0 1.5 1.5 0 003 0z" />
              </svg>
            </div>
            <h3>{{ 'home.benefits.freeShipping' | translate }}</h3>
            <p>{{ 'home.benefits.freeShippingDesc' | translate }}</p>
          </div>
          <div class="benefit-card">
            <div class="benefit-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12.516 2.17a.75.75 0 00-1.032 0 11.209 11.209 0 01-7.877 3.08.75.75 0 00-.722.515A12.74 12.74 0 002.25 9.75c0 5.942 4.064 10.933 9.563 12.348a.749.749 0 00.374 0c5.499-1.415 9.563-6.406 9.563-12.348 0-1.39-.223-2.73-.635-3.985a.75.75 0 00-.722-.516 11.209 11.209 0 01-7.877-3.08zm.924 5.89a.75.75 0 00-1.06-1.06l-3.75 3.75a.75.75 0 000 1.06l1.5 1.5a.75.75 0 001.06 0l3-3a.75.75 0 00-1.06-1.06l-2.47 2.47-.97-.97 3.75-3.75z" clip-rule="evenodd" />
              </svg>
            </div>
            <h3>{{ 'home.benefits.warranty' | translate }}</h3>
            <p>{{ 'home.benefits.warrantyDesc' | translate }}</p>
          </div>
          <div class="benefit-card">
            <div class="benefit-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M4.804 21.644A6.707 6.707 0 006 21.75a6.721 6.721 0 003.583-1.029c.774.182 1.584.279 2.417.279 5.322 0 9.75-3.97 9.75-9 0-5.03-4.428-9-9.75-9s-9.75 3.97-9.75 9c0 2.409 1.025 4.587 2.674 6.192.232.226.277.428.254.543a3.73 3.73 0 01-.814 1.686.75.75 0 00.44 1.223zM8.25 10.875a1.125 1.125 0 100 2.25 1.125 1.125 0 000-2.25zM10.875 12a1.125 1.125 0 112.25 0 1.125 1.125 0 01-2.25 0zm4.875-1.125a1.125 1.125 0 100 2.25 1.125 1.125 0 000-2.25z" clip-rule="evenodd" />
              </svg>
            </div>
            <h3>{{ 'home.benefits.support' | translate }}</h3>
            <p>{{ 'home.benefits.supportDesc' | translate }}</p>
          </div>
          <div class="benefit-card">
            <div class="benefit-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12 6.75a5.25 5.25 0 016.775-5.025.75.75 0 01.313 1.248l-3.32 3.319c.063.475.276.934.641 1.299.365.365.824.578 1.3.64l3.318-3.319a.75.75 0 011.248.313 5.25 5.25 0 01-5.472 6.756c-1.018-.086-1.87.1-2.309.634L7.344 21.3A3.298 3.298 0 112.7 16.657l8.684-7.151c.533-.44.72-1.291.634-2.309A5.342 5.342 0 0112 6.75zM4.117 19.125a.75.75 0 01.75-.75h.008a.75.75 0 01.75.75v.008a.75.75 0 01-.75.75h-.008a.75.75 0 01-.75-.75v-.008z" clip-rule="evenodd" />
              </svg>
            </div>
            <h3>{{ 'home.benefits.installation' | translate }}</h3>
            <p>{{ 'home.benefits.installationDesc' | translate }}</p>
          </div>
        </div>
      </section>

      <!-- HOME-001: Promotional Banners with SVG icons -->
      <section class="promo-section" data-testid="promo-section">
        <div class="promo-grid">
          <a routerLink="/promotions" class="promo-card" style="--promo-color: #ef4444">
            <div class="promo-icon promo-icon--sale">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M5.25 2.25a3 3 0 00-3 3v4.318a3 3 0 00.879 2.121l9.58 9.581c.92.92 2.39.92 3.31 0l4.17-4.17a2.343 2.343 0 000-3.311l-9.58-9.581a3 3 0 00-2.122-.879H5.25zM6.375 7.5a1.125 1.125 0 100-2.25 1.125 1.125 0 000 2.25z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="promo-content">
              <h3>{{ 'home.promo.sale.title' | translate }}</h3>
              <p>{{ 'home.promo.sale.description' | translate }}</p>
            </div>
            <div class="promo-arrow">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd" />
              </svg>
            </div>
          </a>
          <a routerLink="/products" class="promo-card" style="--promo-color: #8b5cf6">
            <div class="promo-icon promo-icon--new">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M14.615 1.595a.75.75 0 01.359.852L12.982 9.75h7.268a.75.75 0 01.548 1.262l-10.5 11.25a.75.75 0 01-1.272-.71l1.992-7.302H3.75a.75.75 0 01-.548-1.262l10.5-11.25a.75.75 0 01.913-.143z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="promo-content">
              <h3>{{ 'home.promo.new.title' | translate }}</h3>
              <p>{{ 'home.promo.new.description' | translate }}</p>
            </div>
            <div class="promo-arrow">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd" />
              </svg>
            </div>
          </a>
          <a routerLink="/products" class="promo-card" style="--promo-color: #10b981">
            <div class="promo-icon promo-icon--bundle">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M9.315 7.584C12.195 3.883 16.695 1.5 21.75 1.5a.75.75 0 01.75.75c0 5.056-2.383 9.555-6.084 12.436A6.75 6.75 0 019.75 22.5a.75.75 0 01-.75-.75v-4.131A15.838 15.838 0 016.382 15H2.25a.75.75 0 01-.75-.75 6.75 6.75 0 017.815-6.666zM15 6.75a2.25 2.25 0 100 4.5 2.25 2.25 0 000-4.5z" clip-rule="evenodd" />
                <path d="M5.26 17.242a.75.75 0 10-.897-1.203 5.243 5.243 0 00-2.05 5.022.75.75 0 00.625.627 5.243 5.243 0 005.022-2.051.75.75 0 10-1.202-.897 3.744 3.744 0 01-3.008 1.51c0-1.23.592-2.323 1.51-3.008z" />
              </svg>
            </div>
            <div class="promo-content">
              <h3>{{ 'home.promo.bundle.title' | translate }}</h3>
              <p>{{ 'home.promo.bundle.description' | translate }}</p>
            </div>
            <div class="promo-arrow">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd" />
              </svg>
            </div>
          </a>
        </div>
      </section>

      <!-- NAV-001 FIX: Use route-based navigation with correct database slugs -->
      <section class="categories-section">
        <h2>{{ 'home.categories.title' | translate }}</h2>
        <div class="categories-grid">
          <a [routerLink]="['/products/category', 'air-conditioners']" class="category-card category-card--cooling" data-testid="category-card">
            <div class="category-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.25a.75.75 0 01.75.75v2.25a.75.75 0 01-1.5 0V3a.75.75 0 01.75-.75zM7.5 12a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM18.894 6.166a.75.75 0 00-1.06-1.06l-1.591 1.59a.75.75 0 101.06 1.061l1.591-1.59zM21.75 12a.75.75 0 01-.75.75h-2.25a.75.75 0 010-1.5H21a.75.75 0 01.75.75zM17.834 18.894a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 10-1.061 1.06l1.59 1.591zM12 18a.75.75 0 01.75.75V21a.75.75 0 01-1.5 0v-2.25A.75.75 0 0112 18zM7.758 17.303a.75.75 0 00-1.061-1.06l-1.591 1.59a.75.75 0 001.06 1.061l1.591-1.59zM6 12a.75.75 0 01-.75.75H3a.75.75 0 010-1.5h2.25A.75.75 0 016 12zM6.697 7.757a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 00-1.061 1.06l1.59 1.591z"/>
              </svg>
            </div>
            <h3>{{ 'categories.airConditioning' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
          <a [routerLink]="['/products/category', 'heating-systems']" class="category-card category-card--heating" data-testid="category-card">
            <div class="category-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12.963 2.286a.75.75 0 00-1.071-.136 9.742 9.742 0 00-3.539 6.177A7.547 7.547 0 016.648 6.61a.75.75 0 00-1.152.082A9 9 0 1015.68 4.534a7.46 7.46 0 01-2.717-2.248zM15.75 14.25a3.75 3.75 0 11-7.313-1.172c.628.465 1.35.81 2.133 1a5.99 5.99 0 011.925-3.545 3.75 3.75 0 013.255 3.717z" clip-rule="evenodd"/>
              </svg>
            </div>
            <h3>{{ 'categories.heatingSystems' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
          <a [routerLink]="['/products/category', 'ventilation']" class="category-card category-card--ventilation" data-testid="category-card">
            <div class="category-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.25c-5.385 0-9.75 4.365-9.75 9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385 2.25 12 2.25zm-.53 5.47a.75.75 0 011.06 0l3 3a.75.75 0 010 1.06l-3 3a.75.75 0 11-1.06-1.06l1.72-1.72H8.25a.75.75 0 010-1.5h4.94l-1.72-1.72a.75.75 0 010-1.06z"/>
              </svg>
            </div>
            <h3>{{ 'categories.ventilation' | translate }}</h3>
            <span class="category-count">{{ 'home.categories.viewProducts' | translate }}</span>
          </a>
          <a [routerLink]="['/products/category', 'accessories']" class="category-card category-card--accessories" data-testid="category-card">
            <div class="category-icon">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M12 6.75a5.25 5.25 0 016.775-5.025.75.75 0 01.313 1.248l-3.32 3.319c.063.475.276.934.641 1.299.365.365.824.578 1.3.64l3.318-3.319a.75.75 0 011.248.313 5.25 5.25 0 01-5.472 6.756c-1.018-.086-1.87.1-2.309.634L7.344 21.3A3.298 3.298 0 112.7 16.657l8.684-7.151c.533-.44.72-1.291.634-2.309A5.342 5.342 0 0112 6.75zM4.117 19.125a.75.75 0 01.75-.75h.008a.75.75 0 01.75.75v.008a.75.75 0 01-.75.75h-.008a.75.75 0 01-.75-.75v-.008z" clip-rule="evenodd"/>
              </svg>
            </div>
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

      <!-- HOME-001: Newsletter Section with proper feedback -->
      <section class="newsletter-section" data-testid="newsletter-section">
        <div class="newsletter-content">
          <div class="newsletter-icon">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
              <path d="M1.5 8.67v8.58a3 3 0 003 3h15a3 3 0 003-3V8.67l-8.928 5.493a3 3 0 01-3.144 0L1.5 8.67z" />
              <path d="M22.5 6.908V6.75a3 3 0 00-3-3h-15a3 3 0 00-3 3v.158l9.714 5.978a1.5 1.5 0 001.572 0L22.5 6.908z" />
            </svg>
          </div>
          <h2>{{ 'home.newsletter.title' | translate }}</h2>
          <p>{{ 'home.newsletter.subtitle' | translate }}</p>
          @if (!newsletterSubmitted()) {
            <form class="newsletter-form" (submit)="subscribeNewsletter($event)">
              <div class="newsletter-input-wrapper">
                <input
                  type="email"
                  [(ngModel)]="newsletterEmail"
                  name="email"
                  [placeholder]="'home.newsletter.placeholder' | translate"
                  class="newsletter-input"
                  [class.newsletter-input--error]="newsletterError()"
                  required
                />
                @if (newsletterError()) {
                  <span class="newsletter-error">{{ newsletterError() }}</span>
                }
              </div>
              <button type="submit" class="newsletter-button" [disabled]="newsletterLoading()">
                @if (newsletterLoading()) {
                  <span class="newsletter-spinner"></span>
                } @else {
                  {{ 'home.newsletter.subscribe' | translate }}
                }
              </button>
            </form>
          } @else {
            <div class="newsletter-success">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm13.36-1.814a.75.75 0 10-1.22-.872l-3.236 4.53L9.53 12.22a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.14-.094l3.75-5.25z" clip-rule="evenodd" />
              </svg>
              <span>{{ 'home.newsletter.success' | translate }}</span>
            </div>
          }
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
      border-radius: 16px;
      padding: 2rem 1.5rem;
      text-align: center;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
      border: 1px solid var(--color-border-primary);
      transition: transform 0.3s, box-shadow 0.3s;

      &:hover {
        transform: translateY(-6px);
        box-shadow: 0 12px 24px rgba(0, 0, 0, 0.1);
      }

      .benefit-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 64px;
        height: 64px;
        margin: 0 auto 1.25rem;
        background: linear-gradient(135deg, var(--color-primary-light) 0%, var(--color-bg-secondary) 100%);
        border-radius: 16px;
        color: var(--color-primary);

        svg {
          width: 32px;
          height: 32px;
        }
      }

      h3 {
        color: var(--color-text-primary);
        font-size: 1.0625rem;
        font-weight: 600;
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        font-size: 0.875rem;
        margin: 0;
        line-height: 1.5;
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
      gap: 1.25rem;
      padding: 1.5rem 1.75rem;
      background: var(--color-bg-primary);
      border-radius: 16px;
      border-left: 4px solid var(--promo-color, var(--color-primary));
      text-decoration: none;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
      transition: transform 0.3s, box-shadow 0.3s;

      &:hover {
        transform: translateX(6px);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);

        .promo-arrow svg {
          transform: translateX(4px);
        }
      }

      .promo-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 52px;
        height: 52px;
        border-radius: 12px;
        flex-shrink: 0;

        svg {
          width: 28px;
          height: 28px;
        }

        &--sale {
          background: rgba(239, 68, 68, 0.1);
          color: #ef4444;
        }

        &--new {
          background: rgba(139, 92, 246, 0.1);
          color: #8b5cf6;
        }

        &--bundle {
          background: rgba(16, 185, 129, 0.1);
          color: #10b981;
        }
      }

      .promo-content {
        flex: 1;

        h3 {
          color: var(--color-text-primary);
          font-size: 1.0625rem;
          font-weight: 600;
          margin-bottom: 0.25rem;
        }

        p {
          color: var(--color-text-secondary);
          font-size: 0.875rem;
          margin: 0;
          line-height: 1.4;
        }
      }

      .promo-arrow {
        color: var(--promo-color, var(--color-primary));
        flex-shrink: 0;

        svg {
          width: 24px;
          height: 24px;
          transition: transform 0.3s;
        }
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
      border: 1px solid var(--color-border-primary);
      border-radius: 20px;
      padding: 2.5rem 2rem;
      text-align: center;
      text-decoration: none;
      transition: all 0.3s;
      position: relative;
      overflow: hidden;

      &::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 4px;
        background: var(--category-color, var(--color-primary));
        transform: scaleX(0);
        transition: transform 0.3s;
      }

      &:hover {
        transform: translateY(-8px);
        box-shadow: 0 16px 40px rgba(0, 0, 0, 0.12);
        border-color: var(--category-color, var(--color-primary));

        &::before {
          transform: scaleX(1);
        }

        .category-icon {
          transform: scale(1.1);
        }
      }

      .category-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 80px;
        height: 80px;
        margin: 0 auto 1.25rem;
        border-radius: 20px;
        transition: transform 0.3s;

        svg {
          width: 40px;
          height: 40px;
        }
      }

      /* Category-specific colors */
      &--cooling {
        --category-color: #06b6d4;

        .category-icon {
          background: linear-gradient(135deg, rgba(6, 182, 212, 0.15) 0%, rgba(6, 182, 212, 0.05) 100%);
          color: #06b6d4;
        }
      }

      &--heating {
        --category-color: #f97316;

        .category-icon {
          background: linear-gradient(135deg, rgba(249, 115, 22, 0.15) 0%, rgba(249, 115, 22, 0.05) 100%);
          color: #f97316;
        }
      }

      &--ventilation {
        --category-color: #22c55e;

        .category-icon {
          background: linear-gradient(135deg, rgba(34, 197, 94, 0.15) 0%, rgba(34, 197, 94, 0.05) 100%);
          color: #22c55e;
        }
      }

      &--accessories {
        --category-color: #8b5cf6;

        .category-icon {
          background: linear-gradient(135deg, rgba(139, 92, 246, 0.15) 0%, rgba(139, 92, 246, 0.05) 100%);
          color: #8b5cf6;
        }
      }

      h3 {
        color: var(--color-text-primary);
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 0.75rem;
      }

      .category-count {
        color: var(--category-color, var(--color-primary));
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
      padding: 5rem 2rem;
      color: white;
      position: relative;
      overflow: hidden;

      &::before {
        content: '';
        position: absolute;
        top: -50%;
        right: -20%;
        width: 500px;
        height: 500px;
        background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
        border-radius: 50%;
      }
    }

    .newsletter-content {
      max-width: 600px;
      margin: 0 auto;
      text-align: center;
      position: relative;
      z-index: 1;

      .newsletter-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 72px;
        height: 72px;
        margin: 0 auto 1.5rem;
        background: rgba(255,255,255,0.15);
        border-radius: 20px;
        backdrop-filter: blur(10px);

        svg {
          width: 36px;
          height: 36px;
        }
      }

      h2 {
        font-size: 2.25rem;
        font-weight: 700;
        margin-bottom: 0.75rem;
      }

      p {
        opacity: 0.9;
        margin-bottom: 2rem;
        font-size: 1.0625rem;
        line-height: 1.6;
      }
    }

    .newsletter-form {
      display: flex;
      gap: 0.75rem;
      max-width: 500px;
      margin: 0 auto;
    }

    .newsletter-input-wrapper {
      flex: 1;
      position: relative;
    }

    .newsletter-input {
      width: 100%;
      padding: 1.125rem 1.5rem;
      border: 2px solid transparent;
      border-radius: 50px;
      font-size: 1rem;
      outline: none;
      transition: border-color 0.2s, box-shadow 0.2s;

      &:focus {
        border-color: rgba(255,255,255,0.5);
        box-shadow: 0 0 0 4px rgba(255,255,255,0.1);
      }

      &--error {
        border-color: #fca5a5;
      }
    }

    .newsletter-error {
      position: absolute;
      bottom: -24px;
      left: 1.5rem;
      font-size: 0.8125rem;
      color: #fca5a5;
    }

    .newsletter-button {
      padding: 1.125rem 2rem;
      background: white;
      color: var(--color-primary);
      border: none;
      border-radius: 50px;
      font-weight: 600;
      font-size: 1rem;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
      white-space: nowrap;
      min-width: 140px;
      display: flex;
      align-items: center;
      justify-content: center;

      &:hover:not(:disabled) {
        transform: scale(1.05);
        box-shadow: 0 8px 20px rgba(0,0,0,0.2);
      }

      &:disabled {
        opacity: 0.8;
        cursor: wait;
      }
    }

    .newsletter-spinner {
      width: 20px;
      height: 20px;
      border: 2px solid var(--color-primary-light);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    .newsletter-success {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      padding: 1.25rem 2rem;
      background: rgba(255,255,255,0.15);
      border-radius: 50px;
      backdrop-filter: blur(10px);
      animation: fadeIn 0.3s ease;

      svg {
        width: 24px;
        height: 24px;
        color: #86efac;
      }

      span {
        font-weight: 500;
      }
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
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
  private readonly translateService = inject(TranslateService);
  private slideInterval: ReturnType<typeof setInterval> | null = null;

  featuredProducts = signal<ProductBrief[]>([]);
  loadingFeatured = signal(true);
  currentSlide = signal(0);

  // Newsletter form state
  newsletterEmail = '';
  newsletterLoading = signal(false);
  newsletterError = signal<string | null>(null);
  newsletterSubmitted = signal(false);

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
    this.newsletterError.set(null);

    // Validate email
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!this.newsletterEmail.trim()) {
      this.translateService.get('home.newsletter.errorRequired').subscribe(msg => {
        this.newsletterError.set(msg);
      });
      return;
    }
    if (!emailRegex.test(this.newsletterEmail.trim())) {
      this.translateService.get('home.newsletter.errorInvalid').subscribe(msg => {
        this.newsletterError.set(msg);
      });
      return;
    }

    // Simulate API call
    this.newsletterLoading.set(true);
    setTimeout(() => {
      this.newsletterLoading.set(false);
      this.newsletterSubmitted.set(true);
      this.newsletterEmail = '';
    }, 1000);
  }
}
