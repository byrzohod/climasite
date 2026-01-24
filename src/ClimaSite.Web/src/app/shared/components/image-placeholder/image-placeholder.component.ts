import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Placeholder variant types for different content contexts
 */
export type PlaceholderVariant = 'product' | 'category' | 'avatar' | 'hero' | 'brand' | 'generic';

/**
 * Placeholder size options
 */
export type PlaceholderSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Size mapping for icon dimensions
 */
const ICON_SIZE_MAP: Record<PlaceholderSize, number> = {
  xs: 16,
  sm: 24,
  md: 32,
  lg: 48,
  xl: 64
};

/**
 * Image Placeholder Component
 * 
 * Displays a styled placeholder when images are loading or missing.
 * Supports different variants for context-appropriate icons and styling.
 * 
 * @example
 * ```html
 * <!-- Product placeholder -->
 * <app-image-placeholder variant="product" size="lg" />
 * 
 * <!-- Avatar placeholder with initials fallback -->
 * <app-image-placeholder variant="avatar" [initials]="'JD'" />
 * 
 * <!-- Category placeholder -->
 * <app-image-placeholder variant="category" [animated]="true" />
 * ```
 */
@Component({
  selector: 'app-image-placeholder',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      [class]="containerClasses()"
      [style.width]="width()"
      [style.height]="height()"
      [style.aspect-ratio]="aspectRatio()"
      role="img"
      [attr.aria-label]="ariaLabel()"
      [attr.data-testid]="testId()"
    >
      @if (variant() === 'avatar' && initials()) {
        <span class="initials">{{ initials() }}</span>
      } @else if (showIcon()) {
        <svg 
          [attr.width]="iconSize()" 
          [attr.height]="iconSize()" 
          viewBox="0 0 24 24" 
          fill="none" 
          stroke="currentColor" 
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
        >
          @switch (variant()) {
            @case ('product') {
              <!-- Package icon for products -->
              <path d="M16.5 9.4l-9-5.19M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
              <path d="M3.27 6.96L12 12.01l8.73-5.05M12 22.08V12"/>
            }
            @case ('category') {
              <!-- Grid/folder icon for categories -->
              <path d="M3 3h7v7H3zM14 3h7v7h-7zM14 14h7v7h-7zM3 14h7v7H3z"/>
            }
            @case ('avatar') {
              <!-- User icon for avatars -->
              <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
              <circle cx="12" cy="7" r="4"/>
            }
            @case ('hero') {
              <!-- Landscape icon for hero images -->
              <path d="M14.5 4h-5L7 7H4a2 2 0 00-2 2v9a2 2 0 002 2h16a2 2 0 002-2V9a2 2 0 00-2-2h-3l-2.5-3z"/>
              <circle cx="12" cy="13" r="3"/>
            }
            @case ('brand') {
              <!-- Award/badge icon for brands -->
              <circle cx="12" cy="8" r="6"/>
              <path d="M15.477 12.89L17 22l-5-3-5 3 1.523-9.11"/>
            }
            @default {
              <!-- Generic image icon -->
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
              <circle cx="8.5" cy="8.5" r="1.5"/>
              <path d="M21 15l-5-5L5 21"/>
            }
          }
        </svg>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .placeholder {
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(
        135deg,
        var(--color-bg-secondary) 0%,
        var(--color-bg-tertiary) 100%
      );
      border-radius: var(--radius-lg);
      color: var(--color-text-tertiary);
      overflow: hidden;
      position: relative;
    }

    /* Variant-specific styling */
    .placeholder--product {
      background: linear-gradient(
        145deg,
        var(--color-primary-50) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-primary-100) 100%
      );
    }

    .placeholder--category {
      background: linear-gradient(
        135deg,
        var(--color-accent-50) 0%,
        var(--color-bg-secondary) 100%
      );
    }

    .placeholder--avatar {
      background: linear-gradient(
        135deg,
        var(--color-primary-100) 0%,
        var(--color-primary-200) 100%
      );
      border-radius: var(--radius-full);
    }

    .placeholder--hero {
      background: linear-gradient(
        135deg,
        var(--color-bg-tertiary) 0%,
        var(--color-bg-secondary) 50%,
        var(--color-bg-tertiary) 100%
      );
      border-radius: var(--radius-xl);
    }

    .placeholder--brand {
      background: var(--color-bg-secondary);
      border: 1px dashed var(--color-border-primary);
    }

    .placeholder--generic {
      background: var(--color-bg-tertiary);
    }

    /* Animated shimmer effect */
    .placeholder--animated::before {
      content: '';
      position: absolute;
      inset: 0;
      background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(255, 255, 255, 0.3) 50%,
        transparent 100%
      );
      background-size: 200% 100%;
      animation: shimmer 2s infinite;
    }

    @keyframes shimmer {
      0% { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }

    /* Dark theme adjustments */
    :host-context([data-theme="dark"]) .placeholder--product,
    :host-context(.dark) .placeholder--product {
      background: linear-gradient(
        145deg,
        rgba(14, 165, 233, 0.1) 0%,
        var(--color-bg-secondary) 50%,
        rgba(14, 165, 233, 0.15) 100%
      );
    }

    :host-context([data-theme="dark"]) .placeholder--category,
    :host-context(.dark) .placeholder--category {
      background: linear-gradient(
        135deg,
        rgba(6, 182, 212, 0.1) 0%,
        var(--color-bg-secondary) 100%
      );
    }

    :host-context([data-theme="dark"]) .placeholder--avatar,
    :host-context(.dark) .placeholder--avatar {
      background: linear-gradient(
        135deg,
        rgba(14, 165, 233, 0.15) 0%,
        rgba(14, 165, 233, 0.25) 100%
      );
    }

    :host-context([data-theme="dark"]) .placeholder--animated::before,
    :host-context(.dark) .placeholder--animated::before {
      background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(255, 255, 255, 0.08) 50%,
        transparent 100%
      );
      background-size: 200% 100%;
    }

    /* Icon styling */
    svg {
      opacity: 0.6;
      transition: opacity var(--duration-fast) var(--ease-smooth);
    }

    .placeholder:hover svg {
      opacity: 0.8;
    }

    /* Initials for avatar variant */
    .initials {
      font-family: var(--font-display);
      font-weight: var(--font-semibold);
      color: var(--color-primary);
      text-transform: uppercase;
      letter-spacing: var(--tracking-wide);
      user-select: none;
    }

    /* Respect reduced motion */
    @media (prefers-reduced-motion: reduce) {
      .placeholder--animated::before {
        animation: none;
      }
    }
  `]
})
export class ImagePlaceholderComponent {
  /** Placeholder variant - determines icon and styling */
  readonly variant = input<PlaceholderVariant>('product');
  
  /** Size of the icon inside the placeholder */
  readonly size = input<PlaceholderSize>('md');
  
  /** Whether to show the centered icon */
  readonly showIcon = input<boolean>(true);
  
  /** Enable shimmer animation */
  readonly animated = input<boolean>(false);
  
  /** Width of the placeholder (CSS value) */
  readonly width = input<string>('100%');
  
  /** Height of the placeholder (CSS value) */
  readonly height = input<string>('100%');
  
  /** Aspect ratio (e.g., '1/1', '16/9', '3/2') */
  readonly aspectRatio = input<string | null>(null);
  
  /** Initials to display (avatar variant only) */
  readonly initials = input<string>('');
  
  /** Accessible label */
  readonly ariaLabel = input<string>('Image placeholder');
  
  /** Test ID for E2E testing */
  readonly testId = input<string>('image-placeholder');

  /** Computed icon size based on size input */
  readonly iconSize = computed(() => ICON_SIZE_MAP[this.size()]);

  /** Build CSS classes */
  readonly containerClasses = computed(() => {
    const classes = ['placeholder', `placeholder--${this.variant()}`];
    
    if (this.animated()) {
      classes.push('placeholder--animated');
    }
    
    return classes.join(' ');
  });
}
