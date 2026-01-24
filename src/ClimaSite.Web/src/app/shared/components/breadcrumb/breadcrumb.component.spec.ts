import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { BreadcrumbComponent, BreadcrumbItem } from './breadcrumb.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { Component } from '@angular/core';

@Component({
  standalone: true,
  template: ''
})
class DummyComponent {}

describe('BreadcrumbComponent', () => {
  let component: BreadcrumbComponent;
  let fixture: ComponentFixture<BreadcrumbComponent>;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        BreadcrumbComponent,
        TranslateModule.forRoot(),
        RouterTestingModule.withRoutes([
          { path: '', component: DummyComponent },
          { path: 'products', component: DummyComponent },
          { path: 'products/:slug', component: DummyComponent },
          { path: 'categories/:slug', component: DummyComponent }
        ])
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(BreadcrumbComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have nav element with aria-label', () => {
    const nav = fixture.debugElement.query(By.css('nav[aria-label]'));
    expect(nav).toBeTruthy();
  });

  it('should have breadcrumb-list with correct schema.org markup', () => {
    const list = fixture.debugElement.query(By.css('[itemtype="https://schema.org/BreadcrumbList"]'));
    expect(list).toBeTruthy();
  });

  it('should always include home as first item', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const breadcrumbs = component.breadcrumbs();
    expect(breadcrumbs.length).toBeGreaterThanOrEqual(1);
    expect(breadcrumbs[0].url).toBe('/');
  }));

  it('should build breadcrumbs from URL segments', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const breadcrumbs = component.breadcrumbs();
    expect(breadcrumbs.length).toBe(2);
    expect(breadcrumbs[1].url).toBe('/products');
  }));

  it('should mark last item as current page', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const breadcrumbs = component.breadcrumbs();
    const lastItem = breadcrumbs[breadcrumbs.length - 1];
    expect(lastItem.isCurrentPage).toBe(true);
  }));

  it('should use override items when provided', () => {
    const customItems: BreadcrumbItem[] = [
      { label: 'Home', url: '/', isCurrentPage: false },
      { label: 'Custom Page', url: '/custom', isCurrentPage: true }
    ];
    
    fixture.componentRef.setInput('items', customItems);
    fixture.detectChanges();
    
    const breadcrumbs = component.breadcrumbs();
    expect(breadcrumbs).toEqual(customItems);
  });

  it('should render links for non-current items', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const links = fixture.debugElement.queryAll(By.css('.breadcrumb-link'));
    expect(links.length).toBeGreaterThan(0);
  }));

  it('should render span for current page', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const currentSpan = fixture.debugElement.query(By.css('.breadcrumb-current'));
    expect(currentSpan).toBeTruthy();
    expect(currentSpan.nativeElement.getAttribute('aria-current')).toBe('page');
  }));

  it('should render separators between items', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const separators = fixture.debugElement.queryAll(By.css('.breadcrumb-separator'));
    expect(separators.length).toBeGreaterThan(0);
  }));

  it('should have schema.org ListItem markup', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const listItems = fixture.debugElement.queryAll(By.css('[itemtype="https://schema.org/ListItem"]'));
    expect(listItems.length).toBeGreaterThan(0);
  }));

  it('should include position meta tag for SEO', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const positionMeta = fixture.debugElement.queryAll(By.css('meta[itemprop="position"]'));
    expect(positionMeta.length).toBeGreaterThan(0);
  }));

  it('should convert URL segments to readable labels', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    const breadcrumbs = component.breadcrumbs();
    const productsItem = breadcrumbs.find(b => b.url === '/products');
    expect(productsItem?.label).toBe('Products');
  }));

  it('should update breadcrumbs on navigation', fakeAsync(() => {
    router.navigate(['/products']);
    tick();
    fixture.detectChanges();
    
    let breadcrumbs = component.breadcrumbs();
    expect(breadcrumbs.length).toBe(2);
    
    router.navigate(['/categories', 'air-conditioners']);
    tick();
    fixture.detectChanges();
    
    breadcrumbs = component.breadcrumbs();
    expect(breadcrumbs.length).toBe(3);
    expect(breadcrumbs[1].url).toBe('/categories');
    expect(breadcrumbs[2].url).toBe('/categories/air-conditioners');
  }));

  it('should have data-testid attributes', () => {
    const nav = fixture.debugElement.query(By.css('[data-testid="breadcrumb"]'));
    expect(nav).toBeTruthy();
  });

  it('should only show home when on home page', fakeAsync(() => {
    router.navigate(['/']);
    tick();
    fixture.detectChanges();
    
    const breadcrumbs = component.breadcrumbs();
    expect(breadcrumbs.length).toBe(1);
    expect(breadcrumbs[0].url).toBe('/');
    expect(breadcrumbs[0].isCurrentPage).toBe(true);
  }));
});
