import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BadgeComponent } from './badge.component';

describe('BadgeComponent', () => {
  let component: BadgeComponent;
  let fixture: ComponentFixture<BadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BadgeComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(BadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default variant as default', () => {
    expect(component.variant()).toBe('default');
  });

  it('should have default size as md', () => {
    expect(component.size()).toBe('md');
  });

  it('should apply correct variant class', () => {
    fixture.componentRef.setInput('variant', 'success');
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('span');
    expect(badge.classList.contains('badge-success')).toBeTrue();
  });

  it('should apply correct size class', () => {
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('span');
    expect(badge.classList.contains('badge-lg')).toBeTrue();
  });

  it('should have correct data-testid', () => {
    fixture.componentRef.setInput('testId', 'status-badge');
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('span');
    expect(badge.getAttribute('data-testid')).toBe('status-badge');
  });
});
