import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

export interface DeliveryOption {
  type: 'standard' | 'express' | 'pickup';
  name: string;
  price: number;
  estimatedDays: { min: number; max: number };
}

@Component({
  selector: 'app-stock-delivery',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="stock-delivery" data-testid="stock-delivery">
      <!-- Stock Status -->
      <div class="stock-status" [class]="stockStatusClass()">
        <div class="status-icon">
          @if (inStock()) {
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
              <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
          } @else {
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="15" y1="9" x2="9" y2="15"/>
              <line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
          }
        </div>
        <div class="status-text">
          @if (inStock()) {
            @if (lowStock()) {
              <span class="label low-stock">{{ 'products.stock.lowStock' | translate: { count: stockQuantity() } }}</span>
            } @else {
              <span class="label in-stock">{{ 'products.stock.inStock' | translate }}</span>
            }
            <span class="availability">{{ 'delivery.readyToShip' | translate }}</span>
          } @else {
            <span class="label out-of-stock">{{ 'products.stock.outOfStock' | translate }}</span>
            @if (restockDate()) {
              <span class="availability">{{ 'delivery.expectedRestock' | translate: { date: restockDate() } }}</span>
            }
          }
        </div>
      </div>

      <!-- Delivery Options -->
      @if (inStock() && deliveryOptions().length > 0) {
        <div class="delivery-options">
          <h4 class="delivery-title">{{ 'delivery.options' | translate }}</h4>
          @for (option of deliveryOptions(); track option.type) {
            <div class="delivery-option" [class.selected]="selectedDelivery() === option.type">
              <div class="option-icon">
                @switch (option.type) {
                  @case ('express') {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>
                    </svg>
                  }
                  @case ('pickup') {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
                      <polyline points="9 22 9 12 15 12 15 22"/>
                    </svg>
                  }
                  @default {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <rect x="1" y="3" width="15" height="13"/>
                      <polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/>
                      <circle cx="5.5" cy="18.5" r="2.5"/>
                      <circle cx="18.5" cy="18.5" r="2.5"/>
                    </svg>
                  }
                }
              </div>
              <div class="option-details">
                <span class="option-name">{{ option.name }}</span>
                <span class="option-estimate">{{ getDeliveryEstimate(option) }}</span>
              </div>
              <div class="option-price">
                @if (option.price === 0) {
                  <span class="free">{{ 'delivery.free' | translate }}</span>
                } @else {
                  <span>{{ option.price | currency:'EUR' }}</span>
                }
              </div>
            </div>
          }
        </div>
      }

      <!-- Delivery Date Estimate -->
      @if (inStock() && estimatedDeliveryDate()) {
        <div class="delivery-estimate">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="6"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
          <span>{{ 'delivery.estimatedArrival' | translate }}: <strong>{{ estimatedDeliveryDate() }}</strong></span>
        </div>
      }
    </div>
  `,
  styles: [`
    .stock-delivery {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      padding: 1rem;
      background: var(--color-bg-secondary);
      border-radius: 8px;
      border: 1px solid var(--color-border);
    }

    .stock-status {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.75rem;
      border-radius: 6px;

      &.in-stock {
        background: var(--color-success-light);
        color: var(--color-success);
      }

      &.low-stock {
        background: var(--color-warning-light);
        color: var(--color-warning);
      }

      &.out-of-stock {
        background: var(--color-error-light);
        color: var(--color-error);
      }
    }

    .status-icon {
      flex-shrink: 0;

      svg {
        width: 24px;
        height: 24px;
      }
    }

    .status-text {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;

      .label {
        font-weight: 600;
        font-size: 1rem;
      }

      .availability {
        font-size: 0.875rem;
        opacity: 0.8;
      }
    }

    .delivery-options {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .delivery-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-secondary);
      margin: 0 0 0.5rem;
    }

    .delivery-option {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        border-color: var(--color-primary-light);
      }

      &.selected {
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.05);
      }
    }

    .option-icon {
      flex-shrink: 0;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--color-bg-secondary);
      border-radius: 50%;

      svg {
        width: 18px;
        height: 18px;
        color: var(--color-text-secondary);
      }
    }

    .option-details {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.125rem;

      .option-name {
        font-weight: 500;
        font-size: 0.875rem;
        color: var(--color-text-primary);
      }

      .option-estimate {
        font-size: 0.75rem;
        color: var(--color-text-secondary);
      }
    }

    .option-price {
      font-weight: 600;
      font-size: 0.875rem;

      .free {
        color: var(--color-success);
      }
    }

    .delivery-estimate {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem;
      background: var(--color-primary-light);
      border-radius: 6px;
      color: var(--color-primary);
      font-size: 0.875rem;

      svg {
        width: 20px;
        height: 20px;
        flex-shrink: 0;
      }

      strong {
        font-weight: 600;
      }
    }

    @media (max-width: 768px) {
      .stock-delivery {
        padding: 0.75rem;
      }

      .delivery-option {
        padding: 0.5rem;
      }
    }
  `]
})
export class StockDeliveryComponent {
  stockQuantity = input<number>(0);
  lowStockThreshold = input<number>(5);
  restockDate = input<string | null>(null);
  deliveryOptions = input<DeliveryOption[]>([
    { type: 'standard', name: 'Standard Delivery', price: 0, estimatedDays: { min: 3, max: 5 } },
    { type: 'express', name: 'Express Delivery', price: 9.99, estimatedDays: { min: 1, max: 2 } }
  ]);
  selectedDelivery = input<'standard' | 'express' | 'pickup'>('standard');

  inStock = computed(() => this.stockQuantity() > 0);

  lowStock = computed(() => {
    const qty = this.stockQuantity();
    return qty > 0 && qty <= this.lowStockThreshold();
  });

  stockStatusClass = computed(() => {
    if (!this.inStock()) return 'out-of-stock';
    if (this.lowStock()) return 'low-stock';
    return 'in-stock';
  });

  estimatedDeliveryDate = computed(() => {
    if (!this.inStock()) return null;

    const selected = this.deliveryOptions().find(o => o.type === this.selectedDelivery());
    if (!selected) return null;

    const today = new Date();
    const minDate = new Date(today);
    minDate.setDate(minDate.getDate() + selected.estimatedDays.min);

    const maxDate = new Date(today);
    maxDate.setDate(maxDate.getDate() + selected.estimatedDays.max);

    const formatDate = (date: Date) =>
      date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });

    if (selected.estimatedDays.min === selected.estimatedDays.max) {
      return formatDate(minDate);
    }

    return `${formatDate(minDate)} - ${formatDate(maxDate)}`;
  });

  getDeliveryEstimate(option: DeliveryOption): string {
    if (option.estimatedDays.min === option.estimatedDays.max) {
      return `${option.estimatedDays.min} day${option.estimatedDays.min > 1 ? 's' : ''}`;
    }
    return `${option.estimatedDays.min}-${option.estimatedDays.max} days`;
  }
}
