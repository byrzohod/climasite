import { Component, input, output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

export interface ProductVariantOption {
  id: string;
  name: string;
  slug: string;
  value: string; // e.g., "9000", "12000", "18000" for BTU
  unit?: string; // e.g., "BTU", "kW"
  price: number;
  salePrice?: number;
  inStock: boolean;
  isSelected?: boolean;
}

export interface VariantGroup {
  name: string; // e.g., "Capacity", "Color", "Size"
  type: 'buttons' | 'dropdown' | 'swatches';
  options: ProductVariantOption[];
}

@Component({
  selector: 'app-product-variants',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <div class="product-variants" data-testid="product-variants">
      @for (group of variantGroups(); track group.name) {
        <div class="variant-group">
          <label class="group-label">{{ group.name }}</label>

          @switch (group.type) {
            @case ('buttons') {
              <div class="variant-buttons">
                @for (option of group.options; track option.id) {
                  <a
                    class="variant-btn"
                    [class.selected]="option.isSelected"
                    [class.out-of-stock]="!option.inStock"
                    [routerLink]="['/products', option.slug]"
                    [attr.data-testid]="'variant-' + option.id"
                    (click)="onVariantSelect(option, $event)">
                    <span class="variant-value">{{ option.value }}{{ option.unit ? ' ' + option.unit : '' }}</span>
                    @if (showPrices()) {
                      <span class="variant-price">
                        @if (option.salePrice && option.salePrice < option.price) {
                          <span class="sale">{{ option.salePrice | currency:'EUR' }}</span>
                        } @else {
                          {{ option.price | currency:'EUR' }}
                        }
                      </span>
                    }
                    @if (!option.inStock) {
                      <span class="stock-badge">{{ 'products.outOfStock' | translate }}</span>
                    }
                  </a>
                }
              </div>
            }

            @case ('dropdown') {
              <select
                class="variant-select"
                (change)="onDropdownChange($event, group)"
                data-testid="variant-dropdown">
                @for (option of group.options; track option.id) {
                  <option
                    [value]="option.id"
                    [selected]="option.isSelected"
                    [disabled]="!option.inStock">
                    {{ option.name }} - {{ option.price | currency:'EUR' }}
                    {{ !option.inStock ? '(' + ('products.outOfStock' | translate) + ')' : '' }}
                  </option>
                }
              </select>
            }

            @case ('swatches') {
              <div class="variant-swatches">
                @for (option of group.options; track option.id) {
                  <button
                    class="swatch"
                    [class.selected]="option.isSelected"
                    [class.out-of-stock]="!option.inStock"
                    [style.background-color]="option.value"
                    [title]="option.name"
                    (click)="onVariantSelect(option, $event)"
                    [attr.data-testid]="'swatch-' + option.id">
                    @if (option.isSelected) {
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
                        <polyline points="20 6 9 17 4 12"/>
                      </svg>
                    }
                  </button>
                }
              </div>
            }
          }
        </div>
      }

      <!-- Price comparison hint -->
      @if (showPriceComparison() && priceRange()) {
        <div class="price-comparison">
          <span class="range-label">{{ 'products.variants.priceRange' | translate }}:</span>
          <span class="range-value">
            {{ priceRange()!.min | currency:'EUR' }} - {{ priceRange()!.max | currency:'EUR' }}
          </span>
        </div>
      }
    </div>
  `,
  styles: [`
    .product-variants {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .variant-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .group-label {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-secondary);
    }

    /* Button style variants */
    .variant-buttons {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .variant-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 0.75rem 1rem;
      min-width: 80px;
      border: 2px solid var(--color-border);
      border-radius: 8px;
      background: var(--color-bg-primary);
      cursor: pointer;
      text-decoration: none;
      color: var(--color-text-primary);
      transition: all 0.2s;
      position: relative;

      &:hover:not(.out-of-stock) {
        border-color: var(--color-primary-light);
      }

      &.selected {
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.05);

        .variant-value {
          color: var(--color-primary);
          font-weight: 600;
        }
      }

      &.out-of-stock {
        opacity: 0.5;
        cursor: not-allowed;

        &::after {
          content: '';
          position: absolute;
          top: 50%;
          left: 0;
          right: 0;
          height: 2px;
          background: var(--color-text-secondary);
          transform: rotate(-10deg);
        }
      }
    }

    .variant-value {
      font-size: 1rem;
      font-weight: 500;
      line-height: 1.2;
    }

    .variant-price {
      font-size: 0.75rem;
      color: var(--color-text-secondary);
      margin-top: 0.25rem;

      .sale {
        color: var(--color-danger);
        font-weight: 600;
      }
    }

    .stock-badge {
      position: absolute;
      top: -8px;
      right: -8px;
      font-size: 0.625rem;
      padding: 0.125rem 0.375rem;
      background: var(--color-danger);
      color: white;
      border-radius: 4px;
      white-space: nowrap;
    }

    /* Dropdown style */
    .variant-select {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 2px solid var(--color-border);
      border-radius: 8px;
      background: var(--color-bg-primary);
      color: var(--color-text-primary);
      font-size: 1rem;
      cursor: pointer;

      &:focus {
        outline: none;
        border-color: var(--color-primary);
      }
    }

    /* Swatch style (for colors) */
    .variant-swatches {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .swatch {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      border: 3px solid transparent;
      cursor: pointer;
      position: relative;
      transition: all 0.2s;
      display: flex;
      align-items: center;
      justify-content: center;

      &:hover {
        transform: scale(1.1);
      }

      &.selected {
        border-color: var(--color-primary);
        box-shadow: 0 0 0 2px var(--color-bg-primary), 0 0 0 4px var(--color-primary);
      }

      &.out-of-stock {
        opacity: 0.4;
        cursor: not-allowed;

        &::after {
          content: '';
          position: absolute;
          top: 50%;
          left: 0;
          right: 0;
          height: 2px;
          background: var(--color-text-secondary);
          transform: rotate(-45deg);
        }
      }

      svg {
        width: 20px;
        height: 20px;
        color: white;
        filter: drop-shadow(0 1px 2px rgba(0,0,0,0.3));
      }
    }

    /* Price comparison */
    .price-comparison {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem;
      background: var(--color-bg-secondary);
      border-radius: 6px;
      font-size: 0.875rem;

      .range-label {
        color: var(--color-text-secondary);
      }

      .range-value {
        font-weight: 600;
        color: var(--color-text-primary);
      }
    }

    @media (max-width: 768px) {
      .variant-btn {
        min-width: 70px;
        padding: 0.5rem 0.75rem;
      }

      .variant-value {
        font-size: 0.875rem;
      }

      .swatch {
        width: 36px;
        height: 36px;
      }
    }
  `]
})
export class ProductVariantsComponent {
  variantGroups = input.required<VariantGroup[]>();
  showPrices = input<boolean>(true);
  showPriceComparison = input<boolean>(true);
  navigateOnSelect = input<boolean>(true);

  variantSelected = output<ProductVariantOption>();

  priceRange = computed(() => {
    const groups = this.variantGroups();
    if (!groups.length) return null;

    const allOptions = groups.flatMap(g => g.options);
    if (!allOptions.length) return null;

    const prices = allOptions.map(o => o.salePrice && o.salePrice < o.price ? o.salePrice : o.price);
    return {
      min: Math.min(...prices),
      max: Math.max(...prices)
    };
  });

  onVariantSelect(option: ProductVariantOption, event: Event): void {
    if (!option.inStock) {
      event.preventDefault();
      return;
    }

    if (!this.navigateOnSelect()) {
      event.preventDefault();
    }

    this.variantSelected.emit(option);
  }

  onDropdownChange(event: Event, group: VariantGroup): void {
    const select = event.target as HTMLSelectElement;
    const option = group.options.find(o => o.id === select.value);
    if (option && option.inStock) {
      this.variantSelected.emit(option);
    }
  }
}
