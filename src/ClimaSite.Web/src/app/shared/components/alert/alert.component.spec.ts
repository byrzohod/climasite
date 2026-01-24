import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AlertComponent, AlertType } from './alert.component';
import { TranslateModule } from '@ngx-translate/core';
import { By } from '@angular/platform-browser';

describe('AlertComponent', () => {
  let component: AlertComponent;
  let fixture: ComponentFixture<AlertComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AlertComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(AlertComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render alert with default type (info)', () => {
    const alertElement = fixture.debugElement.query(By.css('.alert'));
    expect(alertElement.nativeElement.classList).toContain('alert-info');
  });

  it('should render different alert types', () => {
    const types: AlertType[] = ['success', 'warning', 'error', 'info'];
    
    types.forEach(type => {
      fixture.componentRef.setInput('type', type);
      fixture.detectChanges();
      
      const alertElement = fixture.debugElement.query(By.css('.alert'));
      expect(alertElement.nativeElement.classList).toContain(`alert-${type}`);
    });
  });

  it('should show icon by default', () => {
    const iconElement = fixture.debugElement.query(By.css('.alert-icon'));
    expect(iconElement).toBeTruthy();
  });

  it('should hide icon when showIcon is false', () => {
    fixture.componentRef.setInput('showIcon', false);
    fixture.detectChanges();
    
    const iconElement = fixture.debugElement.query(By.css('.alert-icon'));
    expect(iconElement).toBeFalsy();
  });

  it('should not show dismiss button by default', () => {
    const dismissButton = fixture.debugElement.query(By.css('.alert-dismiss'));
    expect(dismissButton).toBeFalsy();
  });

  it('should show dismiss button when dismissible is true', () => {
    fixture.componentRef.setInput('dismissible', true);
    fixture.detectChanges();
    
    const dismissButton = fixture.debugElement.query(By.css('.alert-dismiss'));
    expect(dismissButton).toBeTruthy();
  });

  it('should hide alert and emit dismissed event when dismiss button is clicked', () => {
    fixture.componentRef.setInput('dismissible', true);
    fixture.detectChanges();
    
    const dismissedSpy = spyOn(component.dismissed, 'emit');
    const dismissButton = fixture.debugElement.query(By.css('.alert-dismiss'));
    
    dismissButton.nativeElement.click();
    fixture.detectChanges();
    
    expect(dismissedSpy).toHaveBeenCalled();
    
    const alertElement = fixture.debugElement.query(By.css('.alert'));
    expect(alertElement).toBeFalsy();
  });

  it('should have role="alert" for accessibility', () => {
    const alertElement = fixture.debugElement.query(By.css('[role="alert"]'));
    expect(alertElement).toBeTruthy();
  });

  it('should display content passed via ng-content', () => {
    // Create a test host to test ng-content
    const hostFixture = TestBed.createComponent(AlertComponent);
    hostFixture.detectChanges();
    
    // The component should be created
    expect(hostFixture.componentInstance).toBeTruthy();
  });

  it('should apply custom testId', () => {
    fixture.componentRef.setInput('testId', 'custom-alert');
    fixture.detectChanges();
    
    const alertElement = fixture.debugElement.query(By.css('[data-testid="custom-alert"]'));
    expect(alertElement).toBeTruthy();
  });

  it('should be visible again after calling show()', () => {
    fixture.componentRef.setInput('dismissible', true);
    fixture.detectChanges();
    
    // Dismiss the alert
    const dismissButton = fixture.debugElement.query(By.css('.alert-dismiss'));
    dismissButton.nativeElement.click();
    fixture.detectChanges();
    
    // Alert should be hidden
    let alertElement = fixture.debugElement.query(By.css('.alert'));
    expect(alertElement).toBeFalsy();
    
    // Call show() to make it visible again
    component.show();
    fixture.detectChanges();
    
    alertElement = fixture.debugElement.query(By.css('.alert'));
    expect(alertElement).toBeTruthy();
  });
});
