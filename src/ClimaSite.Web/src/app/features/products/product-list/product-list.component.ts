import { Component, inject, signal, computed, OnInit, OnDestroy, effect, ViewChild, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { LanguageService } from '../../../core/services/language.service';
import { ProductBrief, FilterOptions, ProductFilter, PaginatedResult } from '../../../core/models/product.model';
import { Category } from '../../../core/models/category.model';
import { ProductCardComponent } from '../product-card/product-card.component';
import { CategoryHeaderComponent, CategoryInfo } from '../../../shared/components/category-header/category-header.component';
import { RevealDirective } from '../../../shared/directives/reveal.directive';
import { EmptyStateComponent } from '../../../shared/components/empty-state';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    TranslateModule,
    ProductCardComponent,
    CategoryHeaderComponent,
    RevealDirective,
    EmptyStateComponent
  ],
  template: `
    <div class="product-list-page">
      <!-- Filter Backdrop (mobile only) -->
      <div 
        class="filter-backdrop" 
        [class.open]="filterOpen()"
        (click)="closeFilterSidebar()"
        aria-hidden="true">
      </div>

      <!-- Category Header (when a category is selected) -->
      @if (category() && !searchQuery()) {
        <app-category-header [category]="categoryInfo()" />
      } @else {
        <!-- Default breadcrumb for all products or search results -->
        <div class="breadcrumb" data-testid="breadcrumb">
          <a routerLink="/">{{ 'nav.home' | translate }}</a>
          <span class="separator">/</span>
          <span class="current">{{ 'products.all' | translate }}</span>
        </div>

        <div class="page-header">
          @if (searchQuery()) {
            <h1 data-testid="search-results-title">{{ 'products.searchResults' | translate }}: "{{ searchQuery() }}"</h1>
          } @else {
            <h1>{{ 'products.all' | translate }}</h1>
          }
          <p class="result-count">{{ totalCount() }} {{ 'products.title' | translate | lowercase }}</p>
        </div>
      }

      <div class="product-list-layout">
        <aside 
          #filterSidebarRef
          class="filter-sidebar" 
          [class.open]="filterOpen()"
          role="dialog"
          aria-modal="true"
          [attr.aria-label]="'products.filter_sidebar' | translate"
          (keydown)="onFilterSidebarKeydown($event)">
          <div class="filter-header">
            <h2>{{ 'products.filters.title' | translate }}</h2>
            <button 
              class="close-filters" 
              (click)="closeFilterSidebar()" 
              [attr.aria-label]="'common.close' | translate" 
              data-testid="close-filters-btn">
              <span>&times;</span>
            </button>
          </div>

          <div class="filter-content">
            @if (!filterOptions()) {
              <!-- Loading skeleton for filters -->
              <div class="filter-skeleton" data-testid="filter-skeleton">
                <div class="skeleton-section">
                  <div class="skeleton-title"></div>
                  <div class="skeleton-input"></div>
                </div>
                <div class="skeleton-section">
                  <div class="skeleton-title"></div>
                  <div class="skeleton-option"></div>
                  <div class="skeleton-option"></div>
                  <div class="skeleton-option"></div>
                </div>
                <div class="skeleton-section">
                  <div class="skeleton-title"></div>
                  <div class="skeleton-option"></div>
                  <div class="skeleton-option"></div>
                </div>
              </div>
            }

            @if (filterOptions()) {
              <div class="filter-group">
                <h3 id="price-range-label">{{ 'products.filters.priceRange' | translate }}</h3>
                <div class="price-inputs" role="group" aria-labelledby="price-range-label">
                  <div class="price-input-wrapper">
                    <label for="filter-min-price" class="sr-only">{{ 'products.filters.minPriceLabel' | translate }}</label>
                    <input
                      type="number"
                      id="filter-min-price"
                      [placeholder]="'products.filters.minPrice' | translate"
                      [(ngModel)]="minPrice"
                      (change)="applyFilters()"
                      [attr.aria-describedby]="'price-filter-hint'"
                      data-testid="filter-price-min"
                    />
                  </div>
                  <span aria-hidden="true">-</span>
                  <div class="price-input-wrapper">
                    <label for="filter-max-price" class="sr-only">{{ 'products.filters.maxPriceLabel' | translate }}</label>
                    <input
                      type="number"
                      id="filter-max-price"
                      [placeholder]="'products.filters.maxPrice' | translate"
                      [(ngModel)]="maxPrice"
                      (change)="applyFilters()"
                      [attr.aria-describedby]="'price-filter-hint'"
                      data-testid="filter-price-max"
                    />
                  </div>
                </div>
                <span id="price-filter-hint" class="sr-only">{{ 'products.filters.priceFilterHint' | translate }}</span>
              </div>

              @if (filterOptions()!.brands.length > 0) {
                <div class="filter-group">
                  <h3 id="brand-filter-label">{{ 'products.filters.brand' | translate }}</h3>
                  <div class="filter-options" role="group" aria-labelledby="brand-filter-label">
                    @for (brand of filterOptions()!.brands; track brand.name) {
                      <div class="filter-option">
                        <input
                          type="checkbox"
                          [id]="'brand-' + brand.name"
                          [checked]="selectedBrand() === brand.name"
                          (change)="toggleBrand(brand.name)"
                          data-testid="filter-brand-checkbox"
                        />
                        <label [for]="'brand-' + brand.name">
                          <span class="brand-name">{{ brand.name }}</span>
                          <span class="count">({{ brand.count }})</span>
                        </label>
                      </div>
                    }
                  </div>
                </div>
              }

              <div class="filter-group">
                <h3>{{ 'products.details.inStock' | translate }}</h3>
                <label class="filter-option">
                  <input
                    type="checkbox"
                    [checked]="inStockOnly()"
                    (change)="toggleInStock()"
                    data-testid="filter-in-stock"
                  />
                  <span>{{ 'products.filters.inStock' | translate }}</span>
                </label>
                <label class="filter-option">
                  <input
                    type="checkbox"
                    [checked]="onSaleOnly()"
                    (change)="toggleOnSale()"
                    data-testid="filter-on-sale"
                  />
                  <span>{{ 'products.onSale' | translate }}</span>
                </label>
              </div>

              <div class="filter-actions">
                @if (hasActiveFilters()) {
                  <button class="clear-filters" (click)="clearFilters()" data-testid="clear-filters-btn">
                    {{ 'products.filters.clearAll' | translate }}
                  </button>
                }
              </div>
            }
          </div>
        </aside>

        <main class="product-grid-container">
          <div class="toolbar">
            <button class="filter-toggle" (click)="openFilterSidebar()" [attr.aria-label]="'products.filters.title' | translate">
              <svg class="filter-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" /></svg>
              {{ 'products.filters.title' | translate }}
            </button>

            <div class="view-options" role="group" aria-label="View options">
              <button
                class="view-btn"
                [class.active]="viewMode() === 'grid'"
                (click)="viewMode.set('grid')"
                [attr.aria-label]="'products.view.grid' | translate"
                [attr.aria-pressed]="viewMode() === 'grid'"
              >
                <svg class="grid-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6A2.25 2.25 0 016 3.75h2.25A2.25 2.25 0 0110.5 6v2.25a2.25 2.25 0 01-2.25 2.25H6a2.25 2.25 0 01-2.25-2.25V6zM3.75 15.75A2.25 2.25 0 016 13.5h2.25a2.25 2.25 0 012.25 2.25V18a2.25 2.25 0 01-2.25 2.25H6A2.25 2.25 0 013.75 18v-2.25zM13.5 6a2.25 2.25 0 012.25-2.25H18A2.25 2.25 0 0120.25 6v2.25A2.25 2.25 0 0118 10.5h-2.25a2.25 2.25 0 01-2.25-2.25V6zM13.5 15.75a2.25 2.25 0 012.25-2.25H18a2.25 2.25 0 012.25 2.25V18A2.25 2.25 0 0118 20.25h-2.25A2.25 2.25 0 0113.5 18v-2.25z" /></svg>
              </button>
              <button
                class="view-btn"
                [class.active]="viewMode() === 'list'"
                (click)="viewMode.set('list')"
                [attr.aria-label]="'products.view.list' | translate"
                [attr.aria-pressed]="viewMode() === 'list'"
              >
                <svg class="list-icon" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zM3.75 12h.007v.008H3.75V12zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm-.375 5.25h.007v.008H3.75v-.008zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" /></svg>
              </button>
            </div>

            <div class="sort-options">
              <label>{{ 'products.sort.title' | translate }}:</label>
              <select [(ngModel)]="sortBy" (change)="applyFilters()" data-testid="sort-dropdown">
                <option value="newest">{{ 'products.sort.newest' | translate }}</option>
                <option value="price">{{ 'products.sort.priceAsc' | translate }}</option>
                <option value="price-desc">{{ 'products.sort.priceDesc' | translate }}</option>
                <option value="name">{{ 'products.sort.nameAsc' | translate }}</option>
                <option value="name-desc">{{ 'products.sort.nameDesc' | translate }}</option>
              </select>
            </div>
          </div>

          @if (loading()) {
            <!-- Skeleton product cards for better loading UX -->
            <div class="product-grid" data-testid="product-skeleton-grid">
              @for (i of [1, 2, 3, 4, 5, 6, 7, 8]; track i) {
                <div class="product-skeleton-card" data-testid="product-skeleton">
                  <div class="skeleton-image"></div>
                  <div class="skeleton-content">
                    <div class="skeleton-badge"></div>
                    <div class="skeleton-title"></div>
                    <div class="skeleton-title-short"></div>
                    <div class="skeleton-price"></div>
                    <div class="skeleton-rating"></div>
                    <div class="skeleton-button"></div>
                  </div>
                </div>
              }
            </div>
          } @else if (error()) {
            <div class="error-state" data-testid="product-list-error">
              <div class="error-icon">!</div>
              <h2>{{ error() | translate }}</h2>
              <p>{{ 'products.error.tryAgain' | translate }}</p>
              <button class="btn-primary" (click)="retryLoad()" data-testid="retry-button">
                {{ 'common.retry' | translate }}
              </button>
            </div>
          } @else if (products().length === 0) {
            <app-empty-state
              variant="search"
              [title]="'emptyState.search.title' | translate"
              [description]="'emptyState.search.description' | translate"
              data-testid="products-empty"
            >
              @if (hasActiveFilters()) {
                <button class="clear-filters-action" (click)="clearFilters()">
                  {{ 'products.filters.clearAll' | translate }}
                </button>
              }
            </app-empty-state>
          } @else {
            <div class="product-grid" [class.list-view]="viewMode() === 'list'">
              @for (product of products(); track product.id; let i = $index) {
                <div class="product-card-wrapper" appReveal="fade-up" [delay]="(i % 4) * 75" [once]="true">
                  <app-product-card [product]="product" [listView]="viewMode() === 'list'" />
                </div>
              }
            </div>

            @if (totalPages() > 1) {
              <nav class="pagination" role="navigation" aria-label="Pagination" data-testid="pagination">
                <button
                  class="page-btn"
                  [disabled]="currentPage() === 1"
                  (click)="goToPage(currentPage() - 1)"
                  [attr.aria-label]="'common.previous' | translate"
                  data-testid="pagination-prev"
                >
                  {{ 'common.previous' | translate }}
                </button>

                @for (page of visiblePages(); track page) {
                  <button
                    class="page-btn"
                    [class.active]="page === currentPage()"
                    (click)="goToPage(page)"
                    [attr.aria-label]="'Page ' + page"
                    [attr.aria-current]="page === currentPage() ? 'page' : null"
                    data-testid="pagination-page"
                  >
                    {{ page }}
                  </button>
                }

                <button
                  class="page-btn"
                  [disabled]="currentPage() === totalPages()"
                  (click)="goToPage(currentPage() + 1)"
                  [attr.aria-label]="'common.next' | translate"
                  data-testid="pagination-next"
                >
                  {{ 'common.next' | translate }}
                </button>
              </nav>
            }
          }
        </main>
      </div>
    </div>
  `,
  styles: [`
    .product-list-page {
      max-width: 1400px;
      margin: 0 auto;
      padding: 1rem;
    }

    /* Filter Backdrop - Mobile Only */
    .filter-backdrop {
      display: none;
      
      @media (max-width: 1024px) {
        display: block;
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0);
        transition: background 0.3s ease-out;
        pointer-events: none;
        z-index: var(--z-modal-backdrop, 999);
        
        &.open {
          background: rgba(0, 0, 0, 0.5);
          pointer-events: auto;
        }
      }
    }

    .breadcrumb {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
      padding: 1rem 0;
      font-size: 0.875rem;
      color: var(--color-text-secondary);

      a {
        color: var(--color-primary);
        text-decoration: none;
        &:hover { text-decoration: underline; }
      }

      .separator { color: var(--color-text-secondary); }
      .current { color: var(--color-text-primary); }
    }

    .page-header {
      margin-bottom: 2rem;

      h1 {
        font-size: 2rem;
        font-weight: 700;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      .category-description {
        color: var(--color-text-secondary);
        margin-bottom: 0.5rem;
      }

      .result-count {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }
    }

    .product-list-layout {
      display: grid;
      grid-template-columns: 280px 1fr;
      gap: 2rem;

      @media (max-width: 1024px) {
        grid-template-columns: 1fr;
      }
    }

    .filter-sidebar {
      position: sticky;
      top: 1rem;
      height: fit-content;
      background: var(--glass-bg, rgba(255, 255, 255, 0.8));
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      border-radius: var(--radius-lg, 16px);
      padding: 1.5rem;
      box-shadow: var(--shadow-md, 0 4px 6px -1px rgba(0, 0, 0, 0.1));
      border: 1px solid var(--glass-border, rgba(255, 255, 255, 0.2));

      @media (max-width: 1024px) {
        position: fixed;
        top: 0;
        left: 0;
        width: min(320px, 85vw);
        height: 100vh;
        z-index: var(--z-modal, 1000);
        border-radius: 0;
        transform: translateX(-100%);
        transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        box-shadow: var(--shadow-2xl, 0 25px 50px -12px rgba(0, 0, 0, 0.25));
        overflow-y: auto;
        display: flex;
        flex-direction: column;

        &.open {
          transform: translateX(0);
        }
      }

      .filter-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 1.5rem;

        h2 {
          font-size: 1.25rem;
          font-weight: 600;
          color: var(--color-text-primary);
        }

        .close-filters {
          display: none;
          background: none;
          border: none;
          font-size: 1.5rem;
          cursor: pointer;
          color: var(--color-text-secondary);
          transition: color 0.2s ease-out;

          &:hover {
            color: var(--color-text-primary);
          }

          @media (max-width: 1024px) {
            display: block;
          }
        }
      }
    }

    .filter-content {
      flex: 1;
      overflow-y: auto;
    }

    /* Filter Groups with staggered animation */
    .filter-group {
      margin-bottom: 1.5rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border);

      &:last-of-type {
        border-bottom: none;
        margin-bottom: 0;
        padding-bottom: 0;
      }

      h3 {
        font-size: 0.875rem;
        font-weight: 600;
        color: var(--color-text-primary);
        margin-bottom: 1rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }

      /* Staggered animation on mobile */
      @media (max-width: 1024px) {
        opacity: 0;
        transform: translateX(-20px);
        transition: opacity 0.3s ease-out, transform 0.3s ease-out;

        .filter-sidebar.open & {
          opacity: 1;
          transform: translateX(0);
        }

        &:nth-child(1) { transition-delay: 50ms; }
        &:nth-child(2) { transition-delay: 100ms; }
        &:nth-child(3) { transition-delay: 150ms; }
        &:nth-child(4) { transition-delay: 200ms; }
        &:nth-child(5) { transition-delay: 250ms; }
        &:nth-child(6) { transition-delay: 300ms; }
      }
    }

    /* Filter Actions with delayed entrance */
    .filter-actions {
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border);

      @media (max-width: 1024px) {
        opacity: 0;
        transform: translateY(10px);
        transition: opacity 0.3s ease-out 0.2s, transform 0.3s ease-out 0.2s;

        .filter-sidebar.open & {
          opacity: 1;
          transform: translateY(0);
        }
      }
    }

    .price-inputs {
      display: flex;
      align-items: center;
      gap: 0.5rem;

      .price-input-wrapper {
        flex: 1;
      }

      input {
        width: 100%;
        padding: 0.5rem;
        border: 1px solid var(--color-border);
        border-radius: 6px;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }
      }

      span {
        color: var(--color-text-secondary);
      }
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    .filter-options {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .filter-option {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      font-size: 0.875rem;
      color: var(--color-text-primary);
      padding: 0.5rem;
      margin: -0.5rem;
      margin-bottom: 0.25rem;
      border-radius: 6px;
      transition: background-color 0.2s ease-out;

      &:hover {
        background-color: var(--color-bg-secondary);
      }

      &:last-child {
        margin-bottom: -0.5rem;
      }

      input[type="checkbox"] {
        width: 18px;
        height: 18px;
        accent-color: var(--color-primary);
        cursor: pointer;
        transition: transform 0.2s ease-out;

        &:checked {
          transform: scale(1.1);
        }

        &:checked + label,
        &:checked ~ span {
          color: var(--color-primary);
          font-weight: 500;
        }
      }

      label {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        cursor: pointer;
        transition: color 0.2s ease-out;
      }

      .count {
        color: var(--color-text-secondary);
        font-size: 0.75rem;
      }
    }

    .clear-filters {
      width: 100%;
      padding: 0.75rem;
      background: var(--color-bg-secondary);
      border: 1px solid var(--color-border);
      border-radius: 6px;
      color: var(--color-text-primary);
      cursor: pointer;
      font-weight: 500;
      transition: all 0.2s ease-out;

      &:hover {
        background: var(--color-error-bg);
        color: var(--color-error);
        border-color: var(--color-error);
      }

      &:active {
        transform: scale(0.98);
      }
    }

    .filter-skeleton {
      .skeleton-section {
        margin-bottom: 1.5rem;
        padding-bottom: 1.5rem;
        border-bottom: 1px solid var(--color-border);

        &:last-child {
          border-bottom: none;
        }
      }

      .skeleton-title {
        height: 14px;
        width: 60%;
        background: var(--color-border);
        border-radius: 4px;
        margin-bottom: 1rem;
        animation: skeleton-pulse 1.5s ease-in-out infinite;
      }

      .skeleton-input {
        height: 36px;
        width: 100%;
        background: var(--color-border);
        border-radius: 6px;
        animation: skeleton-pulse 1.5s ease-in-out infinite;
      }

      .skeleton-option {
        height: 20px;
        width: 80%;
        background: var(--color-border);
        border-radius: 4px;
        margin-bottom: 0.5rem;
        animation: skeleton-pulse 1.5s ease-in-out infinite;

        &:nth-child(2) { width: 70%; animation-delay: 0.1s; }
        &:nth-child(3) { width: 85%; animation-delay: 0.2s; }
        &:nth-child(4) { width: 65%; animation-delay: 0.3s; }
      }
    }

    @keyframes skeleton-pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
    }

    .product-grid-container {
      min-height: 400px;
    }

    .toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 1rem;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.5rem;
      background: var(--glass-bg, rgba(255, 255, 255, 0.8));
      backdrop-filter: blur(12px);
      -webkit-backdrop-filter: blur(12px);
      border-radius: var(--radius-lg, 16px);
      margin-bottom: 1.5rem;
      box-shadow: var(--shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.1));
      border: 1px solid var(--glass-border, rgba(255, 255, 255, 0.2));

      .filter-toggle {
        display: none;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 1rem;
        background: var(--color-bg-secondary);
        border: 1px solid var(--color-border);
        border-radius: 6px;
        cursor: pointer;
        color: var(--color-text-primary);

        .filter-icon {
          width: 20px;
          height: 20px;
        }

        @media (max-width: 1024px) {
          display: flex;
        }
      }
    }

    .view-options {
      display: flex;
      gap: 0.5rem;

      .view-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 0.5rem;
        background: var(--color-bg-secondary);
        border: 1px solid var(--color-border);
        border-radius: 6px;
        cursor: pointer;
        color: var(--color-text-secondary);
        transition: all 0.2s ease;

        .grid-icon,
        .list-icon {
          width: 20px;
          height: 20px;
        }

        &:hover, &.active {
          background: var(--color-primary);
          color: var(--color-text-inverse);
          border-color: var(--color-primary);
        }
      }
    }

    .sort-options {
      display: flex;
      align-items: center;
      gap: 0.5rem;

      label {
        font-size: 0.875rem;
        color: var(--color-text-secondary);
      }

      select {
        padding: 0.5rem 1rem;
        border: 1px solid var(--color-border);
        border-radius: 6px;
        background: var(--color-bg-secondary);
        color: var(--color-text-primary);
        cursor: pointer;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }
      }
    }

    .product-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.5rem;

      &.list-view {
        grid-template-columns: 1fr;
      }

      .product-card-wrapper {
        height: 100%;
      }
    }

    /* Product Skeleton Cards */
    .product-skeleton-card {
      background: var(--glass-bg, rgba(255, 255, 255, 0.8));
      backdrop-filter: blur(8px);
      -webkit-backdrop-filter: blur(8px);
      border: 1px solid var(--glass-border, rgba(255, 255, 255, 0.2));
      border-radius: var(--radius-lg, 16px);
      overflow: hidden;
      animation: skeleton-shimmer 1.5s ease-in-out infinite;
    }

    .skeleton-image {
      width: 100%;
      aspect-ratio: 4 / 3;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
    }

    .skeleton-content {
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .skeleton-badge {
      width: 60px;
      height: 20px;
      border-radius: 4px;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
      animation-delay: 0.1s;
    }

    .skeleton-title {
      height: 20px;
      width: 90%;
      border-radius: 4px;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
      animation-delay: 0.2s;
    }

    .skeleton-title-short {
      height: 16px;
      width: 60%;
      border-radius: 4px;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
      animation-delay: 0.3s;
    }

    .skeleton-price {
      height: 24px;
      width: 80px;
      border-radius: 4px;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
      animation-delay: 0.4s;
    }

    .skeleton-rating {
      height: 16px;
      width: 100px;
      border-radius: 4px;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
      animation-delay: 0.5s;
    }

    .skeleton-button {
      height: 44px;
      width: 100%;
      border-radius: 8px;
      margin-top: 0.5rem;
      background: linear-gradient(
        90deg,
        var(--color-border) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-border) 100%
      );
      background-size: 200% 100%;
      animation: skeleton-slide 1.5s ease-in-out infinite;
      animation-delay: 0.6s;
    }

    @keyframes skeleton-slide {
      0% {
        background-position: 200% 0;
      }
      100% {
        background-position: -200% 0;
      }
    }

    @keyframes skeleton-shimmer {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.8;
      }
    }

    /* Clear filters button in EmptyState slot */
    .clear-filters-action {
      padding: 0.75rem 1.5rem;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      border: 1px solid var(--color-border);
      border-radius: 8px;
      cursor: pointer;
      font-weight: 500;
      transition: all 0.2s ease;

      &:hover {
        background: var(--color-error-bg);
        color: var(--color-error);
        border-color: var(--color-error);
      }
    }

    .error-state {
      text-align: center;
      padding: 4rem 2rem;
      background: var(--color-bg-primary);
      border-radius: 12px;

      h2 {
        font-size: 1.5rem;
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 1.5rem;
      }

      .btn-primary {
        padding: 0.75rem 1.5rem;
        background: var(--color-primary);
        color: var(--color-text-inverse);
        border: none;
        border-radius: 8px;
        cursor: pointer;
        font-weight: 500;
        transition: background 0.2s ease;

        &:hover {
          background: var(--color-primary-dark);
        }
      }
    }

    .error-state {
      background: var(--color-error-bg);
      border: 1px solid var(--color-error);

      .error-icon {
        width: 60px;
        height: 60px;
        margin: 0 auto 1rem;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--color-error);
        color: var(--color-text-inverse);
        font-size: 2rem;
        font-weight: bold;
        border-radius: 50%;
      }

      h2 {
        color: var(--color-error);
      }
    }

    .pagination {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
      margin-top: 2rem;
      padding: 1rem;
      background: var(--glass-bg, rgba(255, 255, 255, 0.6));
      backdrop-filter: blur(8px);
      -webkit-backdrop-filter: blur(8px);
      border-radius: var(--radius-full, 9999px);
      border: 1px solid var(--glass-border, rgba(255, 255, 255, 0.2));
      width: fit-content;
      margin-left: auto;
      margin-right: auto;

      .page-btn {
        min-width: 40px;
        padding: 0.5rem 1rem;
        background: transparent;
        border: 1px solid transparent;
        border-radius: var(--radius-full, 9999px);
        cursor: pointer;
        color: var(--color-text-primary);
        font-weight: 500;
        transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);

        &:hover:not(:disabled) {
          background: var(--color-bg-secondary);
          border-color: var(--color-border);
          transform: translateY(-2px);
        }

        &.active {
          background: var(--color-primary);
          color: var(--color-text-inverse);
          border-color: var(--color-primary);
          box-shadow: 0 4px 12px rgba(var(--color-primary-rgb, 59, 130, 246), 0.3);
        }

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
          pointer-events: none;
        }
      }
    }

    /* Reduced Motion Support */
    @media (prefers-reduced-motion: reduce) {
      .filter-sidebar,
      .filter-backdrop,
      .filter-group,
      .filter-option,
      .filter-actions,
      .clear-filters {
        transition: none !important;
        animation: none !important;
      }

      .filter-group {
        opacity: 1 !important;
        transform: none !important;
      }

      .filter-actions {
        opacity: 1 !important;
        transform: none !important;
      }

      .filter-sidebar {
        @media (max-width: 1024px) {
          &.open {
            transform: translateX(0) !important;
          }
          
          &:not(.open) {
            transform: translateX(-100%) !important;
          }
        }
      }

      .filter-backdrop.open {
        background: rgba(0, 0, 0, 0.5) !important;
      }
    }
  `]
})
export class ProductListComponent implements OnInit, OnDestroy {
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly languageService = inject(LanguageService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();
  private isInitialized = false;
  private lastLanguage: string | null = null;
  private previouslyFocusedElement: HTMLElement | null = null;

  @ViewChild('filterSidebarRef') filterSidebarRef!: ElementRef<HTMLElement>;

  products = signal<ProductBrief[]>([]);
  category = signal<Category | null>(null);
  categoryAncestors = signal<Category[]>([]);
  filterOptions = signal<FilterOptions | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  totalCount = signal(0);
  currentPage = signal(1);
  totalPages = signal(1);
  filterOpen = signal(false);
  viewMode = signal<'grid' | 'list'>('grid');

  selectedBrand = signal<string | null>(null);
  inStockOnly = signal(false);
  onSaleOnly = signal(false);
  searchQuery = signal<string | null>(null);

  constructor() {
    // Refresh products when language changes
    effect(() => {
      const currentLang = this.languageService.currentLanguage();
      // Only reload if initialized and language actually changed
      if (this.isInitialized && this.lastLanguage !== null && this.lastLanguage !== currentLang) {
        this.fetchProducts(this.category()?.id);
      }
      this.lastLanguage = currentLang;
    });
  }

  minPrice: number | null = null;
  maxPrice: number | null = null;
  sortBy = 'newest';

  hasActiveFilters = computed(() =>
    this.selectedBrand() !== null ||
    this.inStockOnly() ||
    this.onSaleOnly() ||
    this.minPrice !== null ||
    this.maxPrice !== null ||
    this.searchQuery() !== null
  );

  categoryInfo = computed((): CategoryInfo | null => {
    const cat = this.category();
    if (!cat) return null;
    return {
      id: cat.id,
      name: cat.name,
      slug: cat.slug,
      description: cat.description,
      icon: cat.icon,
      imageUrl: cat.imageUrl,
      productCount: this.totalCount(),
      parentCategory: cat.parentCategory ? {
        name: cat.parentCategory.name,
        slug: cat.parentCategory.slug
      } : undefined
    };
  });

  visiblePages = computed(() => {
    const current = this.currentPage();
    const total = this.totalPages();
    const pages: number[] = [];

    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const categorySlug = params['categorySlug'];
      this.currentPage.set(1);
      this.loadProducts(categorySlug);
      this.isInitialized = true;
    });

    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['page']) {
        this.currentPage.set(parseInt(params['page'], 10));
      }
      if (params['brand']) {
        this.selectedBrand.set(params['brand']);
      }
      if (params['inStock']) {
        this.inStockOnly.set(params['inStock'] === 'true');
      }
      if (params['onSale']) {
        this.onSaleOnly.set(params['onSale'] === 'true');
      }
      if (params['minPrice']) {
        this.minPrice = parseFloat(params['minPrice']);
      }
      if (params['maxPrice']) {
        this.maxPrice = parseFloat(params['maxPrice']);
      }
      if (params['sort']) {
        this.sortBy = params['sort'];
      }
      // Handle search query parameter
      if (params['search']) {
        const newSearchQuery = params['search'];
        const oldSearchQuery = this.searchQuery();
        this.searchQuery.set(newSearchQuery);
        // If search query changed and we're initialized, reload products
        if (this.isInitialized && newSearchQuery !== oldSearchQuery) {
          this.fetchProducts(this.category()?.id);
        }
      } else if (this.searchQuery()) {
        // Clear search if param is removed
        this.searchQuery.set(null);
        if (this.isInitialized) {
          this.fetchProducts(this.category()?.id);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadProducts(categorySlug?: string): void {
    this.loading.set(true);

    if (categorySlug) {
      this.categoryService.getCategoryBySlug(categorySlug).subscribe({
        next: (category) => {
          this.category.set(category);
          this.loadFilterOptions(categorySlug);
          this.fetchProducts(category.id);
        },
        error: () => {
          this.loading.set(false);
          this.router.navigate(['/products']);
        }
      });
    } else {
      this.category.set(null);
      this.loadFilterOptions();
      this.fetchProducts();
    }
  }

  loadFilterOptions(categorySlug?: string): void {
    this.productService.getFilterOptions(categorySlug).subscribe({
      next: (options) => this.filterOptions.set(options),
      error: () => {
        // Fallback to empty filter options if fetch fails
        this.filterOptions.set({
          brands: [],
          priceRange: { min: 0, max: 10000 },
          specifications: {},
          tags: []
        });
      }
    });
  }

  fetchProducts(categoryId?: string): void {
    const filter: ProductFilter = {
      pageNumber: this.currentPage(),
      pageSize: 12,
      categoryId,
      searchTerm: this.searchQuery() || undefined,
      brand: this.selectedBrand() || undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined,
      inStock: this.inStockOnly() || undefined,
      onSale: this.onSaleOnly() || undefined,
      sortBy: this.sortBy === 'price-desc' ? 'price' : this.sortBy,
      sortDescending: this.sortBy === 'price-desc' || this.sortBy === 'name-desc'
    };

    this.productService.getProducts(filter).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
        this.error.set(null);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('products.error.loadFailed');
      }
    });
  }

  toggleBrand(brand: string): void {
    this.selectedBrand.set(this.selectedBrand() === brand ? null : brand);
    this.applyFilters();
  }

  toggleInStock(): void {
    this.inStockOnly.set(!this.inStockOnly());
    this.applyFilters();
  }

  toggleOnSale(): void {
    this.onSaleOnly.set(!this.onSaleOnly());
    this.applyFilters();
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.updateQueryParams();
    this.fetchProducts(this.category()?.id);
  }

  clearFilters(): void {
    this.selectedBrand.set(null);
    this.inStockOnly.set(false);
    this.onSaleOnly.set(false);
    this.searchQuery.set(null);
    this.minPrice = null;
    this.maxPrice = null;
    this.sortBy = 'newest';
    // Clear search from URL as well
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { search: null },
      queryParamsHandling: 'merge'
    });
    this.applyFilters();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.updateQueryParams();
      this.fetchProducts(this.category()?.id);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  retryLoad(): void {
    this.error.set(null);
    this.loadProducts(this.category()?.slug);
  }

  openFilterSidebar(): void {
    this.previouslyFocusedElement = document.activeElement as HTMLElement;
    this.filterOpen.set(true);
    
    // Focus the close button after view updates
    setTimeout(() => {
      const closeButton = this.filterSidebarRef?.nativeElement?.querySelector('[data-testid="close-filters-btn"]') as HTMLElement;
      closeButton?.focus();
    });
  }

  closeFilterSidebar(): void {
    this.filterOpen.set(false);
    this.previouslyFocusedElement?.focus();
    this.previouslyFocusedElement = null;
  }

  onFilterSidebarKeydown(event: KeyboardEvent): void {
    // Only apply focus trap on mobile when sidebar is open as a modal
    if (!this.filterOpen() || window.innerWidth > 1024) return;

    if (event.key === 'Escape') {
      this.closeFilterSidebar();
      return;
    }

    if (event.key !== 'Tab') return;

    const focusableSelector = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';
    const focusableElements = this.filterSidebarRef?.nativeElement?.querySelectorAll(focusableSelector);
    
    if (!focusableElements || focusableElements.length === 0) return;

    const firstElement = focusableElements[0] as HTMLElement;
    const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

    if (event.shiftKey && document.activeElement === firstElement) {
      event.preventDefault();
      lastElement.focus();
    } else if (!event.shiftKey && document.activeElement === lastElement) {
      event.preventDefault();
      firstElement.focus();
    }
  }

  private updateQueryParams(): void {
    const queryParams: Record<string, string | null> = {
      page: this.currentPage() > 1 ? this.currentPage().toString() : null,
      brand: this.selectedBrand(),
      inStock: this.inStockOnly() ? 'true' : null,
      onSale: this.onSaleOnly() ? 'true' : null,
      minPrice: this.minPrice !== null ? this.minPrice.toString() : null,
      maxPrice: this.maxPrice !== null ? this.maxPrice.toString() : null,
      sort: this.sortBy !== 'newest' ? this.sortBy : null
    };

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge'
    });
  }
}
