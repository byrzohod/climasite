import { Component, input, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import {
  AdminRelatedProductsService,
  ProductRelationsDto,
  RelatedProductDto,
  RelationGroupDto
} from '../../services/admin-related-products.service';

@Component({
  selector: 'app-related-products-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="related-products-manager">
      <h4>{{ 'admin.products.relatedProducts' | translate }}</h4>

      @if (loading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      @if (!loading() && relations()) {
        <!-- Tabs for Relation Types -->
        <div class="relation-tabs">
          @for (type of relationTypes; track type) {
            <button
              type="button"
              [class.active]="activeType() === type"
              (click)="setActiveType(type)"
            >
              {{ 'admin.products.relations.' + type | translate }}
              <span class="count">({{ getCount(type) }})</span>
            </button>
          }
        </div>

        <!-- Current Relations -->
        <div class="current-relations">
          @if (currentRelations().length === 0) {
            <div class="empty-state">
              {{ 'admin.products.noRelations' | translate }}
            </div>
          } @else {
            @for (relation of currentRelations(); track relation.id; let i = $index) {
              <div class="relation-item" [attr.data-index]="i">
                <span class="drag-handle" title="Drag to reorder">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                    <circle cx="9" cy="6" r="2"/>
                    <circle cx="15" cy="6" r="2"/>
                    <circle cx="9" cy="12" r="2"/>
                    <circle cx="15" cy="12" r="2"/>
                    <circle cx="9" cy="18" r="2"/>
                    <circle cx="15" cy="18" r="2"/>
                  </svg>
                </span>
                <img
                  [src]="relation.primaryImageUrl || 'assets/images/no-image.svg'"
                  [alt]="relation.name"
                  class="product-image"
                />
                <div class="product-info">
                  <span class="name">{{ relation.name }}</span>
                  <span class="sku">{{ relation.sku }}</span>
                </div>
                <span class="price">{{ relation.price | number:'1.2-2' }} EUR</span>
                <div class="order-controls">
                  <button
                    type="button"
                    class="order-btn"
                    [disabled]="i === 0"
                    (click)="moveUp(i)"
                    title="Move up"
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M12 4l-8 8h6v8h4v-8h6z"/>
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="order-btn"
                    [disabled]="i === currentRelations().length - 1"
                    (click)="moveDown(i)"
                    title="Move down"
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                      <path d="M12 20l8-8h-6V4h-4v8H4z"/>
                    </svg>
                  </button>
                </div>
                <button
                  type="button"
                  class="remove-btn"
                  (click)="removeRelation(relation.id)"
                  title="Remove relation"
                >
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                  </svg>
                </button>
              </div>
            }
          }
        </div>

        <!-- Add New Relation -->
        <div class="add-relation">
          <h5>{{ 'admin.products.addRelation' | translate }}</h5>
          <div class="search-input">
            <input
              type="text"
              [(ngModel)]="searchTerm"
              [placeholder]="'admin.products.searchProducts' | translate"
              (keyup)="searchProducts()"
            />
          </div>
          @if (searchResults().length > 0) {
            <div class="search-results">
              @for (product of searchResults(); track product.id) {
                <div class="search-result-item" (click)="addRelation(product.id)">
                  <img
                    [src]="product.primaryImageUrl || 'assets/images/no-image.svg'"
                    [alt]="product.name"
                    class="product-image"
                  />
                  <div class="product-info">
                    <span class="name">{{ product.name }}</span>
                    <span class="sku">{{ product.sku }}</span>
                  </div>
                  <button type="button" class="add-btn">+</button>
                </div>
              }
            </div>
          }
        </div>
      }

      @if (error()) {
        <div class="error-state">{{ error() }}</div>
      }
    </div>
  `,
  styles: [`
    .related-products-manager {
      background: var(--surface-color);
      border-radius: 8px;
      padding: 1.5rem;
    }

    h4 {
      margin: 0 0 1.5rem 0;
      font-size: 1.25rem;
      color: var(--text-primary);
    }

    h5 {
      margin: 1.5rem 0 1rem 0;
      font-size: 1rem;
      color: var(--text-secondary);
    }

    .loading-state {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 2rem;
      color: var(--text-secondary);
    }

    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid var(--border-color);
      border-top-color: var(--primary-color);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .relation-tabs {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
      border-bottom: 1px solid var(--border-color);
      padding-bottom: 1rem;
    }

    .relation-tabs button {
      padding: 0.5rem 1rem;
      border: none;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
      font-size: 0.875rem;
      border-radius: 4px;
      transition: all 0.2s;
    }

    .relation-tabs button:hover {
      background: var(--hover-bg);
    }

    .relation-tabs button.active {
      background: var(--primary-color);
      color: white;
    }

    .relation-tabs button .count {
      font-size: 0.75rem;
      opacity: 0.7;
    }

    .current-relations {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .empty-state {
      text-align: center;
      padding: 2rem;
      color: var(--text-secondary);
      font-style: italic;
    }

    .relation-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem;
      background: var(--bg-color);
      border-radius: 6px;
      border: 1px solid var(--border-color);
    }

    .drag-handle {
      cursor: grab;
      color: var(--text-secondary);
      padding: 0.25rem;
    }

    .drag-handle:active {
      cursor: grabbing;
    }

    .product-image {
      width: 48px;
      height: 48px;
      object-fit: cover;
      border-radius: 4px;
      background: var(--surface-color);
    }

    .product-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .product-info .name {
      font-weight: 500;
      color: var(--text-primary);
    }

    .product-info .sku {
      font-size: 0.75rem;
      color: var(--text-secondary);
    }

    .price {
      font-weight: 600;
      color: var(--primary-color);
    }

    .order-controls {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .order-btn {
      padding: 4px;
      border: none;
      background: var(--surface-color);
      border-radius: 4px;
      cursor: pointer;
      color: var(--text-secondary);
      transition: all 0.2s;
    }

    .order-btn:hover:not(:disabled) {
      background: var(--primary-color);
      color: white;
    }

    .order-btn:disabled {
      opacity: 0.3;
      cursor: not-allowed;
    }

    .remove-btn {
      padding: 0.5rem;
      border: none;
      background: transparent;
      color: var(--error-color, #ef4444);
      cursor: pointer;
      border-radius: 4px;
      transition: background 0.2s;
    }

    .remove-btn:hover {
      background: var(--error-color, #ef4444);
      color: white;
    }

    .add-relation {
      margin-top: 1.5rem;
      border-top: 1px solid var(--border-color);
      padding-top: 1rem;
    }

    .search-input input {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 1px solid var(--border-color);
      border-radius: 6px;
      font-size: 0.875rem;
      background: var(--bg-color);
      color: var(--text-primary);
    }

    .search-input input:focus {
      outline: none;
      border-color: var(--primary-color);
    }

    .search-results {
      margin-top: 0.5rem;
      max-height: 300px;
      overflow-y: auto;
      border: 1px solid var(--border-color);
      border-radius: 6px;
    }

    .search-result-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.75rem;
      cursor: pointer;
      transition: background 0.2s;
    }

    .search-result-item:hover {
      background: var(--hover-bg);
    }

    .search-result-item:not(:last-child) {
      border-bottom: 1px solid var(--border-color);
    }

    .add-btn {
      padding: 0.5rem 1rem;
      border: none;
      background: var(--primary-color);
      color: white;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }

    .error-state {
      color: var(--error-color, #ef4444);
      padding: 1rem;
      background: rgba(239, 68, 68, 0.1);
      border-radius: 6px;
      margin-top: 1rem;
    }
  `]
})
export class RelatedProductsManagerComponent {
  productId = input.required<string>();

  relations = signal<ProductRelationsDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  activeType = signal<string>('Similar');
  searchTerm = '';
  searchResults = signal<{ id: string; name: string; sku: string; primaryImageUrl?: string }[]>([]);

  readonly relationTypes = ['Similar', 'Accessory', 'Upgrade', 'Bundle', 'FrequentlyBoughtTogether'];

  constructor(private relatedProductsService: AdminRelatedProductsService) {
    effect(() => {
      const id = this.productId();
      if (id) {
        this.loadRelations(id);
      }
    });
  }

  currentRelations = computed<RelatedProductDto[]>(() => {
    const rel = this.relations();
    if (!rel) return [];

    const group = rel.relationGroups.find(g => g.relationType === this.activeType());
    return group?.relations || [];
  });

  private loadRelations(productId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.relatedProductsService.getProductRelations(productId).subscribe({
      next: (data) => {
        this.relations.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to load relations');
        this.loading.set(false);
      }
    });
  }

  setActiveType(type: string): void {
    this.activeType.set(type);
  }

  getCount(type: string): number {
    const rel = this.relations();
    if (!rel) return 0;

    const group = rel.relationGroups.find(g => g.relationType === type);
    return group?.relations.length || 0;
  }

  addRelation(relatedProductId: string): void {
    const productId = this.productId();
    const relationType = this.activeType();

    this.relatedProductsService.addRelation(productId, {
      relatedProductId,
      relationType
    }).subscribe({
      next: () => {
        this.loadRelations(productId);
        this.searchTerm = '';
        this.searchResults.set([]);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to add relation');
      }
    });
  }

  removeRelation(relationId: string): void {
    const productId = this.productId();

    this.relatedProductsService.removeRelation(productId, relationId).subscribe({
      next: () => {
        this.loadRelations(productId);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to remove relation');
      }
    });
  }

  moveUp(index: number): void {
    if (index === 0) return;
    this.reorderRelations(index, index - 1);
  }

  moveDown(index: number): void {
    const relations = this.currentRelations();
    if (index === relations.length - 1) return;
    this.reorderRelations(index, index + 1);
  }

  private reorderRelations(fromIndex: number, toIndex: number): void {
    const relations = [...this.currentRelations()];
    const [moved] = relations.splice(fromIndex, 1);
    relations.splice(toIndex, 0, moved);

    const productId = this.productId();
    const relationType = this.activeType();
    const relationIds = relations.map(r => r.id);

    this.relatedProductsService.reorderRelations(productId, {
      relationType,
      relationIds
    }).subscribe({
      next: () => {
        this.loadRelations(productId);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to reorder relations');
      }
    });
  }

  searchProducts(): void {
    // This would typically call an API to search for products
    // For now, we'll leave it as a placeholder that can be implemented
    // when the product search endpoint is available
    if (this.searchTerm.length < 2) {
      this.searchResults.set([]);
      return;
    }

    // TODO: Implement actual product search
    // For now, clear results to indicate search is not fully implemented
    this.searchResults.set([]);
  }
}
