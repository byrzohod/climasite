import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FaqComponent, FAQ_ITEM_COUNT } from './faq.component';

describe('FaqComponent', () => {
  let fixture: ComponentFixture<FaqComponent>;
  let component: FaqComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FaqComponent, TranslateModule.forRoot()]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    const dict: Record<string, string> = {
      'faq.title': 'FAQ title',
      'faq.intro': 'FAQ intro'
    };
    for (let i = 0; i < FAQ_ITEM_COUNT; i++) {
      dict[`faq.items.${i}.question`] = `Question ${i}`;
      dict[`faq.items.${i}.answer`] = `Answer ${i}`;
    }
    translate.setTranslation('en', dict);
    translate.use('en');

    fixture = TestBed.createComponent(FaqComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render all FAQ questions', () => {
    const questions = fixture.debugElement.queryAll(By.css('[data-testid^="faq-question-"]'));
    expect(questions.length).toBe(FAQ_ITEM_COUNT);
  });

  it('should have all items collapsed initially', () => {
    expect(component.isOpen(0)).toBeFalse();
    expect(fixture.debugElement.query(By.css('[data-testid="faq-answer-0"]'))).toBeNull();
  });

  it('should expand an item on toggle and set aria-expanded', () => {
    const button = fixture.debugElement.query(By.css('[data-testid="faq-question-0"]'));
    button.nativeElement.click();
    fixture.detectChanges();

    expect(component.isOpen(0)).toBeTrue();
    expect(button.nativeElement.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.debugElement.query(By.css('[data-testid="faq-answer-0"]'))).toBeTruthy();
  });

  it('should collapse an open item when toggled again', () => {
    const button = fixture.debugElement.query(By.css('[data-testid="faq-question-0"]'));
    button.nativeElement.click();
    fixture.detectChanges();
    button.nativeElement.click();
    fixture.detectChanges();

    expect(component.isOpen(0)).toBeFalse();
    expect(fixture.debugElement.query(By.css('[data-testid="faq-answer-0"]'))).toBeNull();
  });

  it('should keep only one item open at a time (accordion behaviour)', () => {
    fixture.debugElement.query(By.css('[data-testid="faq-question-0"]')).nativeElement.click();
    fixture.detectChanges();
    fixture.debugElement.query(By.css('[data-testid="faq-question-1"]')).nativeElement.click();
    fixture.detectChanges();

    expect(component.isOpen(0)).toBeFalse();
    expect(component.isOpen(1)).toBeTrue();
  });
});
