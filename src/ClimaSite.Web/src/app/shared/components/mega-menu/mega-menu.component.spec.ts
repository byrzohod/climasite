import { ElementRef } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { MegaMenuComponent } from './mega-menu.component';
import { CategoryService } from '../../../core/services/category.service';
import { CategoryTree } from '../../../core/models/category.model';

/**
 * Plan-19 B2 (batch 2): unit coverage for the mega-menu's open/close + navigation state machine.
 *
 * The component reads categories from CategoryService and drives open/active/position signals plus
 * a hover close-timeout. We provide jasmine spy doubles for CategoryService + Router and a stub
 * ElementRef whose nativeElement supports `contains` (document:click outside-detection) and
 * `querySelector` (ngAfterViewInit trigger lookup). Tests instantiate the component without
 * rendering the template (it imports RouterModule + ngx-translate), mirroring
 * checkout.component.spec.ts / cart.component.spec.ts.
 *
 * triggerElement stays null unless ngAfterViewInit finds one, so calculateMenuPosition()
 * short-circuits and menuLeftPosition stays null in these template-less tests — which is the
 * intended "centered" default. The hover close uses setTimeout(...,100), so those paths use
 * fakeAsync + tick(100).
 */

function makeCategory(overrides: Partial<CategoryTree> = {}): CategoryTree {
  return {
    id: 'c-1',
    name: 'categories.airConditioning',
    slug: 'air-conditioners',
    children: [],
    ...overrides
  };
}

function categoriesFixture(): CategoryTree[] {
  return [
    makeCategory({
      id: 'c-1',
      slug: 'air-conditioners',
      children: [
        makeCategory({ id: 'c-1-1', slug: 'split-air-conditioners', children: [] })
      ]
    }),
    makeCategory({ id: 'c-2', slug: 'heating-systems', children: [] })
  ];
}

interface MegaMenuSetup {
  component: MegaMenuComponent;
  categoryService: jasmine.SpyObj<CategoryService>;
  router: jasmine.SpyObj<Router>;
  nativeElement: { contains: jasmine.Spy; querySelector: jasmine.Spy };
}

function setup(): MegaMenuSetup {
  const categoryService = jasmine.createSpyObj<CategoryService>('CategoryService', [
    'getCategoryTree',
    'getCategoryBySlug'
  ]);
  categoryService.getCategoryTree.and.returnValue(of(categoriesFixture()));

  const router = jasmine.createSpyObj<Router>('Router', ['navigate']);

  const nativeElement = {
    contains: jasmine.createSpy('contains').and.returnValue(false),
    querySelector: jasmine.createSpy('querySelector').and.returnValue(null)
  };

  TestBed.configureTestingModule({
    providers: [
      { provide: CategoryService, useValue: categoryService },
      { provide: Router, useValue: router },
      { provide: ElementRef, useValue: { nativeElement } }
    ]
  });

  const component = TestBed.runInInjectionContext(() => new MegaMenuComponent());
  return { component, categoryService, router, nativeElement };
}

