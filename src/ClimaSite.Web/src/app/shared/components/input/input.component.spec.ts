import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InputComponent } from './input.component';
import { FormsModule } from '@angular/forms';

describe('InputComponent', () => {
  let component: InputComponent;
  let fixture: ComponentFixture<InputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InputComponent, FormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(InputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default type as text', () => {
    expect(component.type()).toBe('text');
  });

  it('should render label when provided', () => {
    fixture.componentRef.setInput('label', 'Email');
    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('.input-label');
    expect(label.textContent).toContain('Email');
  });

  it('should show required mark when required is true', () => {
    fixture.componentRef.setInput('label', 'Email');
    fixture.componentRef.setInput('required', true);
    fixture.detectChanges();

    const requiredMark = fixture.nativeElement.querySelector('.required-mark');
    expect(requiredMark).toBeTruthy();
  });

  it('should show error message when error is provided', () => {
    fixture.componentRef.setInput('error', 'Invalid email');
    fixture.detectChanges();

    const errorMessage = fixture.nativeElement.querySelector('.input-error-message');
    expect(errorMessage.textContent).toContain('Invalid email');
  });

  it('should show hint when provided and no error', () => {
    fixture.componentRef.setInput('hint', 'Enter your email address');
    fixture.detectChanges();

    const hint = fixture.nativeElement.querySelector('.input-hint');
    expect(hint.textContent).toContain('Enter your email address');
  });

  it('should not show hint when error is present', () => {
    fixture.componentRef.setInput('hint', 'Enter your email');
    fixture.componentRef.setInput('error', 'Invalid email');
    fixture.detectChanges();

    const hint = fixture.nativeElement.querySelector('.input-hint');
    expect(hint).toBeFalsy();
  });

  it('should have correct data-testid', () => {
    fixture.componentRef.setInput('testId', 'email-input');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input');
    expect(input.getAttribute('data-testid')).toBe('email-input');
  });

  it('should emit valueChange when value changes', () => {
    spyOn(component.valueChange, 'emit');
    const input = fixture.nativeElement.querySelector('input');
    input.value = 'test';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.valueChange.emit).toHaveBeenCalledWith('test');
  });
});
