import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkeletonTextComponent, SkeletonFontSize } from './skeleton-text.component';

describe('SkeletonTextComponent', () => {
  let component: SkeletonTextComponent;
  let fixture: ComponentFixture<SkeletonTextComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkeletonTextComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SkeletonTextComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Line rendering', () => {
    it('should render a single line by default', () => {
      expect(component.linesArray().length).toBe(1);
      expect(fixture.nativeElement.querySelectorAll('app-skeleton').length).toBe(1);
    });

    it('should render the requested number of lines', () => {
      fixture.componentRef.setInput('lines', 4);
      fixture.detectChanges();

      expect(component.linesArray()).toEqual([0, 1, 2, 3]);
      expect(fixture.nativeElement.querySelectorAll('app-skeleton').length).toBe(4);
    });

    it('should render the skeleton-text wrapper with role="status"', () => {
      const wrapper: HTMLElement = fixture.nativeElement.querySelector('.skeleton-text');
      expect(wrapper).toBeTruthy();
      expect(wrapper.getAttribute('role')).toBe('status');
    });
  });

  describe('getLineWidth', () => {
    it('should return 100% for a single line even when it is the last', () => {
      fixture.componentRef.setInput('lines', 1);
      fixture.detectChanges();
      expect(component.getLineWidth(0, true)).toBe('100%');
    });

    it('should shorten only the last line when multiple lines and no custom widths', () => {
      fixture.componentRef.setInput('lines', 3);
      fixture.detectChanges();

      expect(component.getLineWidth(0, false)).toBe('100%');
      expect(component.getLineWidth(1, false)).toBe('100%');
      expect(component.getLineWidth(2, true)).toBe('60%'); // default lastLineWidth
    });

    it('should honour a custom lastLineWidth', () => {
      fixture.componentRef.setInput('lines', 2);
      fixture.componentRef.setInput('lastLineWidth', '35%');
      fixture.detectChanges();

      expect(component.getLineWidth(1, true)).toBe('35%');
    });

    it('should use a provided custom string width verbatim', () => {
      fixture.componentRef.setInput('lines', 2);
      fixture.componentRef.setInput('widths', ['80%', '120px']);
      fixture.detectChanges();

      expect(component.getLineWidth(0, false)).toBe('80%');
      expect(component.getLineWidth(1, true)).toBe('120px');
    });

    it('should convert a numeric custom width to a percentage', () => {
      fixture.componentRef.setInput('lines', 2);
      fixture.componentRef.setInput('widths', [50, 90]);
      fixture.detectChanges();

      expect(component.getLineWidth(0, false)).toBe('50%');
      expect(component.getLineWidth(1, true)).toBe('90%');
    });

    it('should fall back to default logic for indices beyond the custom widths array', () => {
      fixture.componentRef.setInput('lines', 3);
      fixture.componentRef.setInput('widths', ['100%']); // only covers index 0
      fixture.detectChanges();

      expect(component.getLineWidth(0, false)).toBe('100%'); // from custom widths
      expect(component.getLineWidth(1, false)).toBe('100%'); // default
      expect(component.getLineWidth(2, true)).toBe('60%');   // default last-line width
    });
  });

  describe('lineHeight computed', () => {
    const cases: Array<[SkeletonFontSize, string]> = [
      ['xs', '0.75rem'],
      ['sm', '0.875rem'],
      ['md', '1rem'],
      ['lg', '1.25rem'],
      ['xl', '1.5rem']
    ];

    it('should default to the md height (1rem)', () => {
      expect(component.lineHeight()).toBe('1rem');
    });

    cases.forEach(([fontSize, expectedHeight]) => {
      it(`should map fontSize="${fontSize}" to ${expectedHeight}`, () => {
        fixture.componentRef.setInput('fontSize', fontSize);
        fixture.detectChanges();
        expect(component.lineHeight()).toBe(expectedHeight);
      });
    });
  });

  describe('Accessibility', () => {
    it('should use the default aria-label and sr-only text', () => {
      const wrapper: HTMLElement = fixture.nativeElement.querySelector('.skeleton-text');
      expect(wrapper.getAttribute('aria-label')).toBe('Loading text content...');

      const srOnly: HTMLElement = fixture.nativeElement.querySelector('.sr-only');
      expect(srOnly.textContent?.trim()).toBe('Loading text content...');
    });

    it('should reflect custom aria-label and test id', () => {
      fixture.componentRef.setInput('ariaLabel', 'Loading description');
      fixture.componentRef.setInput('testId', 'desc-loader');
      fixture.detectChanges();

      const wrapper: HTMLElement = fixture.nativeElement.querySelector('.skeleton-text');
      expect(wrapper.getAttribute('aria-label')).toBe('Loading description');
      expect(wrapper.getAttribute('data-testid')).toBe('desc-loader');
    });
  });
});
