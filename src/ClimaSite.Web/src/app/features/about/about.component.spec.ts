import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AboutComponent } from './about.component';

const translations: Record<string, string> = {
  'about.title': 'About ClimaSite',
  'about.subtitle': 'Your HVAC partner',
  'about.stats.customers': 'Happy customers',
  'about.stats.products': 'Products',
  'about.stats.years': 'Years',
  'about.stats.support': 'Support',
  'about.story.title': 'Our story',
  'about.story.content': 'Story content',
  'about.mission.title': 'Our mission',
  'about.mission.content': 'Mission content',
  'about.values.title': 'Our values',
  'about.values.quality': 'Quality',
  'about.values.qualityDesc': 'Quality desc',
  'about.values.service': 'Service',
  'about.values.serviceDesc': 'Service desc',
  'about.values.expertise': 'Expertise',
  'about.values.expertiseDesc': 'Expertise desc',
  'about.values.sustainability': 'Sustainability',
  'about.values.sustainabilityDesc': 'Sustainability desc',
  'about.team.title': 'Our team',
  'about.team.subtitle': 'Meet the people',
  'about.cta.title': 'Ready to buy?',
  'about.cta.subtitle': 'Browse the catalogue',
  'common.viewAll': 'View all',
  'nav.products': 'Products'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

describe('AboutComponent', () => {
  let fixture: ComponentFixture<AboutComponent>;
  let component: AboutComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AboutComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [provideRouter([])]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    fixture = TestBed.createComponent(AboutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and render the about page container', () => {
    expect(component).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="about-page"]')).toBeTruthy();
  });

  it('renders the four headline stats with their numbers', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="stat-customers"]')?.textContent).toContain('10,000+');
    expect(el.querySelector('[data-testid="stat-products"]')?.textContent).toContain('500+');
    expect(el.querySelector('[data-testid="stat-years"]')?.textContent).toContain('15+');
    expect(el.querySelector('[data-testid="stat-support"]')?.textContent).toContain('24/7');
  });

  it('renders all four value cards', () => {
    const el = fixture.nativeElement as HTMLElement;
    ['value-quality', 'value-service', 'value-expertise', 'value-sustainability'].forEach(id => {
      expect(el.querySelector(`[data-testid="${id}"]`)).toBeTruthy();
    });
  });

  it('renders the translated heading text', () => {
    const heading = fixture.nativeElement.querySelector('.about-header h1') as HTMLElement;
    expect(heading.textContent).toContain(translations['about.title']);
  });

  it('renders four team members', () => {
    const members = fixture.nativeElement.querySelectorAll('.team-member');
    expect(members.length).toBe(4);
  });

  it('links the CTA to the products route', () => {
    const cta = fixture.nativeElement.querySelector('[data-testid="about-cta"]') as HTMLAnchorElement;
    expect(cta).toBeTruthy();
    expect(cta.getAttribute('href')).toContain('/products');
  });
});
