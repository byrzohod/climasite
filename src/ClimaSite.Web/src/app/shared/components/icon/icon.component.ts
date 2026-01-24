import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

/**
 * Icon size options following Nordic Tech Design System
 */
export type IconSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/**
 * Size mapping from semantic sizes to pixel values
 * xs=14, sm=16, md=20, lg=24, xl=32
 */
export const ICON_SIZE_MAP: Record<IconSize, number> = {
  xs: 14,
  sm: 16,
  md: 20,
  lg: 24,
  xl: 32
};

/**
 * Icon Component - Wrapper for Lucide icons with consistent sizing and styling.
 * 
 * This component provides a unified interface for using Lucide icons throughout
 * the application with predefined sizes and customizable stroke width.
 * 
 * @example
 * ```html
 * <!-- Basic usage -->
 * <app-icon name="shopping-cart" />
 * 
 * <!-- With size -->
 * <app-icon name="heart" size="lg" />
 * 
 * <!-- With custom stroke width -->
 * <app-icon name="search" [strokeWidth]="1.5" />
 * 
 * <!-- With additional classes -->
 * <app-icon name="user" class="text-primary" />
 * ```
 */
@Component({
  selector: 'app-icon',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <lucide-icon
      [name]="name()"
      [size]="computedSize()"
      [strokeWidth]="strokeWidth()"
      [class]="iconClasses()"
      [attr.aria-hidden]="ariaHidden()"
      [attr.role]="ariaHidden() ? null : 'img'"
      [attr.aria-label]="ariaLabel()"
      [attr.data-testid]="testId()"
    />
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      line-height: 0;
    }

    lucide-icon {
      display: inline-flex;
      flex-shrink: 0;
    }

    :host(.spin) lucide-icon {
      animation: icon-spin 1s linear infinite;
    }

    @keyframes icon-spin {
      from { transform: rotate(0deg); }
      to { transform: rotate(360deg); }
    }

    @media (prefers-reduced-motion: reduce) {
      :host(.spin) lucide-icon {
        animation: none;
      }
    }
  `],
  host: {
    '[class.spin]': 'spin()'
  }
})
export class IconComponent {
  // ----- Required Inputs -----
  
  /**
   * Icon name from Lucide library (e.g., 'shopping-cart', 'heart', 'search')
   * @see https://lucide.dev/icons for available icons
   */
  readonly name = input.required<string>();

  // ----- Optional Inputs -----

  /**
   * Icon size using semantic scale
   * Maps to: xs=14px, sm=16px, md=20px, lg=24px, xl=32px
   * @default 'md'
   */
  readonly size = input<IconSize>('md');

  /**
   * Stroke width for the icon paths
   * @default 2
   */
  readonly strokeWidth = input<number>(2);

  /**
   * Additional CSS classes to apply to the icon
   */
  readonly class = input<string>('');

  /**
   * Whether to animate the icon with a spin effect
   * Useful for loading indicators
   * @default false
   */
  readonly spin = input<boolean>(false);

  /**
   * Whether the icon is decorative (hidden from screen readers)
   * Set to false and provide ariaLabel for meaningful icons
   * @default true
   */
  readonly ariaHidden = input<boolean>(true);

  /**
   * Accessible label for the icon (only used when ariaHidden is false)
   */
  readonly ariaLabel = input<string>('');

  /**
   * Test ID for E2E testing
   * @default 'icon'
   */
  readonly testId = input<string>('icon');

  // ----- Computed Properties -----

  /**
   * Convert semantic size to pixel value
   */
  readonly computedSize = computed(() => ICON_SIZE_MAP[this.size()]);

  /**
   * Build CSS class string for the icon
   */
  readonly iconClasses = computed(() => {
    const classes: string[] = [];
    
    const customClass = this.class();
    if (customClass) {
      classes.push(customClass);
    }

    return classes.join(' ');
  });
}
