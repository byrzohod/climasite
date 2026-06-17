import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import {
  AdminProductsService,
  AdminProductListItem,
  AdminProductDetail,
  CreateProductPayload,
  UpdateProductPayload,
  PRODUCT_STATUSES
} from '../../../core/services/admin-products.service';
import { ProductTranslationEditorComponent } from './components/product-translation-editor/product-translation-editor.component';
import { RelatedProductsManagerComponent } from './components/related-products-manager/related-products-manager.component';
import { apiErrorToTranslationKey } from '../../../core/utils/translation-key.util';

type FormMode = 'create' | 'edit';

interface ProductForm {
  name: string;
  sku: string;
  slug: string;
  shortDescription: string;
  description: string;
  basePrice: number | null;
  compareAtPrice: number | null;
  costPrice: number | null;
  categoryId: string;
  brand: string;
  model: string;
  isActive: boolean;
  isFeatured: boolean;
  requiresInstallation: boolean;
  warrantyMonths: number | null;
  weightKg: number | null;
  metaTitle: string;
  metaDescription: string;
}

function emptyForm(): ProductForm {
  return {
    name: '',
    sku: '',
    slug: '',
    shortDescription: '',
    description: '',
    basePrice: null,
    compareAtPrice: null,
    costPrice: null,
    categoryId: '',
    brand: '',
    model: '',
    isActive: true,
    isFeatured: false,
    requiresInstallation: false,
    warrantyMonths: 12,
    weightKg: null,
    metaTitle: '',
    metaDescription: ''
  };
}

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ProductTranslationEditorComponent,
    RelatedProductsManagerComponent
  ],
  template: `
    <div class="products-container" data-testid="admin-products-page">
      <div class="products-header">
        <div>
          <h1>{{ 'admin.products.title' | translate }}</h1>
          <p class="subtitle">{{ 'admin.products.subtitle' | translate }}</p>
        </div>
        <button
          type="button"
          class="primary-btn"
          (click)="openCreate()"
          data-testid="create-product">
          {{ 'admin.products.create' | translate }}
        </button>
      </div>

      <!-- Filters -->
      <div class="filters">
        <input
          type="search"
          class="search-input"
          [ngModel]="search()"
          (keyup.enter)="applySearch($any($event.target).value)"
          [placeholder]="'admin.products.search' | translate"
          [attr.aria-label]="'admin.products.search' | translate"
          data-testid="product-search" />

        <select
          class="status-filter"
          [ngModel]="statusFilter()"
          (ngModelChange)="applyStatusFilter($event)"
          [attr.aria-label]="'admin.products.statusFilterLabel' | translate"
          data-testid="product-status-filter">
          <option value="">{{ 'admin.products.allStatuses' | translate }}</option>
          @for (status of statuses; track status) {
            <option [value]="status">{{ statusKey(status) | translate }}</option>
          }
        </select>
      </div>

      <!-- Save/delete error banner -->
      @if (actionError()) {
        <div class="error-message" data-testid="product-action-error">
          <span>{{ actionError()! | translate }}</span>
        </div>
      }

      <!-- Loading State -->
      @if (loading()) {
        <div class="loading" data-testid="products-loading">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      <!-- Error State -->
      @if (!loading() && error()) {
        <div class="error-message" data-testid="products-error">
          <span>{{ error()! | translate }}</span>
          <button (click)="loadProducts()" data-testid="products-retry">
            {{ 'common.retry' | translate }}
          </button>
        </div>
      }

      <!-- Empty State -->
      @if (!loading() && !error() && products().length === 0) {
        <div class="empty-state" data-testid="products-empty">
          <p>{{ 'admin.products.empty' | translate }}</p>
        </div>
      }

      <!-- Products Table -->
      @if (!loading() && !error() && products().length > 0) {
        <div class="table-wrapper">
          <table class="products-table">
            <thead>
              <tr>
                <th>{{ 'admin.products.columns.image' | translate }}</th>
                <th>{{ 'admin.products.columns.name' | translate }}</th>
                <th>{{ 'admin.products.columns.sku' | translate }}</th>
                <th>{{ 'admin.products.columns.price' | translate }}</th>
                <th>{{ 'admin.products.columns.stock' | translate }}</th>
                <th>{{ 'admin.products.columns.status' | translate }}</th>
                <th>{{ 'admin.products.columns.category' | translate }}</th>
                <th>{{ 'admin.products.columns.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (product of products(); track product.id) {
                <tr [attr.data-testid]="'product-row'" [attr.data-product-id]="product.id">
                  <td>
                    <img
                      class="thumb"
                      [src]="product.primaryImageUrl || 'assets/images/no-image.svg'"
                      [alt]="product.name"
                      loading="lazy" />
                  </td>
                  <td class="product-name">{{ product.name }}</td>
                  <td class="product-sku">{{ product.sku }}</td>
                  <td>
                    @if (product.salePrice != null) {
                      <span class="sale-price">{{ product.salePrice | currency:'EUR' }}</span>
                      <span class="old-price">{{ product.price | currency:'EUR' }}</span>
                    } @else {
                      {{ product.price | currency:'EUR' }}
                    }
                  </td>
                  <td class="stock">{{ product.stockQuantity }}</td>
                  <td>
                    <span
                      class="status-badge"
                      [class]="product.status.toLowerCase()"
                      data-testid="product-status-badge">
                      {{ statusKey(product.status) | translate }}
                    </span>
                  </td>
                  <td class="category">{{ product.categoryName || '—' }}</td>
                  <td class="actions">
                    <button
                      type="button"
                      class="link-btn"
                      (click)="openEdit(product)"
                      [attr.data-testid]="'edit-product'"
                      [attr.data-product-id]="product.id">
                      {{ 'admin.products.edit' | translate }}
                    </button>
                    <button
                      type="button"
                      class="link-btn"
                      (click)="toggleFeatured(product)"
                      [attr.data-testid]="'feature-product'"
                      [attr.data-product-id]="product.id">
                      {{ 'admin.products.feature' | translate }}
                    </button>
                    @if (isActive(product)) {
                      <button
                        type="button"
                        class="link-btn danger"
                        (click)="deactivate(product)"
                        [attr.data-testid]="'deactivate-product'"
                        [attr.data-product-id]="product.id">
                        {{ 'admin.products.deactivate' | translate }}
                      </button>
                    } @else {
                      <button
                        type="button"
                        class="link-btn"
                        (click)="activate(product)"
                        [attr.data-testid]="'activate-product'"
                        [attr.data-product-id]="product.id">
                        {{ 'admin.products.activate' | translate }}
                      </button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pagination">
          <button
            class="page-btn"
            [disabled]="page() <= 1"
            (click)="prevPage()"
            data-testid="products-prev">
            {{ 'common.previous' | translate }}
          </button>
          <span class="page-indicator" data-testid="products-page-indicator">
            {{ 'admin.products.pageOf' | translate:{ page: page(), total: totalPages() } }}
          </span>
          <button
            class="page-btn"
            [disabled]="page() >= totalPages()"
            (click)="nextPage()"
            data-testid="products-next">
            {{ 'common.next' | translate }}
          </button>
        </div>
      }

      <!-- Create / Edit Form Panel -->
      @if (showForm()) {
        <div class="form-overlay" (click)="cancelForm()">
          <div class="form-panel" (click)="$event.stopPropagation()" data-testid="product-form">
            <div class="form-panel-header">
              <h2>
                {{ (formMode() === 'create' ? 'admin.products.create' : 'admin.products.edit') | translate }}
              </h2>
              <button
                type="button"
                class="close-btn"
                (click)="cancelForm()"
                [attr.aria-label]="'common.close' | translate">
                ✕
              </button>
            </div>

            <form (ngSubmit)="saveProduct()" #productForm="ngForm">
              <div class="form-grid">
                <div class="form-group">
                  <label for="product-name">
                    {{ 'admin.products.form.name' | translate }} <span class="required">*</span>
                  </label>
                  <input
                    type="text"
                    id="product-name"
                    name="name"
                    [(ngModel)]="form.name"
                    maxlength="255"
                    required
                    data-testid="product-name-input" />
                </div>

                <div class="form-group">
                  <label for="product-sku">
                    {{ 'admin.products.form.sku' | translate }} <span class="required">*</span>
                  </label>
                  <input
                    type="text"
                    id="product-sku"
                    name="sku"
                    [(ngModel)]="form.sku"
                    maxlength="50"
                    required
                    [readonly]="formMode() === 'edit'"
                    data-testid="product-sku-input" />
                </div>

                @if (formMode() === 'edit') {
                  <div class="form-group">
                    <label for="product-slug">
                      {{ 'admin.products.form.slug' | translate }} <span class="required">*</span>
                    </label>
                    <input
                      type="text"
                      id="product-slug"
                      name="slug"
                      [(ngModel)]="form.slug"
                      maxlength="255"
                      required
                      data-testid="product-slug-input" />
                  </div>
                }

                <div class="form-group">
                  <label for="product-price">
                    {{ 'admin.products.form.price' | translate }} <span class="required">*</span>
                  </label>
                  <input
                    type="number"
                    id="product-price"
                    name="basePrice"
                    [(ngModel)]="form.basePrice"
                    min="0"
                    step="0.01"
                    required
                    data-testid="product-price-input" />
                </div>

                <div class="form-group">
                  <label for="product-compare">{{ 'admin.products.form.compareAtPrice' | translate }}</label>
                  <input
                    type="number"
                    id="product-compare"
                    name="compareAtPrice"
                    [(ngModel)]="form.compareAtPrice"
                    min="0"
                    step="0.01"
                    data-testid="product-compare-input" />
                </div>

                <div class="form-group">
                  <label for="product-cost">{{ 'admin.products.form.costPrice' | translate }}</label>
                  <input
                    type="number"
                    id="product-cost"
                    name="costPrice"
                    [(ngModel)]="form.costPrice"
                    min="0"
                    step="0.01"
                    data-testid="product-cost-input" />
                </div>

                <div class="form-group">
                  <label for="product-category">{{ 'admin.products.form.category' | translate }}</label>
                  <input
                    type="text"
                    id="product-category"
                    name="categoryId"
                    [(ngModel)]="form.categoryId"
                    [placeholder]="'admin.products.form.categoryPlaceholder' | translate"
                    data-testid="product-category-input" />
                </div>

                <div class="form-group">
                  <label for="product-brand">{{ 'admin.products.form.brand' | translate }}</label>
                  <input
                    type="text"
                    id="product-brand"
                    name="brand"
                    [(ngModel)]="form.brand"
                    data-testid="product-brand-input" />
                </div>

                <div class="form-group">
                  <label for="product-model">{{ 'admin.products.form.model' | translate }}</label>
                  <input
                    type="text"
                    id="product-model"
                    name="model"
                    [(ngModel)]="form.model"
                    data-testid="product-model-input" />
                </div>

                <div class="form-group">
                  <label for="product-warranty">{{ 'admin.products.form.warrantyMonths' | translate }}</label>
                  <input
                    type="number"
                    id="product-warranty"
                    name="warrantyMonths"
                    [(ngModel)]="form.warrantyMonths"
                    min="0"
                    data-testid="product-warranty-input" />
                </div>

                <div class="form-group">
                  <label for="product-weight">{{ 'admin.products.form.weightKg' | translate }}</label>
                  <input
                    type="number"
                    id="product-weight"
                    name="weightKg"
                    [(ngModel)]="form.weightKg"
                    min="0"
                    step="0.01"
                    data-testid="product-weight-input" />
                </div>

                <div class="form-group full">
                  <label for="product-short">{{ 'admin.products.form.shortDescription' | translate }}</label>
                  <textarea
                    id="product-short"
                    name="shortDescription"
                    [(ngModel)]="form.shortDescription"
                    rows="2"
                    maxlength="500"
                    data-testid="product-short-input"></textarea>
                </div>

                <div class="form-group full">
                  <label for="product-desc">{{ 'admin.products.form.description' | translate }}</label>
                  <textarea
                    id="product-desc"
                    name="description"
                    [(ngModel)]="form.description"
                    rows="4"
                    data-testid="product-description-input"></textarea>
                </div>

                <div class="form-group checkbox">
                  <label>
                    <input
                      type="checkbox"
                      name="isActive"
                      [(ngModel)]="form.isActive"
                      data-testid="product-active-input" />
                    {{ 'admin.products.form.isActive' | translate }}
                  </label>
                </div>

                <div class="form-group checkbox">
                  <label>
                    <input
                      type="checkbox"
                      name="isFeatured"
                      [(ngModel)]="form.isFeatured"
                      data-testid="product-featured-input" />
                    {{ 'admin.products.form.isFeatured' | translate }}
                  </label>
                </div>

                <div class="form-group checkbox">
                  <label>
                    <input
                      type="checkbox"
                      name="requiresInstallation"
                      [(ngModel)]="form.requiresInstallation"
                      data-testid="product-installation-input" />
                    {{ 'admin.products.form.requiresInstallation' | translate }}
                  </label>
                </div>
              </div>

              <div class="form-actions">
                <button
                  type="button"
                  class="secondary-btn"
                  (click)="cancelForm()"
                  data-testid="cancel-product">
                  {{ 'admin.products.form.cancel' | translate }}
                </button>
                <button
                  type="submit"
                  class="primary-btn"
                  [disabled]="saving() || productForm.invalid"
                  data-testid="save-product">
                  @if (saving()) {
                    <span class="spinner-small"></span>
                  }
                  {{ 'admin.products.form.save' | translate }}
                </button>
              </div>
            </form>

            <!-- Edit-only child editors -->
            @if (formMode() === 'edit' && editingId()) {
              <div class="child-editors">
                <app-product-translation-editor
                  [productId]="editingId()!" />
                <app-related-products-manager
                  [productId]="editingId()!" />
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .products-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .products-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 2rem;

      h1 {
        font-size: 2rem;
        color: var(--color-text-primary);
        margin: 0 0 0.5rem;
      }

      .subtitle {
        color: var(--color-text-secondary);
        margin: 0;
      }
    }

    .filters {
      display: flex;
      gap: 1rem;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
    }

    .search-input,
    .status-filter {
      padding: 0.625rem 0.875rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      background: var(--color-bg-input);
      color: var(--color-text-primary);
      font-size: 0.9375rem;
    }

    .search-input {
      flex: 1;
      min-width: 220px;
    }

    .status-filter {
      min-width: 180px;
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 3rem;
      color: var(--color-text-secondary);
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid var(--color-border-primary);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    .spinner-small {
      display: inline-block;
      width: 14px;
      height: 14px;
      border: 2px solid var(--color-border-primary);
      border-top-color: var(--color-text-inverse);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin-right: 0.5rem;
      vertical-align: middle;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem;
      background: var(--color-bg-card);
      border-radius: 8px;
      color: var(--color-text-secondary);
    }

    .table-wrapper {
      overflow-x: auto;
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 8px;
    }

    .products-table {
      width: 100%;
      border-collapse: collapse;

      th,
      td {
        padding: 0.875rem 1rem;
        text-align: left;
        border-bottom: 1px solid var(--color-border-primary);
      }

      th {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--color-text-secondary);
        background: var(--color-bg-secondary);
      }

      td {
        color: var(--color-text-primary);
        font-size: 0.9375rem;
      }

      tbody tr:hover {
        background: var(--color-bg-hover);
      }
    }

    .thumb {
      width: 48px;
      height: 48px;
      object-fit: cover;
      border-radius: 4px;
      background: var(--color-bg-secondary);
    }

    .product-name {
      font-weight: 600;
    }

    .product-sku,
    .stock,
    .category {
      color: var(--color-text-secondary);
    }

    .sale-price {
      font-weight: 600;
      color: var(--color-primary);
      margin-right: 0.5rem;
    }

    .old-price {
      text-decoration: line-through;
      color: var(--color-text-tertiary);
      font-size: 0.8125rem;
    }

    .status-badge {
      padding: 0.25rem 0.625rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      background: var(--color-bg-secondary);
      color: var(--color-text-secondary);

      &.active { background: var(--color-success-light); color: var(--color-success-dark); }
      &.inactive { background: var(--color-bg-secondary); color: var(--color-text-secondary); }
      &.outofstock { background: var(--color-error-light); color: var(--color-error-dark); }
      &.lowstock { background: var(--color-warning-light); color: var(--color-warning-dark); }
    }

    .actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .link-btn {
      background: none;
      border: none;
      color: var(--color-primary);
      font-weight: 500;
      cursor: pointer;
      padding: 0;
      font-size: 0.875rem;

      &:hover {
        text-decoration: underline;
      }

      &.danger {
        color: var(--color-error);
      }
    }

    .pagination {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      margin-top: 1.5rem;
    }

    .page-btn,
    .primary-btn,
    .secondary-btn {
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border-primary);
      border-radius: 6px;
      background: var(--color-bg-card);
      color: var(--color-text-primary);
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;

      &:hover:not(:disabled) {
        background: var(--color-bg-hover);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .primary-btn {
      background: var(--color-primary);
      color: var(--color-text-inverse);
      border-color: var(--color-primary);

      &:hover:not(:disabled) {
        background: var(--color-primary-dark);
      }
    }

    .page-indicator {
      color: var(--color-text-secondary);
      font-size: 0.9375rem;
    }

    .error-message {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      padding: 1rem;
      background: var(--color-error-light);
      color: var(--color-error-dark);
      border-radius: 8px;
      margin-bottom: 1rem;

      button {
        padding: 0.5rem 1rem;
        background: var(--color-error);
        color: var(--color-text-inverse);
        border: none;
        border-radius: 4px;
        cursor: pointer;

        &:hover {
          background: var(--color-error-dark);
        }
      }
    }

    .form-overlay {
      position: fixed;
      inset: 0;
      background: var(--color-bg-overlay);
      display: flex;
      align-items: flex-start;
      justify-content: center;
      padding: 2rem 1rem;
      overflow-y: auto;
      z-index: 1000;
    }

    .form-panel {
      background: var(--color-bg-card);
      border: 1px solid var(--color-border-primary);
      border-radius: 12px;
      width: 100%;
      max-width: 760px;
      padding: 1.5rem;
    }

    .form-panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1.5rem;

      h2 {
        margin: 0;
        font-size: 1.5rem;
        color: var(--color-text-primary);
      }
    }

    .close-btn {
      background: none;
      border: none;
      color: var(--color-text-secondary);
      font-size: 1.25rem;
      cursor: pointer;

      &:hover {
        color: var(--color-text-primary);
      }
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1rem;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;

      &.full {
        grid-column: 1 / -1;
      }

      &.checkbox label {
        flex-direction: row;
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-weight: 500;
      }

      label {
        font-size: 0.875rem;
        font-weight: 500;
        color: var(--color-text-primary);

        .required {
          color: var(--color-error);
        }
      }

      input,
      textarea {
        padding: 0.625rem 0.75rem;
        border: 1px solid var(--color-border-primary);
        border-radius: 6px;
        background: var(--color-bg-input);
        color: var(--color-text-primary);
        font-size: 0.9375rem;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
        }
      }

      input[type="checkbox"] {
        width: auto;
      }

      textarea {
        resize: vertical;
      }
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }

    .child-editors {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      margin-top: 2rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--color-border-primary);
    }

    @media (max-width: 768px) {
      .products-container {
        padding: 1rem;
      }

      .filters {
        flex-direction: column;
      }

      .search-input,
      .status-filter {
        width: 100%;
      }

      .form-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AdminProductsComponent implements OnInit {
  private readonly productsService = inject(AdminProductsService);
  private readonly translate = inject(TranslateService);

  protected readonly statuses = PRODUCT_STATUSES;

  products = signal<AdminProductListItem[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  actionError = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(20);
  search = signal('');
  statusFilter = signal('');
  totalPages = signal(1);

  // Form state
  showForm = signal(false);
  formMode = signal<FormMode>('create');
  saving = signal(false);
  editingId = signal<string | null>(null);
  form: ProductForm = emptyForm();

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productsService.getProducts({
      pageNumber: this.page(),
      pageSize: this.pageSize(),
      search: this.search() || undefined,
      status: this.statusFilter() || undefined
    }).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalPages.set(Math.max(result.totalPages, 1));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('admin.products.error');
        this.loading.set(false);
      }
    });
  }

  statusKey(status: string): string {
    const map: Record<string, string> = {
      Active: 'admin.products.status.active',
      Inactive: 'admin.products.status.inactive',
      OutOfStock: 'admin.products.status.outOfStock',
      LowStock: 'admin.products.status.lowStock'
    };
    return map[status] ?? status;
  }

  isActive(product: AdminProductListItem): boolean {
    return product.status !== 'Inactive';
  }

  applySearch(value: string): void {
    this.search.set(value.trim());
    this.page.set(1);
    this.loadProducts();
  }

  applyStatusFilter(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.loadProducts();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.loadProducts();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update(p => p + 1);
      this.loadProducts();
    }
  }

  // --- Form handling ---

  openCreate(): void {
    this.actionError.set(null);
    this.form = emptyForm();
    this.formMode.set('create');
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(product: AdminProductListItem): void {
    this.actionError.set(null);
    this.formMode.set('edit');
    this.editingId.set(product.id);
    this.showForm.set(true);
    // Seed the form from the list row, then hydrate from detail.
    this.form = {
      ...emptyForm(),
      name: product.name,
      sku: product.sku,
      slug: product.slug,
      basePrice: product.price
    };

    this.productsService.getProduct(product.id).subscribe({
      next: (detail) => this.hydrateForm(detail),
      error: () => {
        this.actionError.set('admin.products.errors.loadDetailFailed');
      }
    });
  }

  private hydrateForm(detail: AdminProductDetail): void {
    // Keep the already-seeded/edited identity fields (name, sku, slug, basePrice — the row carries
    // the same values as the detail) so a fast edit made before this async detail arrives is NOT
    // clobbered. Only fill the fields the list row could not provide.
    this.form = {
      ...this.form,
      shortDescription: detail.shortDescription ?? '',
      description: detail.description ?? '',
      compareAtPrice: detail.compareAtPrice ?? null,
      costPrice: detail.costPrice ?? null,
      categoryId: detail.categoryId ?? '',
      brand: detail.brand ?? '',
      model: detail.model ?? '',
      isActive: detail.isActive,
      isFeatured: detail.isFeatured,
      requiresInstallation: detail.requiresInstallation,
      warrantyMonths: detail.warrantyMonths,
      weightKg: detail.weightKg ?? null,
      metaTitle: detail.metaTitle ?? '',
      metaDescription: detail.metaDescription ?? ''
    };
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.saving.set(false);
    this.editingId.set(null);
  }

  saveProduct(): void {
    if (!this.form.name.trim() || !this.form.sku.trim() || this.form.basePrice == null) {
      return;
    }

    this.saving.set(true);
    this.actionError.set(null);

    if (this.formMode() === 'create') {
      this.productsService.createProduct(this.buildCreatePayload()).subscribe({
        next: () => this.onSaved(),
        error: (err) => this.onSaveError(err)
      });
    } else {
      const id = this.editingId();
      if (!id) {
        this.saving.set(false);
        return;
      }
      this.productsService.updateProduct(id, this.buildUpdatePayload(id)).subscribe({
        next: () => this.onSaved(),
        error: (err) => this.onSaveError(err)
      });
    }
  }

  private buildCreatePayload(): CreateProductPayload {
    return {
      name: this.form.name.trim(),
      sku: this.form.sku.trim(),
      shortDescription: this.form.shortDescription.trim() || undefined,
      description: this.form.description.trim() || undefined,
      basePrice: this.form.basePrice ?? 0,
      compareAtPrice: this.form.compareAtPrice ?? undefined,
      costPrice: this.form.costPrice ?? undefined,
      categoryId: this.form.categoryId.trim() || undefined,
      brand: this.form.brand.trim() || undefined,
      model: this.form.model.trim() || undefined,
      isActive: this.form.isActive,
      isFeatured: this.form.isFeatured,
      requiresInstallation: this.form.requiresInstallation,
      warrantyMonths: this.form.warrantyMonths ?? 12,
      weightKg: this.form.weightKg ?? undefined,
      metaTitle: this.form.metaTitle.trim() || undefined,
      metaDescription: this.form.metaDescription.trim() || undefined
    };
  }

  private buildUpdatePayload(id: string): UpdateProductPayload {
    // UpdateProductCommand has no Sku field — drop sku from the update payload.
    // (SKU stays read-only in edit mode and is not editable post-create.)
    const { sku: _sku, ...rest } = this.buildCreatePayload();
    return {
      ...rest,
      id,
      slug: this.form.slug.trim()
    } as UpdateProductPayload;
  }

  private onSaved(): void {
    this.saving.set(false);
    this.showForm.set(false);
    this.editingId.set(null);
    this.loadProducts();
  }

  private onSaveError(err: unknown): void {
    this.saving.set(false);
    this.actionError.set(apiErrorToTranslationKey(err, 'admin.products.errors.saveFailed'));
  }

  // --- Row actions ---

  deactivate(product: AdminProductListItem): void {
    const message = this.translate.instant('admin.products.confirmDeactivate', { name: product.name });
    if (!confirm(message)) {
      return;
    }
    this.setStatus(product.id, false);
  }

  activate(product: AdminProductListItem): void {
    this.setStatus(product.id, true);
  }

  private setStatus(id: string, isActive: boolean): void {
    this.actionError.set(null);
    this.productsService.toggleStatus(id, isActive).subscribe({
      next: () => this.loadProducts(),
      error: (err) => {
        this.actionError.set(apiErrorToTranslationKey(err, 'admin.products.errors.statusFailed'));
      }
    });
  }

  toggleFeatured(product: AdminProductListItem): void {
    this.actionError.set(null);
    this.productsService.toggleFeatured(product.id, true).subscribe({
      next: () => this.loadProducts(),
      error: (err) => {
        this.actionError.set(apiErrorToTranslationKey(err, 'admin.products.errors.featuredFailed'));
      }
    });
  }
}
