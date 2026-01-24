import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ToastService, Toast } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start with no toasts', () => {
    expect(service.toasts()).toEqual([]);
  });

  it('should add a success toast', () => {
    service.success('Test success message');
    
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].type).toBe('success');
    expect(toasts[0].message).toBe('Test success message');
  });

  it('should add an error toast', () => {
    service.error('Test error message');
    
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].type).toBe('error');
    expect(toasts[0].message).toBe('Test error message');
  });

  it('should add a warning toast', () => {
    service.warning('Test warning message');
    
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].type).toBe('warning');
    expect(toasts[0].message).toBe('Test warning message');
  });

  it('should add an info toast', () => {
    service.info('Test info message');
    
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].type).toBe('info');
    expect(toasts[0].message).toBe('Test info message');
  });

  it('should return toast id when adding a toast', () => {
    const id = service.success('Test message');
    
    expect(id).toBeTruthy();
    expect(typeof id).toBe('string');
    expect(id.startsWith('toast-')).toBe(true);
  });

  it('should dismiss a toast by id', () => {
    const id = service.success('Test message');
    expect(service.toasts().length).toBe(1);
    
    service.dismiss(id);
    expect(service.toasts().length).toBe(0);
  });

  it('should dismiss all toasts', () => {
    service.success('Message 1');
    service.error('Message 2');
    service.warning('Message 3');
    expect(service.toasts().length).toBe(3);
    
    service.dismissAll();
    expect(service.toasts().length).toBe(0);
  });

  it('should set duration on toast', () => {
    // Auto-dismiss is now handled by ToastItemComponent for pause-on-hover functionality
    service.success('Test message', { duration: 1000 });
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].duration).toBe(1000);
  });

  it('should not auto-dismiss when duration is 0', fakeAsync(() => {
    service.success('Test message', { duration: 0 });
    expect(service.toasts().length).toBe(1);
    
    tick(10000);
    expect(service.toasts().length).toBe(1);
  }));

  it('should respect custom duration', () => {
    // Auto-dismiss is now handled by ToastItemComponent for pause-on-hover functionality
    service.success('Test message', { duration: 2000 });
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].duration).toBe(2000);
  });

  it('should limit number of toasts to maxToasts', () => {
    // Add more toasts than max
    for (let i = 0; i < 10; i++) {
      service.success(`Message ${i}`);
    }
    
    expect(service.toasts().length).toBe(service.maxToasts);
  });

  it('should remove oldest toast when max is reached', () => {
    service.success('First message');
    service.success('Second message');
    service.success('Third message');
    service.success('Fourth message');
    service.success('Fifth message');
    service.success('Sixth message'); // This should remove "First message"
    
    const toasts = service.toasts();
    expect(toasts.length).toBe(5);
    expect(toasts.find(t => t.message === 'First message')).toBeFalsy();
    expect(toasts.find(t => t.message === 'Sixth message')).toBeTruthy();
  });

  it('should set dismissible to true by default', () => {
    service.success('Test message');
    
    const toast = service.toasts()[0];
    expect(toast.dismissible).toBe(true);
  });

  it('should respect custom dismissible option', () => {
    service.success('Test message', { dismissible: false });
    
    const toast = service.toasts()[0];
    expect(toast.dismissible).toBe(false);
  });

  it('should give errors a longer default duration', () => {
    // Auto-dismiss is now handled by ToastItemComponent for pause-on-hover functionality
    service.error('Error message');
    
    // Error toasts have 8000ms duration by default vs 5000ms for others
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].duration).toBe(8000);
  });
});
