import { Component, inject, computed, input, OnInit, DestroyRef } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { Router, ActivatedRoute, NavigationEnd, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { filter, map, startWith } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';

export interface BreadcrumbItem {
  label: string;
  translationKey?: string;
  url: string;
  isCurrentPage: boolean;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  template: `
    <nav 
      class="breadcrumb" 
      [attr.aria-label]="'common.aria.breadcrumb' | translate"
      data-testid="breadcrumb"
    >
      <ol class="breadcrumb-list" itemscope itemtype="https://schema.org/BreadcrumbList">
        @for (item of breadcrumbs(); track item.url; let i = $index; let last = $last) {
          <li 
            class="breadcrumb-item"
            itemprop="itemListElement"
            itemscope
            itemtype="https://schema.org/ListItem"
          >
            @if (!last) {
              <a
                [routerLink]="item.url"
                class="breadcrumb-link"
                itemprop="item"
                [attr.data-testid]="'breadcrumb-link-' + i"
              >
                <span itemprop="name">
                  @if (item.translationKey) {
                    {{ item.translationKey | translate }}
                  } @else {
                    {{ item.label }}
                  }
                </span>
              </a>
              <span class="breadcrumb-separator" aria-hidden="true">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
                </svg>
              </span>
            } @else {
              <span 
                class="breadcrumb-current" 
                aria-current="page"
                itemprop="item"
                [attr.data-testid]="'breadcrumb-current'"
              >
                <span itemprop="name">
                  @if (item.translationKey) {
                    {{ item.translationKey | translate }}
                  } @else {
                    {{ item.label }}
                  }
                </span>
              </span>
            }
            <meta itemprop="position" [content]="(i + 1).toString()" />
          </li>
        }
      </ol>
    </nav>

    <!-- JSON-LD Structured Data for SEO -->
    @if (breadcrumbs().length > 0) {
      <script type="application/ld+json" [innerHTML]="structuredData()"></script>
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .breadcrumb {
      padding: 0.75rem 0;
    }

    .breadcrumb-list {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.25rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .breadcrumb-item {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .breadcrumb-link {
      font-size: 0.875rem;
      color: var(--color-text-secondary);
      text-decoration: none;
      transition: color 0.15s ease;
      white-space: nowrap;
    }

    .breadcrumb-link:hover {
      color: var(--color-primary);
      text-decoration: underline;
    }

    .breadcrumb-link:focus-visible {
      outline: 2px solid var(--color-ring);
      outline-offset: 2px;
      border-radius: var(--radius-sm);
    }

    .breadcrumb-separator {
      display: flex;
      align-items: center;
      color: var(--color-text-tertiary);
    }

    .breadcrumb-separator svg {
      width: 1rem;
      height: 1rem;
    }

    .breadcrumb-current {
      font-size: 0.875rem;
      color: var(--color-text-primary);
      font-weight: 500;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 200px;
    }

    @media (max-width: 640px) {
      .breadcrumb-current {
        max-width: 150px;
      }

      .breadcrumb-link {
        font-size: 0.8125rem;
      }

      .breadcrumb-current {
        font-size: 0.8125rem;
      }
    }
  `]
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly translateService = inject(TranslateService);
  private readonly document = inject(DOCUMENT);

  /** Override breadcrumb items (optional - if not provided, auto-generates from route) */
  readonly items = input<BreadcrumbItem[] | null>(null);

  /** Home label translation key */
  readonly homeKey = input<string>('nav.home');

  private readonly routeChange$ = this.router.events.pipe(
    filter(event => event instanceof NavigationEnd),
    startWith(null),
    map(() => this.buildBreadcrumbs())
  );

  private readonly autoBreadcrumbs = toSignal(this.routeChange$, { initialValue: [] });

  readonly breadcrumbs = computed(() => {
    const overrideItems = this.items();
    if (overrideItems && overrideItems.length > 0) {
      return overrideItems;
    }
    return this.autoBreadcrumbs();
  });

  readonly structuredData = computed(() => {
    const items = this.breadcrumbs();
    const baseUrl = this.document.location?.origin || '';
    
    const itemListElement = items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.translationKey 
        ? this.translateService.instant(item.translationKey) 
        : item.label,
      item: item.url.startsWith('http') ? item.url : `${baseUrl}${item.url}`
    }));

    return JSON.stringify({
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement
    });
  });

  private buildBreadcrumbs(): BreadcrumbItem[] {
    const breadcrumbs: BreadcrumbItem[] = [];
    
    // Always add home
    breadcrumbs.push({
      label: 'Home',
      translationKey: this.homeKey(),
      url: '/',
      isCurrentPage: this.router.url === '/'
    });

    // Don't process further if we're on home
    if (this.router.url === '/') {
      return breadcrumbs;
    }

    // Build from URL segments
    const urlSegments = this.router.url.split('?')[0].split('/').filter(segment => segment);
    let currentUrl = '';

    for (let i = 0; i < urlSegments.length; i++) {
      const segment = urlSegments[i];
      currentUrl += `/${segment}`;
      const isLast = i === urlSegments.length - 1;

      // Try to get a nice label from route data or segment
      const label = this.getSegmentLabel(segment);
      const translationKey = this.getSegmentTranslationKey(segment);

      breadcrumbs.push({
        label,
        translationKey,
        url: currentUrl,
        isCurrentPage: isLast
      });
    }

    return breadcrumbs;
  }

  private getSegmentLabel(segment: string): string {
    // Convert URL segment to readable label
    // e.g., "air-conditioners" -> "Air Conditioners"
    return segment
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  private getSegmentTranslationKey(segment: string): string | undefined {
    // Map common segments to translation keys
    const segmentKeyMap: Record<string, string> = {
      'products': 'nav.products',
      'categories': 'nav.categories',
      'cart': 'nav.cart',
      'checkout': 'checkout.title',
      'account': 'nav.account',
      'orders': 'nav.orders',
      'wishlist': 'nav.wishlist',
      'admin': 'nav.admin',
      'login': 'nav.login',
      'register': 'nav.register',
      'about': 'nav.about',
      'contact': 'nav.contact',
      'brands': 'nav.brands',
      'promotions': 'nav.promotions',
      'resources': 'nav.resources',
      'air-conditioners': 'nav.airConditioners',
      'heating-systems': 'nav.heatingSystems',
      'ventilation': 'nav.ventilation',
      'accessories': 'nav.accessories'
    };

    return segmentKeyMap[segment];
  }
}
