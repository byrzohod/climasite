import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkeletonComponent, type SkeletonAnimation } from './skeleton.component';

/**
 * Font size options for text skeleton
 */
export type SkeletonFontSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Height mapping for font sizes
 */
const fontSizeHeightMap: Record<SkeletonFontSize, string> = {
  xs: '0.75rem',   // 12px
  sm: '0.875rem',  // 14px
  md: '1rem',      // 16px
  lg: '1.25rem',   // 20px
  xl: '1.5rem',    // 24px
};

/**
 * Skeleton Text Component
 * 
 * Specialized skeleton component for text blocks with multiple lines support.
 * Automatically makes the last line shorter for a more natural look.
 * 
 * @example
 * ```html
 * <!-- Single line -->
 * <app-skeleton-text />
 * 
 * <!-- Paragraph (3 lines) -->
 * <app-skeleton-text [lines]="3" />
 * 
 * <!-- With custom widths per line -->
 * <app-skeleton-text [lines]="3" [widths]="['100%', '90%', '60%']" />
 * 
 * <!-- Larger font size -->
 * <app-skeleton-text fontSize="lg" [lines]="2" />
 * ```
 */
@Component({
  selector: 'app-skeleton-text',
  standalone: true,
  imports: [CommonModule, SkeletonComponent],
  template: `
    <div 
      class="skeleton-text"
      role="status"
      [attr.aria-label]="ariaLabel()"
      [attr.data-testid]="testId()"
    >
      <span class="sr-only">{{ ariaLabel() }}</span>
      @for (line of linesArray(); track $index; let isLast = $last) {
        <app-skeleton
          variant="text"
          [width]="getLineWidth($index, isLast)"
          [height]="lineHeight()"
          [animation]="animation()"
          [ariaLabel]="''"
          [style.margin-bottom]="isLast ? '0' : spacing()"
        />
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .skeleton-text {
      display: flex;
      flex-direction: column;
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
export class SkeletonTextComponent {
  /** Number of text lines to display */
  readonly lines = input<number>(1);
  
  /** Custom width for each line (optional) */
  readonly widths = input<(string | number)[]>([]);
  
  /** Width of the last line (used when widths array is not provided) */
  readonly lastLineWidth = input<string>('60%');
  
  /** Spacing between lines */
  readonly spacing = input<string>('var(--space-2)');
  
  /** Font size preset */
  readonly fontSize = input<SkeletonFontSize>('md');
  
  /** Animation type */
  readonly animation = input<SkeletonAnimation>('pulse');
  
  /** Accessible label for screen readers */
  readonly ariaLabel = input<string>('Loading text content...');
  
  /** Test ID for E2E testing */
  readonly testId = input<string>('skeleton-text');

  /** Computed line height based on font size */
  readonly lineHeight = computed(() => fontSizeHeightMap[this.fontSize()]);

  /** Array for ngFor iteration */
  readonly linesArray = computed(() => 
    Array.from({ length: this.lines() }, (_, i) => i)
  );

  /**
   * Get width for a specific line
   * Uses custom widths if provided, otherwise defaults to 100% with shorter last line
   */
  getLineWidth(index: number, isLast: boolean): string {
    const customWidths = this.widths();
    
    // Use custom width if provided for this index
    if (customWidths.length > index) {
      const width = customWidths[index];
      return typeof width === 'number' ? `${width}%` : width;
    }
    
    // Default: full width except last line
    if (isLast && this.lines() > 1) {
      return this.lastLineWidth();
    }
    
    return '100%';
  }
}
