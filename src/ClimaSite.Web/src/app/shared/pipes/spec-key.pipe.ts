import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

/**
 * Pipe to translate product specification keys.
 *
 * Usage: {{ specKey | specKey }}
 *
 * Example:
 *   Input: "cooling_capacity"
 *   Output: "Cooling Capacity" (or translated version based on current language)
 *
 * The pipe looks up translations in the format: specs.{key}
 * If no translation is found, it falls back to a humanized version of the key.
 */
@Pipe({
  name: 'specKey',
  standalone: true,
  pure: false // Allow for language changes
})
export class SpecKeyPipe implements PipeTransform {
  private readonly translate = inject(TranslateService);

  transform(key: string): string {
    if (!key) return '';

    // Try to get translation from i18n
    const translationKey = `specs.${key}`;
    const translated = this.translate.instant(translationKey);

    // If translation exists and is different from the key, use it
    if (translated && translated !== translationKey) {
      return translated;
    }

    // Fallback: humanize the key
    return this.humanizeKey(key);
  }

  private humanizeKey(key: string): string {
    return key
      // Replace underscores and hyphens with spaces
      .replace(/[_-]/g, ' ')
      // Insert space before capital letters (for camelCase)
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      // Capitalize first letter of each word
      .replace(/\b\w/g, char => char.toUpperCase())
      .trim();
  }
}
