import {
  Component,
  inject,
  input,
  output,
  signal,
  effect,
  ElementRef,
  OnDestroy,
  AfterViewInit,
  viewChild,
  PLATFORM_ID
} from '@angular/core';
import { CommonModule, CurrencyPipe, isPlatformBrowser } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import {
  trigger,
  transition,
  style,
  animate,
  query,
  stagger
} from '@angular/animations';
import { CartService } from '../../../core/services/cart.service';
import { CartItem } from '../../../core/models/cart.model';
import { MiniCartItemComponent } from './mini-cart-item.component';

/**
 * Mini-Cart Drawer Component
 * 
 * A slide-out drawer that displays the shopping cart contents.
 * Features:
 * - Slide animation from right side
 * - Click outside or press Escape to close
 * - Focus trap for accessibility
 * - Quantity editing inline
 * - Smooth item removal animations
 */
@Component({
  selector: 'app-mini-cart-drawer',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TranslateModule,
    CurrencyPipe,
    MiniCartItemComponent
  ],
  templateUrl: './mini-cart-drawer.component.html',
  styleUrl: './mini-cart-drawer.component.scss',
  animations: [
    trigger('fadeBackdrop', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ]),
    trigger('slideDrawer', [
      transition(':enter', [
        style({ transform: 'translateX(100%)' }),
        animate('300ms cubic-bezier(0.25, 0.1, 0.25, 1)', style({ transform: 'translateX(0)' }))
      ]),
      transition(':leave', [
        animate('200ms cubic-bezier(0.25, 0.1, 0.25, 1)', style({ transform: 'translateX(100%)' }))
      ])
    ]),
    trigger('listAnimation', [
      transition('* => *', [
        query(':enter', [
          style({ opacity: 0, transform: 'translateY(-10px)' }),
          stagger(50, [
            animate('200ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
          ])
        ], { optional: true })
      ])
    ]),
    trigger('itemRemove', [
      transition(':leave', [
        animate('200ms ease-in', style({ 
          opacity: 0, 
          transform: 'translateX(100%)',
          height: 0,
          marginBottom: 0,
          paddingTop: 0,
          paddingBottom: 0
        }))
      ])
    ])
  ]
})
export class MiniCartDrawerComponent implements AfterViewInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  readonly cartService = inject(CartService);
  
  // Inputs
  isOpen = input<boolean>(false);
  
  // Outputs
  closed = output<void>();
  
  // View children
  drawerPanel = viewChild<ElementRef>('drawerPanel');
  closeButton = viewChild<ElementRef>('closeButton');
  
  // Local state
  private previouslyFocusedElement: HTMLElement | null = null;
  private focusableElements: HTMLElement[] = [];
  private keydownListener: ((e: KeyboardEvent) => void) | null = null;
  
  // Computed values from cart service
  readonly items = this.cartService.items;
  readonly itemCount = this.cartService.itemCount;
  readonly subtotal = this.cartService.subtotal;
  readonly isEmpty = this.cartService.isEmpty;
  readonly isLoading = this.cartService.isLoading;
  
  // Loading states for individual items
  readonly loadingItems = signal<Set<string>>(new Set());
  
  constructor() {
    // React to isOpen changes
    effect(() => {
      if (this.isOpen()) {
        this.onOpen();
      } else {
        this.onClose();
      }
    });
  }
  
  ngAfterViewInit(): void {
    // Set up keyboard listener after view init
    if (isPlatformBrowser(this.platformId)) {
      this.keydownListener = this.handleKeydown.bind(this);
    }
  }
  
  ngOnDestroy(): void {
    this.removeKeyboardListener();
    this.restoreBodyScroll();
  }
  
  /**
   * Close the drawer
   */
  close(): void {
    this.closed.emit();
  }
  
  /**
   * Handle backdrop click
   */
  onBackdropClick(event: MouseEvent): void {
    // Only close if clicking the backdrop itself, not its children
    if (event.target === event.currentTarget) {
      this.close();
    }
  }
  
  /**
   * Update quantity for an item
   */
  updateQuantity(itemId: string, quantity: number): void {
    if (quantity < 1) return;
    
    this.setItemLoading(itemId, true);
    this.cartService.updateQuantity(itemId, quantity).subscribe({
      next: () => this.setItemLoading(itemId, false),
      error: () => this.setItemLoading(itemId, false)
    });
  }
  
  /**
   * Remove an item from cart
   */
  removeItem(itemId: string): void {
    this.setItemLoading(itemId, true);
    this.cartService.removeItem(itemId).subscribe({
      next: () => this.setItemLoading(itemId, false),
      error: () => this.setItemLoading(itemId, false)
    });
  }
  
  /**
   * Track items by ID for animation
   */
  trackByItemId(_index: number, item: CartItem): string {
    return item.id;
  }
  
  /**
   * Check if an item is loading
   */
  isItemLoading(itemId: string): boolean {
    return this.loadingItems().has(itemId);
  }
  
  // --- Private Methods ---
  
  private setItemLoading(itemId: string, loading: boolean): void {
    const current = new Set(this.loadingItems());
    if (loading) {
      current.add(itemId);
    } else {
      current.delete(itemId);
    }
    this.loadingItems.set(current);
  }
  
  private onOpen(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    // Store the currently focused element
    this.previouslyFocusedElement = document.activeElement as HTMLElement;
    
    // Prevent body scroll
    document.body.style.overflow = 'hidden';
    
    // Add keyboard listener
    if (this.keydownListener) {
      document.addEventListener('keydown', this.keydownListener);
    }
    
    // Focus the close button after animation
    setTimeout(() => {
      this.updateFocusableElements();
      this.closeButton()?.nativeElement?.focus();
    }, 100);
  }
  
  private onClose(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    this.restoreBodyScroll();
    this.removeKeyboardListener();
    
    // Restore focus to previously focused element
    if (this.previouslyFocusedElement) {
      this.previouslyFocusedElement.focus();
      this.previouslyFocusedElement = null;
    }
  }
  
  private restoreBodyScroll(): void {
    if (isPlatformBrowser(this.platformId)) {
      document.body.style.overflow = '';
    }
  }
  
  private removeKeyboardListener(): void {
    if (isPlatformBrowser(this.platformId) && this.keydownListener) {
      document.removeEventListener('keydown', this.keydownListener);
    }
  }
  
  private handleKeydown(event: KeyboardEvent): void {
    if (!this.isOpen()) return;
    
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }
    
    if (event.key === 'Tab') {
      this.handleTabKey(event);
    }
  }
  
  private handleTabKey(event: KeyboardEvent): void {
    this.updateFocusableElements();
    
    if (this.focusableElements.length === 0) return;
    
    const firstElement = this.focusableElements[0];
    const lastElement = this.focusableElements[this.focusableElements.length - 1];
    
    if (event.shiftKey) {
      // Shift + Tab
      if (document.activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      }
    } else {
      // Tab
      if (document.activeElement === lastElement) {
        event.preventDefault();
        firstElement.focus();
      }
    }
  }
  
  private updateFocusableElements(): void {
    const panel = this.drawerPanel()?.nativeElement;
    if (!panel) return;
    
    const selector = [
      'button:not([disabled])',
      'a[href]',
      'input:not([disabled])',
      'select:not([disabled])',
      'textarea:not([disabled])',
      '[tabindex]:not([tabindex="-1"])'
    ].join(', ');
    
    this.focusableElements = Array.from(panel.querySelectorAll(selector));
  }
}
