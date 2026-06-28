import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';

import { ResourcesComponent } from './resources.component';

describe('ResourcesComponent', () => {
  let component: ResourcesComponent;
  let fixture: ComponentFixture<ResourcesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResourcesComponent, TranslateModule.forRoot(), RouterTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(ResourcesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Static sections', () => {
    it('should render the hero with the title key (no loader => keys passthrough)', () => {
      const h1: HTMLElement = fixture.nativeElement.querySelector('.hero-content h1');
      expect(h1).toBeTruthy();
      expect(h1.textContent?.trim()).toBe('resources.title');
    });

    it('should render the FAQ and contact-CTA sections', () => {
      expect(fixture.nativeElement.querySelector('.faq-section')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.contact-cta')).toBeTruthy();
    });

    it('should link the CTA button to /contact', () => {
      const cta: HTMLAnchorElement = fixture.nativeElement.querySelector('.btn-contact');
      expect(cta).toBeTruthy();
      expect(cta.getAttribute('href')).toBe('/contact');
    });
  });

  describe('Categories & resources', () => {
    it('should expose three categories with nine resources total', () => {
      const categories = component.categories();
      expect(categories.length).toBe(3);
      const totalResources = categories.reduce((sum, c) => sum + c.resources.length, 0);
      expect(totalResources).toBe(9);
    });

    it('should render a card per category', () => {
      const cards = fixture.nativeElement.querySelectorAll('.category-card');
      expect(cards.length).toBe(3);
    });

    it('should render every resource as a resource-item', () => {
      const items = fixture.nativeElement.querySelectorAll('.resource-item');
      expect(items.length).toBe(9);
    });

    it('should mark resources without a url as no-link and href="#"', () => {
      const firstItem: HTMLAnchorElement = fixture.nativeElement.querySelector('.resource-item');
      expect(firstItem.classList.contains('no-link')).toBeTrue();
      expect(firstItem.getAttribute('href')).toBe('#');
    });
  });

  describe('getTypeIcon', () => {
    it('should map each known resource type to its emoji', () => {
      expect(component.getTypeIcon('pdf')).toBe('📄');
      expect(component.getTypeIcon('video')).toBe('🎬');
      expect(component.getTypeIcon('article')).toBe('📰');
      expect(component.getTypeIcon('link')).toBe('🔗');
    });

    it('should fall back to the pdf icon for an unknown type', () => {
      expect(component.getTypeIcon('unknown')).toBe('📄');
    });
  });

  describe('FAQ toggling', () => {
    it('should have five FAQ items and none expanded initially', () => {
      expect(component.faqs().length).toBe(5);
      expect(component.expandedFaq()).toBeNull();
      expect(fixture.nativeElement.querySelector('.faq-answer')).toBeNull();
    });

    it('should expand a FAQ when its question is clicked', () => {
      const firstQuestion: HTMLButtonElement = fixture.nativeElement.querySelector('.faq-question');
      firstQuestion.click();
      fixture.detectChanges();

      expect(component.expandedFaq()).toBe(0);
      expect(fixture.nativeElement.querySelector('.faq-answer')).toBeTruthy();

      const firstItem: HTMLElement = fixture.nativeElement.querySelector('.faq-item');
      expect(firstItem.classList.contains('expanded')).toBeTrue();
      expect(firstQuestion.querySelector('.toggle-icon')?.textContent?.trim()).toBe('−');
    });

    it('should collapse the FAQ when the same question is clicked again', () => {
      component.toggleFaq(0);
      expect(component.expandedFaq()).toBe(0);

      component.toggleFaq(0);
      expect(component.expandedFaq()).toBeNull();
    });

    it('should switch the expanded item when a different question is clicked', () => {
      component.toggleFaq(0);
      expect(component.expandedFaq()).toBe(0);

      component.toggleFaq(2);
      expect(component.expandedFaq()).toBe(2);
    });

    it('should render only one answer at a time', () => {
      component.toggleFaq(1);
      fixture.detectChanges();

      const answers = fixture.nativeElement.querySelectorAll('.faq-answer');
      expect(answers.length).toBe(1);
    });
  });
});
