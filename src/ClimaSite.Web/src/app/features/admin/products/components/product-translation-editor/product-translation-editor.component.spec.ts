import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component } from '@angular/core';
import { ProductTranslationEditorComponent } from './product-translation-editor.component';
import { AdminTranslationsService, ProductTranslationsDto } from '../../services/admin-translations.service';
import { environment } from '../../../../../../environments/environment';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<Record<string, string>> {
    return of({
      'admin.products.translations.title': 'Product Translations',
      'admin.products.translations.subtitle': 'Manage product content',
      'admin.products.translations.addTranslation': 'Add Translation',
      'admin.products.translations.defaultLanguageNotice': 'English is the default language',
      'admin.products.translations.seoSection': 'SEO Settings',
      'admin.products.translations.confirmDelete': 'Are you sure?',
      'admin.products.name': 'Name',
      'admin.products.shortDescription': 'Short Description',
      'admin.products.description': 'Description',
      'admin.products.metaTitle': 'Meta Title',
      'admin.products.metaDescription': 'Meta Description',
      'common.loading': 'Loading...',
      'common.save': 'Save',
      'common.delete': 'Delete'
    });
  }
}

@Component({
  standalone: true,
  imports: [ProductTranslationEditorComponent],
  template: '<app-product-translation-editor [productId]="productId" />'
})
class TestHostComponent {
  productId = '123e4567-e89b-12d3-a456-426614174000';
}

describe('ProductTranslationEditorComponent', () => {
  let component: ProductTranslationEditorComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let httpMock: HttpTestingController;

  const mockTranslations: ProductTranslationsDto = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    productName: 'Test Product',
    defaultLanguage: 'en',
    translations: [
      {
        id: 'trans-1',
        languageCode: 'bg',
        name: 'Тестов продукт',
        shortDescription: 'Кратко описание',
        description: 'Пълно описание',
        metaTitle: 'Мета заглавие',
        metaDescription: 'Мета описание',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      }
    ],
    availableLanguages: ['de']
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        ProductTranslationEditorComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        AdminTranslationsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    component = fixture.debugElement.children[0].componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    expect(component).toBeTruthy();
  }));

  it('should load translations on init', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    expect(component.translations()).toEqual(mockTranslations);
    expect(component.loading()).toBeFalsy();
  }));

  it('should display language tabs', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    const tabs = fixture.nativeElement.querySelectorAll('.tab');
    expect(tabs.length).toBe(3); // en, bg, de
  }));

  it('should show English as default active language', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    expect(component.activeLanguage()).toBe('en');
  }));

  it('should change active language when tab clicked', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    expect(component.activeLanguage()).toBe('en');

    component.setActiveLanguage('bg');
    expect(component.activeLanguage()).toBe('bg');
  }));

  it('should return correct hasTranslation result', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    expect(component.hasTranslation('en')).toBe(true); // Default language always has translation
    expect(component.hasTranslation('bg')).toBe(true); // Has translation in mockData
    expect(component.hasTranslation('de')).toBe(false); // No translation
  }));

  it('should show loading state initially', fakeAsync(() => {
    fixture.detectChanges();

    expect(component.loading()).toBeTruthy();
    const loadingElement = fixture.nativeElement.querySelector('.loading-state');
    expect(loadingElement).toBeTruthy();

    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    expect(component.loading()).toBeFalsy();
  }));

  it('should show default language notice for English', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    // English is active by default
    const notice = fixture.nativeElement.querySelector('.default-language-notice');
    expect(notice).toBeTruthy();
  }));

  it('should show form for non-English languages', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/translations'));
    req.flush(mockTranslations);
    tick();
    fixture.detectChanges();

    component.setActiveLanguage('bg');
    fixture.detectChanges();

    const formGroups = fixture.nativeElement.querySelectorAll('.form-group');
    expect(formGroups.length).toBeGreaterThan(0);
  }));
});
