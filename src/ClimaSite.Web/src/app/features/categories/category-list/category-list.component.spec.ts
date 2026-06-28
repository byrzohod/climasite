import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { CategoryListComponent } from './category-list.component';

/**
 * Plan-19 B3: CategoryListComponent is a presentational placeholder. We assert it renders
 * without error and surfaces its (translated) heading + coming-soon copy from the i18n keys.
 */
describe('CategoryListComponent', () => {
  let fixture: ComponentFixture<CategoryListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryListComponent, TranslateModule.forRoot()]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      nav: { categories: 'Categories' },
      categories: { comingSoon: 'Coming soon' }
    });
    translate.use('en');

    fixture = TestBed.createComponent(CategoryListComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the translated heading', () => {
    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading?.textContent).toContain('Categories');
  });

  it('renders the translated coming-soon copy', () => {
    const paragraph = fixture.nativeElement.querySelector('.category-list-container p');
    expect(paragraph?.textContent).toContain('Coming soon');
  });
});
