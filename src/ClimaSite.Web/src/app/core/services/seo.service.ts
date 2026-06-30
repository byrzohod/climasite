import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { SupportedLanguage } from './language.service';

/**
 * The single brand authority for the document title. Applied exactly once as a
 * suffix; {@link SeoService.buildTitle} guards against double-branding so calling
 * `setMeta` repeatedly (or with an already-branded title) is idempotent.
 */
const BRAND = 'ClimaSite';
const BRAND_SUFFIX = ` | ${BRAND}`;

/** Default Open Graph image — a 1200×630 raster brand card (PNG; social scrapers ignore SVG og:image).
 *  Source vector: og-default.svg; regenerate the PNG from it if the card design changes. */
const DEFAULT_OG_IMAGE = '/assets/images/og-default.png';

/** Fallback i18n keys used when a caller supplies no title/description. */
const DEFAULT_TITLE_KEY = 'seo.default.title';
const DEFAULT_DESCRIPTION_KEY = 'seo.default.description';

/** og:locale per supported language (language_TERRITORY). */
const OG_LOCALES: Record<SupportedLanguage, string> = {
  en: 'en_US',
  bg: 'bg_BG',
  de: 'de_DE',
};

export interface SeoMeta {
  /** Literal title (already-translated, e.g. a product name). Wins over `titleKey`. */
  title?: string;
  /** i18n key resolved via TranslateService. Used when `title` is absent. */
  titleKey?: string;
  /** Literal description (already-translated). Wins over `descriptionKey`. */
  description?: string;
  /** i18n key resolved via TranslateService. Used when `description` is absent. */
  descriptionKey?: string;
  /** og:image / twitter:image. Relative paths are absolutized against the origin. */
  image?: string;
  /** og:type (default `website`). */
  type?: string;
  /** `<meta name="robots">` content (default `index,follow`). */
  robots?: string;
  /**
   * Path used to build the self-referential canonical + og:url. Defaults to the
   * current router URL. Query string + fragment are always stripped.
   */
  canonicalPath?: string;
}

/**
 * Centralised head/meta authority for the CSR storefront (B-044). Sets `<title>`,
 * `<meta name=description>`, `<link rel=canonical>`, Open Graph and Twitter cards,
 * and `<meta name=robots>` idempotently. i18n titles/descriptions are re-resolved
 * and re-applied on every `onLangChange` for the current route.
 *
 * Canonical/og:url base is `window.location.origin` (a CSR app always has `window`),
 * so there is no build-time/runtime base-URL drift. Mirrors the DOCUMENT-injection +
 * `isPlatformBrowser` pattern of {@link StructuredDataService}.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly translate = inject(TranslateService);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  /** Last applied meta, re-applied (with fresh translations) on language change. */
  private lastMeta: SeoMeta | null = null;

  constructor() {
    // Re-apply the current route's meta whenever the active language changes so
    // translated titles/descriptions/og copy track EN <-> BG <-> DE switches.
    this.translate.onLangChange.subscribe(() => {
      if (this.lastMeta) {
        this.apply(this.lastMeta);
      }
    });
  }

  /**
   * Apply (or reset to defaults) the full head meta for the current view.
   * Idempotent: the brand suffix is never applied twice and tags are updated
   * in place rather than duplicated.
   */
  setMeta(meta: SeoMeta = {}): void {
    this.lastMeta = meta;
    this.apply(meta);
  }

  /** Reset every head field to the brand default (used when a route has no `seo`). */
  reset(): void {
    this.setMeta({});
  }

  private apply(meta: SeoMeta): void {
    if (!this.isBrowser) return;

    const rawTitle = meta.title ?? (meta.titleKey ? this.resolve(meta.titleKey) : this.resolve(DEFAULT_TITLE_KEY));
    const title = this.buildTitle(rawTitle);
    const description = meta.description
      ?? (meta.descriptionKey ? this.resolve(meta.descriptionKey) : this.resolve(DEFAULT_DESCRIPTION_KEY));
    const canonical = this.buildCanonical(meta.canonicalPath);
    const image = this.absoluteUrl(meta.image ?? DEFAULT_OG_IMAGE);
    const type = meta.type ?? 'website';
    const robots = meta.robots ?? 'index,follow';
    const locale = OG_LOCALES[this.translate.currentLang as SupportedLanguage] ?? OG_LOCALES.en;

    this.title.setTitle(title);
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ name: 'robots', content: robots });
    this.updateCanonical(canonical);

    // Open Graph
    this.meta.updateTag({ property: 'og:title', content: title });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:type', content: type });
    this.meta.updateTag({ property: 'og:url', content: canonical });
    this.meta.updateTag({ property: 'og:image', content: image });
    this.meta.updateTag({ property: 'og:site_name', content: BRAND });
    this.meta.updateTag({ property: 'og:locale', content: locale });

    // Twitter
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: title });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
  }

  /** Append the brand suffix exactly once (idempotent — guards double-branding). */
  private buildTitle(raw: string): string {
    const value = (raw ?? '').trim();
    if (!value) return BRAND;
    if (value === BRAND || value.endsWith(BRAND_SUFFIX)) return value;
    return `${value}${BRAND_SUFFIX}`;
  }

  /**
   * origin + clean path: strip query + fragment, strip trailing slash except root.
   * Filtered/paginated/search states are noindex, so collapsing them to the clean
   * canonical is correct.
   */
  private buildCanonical(path?: string): string {
    const origin = this.document.location.origin;
    // Default to the live DOM path (NOT Router.url) — injecting Router here would create a
    // circular DI dependency, because the Router constructs the SeoTitleStrategy that consumes
    // this service during the Router's own initialization (NG0200).
    let p = (path ?? this.document.location.pathname).split('#')[0].split('?')[0];
    if (!p.startsWith('/')) p = `/${p}`;
    if (p.length > 1) p = p.replace(/\/+$/, '') || '/';
    return `${origin}${p}`;
  }

  private absoluteUrl(url: string): string {
    if (/^https?:\/\//i.test(url)) return url;
    const origin = this.document.location.origin;
    return `${origin}${url.startsWith('/') ? '' : '/'}${url}`;
  }

  private resolve(key: string): string {
    const value = this.translate.instant(key);
    // ngx-translate returns the key verbatim when missing — fall back to brand copy.
    return value === key ? '' : value;
  }

  private updateCanonical(href: string): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', href);
  }
}
