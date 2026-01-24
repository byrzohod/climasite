import { Component, inject, input, output, signal, OnInit, OnDestroy, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService, Toast, ToastType } from './toast.service';

@Component({
  selector: 'app-toast-item',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div
      role="alert"
      [class]="toastClasses()"
      [attr.data-testid]="'toast-' + toast().id"
      (mouseenter)="onMouseEnter()"
      (mouseleave)="onMouseLeave()"
    >
      <div class="toast-content">
        <span class="toast-icon" [class]="toast().type" aria-hidden="true">
          @switch (toast().type) {
            @case ('success') {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon-success">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
              </svg>
            }
            @case ('warning') {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon-warning">
                <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
              </svg>
            }
            @case ('error') {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon-error">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
              </svg>
            }
            @case ('info') {
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="icon-info">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
              </svg>
            }
          }
        </span>
        <p class="toast-message">{{ toast().message }}</p>
      </div>
      @if (toast().dismissible) {
        <button
          type="button"
          class="toast-dismiss"
          (click)="onDismiss()"
          [attr.aria-label]="'common.dismiss' | translate"
          data-testid="toast-dismiss-button"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
          </svg>
        </button>
      }
      @if (toast().duration > 0) {
        <div 
          class="toast-progress"
          [class.paused]="isPaused()"
          [style.animation-duration.ms]="toast().duration"
        ></div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      overflow: hidden;
      transition: transform 0.3s ease-out;
    }

    .toast {
      position: relative;
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      width: 100%;
      max-width: 24rem;
      padding: 1rem;
      background-color: var(--color-bg-card);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      border: 1px solid var(--color-border-primary);
      overflow: hidden;
      animation: toastEnter 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
      transition: transform 0.2s ease;
    }

    .toast:hover {
      transform: scale(1.02);
    }

    .toast.exiting {
      animation: toastExit 0.3s ease-in forwards;
    }

    @keyframes toastEnter {
      from {
        opacity: 0;
        transform: translateX(100%) scale(0.9);
      }
      to {
        opacity: 1;
        transform: translateX(0) scale(1);
      }
    }

    @keyframes toastExit {
      0% {
        opacity: 1;
        transform: translateX(0);
        max-height: 100px;
        margin-bottom: 0;
        padding-top: 1rem;
        padding-bottom: 1rem;
      }
      50% {
        opacity: 0;
        transform: translateX(100%);
        max-height: 100px;
        margin-bottom: 0;
        padding-top: 1rem;
        padding-bottom: 1rem;
      }
      100% {
        opacity: 0;
        transform: translateX(100%);
        max-height: 0;
        margin-bottom: -0.75rem;
        padding-top: 0;
        padding-bottom: 0;
      }
    }

    /* Progress bar */
    .toast-progress {
      position: absolute;
      bottom: 0;
      left: 0;
      height: 3px;
      background: currentColor;
      opacity: 0.3;
      width: 100%;
      animation: progressShrink linear forwards;
    }

    .toast-progress.paused {
      animation-play-state: paused;
    }

    @keyframes progressShrink {
      from { width: 100%; }
      to { width: 0%; }
    }

    .toast-content {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      flex: 1;
      min-width: 0;
    }

    .toast-icon {
      flex-shrink: 0;
      width: 1.25rem;
      height: 1.25rem;
      margin-top: 0.125rem;
    }

    .toast-icon svg {
      width: 100%;
      height: 100%;
    }

    /* Icon animations by type */
    .toast-icon.success svg {
      animation: successPop 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
    }

    .toast-icon.error svg {
      animation: errorShake 0.5s ease-out;
    }

    .toast-icon.warning svg {
      animation: warningPulse 0.6s ease-in-out;
    }

    .toast-icon.info svg {
      animation: infoFadeIn 0.4s ease-out forwards;
    }

    @keyframes successPop {
      0% {
        opacity: 0;
        transform: scale(0.5);
      }
      50% {
        transform: scale(1.2);
      }
      100% {
        opacity: 1;
        transform: scale(1);
      }
    }

    @keyframes errorShake {
      0%, 100% {
        transform: translateX(0);
      }
      10%, 30%, 50%, 70%, 90% {
        transform: translateX(-2px);
      }
      20%, 40%, 60%, 80% {
        transform: translateX(2px);
      }
    }

    @keyframes warningPulse {
      0% {
        opacity: 0.5;
        transform: scale(0.9);
      }
      50% {
        opacity: 1;
        transform: scale(1.1);
      }
      100% {
        opacity: 1;
        transform: scale(1);
      }
    }

    @keyframes infoFadeIn {
      from {
        opacity: 0;
        transform: scale(0.8) rotate(-10deg);
      }
      to {
        opacity: 1;
        transform: scale(1) rotate(0deg);
      }
    }

    .toast-message {
      flex: 1;
      margin: 0;
      font-size: 0.875rem;
      line-height: 1.5;
      color: var(--color-text-primary);
    }

    .toast-dismiss {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 1.5rem;
      height: 1.5rem;
      padding: 0;
      border: none;
      background: transparent;
      cursor: pointer;
      border-radius: var(--radius-md);
      color: var(--color-text-tertiary);
      transition: color 0.15s ease, background-color 0.15s ease;
    }

    .toast-dismiss:hover {
      color: var(--color-text-primary);
      background-color: var(--color-bg-hover);
    }

    .toast-dismiss:focus-visible {
      outline: 2px solid var(--color-ring);
      outline-offset: 2px;
    }

    .toast-dismiss svg {
      width: 1rem;
      height: 1rem;
    }

    /* Type-specific icon colors */
    .toast-success .toast-icon {
      color: var(--color-success);
    }

    .toast-warning .toast-icon {
      color: var(--color-warning);
    }

    .toast-error .toast-icon {
      color: var(--color-error);
    }

    .toast-info .toast-icon {
      color: var(--color-info);
    }

    /* Type-specific progress bar colors */
    .toast-success .toast-progress {
      color: var(--color-success);
    }

    .toast-warning .toast-progress {
      color: var(--color-warning);
    }

    .toast-error .toast-progress {
      color: var(--color-error);
    }

    .toast-info .toast-progress {
      color: var(--color-info);
    }

    /* Reduced motion support */
    @media (prefers-reduced-motion: reduce) {
      .toast {
        animation: none;
        opacity: 1;
        transform: none;
      }

      .toast:hover {
        transform: none;
      }

      .toast.exiting {
        animation: fadeOut 0.15s ease forwards;
      }

      @keyframes fadeOut {
        to {
          opacity: 0;
        }
      }

      .toast-progress {
        animation: none;
        width: 100%;
      }

      .toast-icon svg {
        animation: none !important;
      }
    }
  `]
})
export class ToastItemComponent implements OnInit, OnDestroy {
  readonly toast = input.required<Toast>();
  readonly dismissed = output<string>();

  protected isExiting = signal(false);
  protected isPaused = signal(false);

  private dismissTimeout: ReturnType<typeof setTimeout> | null = null;
  private remainingTime = 0;
  private pauseStartTime = 0;

  ngOnInit(): void {
    // Store the initial duration for pause/resume functionality
    this.remainingTime = this.toast().duration;
    this.startDismissTimer();
  }

  ngOnDestroy(): void {
    if (this.dismissTimeout) {
      clearTimeout(this.dismissTimeout);
    }
  }

  protected toastClasses(): string {
    const base = `toast toast-${this.toast().type}`;
    return this.isExiting() ? `${base} exiting` : base;
  }

  protected onDismiss(): void {
    this.triggerExit();
  }

  protected onMouseEnter(): void {
    if (this.toast().duration > 0 && !this.isExiting()) {
      this.isPaused.set(true);
      this.pauseStartTime = Date.now();
      
      if (this.dismissTimeout) {
        clearTimeout(this.dismissTimeout);
        this.dismissTimeout = null;
      }
    }
  }

  protected onMouseLeave(): void {
    if (this.toast().duration > 0 && !this.isExiting()) {
      this.isPaused.set(false);
      
      // Calculate remaining time
      if (this.pauseStartTime > 0) {
        const pausedFor = Date.now() - this.pauseStartTime;
        this.remainingTime = Math.max(0, this.remainingTime - pausedFor);
      }
      
      // Resume timer with remaining time
      this.startDismissTimer();
    }
  }

  private startDismissTimer(): void {
    if (this.remainingTime > 0) {
      this.dismissTimeout = setTimeout(() => {
        this.triggerExit();
      }, this.remainingTime);
    }
  }

  private triggerExit(): void {
    if (this.isExiting()) return;
    
    this.isExiting.set(true);
    
    // Wait for exit animation to complete before emitting dismiss
    setTimeout(() => {
      this.dismissed.emit(this.toast().id);
    }, 300); // Match the exit animation duration
  }
}

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, ToastItemComponent],
  template: `
    <div 
      class="toast-container" 
      [attr.data-testid]="'toast-container'"
      role="region"
      aria-label="Notifications"
      [attr.aria-live]="'polite'"
    >
      @for (toast of toasts(); track toast.id) {
        <app-toast-item
          [toast]="toast"
          (dismissed)="onDismiss($event)"
        />
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 1100;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      pointer-events: none;
    }

    .toast-container > * {
      pointer-events: auto;
    }

    @media (max-width: 640px) {
      .toast-container {
        top: auto;
        bottom: 1rem;
        left: 1rem;
        right: 1rem;
      }
    }
  `]
})
export class ToastContainerComponent {
  private readonly toastService = inject(ToastService);

  protected readonly toasts = this.toastService.toasts;

  protected onDismiss(id: string): void {
    this.toastService.dismiss(id);
  }
}
