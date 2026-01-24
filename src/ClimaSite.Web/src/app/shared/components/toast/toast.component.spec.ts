import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ToastItemComponent, ToastContainerComponent } from './toast.component';
import { ToastService, Toast } from './toast.service';
import { TranslateModule } from '@ngx-translate/core';
import { By } from '@angular/platform-browser';

describe('ToastItemComponent', () => {
  let component: ToastItemComponent;
  let fixture: ComponentFixture<ToastItemComponent>;

  const mockToast: Toast = {
    id: 'test-toast-1',
    type: 'success',
    message: 'Test success message',
    duration: 5000,
    dismissible: true
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastItemComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(ToastItemComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('toast', mockToast);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the toast message', () => {
    const messageElement = fixture.debugElement.query(By.css('.toast-message'));
    expect(messageElement.nativeElement.textContent).toContain('Test success message');
  });

  it('should apply correct type class', () => {
    const toastElement = fixture.debugElement.query(By.css('.toast'));
    expect(toastElement.nativeElement.classList).toContain('toast-success');
  });

  it('should show dismiss button when dismissible', () => {
    const dismissButton = fixture.debugElement.query(By.css('.toast-dismiss'));
    expect(dismissButton).toBeTruthy();
  });

  it('should hide dismiss button when not dismissible', () => {
    const nonDismissibleToast: Toast = { ...mockToast, dismissible: false };
    fixture.componentRef.setInput('toast', nonDismissibleToast);
    fixture.detectChanges();
    
    const dismissButton = fixture.debugElement.query(By.css('.toast-dismiss'));
    expect(dismissButton).toBeFalsy();
  });

  it('should emit dismissed event when dismiss button is clicked', fakeAsync(() => {
    const dismissedSpy = spyOn(component.dismissed, 'emit');
    const dismissButton = fixture.debugElement.query(By.css('.toast-dismiss'));
    
    dismissButton.nativeElement.click();
    
    // Wait for exit animation delay (300ms)
    tick(350);
    
    expect(dismissedSpy).toHaveBeenCalledWith('test-toast-1');
  }));

  it('should have role="alert" for accessibility', () => {
    const toastElement = fixture.debugElement.query(By.css('[role="alert"]'));
    expect(toastElement).toBeTruthy();
  });

  it('should display correct icon for each type', () => {
    const types: Array<Toast['type']> = ['success', 'error', 'warning', 'info'];
    
    types.forEach(type => {
      const toast: Toast = { ...mockToast, type };
      fixture.componentRef.setInput('toast', toast);
      fixture.detectChanges();
      
      const toastElement = fixture.debugElement.query(By.css('.toast'));
      expect(toastElement.nativeElement.classList).toContain(`toast-${type}`);
      
      const icon = fixture.debugElement.query(By.css('.toast-icon svg'));
      expect(icon).toBeTruthy();
    });
  });
});

describe('ToastContainerComponent', () => {
  let component: ToastContainerComponent;
  let fixture: ComponentFixture<ToastContainerComponent>;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastContainerComponent, TranslateModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(ToastContainerComponent);
    component = fixture.componentInstance;
    toastService = TestBed.inject(ToastService);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have toast-container', () => {
    const container = fixture.debugElement.query(By.css('.toast-container'));
    expect(container).toBeTruthy();
  });

  it('should display toasts from service', () => {
    toastService.success('Message 1');
    toastService.error('Message 2');
    fixture.detectChanges();
    
    const toastItems = fixture.debugElement.queryAll(By.directive(ToastItemComponent));
    expect(toastItems.length).toBe(2);
  });

  it('should dismiss toast when item emits dismissed event', fakeAsync(() => {
    const id = toastService.success('Test message');
    fixture.detectChanges();
    
    expect(toastService.toasts().length).toBe(1);
    
    const toastItem = fixture.debugElement.query(By.directive(ToastItemComponent));
    const dismissButton = toastItem.query(By.css('.toast-dismiss'));
    dismissButton.nativeElement.click();
    
    // Wait for exit animation delay (300ms) in ToastItemComponent
    tick(350);
    fixture.detectChanges();
    
    expect(toastService.toasts().length).toBe(0);
  }));

  it('should update when toasts change', () => {
    toastService.success('Message 1');
    fixture.detectChanges();
    expect(fixture.debugElement.queryAll(By.directive(ToastItemComponent)).length).toBe(1);
    
    toastService.success('Message 2');
    fixture.detectChanges();
    expect(fixture.debugElement.queryAll(By.directive(ToastItemComponent)).length).toBe(2);
    
    toastService.dismissAll();
    fixture.detectChanges();
    expect(fixture.debugElement.queryAll(By.directive(ToastItemComponent)).length).toBe(0);
  });
});
