import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { CategoryHeaderComponent, CategoryInfo } from './category-header.component';

describe('CategoryHeaderComponent', () => {
  let component: CategoryHeaderComponent;
  let fixture: ComponentFixture<CategoryHeaderComponent>;
  let host: HTMLElement;

  const baseCategory: CategoryInfo = {
    id: 'cat-1',
    name: 'Air Conditioners',
    slug: 'air-conditioners'
  };

  function setCategory(category: CategoryInfo | null): void {
    fixture.componentRef.setInput('category', category);
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CategoryHeaderComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryHeaderComponent);
    component = fixture.componentInstance;
    host = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the root header container', () => {
    expect(host.querySelector('[data-testid="category-header"]')).toBeTruthy();
  });

  it('should render without error when category is null', () => {
    setCategory(null);
    const title = host.querySelector('[data-testid="category-title"]');
    expect(title).toBeTruthy();
    expect(title?.textContent?.trim()).toBe('');
  });

  describe('with a category', () => {
    it('should render the category title', () => {
      setCategory(baseCategory);
      const title = host.querySelector('[data-testid="category-title"]');
      expect(title?.textContent).toContain('Air Conditioners');
    });

    it('should render the description when present', () => {
      setCategory({ ...baseCategory, description: 'Cool your home efficiently' });
      const desc = host.querySelector('[data-testid="category-description"]');
      expect(desc).toBeTruthy();
      expect(desc?.textContent).toContain('Cool your home efficiently');
    });

    it('should not render a description when absent', () => {
      setCategory(baseCategory);
      expect(host.querySelector('[data-testid="category-description"]')).toBeNull();
    });

    it('should render the product count when productCount is defined (including 0)', () => {
      setCategory({ ...baseCategory, productCount: 0 });
      expect(host.querySelector('[data-testid="product-count"]')).toBeTruthy();
    });

    it('should not render the product count when productCount is undefined', () => {
      setCategory(baseCategory);
      expect(host.querySelector('[data-testid="product-count"]')).toBeNull();
    });

    it('should render a parent-category breadcrumb link when provided', () => {
      setCategory({
        ...baseCategory,
        parentCategory: { name: 'Cooling', slug: 'cooling' }
      });
      const links = Array.from(host.querySelectorAll('a.breadcrumb-link'));
      const parentLink = links.find(a => a.textContent?.includes('Cooling'));
      expect(parentLink).toBeTruthy();
      expect((parentLink as HTMLAnchorElement).getAttribute('href'))
        .toBe('/products/category/cooling');
    });
  });

  describe('image vs icon presentation', () => {
    it('should add has-image class and render the background when imageUrl is set', () => {
      setCategory({ ...baseCategory, imageUrl: 'https://cdn.example/ac.jpg' });
      const headerEl = host.querySelector('[data-testid="category-header"]');
      expect(headerEl?.classList.contains('has-image')).toBeTrue();
      expect(host.querySelector('.header-background')).toBeTruthy();
    });

    it('should render the icon when an icon is set and there is no image', () => {
      setCategory({ ...baseCategory, icon: 'fire' });
      const icon = host.querySelector('.category-icon');
      expect(icon).toBeTruthy();
      expect(icon?.textContent).toContain('🔥');
    });

    it('should hide the icon when an image is present', () => {
      setCategory({ ...baseCategory, icon: 'fire', imageUrl: 'https://cdn.example/ac.jpg' });
      expect(host.querySelector('.category-icon')).toBeNull();
    });
  });

  describe('getIconEmoji', () => {
    it('should map known icon names to emoji', () => {
      expect(component.getIconEmoji('snowflake')).toBe('❄️');
      expect(component.getIconEmoji('fire')).toBe('🔥');
      expect(component.getIconEmoji('wind')).toBe('💨');
      expect(component.getIconEmoji('sun')).toBe('☀️');
    });

    it('should fall back to a package emoji for unknown icons', () => {
      expect(component.getIconEmoji('definitely-not-an-icon')).toBe('📦');
    });
  });
});
