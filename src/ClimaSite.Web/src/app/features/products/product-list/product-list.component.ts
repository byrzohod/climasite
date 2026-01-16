import { Component, inject, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
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
import { LoadingComponent } from '../../../shared/components/loading/loading.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    TranslateModule,
    ProductCardComponent,
    LoadingComponent
  ],
  template: `
    <div class="product-list-page">
      <!-- NAV-001 FIX: Updated breadcrumb to use route-based category navigation -->
      <div class="breadcrumb" data-testid="breadcrumb">
        <a routerLink="/">{{ 'nav.home' | translate }}</a>
        @if (category()) {
          <span class="separator">/</span>
          @for (ancestor of categoryAncestors(); track ancestor.id) {
            <a [routerLink]="['/products/category', ancestor.slug]">{{ ancestor.name }}</a>
            <span class="separator">/</span>
          }
          <span class="current">{{ category()?.name }}</span>
        } @else {
          <span class="separator">/</span>
          <span class="current">{{ 'products.all' | translate }}</span>
        }
      </div>

      <div class="page-header">
        @if (searchQuery()) {
          <h1 data-testid="search-results-title">{{ 'products.searchResults' | translate }}: "{{ searchQuery() }}"</h1>
        } @else {
          <h1>{{ category()?.name || ('products.all' | translate) }}</h1>
          @if (category()?.description) {
            <p class="category-description">{{ category()?.description }}</p>
          }
        }
        <p class="result-count">{{ totalCount() }} {{ 'products.title' | translate | lowercase }}</p>
      </div>

      <div class="product-list-layout">
        <aside class="filter-sidebar" [class.open]="filterOpen()">
          <div class="filter-header">
            <h2>{{ 'products.filters.title' | translate }}</h2>
            <button class="close-filters" (click)="filterOpen.set(false)">
              <span>&times;</span>
            </button>
          </div>

          @if (filterOptions()) {
            <div class="filter-section">
              <h3>{{ 'products.filters.priceRange' | translate }}</h3>
              <div class="price-inputs">
                <input
                  type="number"
                  placeholder="Min"
                  [(ngModel)]="minPrice"
                  (change)="applyFilters()"
                />
                <span>-</span>
                <input
                  type="number"
                  placeholder="Max"
                  [(ngModel)]="maxPrice"
                  (change)="applyFilters()"
                />
              </div>
            </div>

            @if (filterOptions()!.brands.length > 0) {
              <div class="filter-section">
                <h3>{{ 'products.filters.brand' | translate }}</h3>
                <div class="filter-options">
                  @for (brand of filterOptions()!.brands; track brand.name) {
                    <label class="filter-option">
                      <input
                        type="checkbox"
                        [checked]="selectedBrand() === brand.name"
                        (change)="toggleBrand(brand.name)"
                      />
                      <span class="brand-name">{{ brand.name }}</span>
                      <span class="count">({{ brand.count }})</span>
                    </label>
                  }
                </div>
              </div>
            }

            <div class="filter-section">
              <h3>{{ 'products.details.inStock' | translate }}</h3>
              <label class="filter-option">
                <input
                  type="checkbox"
                  [checked]="inStockOnly()"
                  (change)="toggleInStock()"
                />
                <span>{{ 'products.filters.inStock' | translate }}</span>
              </label>
              <label class="filter-option">
                <input
                  type="checkbox"
                  [checked]="onSaleOnly()"
                  (change)="toggleOnSale()"
                />
                <span>{{ 'products.onSale' | translate }}</span>
              </label>
            </div>

            @if (hasActiveFilters()) {
              <button class="clear-filters" (click)="clearFilters()">
                {{ 'products.filters.clearAll' | translate }}
              </button>
            }
          }
        </aside>

        <main class="product-grid-container">
          <div class="toolbar">
            <button class="filter-toggle" (click)="filterOpen.set(true)">
              <span class="filter-icon">&#9776;</span>
              {{ 'products.filters.title' | translate }}
            </button>

            <div class="view-options">
              <button
                class="view-btn"
                [class.active]="viewMode() === 'grid'"
                (click)="viewMode.set('grid')"
                title="Grid View"
              >
                <span class="grid-icon">&#9638;</span>
              </button>
              <button
                class="view-btn"
                [class.active]="viewMode() === 'list'"
                (click)="viewMode.set('list')"
                title="List View"
              >
                <span class="list-icon">&#9776;</span>
              </button>
            </div>

            <div class="sort-options">
              <label>{{ 'products.sort.title' | translate }}:</label>
              <select [(ngModel)]="sortBy" (change)="applyFilters()">
                <option value="newest">{{ 'products.sort.newest' | translate }}</option>
                <option value="price">{{ 'products.sort.priceAsc' | translate }}</option>
                <option value="price-desc">{{ 'products.sort.priceDesc' | translate }}</option>
                <option value="name">{{ 'products.sort.nameAsc' | translate }}</option>
                <option value="name-desc">{{ 'products.sort.nameDesc' | translate }}</option>
              </select>
            </div>
          </div>

          @if (loading()) {
            <app-loading />
          } @else if (products().length === 0) {
            <div class="empty-state">
              <h2>{{ 'products.noProducts' | translate }}</h2>
              <p>{{ 'products.filters.clearAll' | translate }}</p>
              @if (hasActiveFilters()) {
                <button class="btn-primary" (click)="clearFilters()">
                  {{ 'products.filters.clearAll' | translate }}
                </button>
              }
            </div>
          } @else {
            <div class="product-grid" [class.list-view]="viewMode() === 'list'">
              @for (product of products(); track product.id) {
                <app-product-card [product]="product" [listView]="viewMode() === 'list'" />
              }
            </div>

            @if (totalPages() > 1) {
              <div class="pagination">
                <button
                  class="page-btn"
                  [disabled]="currentPage() === 1"
                  (click)="goToPage(currentPage() - 1)"
                >
                  {{ 'common.previous' | translate }}
                </button>

                @for (page of visiblePages(); track page) {
                  <button
                    class="page-btn"
                    [class.active]="page === currentPage()"
                    (click)="goToPage(page)"
                  >
                    {{ page }}
                  </button>
                }

                <button
                  class="page-btn"
                  [disabled]="currentPage() === totalPages()"
                  (click)="goToPage(currentPage() + 1)"
                >
                  {{ 'common.next' | translate }}
                </button>
              </div>
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
      background: var(--color-bg-primary);
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);

      @media (max-width: 1024px) {
        position: fixed;
        top: 0;
        left: 0;
        width: 300px;
        height: 100vh;
        z-index: 1000;
        border-radius: 0;
        transform: translateX(-100%);
        transition: transform 0.3s ease;

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

          @media (max-width: 1024px) {
            display: block;
          }
        }
      }
    }

    .filter-section {
      margin-bottom: 1.5rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid var(--color-border);

      &:last-child {
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
    }

    .price-inputs {
      display: flex;
      align-items: center;
      gap: 0.5rem;

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

      input[type="checkbox"] {
        width: 16px;
        height: 16px;
        accent-color: var(--color-primary);
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
      transition: all 0.2s ease;

      &:hover {
        background: var(--color-error-bg);
        color: var(--color-error);
        border-color: var(--color-error);
      }
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
      padding: 1rem;
      background: var(--color-bg-primary);
      border-radius: 12px;
      margin-bottom: 1.5rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);

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

        @media (max-width: 1024px) {
          display: flex;
        }
      }
    }

    .view-options {
      display: flex;
      gap: 0.5rem;

      .view-btn {
        padding: 0.5rem;
        background: var(--color-bg-secondary);
        border: 1px solid var(--color-border);
        border-radius: 6px;
        cursor: pointer;
        color: var(--color-text-secondary);
        transition: all 0.2s ease;

        &:hover, &.active {
          background: var(--color-primary);
          color: white;
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
    }

    .empty-state {
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
        color: white;
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

    .pagination {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
      margin-top: 2rem;

      .page-btn {
        min-width: 40px;
        padding: 0.5rem 1rem;
        background: var(--color-bg-primary);
        border: 1px solid var(--color-border);
        border-radius: 6px;
        cursor: pointer;
        color: var(--color-text-primary);
        transition: all 0.2s ease;

        &:hover:not(:disabled) {
          background: var(--color-bg-secondary);
        }

        &.active {
          background: var(--color-primary);
          color: white;
          border-color: var(--color-primary);
        }

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
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

  products = signal<ProductBrief[]>([]);
  category = signal<Category | null>(null);
  categoryAncestors = signal<Category[]>([]);
  filterOptions = signal<FilterOptions | null>(null);
  loading = signal(true);
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
      next: (options) => this.filterOptions.set(options)
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
      },
      error: () => {
        this.loading.set(false);
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
