import { 
  Directive, 
  ElementRef, 
  inject, 
  input, 
  OnInit, 
  OnDestroy, 
  PLATFORM_ID,
  Renderer2,
  afterNextRender
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from '../../core/services/animation.service';

export type SplitType = 'chars' | 'words' | 'lines';

/**
 * SplitTextDirective - Splits text for staggered animations
 * 
 * Wraps each character, word, or line in a span for individual animation.
 * 
 * Usage:
 * <h1 appSplitText>Animate each character</h1>
 * <h1 appSplitText="words" [staggerDelay]="100">Animate words</h1>
 * <p appSplitText="lines" [animate]="true">Animate lines on scroll</p>
 */
@Directive({
  selector: '[appSplitText]',
  standalone: true
})
export class SplitTextDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly animationService = inject(AnimationService);
  
  /** Split type: chars, words, or lines */
  splitType = input<SplitType>('chars', { alias: 'appSplitText' });
  
  /** Stagger delay between items (ms) */
  staggerDelay = input<number>(30);
  
  /** Auto-animate on scroll */
  animate = input<boolean>(false);
  
  /** Animation duration (ms) */
  duration = input<number>(600);
  
  /** Initial opacity */
  initialOpacity = input<number>(0);
  
  /** Initial Y offset */
  initialY = input<number>(20);
  
  /** Intersection threshold */
  threshold = input<number>(0.2);
  
  private element!: HTMLElement;
  private originalHTML = '';
  private splitElements: HTMLElement[] = [];
  private observer: IntersectionObserver | null = null;
  private hasAnimated = false;
  
  constructor() {
    // Use afterNextRender to ensure DOM is ready
    afterNextRender(() => {
      if (isPlatformBrowser(this.platformId)) {
        this.initializeSplit();
      }
    });
  }
  
  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.element = this.el.nativeElement;
    this.originalHTML = this.element.innerHTML;
  }
  
  ngOnDestroy(): void {
    if (this.observer) {
      this.observer.disconnect();
    }
    // Restore original HTML
    if (this.element && this.originalHTML) {
      this.element.innerHTML = this.originalHTML;
    }
  }
  
  private initializeSplit(): void {
    if (!this.element) return;
    
    // Skip if reduced motion preferred and animate is enabled
    if (this.animate() && this.animationService.prefersReducedMotion()) {
      return;
    }
    
    this.splitText();
    
    if (this.animate()) {
      this.setupAnimation();
    }
  }
  
  private splitText(): void {
    const text = this.element.textContent || '';
    this.element.innerHTML = '';
    this.splitElements = [];
    
    switch (this.splitType()) {
      case 'chars':
        this.splitByChars(text);
        break;
      case 'words':
        this.splitByWords(text);
        break;
      case 'lines':
        this.splitByLines(text);
        break;
    }
  }
  
  private splitByChars(text: string): void {
    const words = text.split(' ');
    
    words.forEach((word, wordIndex) => {
      const wordWrapper = this.renderer.createElement('span');
      this.renderer.setStyle(wordWrapper, 'display', 'inline-block');
      this.renderer.setStyle(wordWrapper, 'white-space', 'nowrap');
      
      const chars = word.split('');
      chars.forEach((char) => {
        const charSpan = this.createSplitSpan(char);
        this.renderer.appendChild(wordWrapper, charSpan);
        this.splitElements.push(charSpan);
      });
      
      this.renderer.appendChild(this.element, wordWrapper);
      
      // Add space between words
      if (wordIndex < words.length - 1) {
        const space = this.renderer.createText(' ');
        this.renderer.appendChild(this.element, space);
      }
    });
  }
  
  private splitByWords(text: string): void {
    const words = text.split(' ');
    
    words.forEach((word, index) => {
      const wordSpan = this.createSplitSpan(word);
      this.renderer.appendChild(this.element, wordSpan);
      this.splitElements.push(wordSpan);
      
      // Add space between words
      if (index < words.length - 1) {
        const space = this.renderer.createText(' ');
        this.renderer.appendChild(this.element, space);
      }
    });
  }
  
  private splitByLines(text: string): void {
    // For lines, we need to handle it differently
    // We'll wrap each line in a span with overflow hidden parent
    const lines = text.split('\n').filter(line => line.trim());
    
    if (lines.length <= 1) {
      // Single line or no newlines - treat entire text as one line
      const lineSpan = this.createSplitSpan(text);
      const lineWrapper = this.createLineWrapper();
      this.renderer.appendChild(lineWrapper, lineSpan);
      this.renderer.appendChild(this.element, lineWrapper);
      this.splitElements.push(lineSpan);
    } else {
      lines.forEach((line) => {
        const lineSpan = this.createSplitSpan(line);
        const lineWrapper = this.createLineWrapper();
        this.renderer.appendChild(lineWrapper, lineSpan);
        this.renderer.appendChild(this.element, lineWrapper);
        this.splitElements.push(lineSpan);
      });
    }
  }
  
  private createSplitSpan(content: string): HTMLElement {
    const span = this.renderer.createElement('span');
    this.renderer.addClass(span, 'split-item');
    this.renderer.setStyle(span, 'display', 'inline-block');
    
    if (this.animate()) {
      this.renderer.setStyle(span, 'opacity', String(this.initialOpacity()));
      this.renderer.setStyle(span, 'transform', `translateY(${this.initialY()}px)`);
      this.renderer.setStyle(
        span, 
        'transition', 
        `opacity ${this.duration()}ms cubic-bezier(0.16, 1, 0.3, 1), transform ${this.duration()}ms cubic-bezier(0.16, 1, 0.3, 1)`
      );
    }
    
    const text = this.renderer.createText(content);
    this.renderer.appendChild(span, text);
    
    return span;
  }
  
  private createLineWrapper(): HTMLElement {
    const wrapper = this.renderer.createElement('div');
    this.renderer.addClass(wrapper, 'line-wrapper');
    this.renderer.setStyle(wrapper, 'overflow', 'hidden');
    this.renderer.setStyle(wrapper, 'display', 'block');
    return wrapper;
  }
  
  private setupAnimation(): void {
    this.observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting && !this.hasAnimated) {
            this.animateIn();
            this.hasAnimated = true;
            this.observer?.unobserve(this.element);
          }
        });
      },
      {
        threshold: this.threshold(),
        rootMargin: '0px 0px -50px 0px'
      }
    );
    
    this.observer.observe(this.element);
  }
  
  private animateIn(): void {
    this.splitElements.forEach((el, index) => {
      const delay = index * this.staggerDelay();
      
      setTimeout(() => {
        this.renderer.setStyle(el, 'opacity', '1');
        this.renderer.setStyle(el, 'transform', 'translateY(0)');
      }, delay);
    });
  }
  
  /**
   * Manually trigger the animation
   */
  triggerAnimation(): void {
    if (!this.hasAnimated) {
      this.animateIn();
      this.hasAnimated = true;
    }
  }
  
  /**
   * Reset the animation state
   */
  resetAnimation(): void {
    this.hasAnimated = false;
    this.splitElements.forEach((el) => {
      this.renderer.setStyle(el, 'opacity', String(this.initialOpacity()));
      this.renderer.setStyle(el, 'transform', `translateY(${this.initialY()}px)`);
    });
  }
}
