/**
 * Skeleton Component Public API
 * Nordic Tech Design System - Loading States
 * 
 * Provides skeleton loading placeholders for various UI elements.
 * All components support pulse and wave animations with reduced motion support.
 */

// Base skeleton component
export { 
  SkeletonComponent,
  type SkeletonVariant,
  type SkeletonAnimation
} from './skeleton.component';

// Specialized text skeleton
export {
  SkeletonTextComponent,
  type SkeletonFontSize
} from './skeleton-text.component';

// Re-export the product card skeleton from its original location
// This maintains backwards compatibility
export { SkeletonProductCardComponent } from '../skeleton-product-card/skeleton-product-card.component';
