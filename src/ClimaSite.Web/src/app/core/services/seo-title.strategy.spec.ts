import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router, RouterStateSnapshot } from '@angular/router';
import { Subject } from 'rxjs';
import { SeoTitleStrategy, RouteSeo, resetRouteOwnedJsonLdOnNavigation } from './seo-title.strategy';
import { SeoService } from './seo.service';
import { StructuredDataService } from './structured-data.service';

/** Build a minimal RouterStateSnapshot-shaped object: root -> firstChild chain. */
function snapshotWithSeoChain(...seoLayers: (RouteSeo | undefined)[]): RouterStateSnapshot {
  let child: unknown = null;
  for (let i = seoLayers.length - 1; i >= 0; i--) {
    const data = seoLayers[i] ? { seo: seoLayers[i] } : {};
    child = { data, firstChild: child };
  }
  return { root: child } as unknown as RouterStateSnapshot;
}

describe('SeoTitleStrategy', () => {
  let strategy: SeoTitleStrategy;
  let seo: jasmine.SpyObj<SeoService>;

  beforeEach(() => {
    seo = jasmine.createSpyObj<SeoService>('SeoService', ['setMeta']);

    // The strategy injects ONLY SeoService — it must NOT depend on Router (the Router
    // constructs the TitleStrategy during its own init, so that would be a circular DI; the
    // NavigationStart JSON-LD reset lives in resetRouteOwnedJsonLdOnNavigation, tested below).
    TestBed.configureTestingModule({
      providers: [
        SeoTitleStrategy,
        { provide: SeoService, useValue: seo }
      ]
    });

    strategy = TestBed.inject(SeoTitleStrategy);
  });

  it('resolves the deepest defined data.seo (child overrides parent)', () => {
    const parent: RouteSeo = { titleKey: 'seo.parent.title' };
    const child: RouteSeo = { titleKey: 'seo.child.title', descriptionKey: 'seo.child.description', robots: 'noindex,follow' };

    strategy.updateTitle(snapshotWithSeoChain(parent, child));

    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({
      titleKey: 'seo.child.title',
      descriptionKey: 'seo.child.description',
      robots: 'noindex,follow'
    }));
  });

  it('inherits a parent data.seo when the deeper child defines none', () => {
    const parent: RouteSeo = { titleKey: 'seo.account.title', robots: 'noindex,follow' };

    strategy.updateTitle(snapshotWithSeoChain(parent, undefined));

    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({
      titleKey: 'seo.account.title',
      robots: 'noindex,follow'
    }));
  });

  it('applies default meta (undefined keys) for a route with no data.seo', () => {
    strategy.updateTitle(snapshotWithSeoChain(undefined, undefined));

    expect(seo.setMeta).toHaveBeenCalledWith(jasmine.objectContaining({
      titleKey: undefined,
      descriptionKey: undefined,
      robots: undefined
    }));
  });

});

describe('resetRouteOwnedJsonLdOnNavigation', () => {
  let structuredData: jasmine.SpyObj<StructuredDataService>;
  let events$: Subject<unknown>;

  beforeEach(() => {
    structuredData = jasmine.createSpyObj<StructuredDataService>('StructuredDataService', ['clearById']);
    events$ = new Subject<unknown>();
  });

  it('clears every route-owned JSON-LD id on NavigationStart (not End)', () => {
    resetRouteOwnedJsonLdOnNavigation(
      { events: events$.asObservable() } as unknown as Router,
      structuredData,
    );

    events$.next(new NavigationStart(1, '/products'));

    for (const id of ['product', 'product-list', 'organization', 'website', 'faq', 'breadcrumb', 'default']) {
      expect(structuredData.clearById).toHaveBeenCalledWith(id);
    }
  });

  it('does NOT clear on a non-NavigationStart router event', () => {
    resetRouteOwnedJsonLdOnNavigation(
      { events: events$.asObservable() } as unknown as Router,
      structuredData,
    );

    events$.next({ id: 1, url: '/products' }); // not a NavigationStart instance

    expect(structuredData.clearById).not.toHaveBeenCalled();
  });
});
