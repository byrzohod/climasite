import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { LegalPageComponent, LEGAL_SECTION_COUNTS } from './legal-page.component';

describe('LegalPageComponent', () => {
  let fixture: ComponentFixture<LegalPageComponent>;

  function setup(pageKey: string): void {
    TestBed.configureTestingModule({
      imports: [LegalPageComponent, TranslateModule.forRoot()],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            data: of({ pageKey }),
            snapshot: { data: { pageKey } }
          }
        }
      ]
    });

    const translate = TestBed.inject(TranslateService);
    const count = LEGAL_SECTION_COUNTS[pageKey];
    const dict: Record<string, string> = {
      [`legal.${pageKey}.title`]: `${pageKey} title`,
      [`legal.${pageKey}.intro`]: `${pageKey} intro`,
      [`legal.${pageKey}.lastUpdated`]: '17 June 2026',
      'legal.common.lastUpdatedLabel': 'Last updated',
      'legal.common.placeholderNote': 'Placeholder content'
    };
    for (let i = 0; i < count; i++) {
      dict[`legal.${pageKey}.sections.${i}.heading`] = `Heading ${i}`;
      dict[`legal.${pageKey}.sections.${i}.body`] = `Body para ${i} line one\nBody para ${i} line two`;
    }
    translate.setTranslation('en', dict);
    translate.use('en');

    fixture = TestBed.createComponent(LegalPageComponent);
    fixture.detectChanges();
  }

  it('should render the title and intro for the given pageKey', () => {
    setup('terms');
    const title = fixture.debugElement.query(By.css('[data-testid="legal-title"]'));
    const intro = fixture.debugElement.query(By.css('[data-testid="legal-intro"]'));

    expect(title.nativeElement.textContent.trim()).toBe('terms title');
    expect(intro.nativeElement.textContent.trim()).toBe('terms intro');
  });

  it('should render all sections defined for the page', () => {
    setup('terms');
    const sections = fixture.debugElement.queryAll(By.css('[data-testid^="legal-section-"]'));
    expect(sections.length).toBe(LEGAL_SECTION_COUNTS['terms']);
    expect(sections[0].query(By.css('h2')).nativeElement.textContent.trim()).toBe('Heading 0');
  });

  it('should split multi-line bodies into separate paragraphs', () => {
    setup('terms');
    const firstSection = fixture.debugElement.query(By.css('[data-testid="legal-section-0"]'));
    const paragraphs = firstSection.queryAll(By.css('p'));
    expect(paragraphs.length).toBe(2);
  });

  it('should show the placeholder note on placeholder pages', () => {
    setup('privacy');
    expect(fixture.debugElement.query(By.css('[data-testid="legal-placeholder-note"]'))).toBeTruthy();
  });

  it('should NOT show the placeholder note on the impressum page', () => {
    setup('impressum');
    expect(fixture.debugElement.query(By.css('[data-testid="legal-placeholder-note"]'))).toBeNull();
  });

  it('should render a last-updated line', () => {
    setup('shipping');
    const updated = fixture.debugElement.query(By.css('[data-testid="legal-last-updated"]'));
    expect(updated.nativeElement.textContent).toContain('17 June 2026');
  });
});
