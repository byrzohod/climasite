import { Component, inject, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ComparisonService, CompareProduct } from '../../../core/services/comparison.service';
import { ProductBrief } from '../../../core/models/product.model';

@Component({
  selector: 'app-compare-button',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <button
      class="compare-btn"
      [class.active]="isInCompare()"
      [class.icon-only]="iconOnly()"
      [disabled]="isDisabled()"
      (click)="toggle($event)"
      [attr.title]="isInCompare() ? ('products.comparison.removeFromCompare' | translate) : ('products.comparison.addToCompare' | translate)"
      [attr.data-testid]="'compare-btn-' + product().id">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        @if (isInCompare()) {
          <polyline points="20 6 9 17 4 12"/>
        } @else {
          <line x1="18" y1="20" x2="18" y2="4"/>
          <line x1="6" y1="20" x2="6" y2="10"/>
          <line x1="12" y1="20" x2="12" y2="7"/>
        }
      </svg>
      @if (!iconOnly()) {
        <span>
          @if (isInCompare()) {
            {{ 'products.comparison.removeFromCompare' | translate }}
          } @else {
            {{ 'products.comparison.addToCompare' | translate }}
          }
        </span>
      }
    </button>
  `,
  styles: [`
    .compare-btn {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      background: var(--color-bg-primary);
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.2s;

      svg {
        width: 18px;
        height: 18px;
        flex-shrink: 0;
      }

      &:hover:not(:disabled) {
        border-color: var(--color-primary);
        color: var(--color-primary);
      }

      &.active {
        border-color: var(--color-primary);
        background: rgba(var(--color-primary-rgb), 0.1);
        color: var(--color-primary);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      &.icon-only {
        padding: 0.5rem;

        span {
          display: none;
        }
      }
    }
  `]
})
export class CompareButtonComponent {
  private readonly comparisonService = inject(ComparisonService);

  product = input.required<ProductBrief | CompareProduct>();
  iconOnly = input<boolean>(false);

  isInCompare = computed(() => this.comparisonService.isInCompare(this.product().id));
  isDisabled = computed(() => !this.isInCompare() && this.comparisonService.isFull());

  toggle(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.comparisonService.toggleCompare(this.product());
  }
}
