import { Injectable, inject } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  NavigationStart,
  Router,
  RouterStateSnapshot,
  TitleStrategy,
} from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { SeoService } from './seo.service';
import { StructuredDataService } from './structured-data.service';

/** Shape of the per-route `data.seo` block consumed by {@link SeoTitleStrategy}. */
export interface RouteSeo {
  titleKey?: string;
  descriptionKey?: string;
  robots?: string;
  image?: string;
  type?: string;
}

/**
 * Route-owned JSON-LD ids cleared on every NavigationStart (by the `provideSeoJsonLdReset`
 * app-initializer in app.config.ts). Includes `breadcrumb`: the detail/list pages emit
 * breadcrumb JSON-LD in their async SEO path (they render inline breadcrumbs, not the
 * head-service BreadcrumbComponent), so it must be cleared per-nav to avoid a stale trail
 * leaking onto the next page. (BreadcrumbComponent, when used, self-clears + re-emits the
 * same id, so this stays consistent.)
 */
export const ROUTE_OWNED_JSON_LD = ['product', 'product-list', 'organization', 'website', 'faq', 'breadcrumb', 'default'];

/**
 * Custom {@link TitleStrategy} (B-044): on each successful navigation it resolves the
 * deepest route's `data.seo` and applies sane DEFAULT title/description/canonical/robots
 * + OG/Twitter via {@link SeoService} — so a previous route's meta never leaks. Routes
 * without `data.seo` get a brand-only default title, the site description, and `index,follow`.
 *
 * IMPORTANT: this strategy (and {@link SeoService}) must NOT inject `Router` — the Router
 * constructs the TitleStrategy during its own initialization, so a Router dependency here is
 * circular (NG0200). The NavigationStart JSON-LD reset therefore lives in an app-initializer
 * (app.config.ts), which can safely inject Router because it runs after the injector is built.
 */
@Injectable({ providedIn: 'root' })
export class SeoTitleStrategy extends TitleStrategy {
  private readonly seo = inject(SeoService);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const seo = this.resolveSeo(snapshot);
    this.seo.setMeta({
      titleKey: seo?.titleKey,
      descriptionKey: seo?.descriptionKey,
      robots: seo?.robots,
      image: seo?.image,
      type: seo?.type,
    });
  }

  /** Walk root -> deepest, letting the most specific defined `data.seo` win. */
  private resolveSeo(snapshot: RouterStateSnapshot): RouteSeo | undefined {
    let route: ActivatedRouteSnapshot | null = snapshot.root;
    let seo: RouteSeo | undefined;
    while (route) {
      const data = route.data as { seo?: RouteSeo };
      if (data?.seo) {
        seo = data.seo;
      }
      route = route.firstChild;
    }
    return seo;
  }
}

/**
 * Clear every route-owned JSON-LD block on each {@link NavigationStart} — BEFORE the next
 * route's component synchronously emits its own (e.g. home's constructor) — so structured
 * data never leaks across navigations. Wired from an app-initializer in app.config.ts; it
 * takes `Router` as a plain argument (NOT injected into a Router-constructed service) to
 * avoid the NG0200 circular dependency. Returns the subscription for completeness/teardown.
 */
export function resetRouteOwnedJsonLdOnNavigation(
  router: Router,
  structuredData: StructuredDataService,
): Subscription {
  return router.events
    .pipe(filter((e): e is NavigationStart => e instanceof NavigationStart))
    .subscribe(() => {
      for (const id of ROUTE_OWNED_JSON_LD) {
        structuredData.clearById(id);
      }
    });
}
