import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent, EmptyStateVariant } from './empty-state.component';
import { LucideAngularModule, ShoppingCart, Search, Package, Heart, MessageSquare, Inbox } from 'lucide-angular';

describe('EmptyStateComponent', () => {
  let component: EmptyStateComponent;
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        EmptyStateComponent,
        TranslateModule.forRoot(),
        LucideAngularModule.pick({
          ShoppingCart,
          Search,
          Package,
          Heart,
          MessageSquare,
          Inbox
        })
      ],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(EmptyStateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default variant as generic', () => {
    expect(component.variant()).toBe('generic');
  });

  it('should show icon by default', () => {
    expect(component.showIcon()).toBe(true);
  });

  describe('variant icons', () => {
    const variants: { variant: EmptyStateVariant; expectedIcon: string }[] = [
      { variant: 'cart', expectedIcon: 'shopping-cart' },
      { variant: 'search', expectedIcon: 'search' },
      { variant: 'orders', expectedIcon: 'package' },
      { variant: 'wishlist', expectedIcon: 'heart' },
      { variant: 'reviews', expectedIcon: 'message-square' },
      { variant: 'generic', expectedIcon: 'inbox' }
    ];

    variants.forEach(({ variant, expectedIcon }) => {
      it(`should display ${expectedIcon} icon for ${variant} variant`, () => {
        fixture.componentRef.setInput('variant', variant);
        fixture.detectChanges();
        expect(component.iconName()).toBe(expectedIcon);
      });
    });
  });

  it('should display title when provided', () => {
    const title = 'Test Title';
    fixture.componentRef.setInput('title', title);
    fixture.detectChanges();

    const titleElement = fixture.nativeElement.querySelector('[data-testid="empty-state-title"]');
    expect(titleElement).toBeTruthy();
    expect(titleElement.textContent.trim()).toBe(title);
  });

  it('should display description when provided', () => {
    const description = 'Test description text';
    fixture.componentRef.setInput('description', description);
    fixture.detectChanges();

    const descElement = fixture.nativeElement.querySelector('[data-testid="empty-state-description"]');
    expect(descElement).toBeTruthy();
    expect(descElement.textContent.trim()).toBe(description);
  });

  it('should display action button when both label and route are provided', () => {
    fixture.componentRef.setInput('actionLabel', 'Start Shopping');
    fixture.componentRef.setInput('actionRoute', '/products');
    fixture.detectChanges();

    const actionElement = fixture.nativeElement.querySelector('[data-testid="empty-state-action"]');
    expect(actionElement).toBeTruthy();
    expect(actionElement.textContent.trim()).toBe('Start Shopping');
    expect(actionElement.getAttribute('href')).toBe('/products');
  });

  it('should not display action button when label is missing', () => {
    fixture.componentRef.setInput('actionRoute', '/products');
    fixture.detectChanges();

    const actionElement = fixture.nativeElement.querySelector('[data-testid="empty-state-action"]');
    expect(actionElement).toBeFalsy();
  });

  it('should not display action button when route is missing', () => {
    fixture.componentRef.setInput('actionLabel', 'Start Shopping');
    fixture.detectChanges();

    const actionElement = fixture.nativeElement.querySelector('[data-testid="empty-state-action"]');
    expect(actionElement).toBeFalsy();
  });

  it('should hide icon when showIcon is false', () => {
    fixture.componentRef.setInput('showIcon', false);
    fixture.detectChanges();

    const iconContainer = fixture.nativeElement.querySelector('.empty-state__icon');
    expect(iconContainer).toBeFalsy();
  });

  it('should show question mark badge for search variant', () => {
    fixture.componentRef.setInput('variant', 'search');
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('.empty-state__icon-badge');
    expect(badge).toBeTruthy();
    expect(badge.textContent.trim()).toBe('?');
  });

  it('should not show question mark badge for non-search variants', () => {
    fixture.componentRef.setInput('variant', 'cart');
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('.empty-state__icon-badge');
    expect(badge).toBeFalsy();
  });

  it('should apply correct variant class', () => {
    fixture.componentRef.setInput('variant', 'wishlist');
    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('.empty-state');
    expect(container.classList.contains('empty-state--wishlist')).toBe(true);
  });

  it('should have correct accessibility attributes', () => {
    const title = 'Empty Cart';
    fixture.componentRef.setInput('title', title);
    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('[data-testid="empty-state"]');
    expect(container.getAttribute('role')).toBe('status');
    expect(container.getAttribute('aria-label')).toBe(title);
  });
});
