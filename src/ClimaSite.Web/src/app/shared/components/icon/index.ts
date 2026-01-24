/**
 * Icon Component Public API
 * Nordic Tech Design System - Icon System
 * 
 * @example
 * ```typescript
 * import { IconComponent, ICON_REGISTRY, IconSize } from '@shared/components/icon';
 * 
 * // In your module/component imports
 * imports: [
 *   IconComponent,
 *   LucideAngularModule.pick(ICON_REGISTRY)
 * ]
 * ```
 */

export { 
  IconComponent,
  type IconSize,
  ICON_SIZE_MAP
} from './icon.component';

export {
  ICON_REGISTRY,
  ICON_CATEGORIES,
  isIconRegistered,
  type RegisteredIconName,
  type LucideIconData
} from './icon-registry';
