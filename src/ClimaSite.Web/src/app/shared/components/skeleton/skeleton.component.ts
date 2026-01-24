import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Skeleton variant types
 * - text: Horizontal line (default height 1rem)
 * - circular: Perfect circle (uses height for both dimensions)
 * - rectangular: Rectangle without rounding
 * - rounded: Rectangle with border radius
 */
export type SkeletonVariant = 'text' | 'circular' | 'rectangular' | 'rounded';

/**
 * Skeleton animation types
 * - pulse: Opacity change animation
 * - wave: Shimmer effect moving across (GPU-accelerated)
 * - none: No animation (respects prefers-reduced-motion)
 */
export type SkeletonAnimation = 'pulse' | 'wave' | 'none';

/**
 * Base Skeleton Component
 * 
 * A flexible skeleton loading placeholder that can represent various UI elements.
 * Uses CSS custom properties from the design system for consistent theming.
 * 
 * @example
 * ```html
 * <!-- Basic text skeleton -->
 * <app-skeleton />
 * 
 * <!-- Circular avatar skeleton -->
 * <app-skeleton variant="circular" width="48px" height="48px" />
 * 
 * <!-- Image placeholder -->
 * <app-skeleton variant="rounded" width="100%" height="200px" />
 * 
 * <!-- Multiple text lines -->
 * <app-skeleton [lines]="3" />
 * ```
 */
@Component({
  selector: 'app-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (variant() === 'text' && lines() > 1) {
      <!-- Multi-line text skeleton -->
      <div 
        class="skeleton-container"
        role="status"
        [attr.aria-label]="ariaLabel()"
        [attr.data-testid]="testId()"
      >
        <span class="sr-only">{{ ariaLabel() }}</span>
        @for (line of linesArray(); track $index; let isLast = $last) {
          <div 
            [class]="skeletonClasses()"
            [style.width]="isLast ? lastLineWidth() : width()"
            [style.height]="height()"
            [style.margin-bottom]="isLast ? '0' : lineSpacing()"
            aria-hidden="true"
          ></div>
        }
      </div>
    } @else {
      <!-- Single skeleton element -->
      <div 
        [class]="skeletonClasses()"
        [style.width]="computedWidth()"
        [style.height]="computedHeight()"
        role="status"
        [attr.aria-label]="ariaLabel()"
        [attr.data-testid]="testId()"
      >
        <span class="sr-only">{{ ariaLabel() }}</span>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .skeleton-container {
      display: flex;
      flex-direction: column;
    }

    .skeleton {
      background-color: var(--color-bg-tertiary);
      position: relative;
      overflow: hidden;
    }

    /* Variant: text */
    .skeleton--text {
      border-radius: var(--radius-md);
    }

    /* Variant: circular */
    .skeleton--circular {
      border-radius: var(--radius-full);
    }

    /* Variant: rectangular */
    .skeleton--rectangular {
      border-radius: 0;
    }

    /* Variant: rounded */
    .skeleton--rounded {
      border-radius: var(--radius-lg);
    }

    /* Animation: pulse (opacity change) */
    .skeleton--pulse {
      animation: skeleton-pulse var(--duration-slowest) var(--ease-in-out) infinite;
    }

    /* Animation: wave (shimmer effect with GPU acceleration) */
    .skeleton--wave::after {
      content: '';
      position: absolute;
      inset: 0;
      background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(255, 255, 255, 0.4) 50%,
        transparent 100%
      );
      background-size: 200% 100%;
      animation: skeleton-wave 1.5s var(--ease-in-out) infinite;
      will-change: background-position;
    }

    /* Dark theme adjustment for wave */
    :host-context([data-theme="dark"]) .skeleton--wave::after,
    :host-context(.dark) .skeleton--wave::after {
      background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(255, 255, 255, 0.1) 50%,
        transparent 100%
      );
      background-size: 200% 100%;
    }

    @keyframes skeleton-pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.4;
      }
    }

    @keyframes skeleton-wave {
      0% {
        background-position: 200% 0;
      }
      100% {
        background-position: -200% 0;
      }
    }

    /* Respect reduced motion preference */
    @media (prefers-reduced-motion: reduce) {
      .skeleton--pulse,
      .skeleton--wave::after {
        animation: none;
      }
    }

    /* Screen reader only text */
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
export class SkeletonComponent {
  /** Skeleton shape variant */
  readonly variant = input<SkeletonVariant>('text');
  
  /** Width of the skeleton (CSS value) */
  readonly width = input<string>('100%');
  
  /** Height of the skeleton (CSS value) */
  readonly height = input<string>('1rem');
  
  /** Animation type */
  readonly animation = input<SkeletonAnimation>('pulse');
  
  /** Number of lines for text variant */
  readonly lines = input<number>(1);
  
  /** Width of the last line (for text variant with multiple lines) */
  readonly lastLineWidth = input<string>('70%');
  
  /** Spacing between lines */
  readonly lineSpacing = input<string>('var(--space-2)');
  
  /** Accessible label for screen readers */
  readonly ariaLabel = input<string>('Loading...');
  
  /** Test ID for E2E testing */
  readonly testId = input<string>('skeleton');

  /** Computed CSS classes based on inputs */
  readonly skeletonClasses = computed(() => {
    const classes = ['skeleton', `skeleton--${this.variant()}`];
    
    if (this.animation() !== 'none') {
      classes.push(`skeleton--${this.animation()}`);
    }
    
    return classes.join(' ');
  });

  /** Array for ngFor iteration based on lines count */
  readonly linesArray = computed(() => 
    Array.from({ length: this.lines() }, (_, i) => i)
  );

  /** Computed width - for circular variant, use height to make a perfect circle */
  readonly computedWidth = computed(() => {
    if (this.variant() === 'circular') {
      return this.height();
    }
    return this.width();
  });

  /** Computed height */
  readonly computedHeight = computed(() => this.height());
}
