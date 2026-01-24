import { 
  Component, 
  input, 
  output, 
  signal, 
  effect, 
  inject, 
  ElementRef, 
  PLATFORM_ID,
  OnDestroy,
  AfterViewInit 
} from '@angular/core';
import { CommonModule, isPlatformBrowser, DOCUMENT } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    @if (isOpen()) {
      <div 
        class="modal-backdrop" 
        (click)="onBackdropClick($event)"
        [attr.data-testid]="testId() + '-backdrop'"
      >
        <div
          class="modal-container"
          role="dialog"
          aria-modal="true"
          [attr.aria-labelledby]="titleId"
          (click)="$event.stopPropagation()"
          [attr.data-testid]="testId()"
        >
          <!-- Header -->
          <div class="modal-header" *ngIf="hasHeader()">
            <h2 [id]="titleId" class="modal-title">
              <ng-content select="[modal-header]"></ng-content>
            </h2>
            @if (showCloseButton()) {
              <button
                type="button"
                class="modal-close"
                (click)="close()"
                [attr.aria-label]="'common.close' | translate"
                data-testid="modal-close-button"
              >
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z" />
                </svg>
              </button>
            }
          </div>

          <!-- Body -->
          <div class="modal-body">
            <ng-content select="[modal-body]"></ng-content>
          </div>

          <!-- Footer -->
          <div class="modal-footer" *ngIf="hasFooter()">
            <ng-content select="[modal-footer]"></ng-content>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-backdrop {
                      position: fixed;
                      inset: 0;
                      z-index: 1000;
                      display: flex;
                      align-items: center;
                      justify-content: center;
                      padding: 1rem;
                      background-color: var(--color-bg-overlay);
                      backdrop-filter: blur(4px);
                      animation: backdropFadeIn 0.2s ease-out forwards;
                      
                      &.closing {
                        animation: backdropFadeOut 0.2s ease-in forwards;
                      }
                    }

                    @keyframes backdropFadeIn {
                      from { opacity: 0; }
                      to { opacity: 1; }
                    }
                    
                    @keyframes backdropFadeOut {
                      from { opacity: 1; }
                      to { opacity: 0; }
                    }

                    @keyframes modalEnter {
                      from { 
                        opacity: 0; 
                        transform: scale(0.9) translateY(20px); 
                      }
                      to { 
                        opacity: 1; 
                        transform: scale(1) translateY(0); 
                      }
                    }
                    
                    @keyframes modalExit {
                      from { 
                        opacity: 1; 
                        transform: scale(1) translateY(0); 
                      }
                      to { 
                        opacity: 0; 
                        transform: scale(0.9) translateY(20px); 
                      }
                    }

                    .modal-container {
                      position: relative;
                      width: 100%;
                      max-width: 32rem;
                      max-height: calc(100vh - 2rem);
                      display: flex;
                      flex-direction: column;
                      background-color: var(--color-bg-card);
                      border-radius: var(--radius-xl);
                      box-shadow: var(--shadow-xl);
                      animation: modalEnter 0.3s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
                      overflow: hidden;
                      
                      &.closing {
                        animation: modalExit 0.2s ease-in forwards;
                      }
                    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid var(--color-border-primary);
    }

    .modal-title {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--color-text-primary);
      line-height: 1.4;
    }

    .modal-close {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2rem;
      height: 2rem;
      padding: 0;
      border: none;
      background-color: transparent;
      border-radius: var(--radius-md);
      color: var(--color-text-tertiary);
      cursor: pointer;
      transition: background-color 0.15s ease, color 0.15s ease;
    }

    .modal-close:hover {
      background-color: var(--color-bg-hover);
      color: var(--color-text-primary);
    }

    .modal-close:focus-visible {
      outline: 2px solid var(--color-ring);
      outline-offset: 2px;
    }

    .modal-close svg {
      width: 1.25rem;
      height: 1.25rem;
    }

    .modal-body {
      flex: 1;
      padding: 1.5rem;
      overflow-y: auto;
      color: var(--color-text-secondary);
    }

    .modal-footer {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.75rem;
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--color-border-primary);
      background-color: var(--color-bg-secondary);
    }

    /* Responsive */
                    @media (max-width: 640px) {
                      .modal-backdrop {
                        align-items: flex-end;
                        padding: 0;
                      }

                      .modal-container {
                        max-width: 100%;
                        max-height: 90vh;
                        border-radius: var(--radius-xl) var(--radius-xl) 0 0;
                      }
                    }
                    
                    /* Reduced motion support */
                    @media (prefers-reduced-motion: reduce) {
                      .modal-backdrop,
                      .modal-container {
                        transition: none !important;
                        animation: none !important;
                        transform: none !important;
                      }
                      
                      .modal-backdrop {
                        opacity: 1;
                      }
                    }
                  `]
})
export class ModalComponent implements AfterViewInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);
  private readonly elementRef = inject(ElementRef);

  readonly isOpen = input<boolean>(false);
  readonly closeOnBackdropClick = input<boolean>(true);
  readonly closeOnEscape = input<boolean>(true);
  readonly showCloseButton = input<boolean>(true);
  readonly hasHeader = input<boolean>(true);
  readonly hasFooter = input<boolean>(true);
  readonly testId = input<string>('modal');

  readonly closed = output<void>();

  protected readonly titleId = `modal-title-${Math.random().toString(36).slice(2, 9)}`;

  private focusableElements: HTMLElement[] = [];
  private firstFocusableElement: HTMLElement | null = null;
  private lastFocusableElement: HTMLElement | null = null;
  private previouslyFocusedElement: HTMLElement | null = null;
  private boundKeydownHandler = this.handleKeydown.bind(this);
  private isInitialized = false;

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        this.onOpen();
      } else {
        this.onClose();
      }
    });
  }

  ngAfterViewInit(): void {
    this.isInitialized = true;
    if (this.isOpen()) {
      this.setupFocusTrap();
    }
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  private onOpen(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    // Store currently focused element
    this.previouslyFocusedElement = this.document.activeElement as HTMLElement;

    // Prevent body scroll
    this.document.body.style.overflow = 'hidden';

    // Add keydown listener for Escape and Tab
    this.document.addEventListener('keydown', this.boundKeydownHandler);

    // Setup focus trap after view is ready
    if (this.isInitialized) {
      setTimeout(() => this.setupFocusTrap(), 0);
    }
  }

  private onClose(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    // Restore body scroll
    this.document.body.style.overflow = '';

    // Remove keydown listener
    this.document.removeEventListener('keydown', this.boundKeydownHandler);

    // Restore focus to previously focused element
    if (this.previouslyFocusedElement) {
      this.previouslyFocusedElement.focus();
      this.previouslyFocusedElement = null;
    }
  }

  private cleanup(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.document.body.style.overflow = '';
      this.document.removeEventListener('keydown', this.boundKeydownHandler);
    }
  }

  private setupFocusTrap(): void {
    const modalContainer = this.elementRef.nativeElement.querySelector('.modal-container');
    if (!modalContainer) return;

    const focusableSelectors = [
      'button:not([disabled])',
      '[href]',
      'input:not([disabled])',
      'select:not([disabled])',
      'textarea:not([disabled])',
      '[tabindex]:not([tabindex="-1"])'
    ].join(', ');

    this.focusableElements = Array.from(modalContainer.querySelectorAll(focusableSelectors));
    this.firstFocusableElement = this.focusableElements[0] || null;
    this.lastFocusableElement = this.focusableElements[this.focusableElements.length - 1] || null;

    // Focus first focusable element
    if (this.firstFocusableElement) {
      this.firstFocusableElement.focus();
    }
  }

  private handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' && this.closeOnEscape()) {
      event.preventDefault();
      this.close();
      return;
    }

    if (event.key === 'Tab') {
      this.handleTabKey(event);
    }
  }

  private handleTabKey(event: KeyboardEvent): void {
    if (this.focusableElements.length === 0) {
      event.preventDefault();
      return;
    }

    if (event.shiftKey) {
      // Shift + Tab
      if (this.document.activeElement === this.firstFocusableElement) {
        event.preventDefault();
        this.lastFocusableElement?.focus();
      }
    } else {
      // Tab
      if (this.document.activeElement === this.lastFocusableElement) {
        event.preventDefault();
        this.firstFocusableElement?.focus();
      }
    }
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (this.closeOnBackdropClick()) {
      this.close();
    }
  }

  close(): void {
    this.closed.emit();
  }
}
