import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';
import { Product, ProductBrief } from '../models/product.model';

export interface BreadcrumbItem {
  name: string;
  url: string;
}

export interface OrganizationData {
  name: string;
  url: string;
  logo: string;
  contactPoint?: {
    telephone: string;
    contactType: string;
    areaServed?: string;
    availableLanguage?: string[];
  };
  sameAs?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class StructuredDataService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private scriptElement: HTMLScriptElement | null = null;

  /**
   * Set Product structured data (JSON-LD).
   *
   * `basePrice` is the *effective* current selling price the customer pays (B-002:
   * `salePrice` is the struck-through original/compare-at, not what is charged).
   * Image URLs are absolutized against `baseUrl`. Availability uses real variant stock
   * when variants exist (the authoritative detail-page signal); with no variants it falls
   * back to the product-level `inStock` flag and is NEVER defaulted to InStock (council M2).
   */
  setProductData(product: Product, baseUrl: string): void {
    const hasVariants = (product.variants?.length ?? 0) > 0;
    const inStock = hasVariants
      ? product.variants.some(v => v.isActive && v.availableQuantity > 0)
      : (product.inStock ?? false);

    const data = {
      '@context': 'https://schema.org',
      '@type': 'Product',
      name: product.name,
      description: product.shortDescription || product.description,
      sku: product.sku,
      brand: product.brand ? {
        '@type': 'Brand',
        name: product.brand
      } : undefined,
      image: (product.images ?? []).map(img => this.absoluteUrl(img.url, baseUrl)),
      offers: {
        '@type': 'Offer',
        url: `${baseUrl}/products/${product.slug}`,
        priceCurrency: 'EUR',
        price: product.basePrice,
        priceValidUntil: this.getDatePlusOneYear(),
        availability: inStock
          ? 'https://schema.org/InStock'
          : 'https://schema.org/OutOfStock',
        seller: {
          '@type': 'Organization',
          name: 'ClimaSite'
        }
      },
      aggregateRating: product.reviewCount > 0 ? {
        '@type': 'AggregateRating',
        ratingValue: product.averageRating,
        reviewCount: product.reviewCount,
        bestRating: 5,
        worstRating: 1
      } : undefined
    };

    // Remove undefined values
    this.cleanObject(data);
    this.setJsonLd(data, 'product');
  }

  /**
   * Set Product List structured data for category / list / brand / promotion pages.
   */
  setProductListData(products: ProductBrief[], listName: string, baseUrl: string): void {
    const data = {
      '@context': 'https://schema.org',
      '@type': 'ItemList',
      name: listName,
      numberOfItems: products.length,
      itemListElement: products.map((product, index) => ({
        '@type': 'ListItem',
        position: index + 1,
        item: {
          '@type': 'Product',
          name: product.name,
          url: `${baseUrl}/products/${product.slug}`,
          image: product.primaryImageUrl ? this.absoluteUrl(product.primaryImageUrl, baseUrl) : undefined,
          offers: {
            '@type': 'Offer',
            priceCurrency: 'EUR',
            price: product.basePrice,
            availability: product.inStock
              ? 'https://schema.org/InStock'
              : 'https://schema.org/OutOfStock'
          }
        }
      }))
    };

    this.cleanObject(data);
    this.setJsonLd(data, 'product-list');
  }

  /**
   * Set Breadcrumb structured data
   */
  setBreadcrumbData(items: BreadcrumbItem[]): void {
    const data = {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: items.map((item, index) => ({
        '@type': 'ListItem',
        position: index + 1,
        name: item.name,
        item: item.url
      }))
    };

    this.setJsonLd(data, 'breadcrumb');
  }

  /**
   * Set Organization structured data (for homepage)
   */
  setOrganizationData(data: OrganizationData): void {
    const structuredData = {
      '@context': 'https://schema.org',
      '@type': 'Organization',
      name: data.name,
      url: data.url,
      logo: data.logo,
      contactPoint: data.contactPoint ? {
        '@type': 'ContactPoint',
        telephone: data.contactPoint.telephone,
        contactType: data.contactPoint.contactType,
        areaServed: data.contactPoint.areaServed,
        availableLanguage: data.contactPoint.availableLanguage
      } : undefined,
      sameAs: data.sameAs
    };

    this.cleanObject(structuredData);
    this.setJsonLd(structuredData, 'organization');
  }

  /**
   * Set WebSite structured data with search action
   */
  setWebsiteData(name: string, url: string, searchUrl: string): void {
    const data = {
      '@context': 'https://schema.org',
      '@type': 'WebSite',
      name: name,
      url: url,
      potentialAction: {
        '@type': 'SearchAction',
        target: {
          '@type': 'EntryPoint',
          // The storefront search param is `search` (NOT `q`) — product-list reads `search`.
          urlTemplate: `${searchUrl}?search={search_term_string}`
        },
        'query-input': 'required name=search_term_string'
      }
    };

    this.setJsonLd(data, 'website');
  }

  /**
   * Set FAQ structured data
   */
  setFaqData(questions: Array<{ question: string; answer: string }>): void {
    const data = {
      '@context': 'https://schema.org',
      '@type': 'FAQPage',
      mainEntity: questions.map(q => ({
        '@type': 'Question',
        name: q.question,
        acceptedAnswer: {
          '@type': 'Answer',
          text: q.answer
        }
      }))
    };

    this.setJsonLd(data, 'faq');
  }

  /**
   * Remove all structured data scripts
   */
  clearAll(): void {
    if (!this.isBrowser) return;

    const scripts = this.document.querySelectorAll('script[type="application/ld+json"]');
    scripts.forEach(script => script.remove());
    this.scriptElement = null;
  }

  /**
   * Remove specific structured data by ID
   */
  clearById(id: string): void {
    if (!this.isBrowser) return;

    const script = this.document.getElementById(`structured-data-${id}`);
    if (script) {
      script.remove();
    }
  }

  private setJsonLd(data: object, id: string = 'default'): void {
    if (!this.isBrowser) return;

    const elementId = `structured-data-${id}`;

    // Remove existing script with same ID
    const existingScript = this.document.getElementById(elementId);
    if (existingScript) {
      existingScript.remove();
    }

    // Create new script element
    const script = this.document.createElement('script');
    script.id = elementId;
    script.type = 'application/ld+json';
    script.text = JSON.stringify(data, null, 0);

    this.document.head.appendChild(script);
  }

  private cleanObject(obj: Record<string, unknown>): void {
    Object.keys(obj).forEach(key => {
      if (obj[key] === undefined || obj[key] === null) {
        delete obj[key];
      } else if (typeof obj[key] === 'object' && !Array.isArray(obj[key])) {
        this.cleanObject(obj[key] as Record<string, unknown>);
      }
    });
  }

  private getDatePlusOneYear(): string {
    const date = new Date();
    date.setFullYear(date.getFullYear() + 1);
    return date.toISOString().split('T')[0];
  }

  /** Absolutize a possibly-relative asset/image URL against the site origin. */
  private absoluteUrl(url: string, baseUrl: string): string {
    if (!url) return url;
    if (/^https?:\/\//i.test(url)) return url;
    return `${baseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
  }
}
