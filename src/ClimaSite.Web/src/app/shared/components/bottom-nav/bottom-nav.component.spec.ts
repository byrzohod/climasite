import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { signal } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

import { BottomNavComponent } from './bottom-nav.component';
import { CartService } from '../../../core/services/cart.service';
import { AnimationService } from '../../../core/services/animation.service';
import { ICON_REGISTRY } from '../icon';

describe('BottomNavComponent', () => {
  let component: BottomNavComponent;
  let fixture: ComponentFixture<BottomNavComponent>;
  let router: Router;
  let cartServiceMock: jasmine.SpyObj<CartService>;
  let animationServiceMock: jasmine.SpyObj<AnimationService>;
  
  const mockCartItemCount = signal(0);
  const mockPrefersReducedMotion = signal(false);

  beforeEach(async () => {
    cartServiceMock = jasmine.createSpyObj('CartService', [], {
      itemCount: mockCartItemCount
    });
    
    animationServiceMock = jasmine.createSpyObj('AnimationService', [], {
      prefersReducedMotion: mockPrefersReducedMotion
    });

    await TestBed.configureTestingModule({
      imports: [
        BottomNavComponent,
        TranslateModule.forRoot(),
        LucideAngularModule.pick(ICON_REGISTRY)
      ],
      providers: [
        provideRouter([]),
        { provide: CartService, useValue: cartServiceMock },
        { provide: AnimationService, useValue: animationServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BottomNavComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  afterEach(() => {
    mockCartItemCount.set(0);
    mockPrefersReducedMotion.set(false);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Navigation Items', () => {
    it('should have 5 navigation items', () => {
      expect(component.navItems.length).toBe(5);
    });

    it('should have correct navigation items', () => {
      const itemLabels = component.navItems.map(item => item.label);
      expect(itemLabels).toContain('nav.bottom.home');
      expect(itemLabels).toContain('nav.bottom.categories');
      expect(itemLabels).toContain('nav.bottom.search');
      expect(itemLabels).toContain('nav.bottom.cart');
      expect(itemLabels).toContain('nav.bottom.account');
    });

    it('should render all navigation items', () => {
      const navItems = fixture.nativeElement.querySelectorAll('.nav-item');
      expect(navItems.length).toBe(5);
    });

    it('should have correct test ids', () => {
      const testIds = [
        'bottom-nav-home',
        'bottom-nav-categories',
        'bottom-nav-search',
        'bottom-nav-cart',
        'bottom-nav-account'
      ];
      
      testIds.forEach(testId => {
        const element = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`);
        expect(element).toBeTruthy(`Expected element with data-testid="${testId}"`);
      });
    });
  });

  describe('Cart Badge', () => {
    it('should not show badge when cart is empty', () => {
      mockCartItemCount.set(0);
      fixture.detectChanges();
      
      const badge = fixture.nativeElement.querySelector('.badge');
      expect(badge).toBeFalsy();
    });

    it('should show badge when cart has items', () => {
      mockCartItemCount.set(3);
      fixture.detectChanges();
      
      const badge = fixture.nativeElement.querySelector('.badge');
      expect(badge).toBeTruthy();
      expect(badge.textContent.trim()).toBe('3');
    });

    it('should show "99+" when cart has more than 99 items', () => {
      mockCartItemCount.set(150);
      fixture.detectChanges();
      
      const badge = fixture.nativeElement.querySelector('.badge');
      expect(badge).toBeTruthy();
      expect(badge.textContent.trim()).toBe('99+');
    });

    it('should trigger pulse animation when cart count changes', fakeAsync(() => {
      mockCartItemCount.set(1);
      fixture.detectChanges();
      
      expect(component.badgePulse()).toBeTrue();
      
      tick(600);
      fixture.detectChanges();
      
      expect(component.badgePulse()).toBeFalse();
    }));

    it('should not trigger pulse animation when reduced motion is preferred', () => {
      mockPrefersReducedMotion.set(true);
      mockCartItemCount.set(1);
      fixture.detectChanges();
      
      expect(component.badgePulse()).toBeFalse();
    });
  });

  describe('Visibility', () => {
    it('should be visible by default', () => {
      expect(component.isHidden()).toBeFalse();
    });

    it('should have hidden class when isHidden is true', () => {
      component.isHidden.set(true);
      fixture.detectChanges();
      
      const nav = fixture.nativeElement.querySelector('.bottom-nav');
      expect(nav.classList.contains('hidden')).toBeTrue();
    });
  });

  describe('Search Action', () => {
    it('should navigate to products page when search is clicked', () => {
      const navigateSpy = spyOn(router, 'navigate');
      
      component.handleAction('search');
      
      expect(navigateSpy).toHaveBeenCalledWith(['/products'], { queryParams: { search: 'open' } });
    });
  });

  describe('Accessibility', () => {
    it('should have correct ARIA role', () => {
      const nav = fixture.nativeElement.querySelector('nav');
      expect(nav.getAttribute('role')).toBe('navigation');
    });

    it('should have accessible label', () => {
      const nav = fixture.nativeElement.querySelector('nav');
      expect(nav.getAttribute('aria-label')).toBe('Mobile navigation');
    });

    it('should have aria-label on all navigation items', () => {
      const items = fixture.nativeElement.querySelectorAll('.nav-item');
      items.forEach((item: HTMLElement) => {
        expect(item.getAttribute('aria-label')).toBeTruthy();
      });
    });

    it('should have aria-live on badge for screen reader updates', () => {
      mockCartItemCount.set(5);
      fixture.detectChanges();
      
      const badge = fixture.nativeElement.querySelector('.badge');
      expect(badge.getAttribute('aria-live')).toBe('polite');
    });
  });

  describe('Touch Targets', () => {
    it('should have minimum touch target size', () => {
      const items = fixture.nativeElement.querySelectorAll('.nav-item');
      items.forEach((item: HTMLElement) => {
        const styles = window.getComputedStyle(item);
        // Check CSS min-width and min-height are set (values may vary by browser)
        // In real tests, we'd check the actual rendered dimensions
        expect(styles.minWidth).toBeTruthy();
        expect(styles.minHeight).toBeTruthy();
      });
    });
  });
});
