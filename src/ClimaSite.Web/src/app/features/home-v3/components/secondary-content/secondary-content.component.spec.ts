import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { SecondaryContentComponent } from './secondary-content.component';

class TestTranslateLoader implements TranslateLoader {
  getTranslation(): Observable<Record<string, string>> {
    return of({
      'homeV3.categories.eyebrow': 'Katalog durchsuchen',
      'homeV3.categories.title': 'Oder nach Kategorie stöbern',
      'homeV3.categories.airConditioners': 'Klimaanlagen',
      'homeV3.categories.airConditionersDesc': 'Split, Multi-Split und mobile Geräte',
      'homeV3.categories.heatPumps': 'Wärmepumpen',
      'homeV3.categories.heatPumpsDesc': 'Heizen und Kühlen, das ganze Jahr',
      'homeV3.categories.dehumidifiers': 'Luftentfeuchter',
      'homeV3.categories.dehumidifiersDesc': 'Feuchtigkeitskontrolle für jeden Raum',
      'homeV3.categories.accessories': 'Zubehör',
      'homeV3.categories.accessoriesDesc': 'Halterungen, Abdeckungen, Smart Controller',
      'homeV3.trust.heading': 'Vertrauenssignale',
      'homeV3.trust.installs': 'Installationen',
      'homeV3.trust.rating': 'Durchschnittsbewertung',
      'homeV3.trust.warranty': 'Garantie',
      'homeV3.trust.delivery': 'Lieferung',
      'homeV3.trust.stats.installsValue': '12.400+',
      'homeV3.trust.stats.ratingValue': '4,8 / 5',
      'homeV3.trust.stats.warrantyValue': '5 Jahre',
      'homeV3.trust.stats.deliveryValue': '72 Std.',
      'homeV3.cta.title': 'Noch unsicher? Sprechen Sie mit einem Menschen.',
      'homeV3.cta.lede': '15-minütige kostenlose Beratung mit einem zertifizierten HVAC-Ingenieur.',
      'homeV3.cta.button': 'Alle Produkte ansehen',
    });
  }
}

describe('SecondaryContentComponent', () => {
  let fixture: ComponentFixture<SecondaryContentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        SecondaryContentComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TestTranslateLoader
          }
        })
      ],
      providers: [provideRouter([])]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setDefaultLang('de');
    translate.use('de');

    fixture = TestBed.createComponent(SecondaryContentComponent);
    fixture.detectChanges();
  });

  it('renders translated category links and CTA copy without leaking keys', () => {
    const airConditionersLink = fixture.debugElement.query(
      By.css('[data-testid="home-v3-cat-air-conditioners"]')
    ).nativeElement as HTMLAnchorElement;
    const cta = fixture.debugElement.query(By.css('[data-testid="home-v3-cta"]')).nativeElement as HTMLAnchorElement;
    const text = fixture.nativeElement.textContent as string;

    expect(airConditionersLink.getAttribute('href')).toBe('/products/category/air-conditioners');
    expect(airConditionersLink.textContent).toContain('Klimaanlagen');
    expect(cta.getAttribute('href')).toBe('/products');
    expect(cta.textContent?.trim()).toBe('Alle Produkte ansehen');
    expect(text).not.toContain('homeV3.');
  });

  it('renders localized trust values and labels', () => {
    const warranty = fixture.debugElement.query(By.css('[data-testid="home-v3-trust-warranty"]')).nativeElement as HTMLElement;
    const delivery = fixture.debugElement.query(By.css('[data-testid="home-v3-trust-delivery"]')).nativeElement as HTMLElement;

    expect(warranty.textContent).toContain('5 Jahre');
    expect(warranty.textContent).toContain('Garantie');
    expect(delivery.textContent).toContain('72 Std.');
    expect(delivery.textContent).toContain('Lieferung');
  });
});
