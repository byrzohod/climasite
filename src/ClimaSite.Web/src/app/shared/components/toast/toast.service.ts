import { Injectable, signal, computed } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
  duration: number;
  dismissible: boolean;
}

export interface ToastOptions {
  duration?: number;
  dismissible?: boolean;
}

const DEFAULT_DURATION = 5000;

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private readonly toastsSignal = signal<Toast[]>([]);
  
  /** Observable list of current toasts */
  readonly toasts = computed(() => this.toastsSignal());

  /** Maximum number of toasts to show at once */
  readonly maxToasts = 5;

  private generateId(): string {
    return `toast-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
  }

  private addToast(type: ToastType, message: string, options: ToastOptions = {}): string {
    const id = this.generateId();
    const duration = options.duration ?? DEFAULT_DURATION;
    const dismissible = options.dismissible ?? true;

    const toast: Toast = {
      id,
      type,
      message,
      duration,
      dismissible
    };

    this.toastsSignal.update(toasts => {
      // Remove oldest toast if we've reached max
      const updated = toasts.length >= this.maxToasts 
        ? toasts.slice(1) 
        : toasts;
      return [...updated, toast];
    });

    // Note: Auto-dismiss is now handled by ToastItemComponent
    // This allows for pause-on-hover functionality

    return id;
  }

  /**
   * Show a success toast
   */
  success(message: string, options?: ToastOptions): string {
    return this.addToast('success', message, options);
  }

  /**
   * Show an error toast
   */
  error(message: string, options?: ToastOptions): string {
    // Errors stay longer by default
    return this.addToast('error', message, { duration: 8000, ...options });
  }

  /**
   * Show a warning toast
   */
  warning(message: string, options?: ToastOptions): string {
    return this.addToast('warning', message, options);
  }

  /**
   * Show an info toast
   */
  info(message: string, options?: ToastOptions): string {
    return this.addToast('info', message, options);
  }

  /**
   * Dismiss a specific toast by ID
   */
  dismiss(id: string): void {
    this.toastsSignal.update(toasts => 
      toasts.filter(toast => toast.id !== id)
    );
  }

  /**
   * Dismiss all toasts
   */
  dismissAll(): void {
    this.toastsSignal.set([]);
  }
}
