import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  SkeletonComponent, 
  type SkeletonAnimation 
} from '../skeleton/skeleton.component';

/**
 * Skeleton Product Card Component (Legacy Location)
 * 
 * This component has been refactored to use the new base SkeletonComponent.
 * For new usage, consider importing from '@shared/components/skeleton'.
 * 
 * @deprecated Import from '../skeleton' instead for the full skeleton system
 */
@Component({
  selector: 'app-skeleton-product-card',
  standalone: true,
  imports: [CommonModule, SkeletonComponent],
  template: `
    <div 
      class="skeleton-card" 
      role="status"
      [attr.aria-label]="ariaLabel()"
      [attr.data-testid]="testId()"
    >
      <span class="sr-only">{{ ariaLabel() }}</span>
      
      <!-- Image placeholder -->
      <div class="skeleton-image">
        <app-skeleton 
          variant="rectangular" 
          width="100%" 
          height="100%" 
          [animation]="animation()"
          ariaLabel=""
        />
      </div>
      
      <!-- Content area -->
      <div class="skeleton-content">
        <!-- Category -->
        <app-skeleton 
          variant="text" 
          width="60%" 
          height="0.75rem" 
          [animation]="animation()"
          ariaLabel=""
        />
        
        <!-- Title line 1 -->
        <app-skeleton 
          variant="text" 
          width="100%" 
          height="1rem" 
          [animation]="animation()"
          ariaLabel=""
        />
        
        <!-- Title line 2 (shorter) -->
        <app-skeleton 
          variant="text" 
          width="70%" 
          height="1rem" 
          [animation]="animation()"
          ariaLabel=""
        />
        
        <!-- Rating -->
        <app-skeleton 
          variant="text" 
          width="40%" 
          height="0.875rem" 
          [animation]="animation()"
          ariaLabel=""
        />
        
        <!-- Price -->
        <app-skeleton 
          variant="text" 
          width="50%" 
          height="1.5rem" 
          [animation]="animation()"
          ariaLabel=""
        />
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .skeleton-card {
      background: var(--color-bg-card);
      border-radius: var(--radius-xl);
      overflow: hidden;
      border: 1px solid var(--color-border-primary);
    }

    .skeleton-image {
      width: 100%;
      height: 200px;
      background: var(--color-bg-secondary);
    }

    .skeleton-content {
      padding: var(--space-4);
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    /* Spacing adjustments for specific elements */
    .skeleton-content app-skeleton:nth-child(3) {
      margin-bottom: var(--space-2);
    }

    .skeleton-content app-skeleton:nth-child(4) {
      margin-bottom: var(--space-2);
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
  `]
})
export class SkeletonProductCardComponent {
  /** Animation type */
  readonly animation = input<SkeletonAnimation>('pulse');
  
  /** Accessible label for screen readers */
  readonly ariaLabel = input<string>('Loading product...');
  
  /** Test ID for E2E testing */
  readonly testId = input<string>('skeleton-product-card');
}