describe('MegaMenuComponent', () => {
  it('creates closed with no active category', () => {
    const { component } = setup();
    expect(component).toBeTruthy();
    expect(component.isOpen()).toBeFalse();
    expect(component.activeCategory()).toBeNull();
    expect(component.menuLeftPosition()).toBeNull();
  });

  describe('loadCategories (ngOnInit)', () => {
    it('loads + translation-keys the category tree and clears loading on success', () => {
      const { component, categoryService } = setup();
      component.ngOnInit();

      expect(categoryService.getCategoryTree).toHaveBeenCalledTimes(1);
      expect(component.categories().length).toBe(2);
      // addTranslationKeys maps known slugs to i18n keys.
      expect(component.categories()[0].name).toBe('categories.airConditioning');
      expect(component.categories()[0].children[0].name).toBe('categories.splitAirConditioners');
      expect(component.isLoading()).toBeFalse();
    });

    it('keeps an unknown slug name verbatim (no mapping)', () => {
      const { component, categoryService } = setup();
      categoryService.getCategoryTree.and.returnValue(
        of([makeCategory({ slug: 'mystery-slug', name: 'Mystery' })])
      );

      component.loadCategories();

      expect(component.categories()[0].name).toBe('Mystery');
    });

    it('normalises a missing children array to [] so the template @for is safe', () => {
      const { component, categoryService } = setup();
      categoryService.getCategoryTree.and.returnValue(
        of([{ id: 'x', name: 'foo', slug: 'air-conditioners' } as CategoryTree])
      );

      component.loadCategories();

      expect(component.categories()[0].children).toEqual([]);
    });

    it('falls back to the built-in mock categories when the API errors', () => {
      const { component, categoryService } = setup();
      categoryService.getCategoryTree.and.returnValue(throwError(() => new Error('500')));

      component.loadCategories();

      // The mock fallback always yields the 5 hard-coded top categories.
      expect(component.categories().length).toBe(5);
      expect(component.categories()[0].slug).toBe('air-conditioning');
      expect(component.isLoading()).toBeFalse();
    });
  });

  describe('openMenu', () => {
    it('opens and auto-selects the first category when none is active', () => {
      const { component } = setup();
      component.ngOnInit();

      component.openMenu();

      expect(component.isOpen()).toBeTrue();
      expect(component.activeCategory()?.id).toBe('c-1');
    });

    it('does not clobber an already-active category on re-open', () => {
      const { component } = setup();
      component.ngOnInit();
      const second = component.categories()[1];
      component.setActiveCategory(second);

      component.openMenu();

      expect(component.activeCategory()?.id).toBe(second.id);
    });

    it('opens without selecting anything when there are no categories', () => {
      const { component, categoryService } = setup();
      categoryService.getCategoryTree.and.returnValue(of([]));
      component.loadCategories();

      component.openMenu();

      expect(component.isOpen()).toBeTrue();
      expect(component.activeCategory()).toBeNull();
    });
  });

  describe('closeMenu', () => {
    it('resets open/active/position state and emits menuClosed', () => {
      const { component } = setup();
      component.ngOnInit();
      component.openMenu();
      component.menuLeftPosition.set(42);
      const closedSpy = jasmine.createSpy('closed');
      component.menuClosed.subscribe(closedSpy);

      component.closeMenu();

      expect(component.isOpen()).toBeFalse();
      expect(component.activeCategory()).toBeNull();
      expect(component.menuLeftPosition()).toBeNull();
      expect(closedSpy).toHaveBeenCalledTimes(1);
    });
  });

  describe('toggleMenu', () => {
    it('opens when closed and closes when open', () => {
      const { component } = setup();
      component.ngOnInit();

      component.toggleMenu();
      expect(component.isOpen()).toBeTrue();

      component.toggleMenu();
      expect(component.isOpen()).toBeFalse();
    });
  });

  describe('setActiveCategory', () => {
    it('updates the active category signal', () => {
      const { component } = setup();
      component.ngOnInit();
      const target = component.categories()[1];

      component.setActiveCategory(target);

      expect(component.activeCategory()?.id).toBe(target.id);
    });
  });

  describe('navigateToCategory', () => {
    it('routes to the category page and closes the menu', () => {
      const { component, router } = setup();
      component.ngOnInit();
      component.openMenu();

      component.navigateToCategory(makeCategory({ slug: 'heating-systems' }));

      expect(router.navigate).toHaveBeenCalledOnceWith(['/products/category', 'heating-systems']);
      expect(component.isOpen()).toBeFalse();
    });
  });

  describe('hover open/close with delayed close', () => {
    it('opens immediately on wrapper mouse-enter', () => {
      const { component } = setup();
      component.ngOnInit();

      component.onWrapperMouseEnter();

      expect(component.isOpen()).toBeTrue();
    });

    it('closes after the 100ms grace period on mouse-leave', fakeAsync(() => {
      const { component } = setup();
      component.ngOnInit();
      component.onWrapperMouseEnter();
      expect(component.isOpen()).toBeTrue();

      component.onWrapperMouseLeave();
      // Still open during the grace window.
      expect(component.isOpen()).toBeTrue();

      tick(100);
      expect(component.isOpen()).toBeFalse();
      expect(component.activeCategory()).toBeNull();
    }));

    it('cancels a scheduled close when the pointer re-enters within the grace window', fakeAsync(() => {
      const { component } = setup();
      component.ngOnInit();
      component.onWrapperMouseEnter();
      component.onWrapperMouseLeave();

      // Re-enter before the timeout fires.
      tick(50);
      component.onWrapperMouseEnter();
      tick(100);

      // The original close was cleared, so the menu stays open.
      expect(component.isOpen()).toBeTrue();
    }));
  });

  describe('document click / escape host listeners', () => {
    it('closes when a click lands outside the component', () => {
      const { component, nativeElement } = setup();
      component.ngOnInit();
      component.openMenu();
      nativeElement.contains.and.returnValue(false);

      component.onDocumentClick({ target: document.body } as unknown as MouseEvent);

      expect(component.isOpen()).toBeFalse();
    });

    it('stays open when the click is inside the component', () => {
      const { component, nativeElement } = setup();
      component.ngOnInit();
      component.openMenu();
      nativeElement.contains.and.returnValue(true);

      component.onDocumentClick({ target: document.createElement('div') } as unknown as MouseEvent);

      expect(component.isOpen()).toBeTrue();
    });

    it('closes on Escape', () => {
      const { component } = setup();
      component.ngOnInit();
      component.openMenu();

      component.onEscapeKey();

      expect(component.isOpen()).toBeFalse();
    });
  });

  describe('onWindowResize', () => {
    it('does nothing while the menu is closed (no triggerElement read crash)', () => {
      const { component } = setup();
      component.ngOnInit();

      // Closed: calculateMenuPosition must not be invoked / must be a no-op.
      expect(() => component.onWindowResize()).not.toThrow();
      expect(component.menuLeftPosition()).toBeNull();
    });

    it('keeps the centered (null) position when open without a measurable trigger element', () => {
      const { component } = setup();
      component.ngOnInit();
      component.openMenu();

      component.onWindowResize();

      // triggerElement is null in template-less tests, so positioning short-circuits.
      expect(component.menuLeftPosition()).toBeNull();
    });
  });

  describe('ngAfterViewInit', () => {
    it('captures the trigger element from the host DOM', () => {
      const { component, nativeElement } = setup();
      const fakeTrigger = document.createElement('button');
      nativeElement.querySelector.and.returnValue(fakeTrigger);

      component.ngAfterViewInit();

      expect(nativeElement.querySelector).toHaveBeenCalledWith('.mega-menu-trigger');
    });
  });
});
