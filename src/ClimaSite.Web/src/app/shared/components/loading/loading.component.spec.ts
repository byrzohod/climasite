import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoadingComponent } from './loading.component';

describe('LoadingComponent', () => {
  let component: LoadingComponent;
  let fixture: ComponentFixture<LoadingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(LoadingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default size as md', () => {
    expect(component.size()).toBe('md');
  });

  it('should have default mode as inline', () => {
    expect(component.mode()).toBe('inline');
  });

  it('should render spinner', () => {
    const spinner = fixture.nativeElement.querySelector('.loading-spinner');
    expect(spinner).toBeTruthy();
  });

  it('should show text when provided', () => {
    fixture.componentRef.setInput('text', 'Loading...');
    fixture.detectChanges();

    const text = fixture.nativeElement.querySelector('.loading-text');
    expect(text.textContent).toContain('Loading...');
  });

  it('should have correct aria-label', () => {
    fixture.componentRef.setInput('ariaLabel', 'Loading products');
    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('[role="status"]');
    expect(container.getAttribute('aria-label')).toBe('Loading products');
  });

  it('should apply correct size class', () => {
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.loading-spinner');
    expect(spinner.classList.contains('loading-lg')).toBeTrue();
  });

  it('should have correct data-testid', () => {
    fixture.componentRef.setInput('testId', 'page-loading');
    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('[data-testid="page-loading"]');
    expect(container).toBeTruthy();
  });
});
