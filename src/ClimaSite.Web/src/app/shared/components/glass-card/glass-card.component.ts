import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type GlassVariant = 'default' | 'light' | 'heavy' | 'primary' | 'accent' | 'warm';

@Component({
  selector: 'app-glass-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      class="glass-card"
      [class]="cardClasses()"
      [class.hoverable]="hoverable()"
      [class.has-glow]="glow()"
      [class.has-border-gradient]="borderGradient()"
    >
      @if (header()) {
        <div class="glass-card-header">
          <ng-content select="[card-header]"></ng-content>
        </div>
      }
      
      <div class="glass-card-body" [class.no-padding]="noPadding()">
        <ng-content></ng-content>
      </div>
      
      @if (footer()) {
        <div class="glass-card-footer">
          <ng-content select="[card-footer]"></ng-content>
        </div>
      }
    </div>
  `,
  styles: [`
    .glass-card {
      position: relative;
      border-radius: var(--radius-xl);
      overflow: hidden;
      transition: 
        transform var(--duration-normal) var(--ease-out-quart),
        box-shadow var(--duration-normal) var(--ease-smooth);
    }

    /* Variants */
    .variant-default {
      background: var(--glass-bg);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid var(--glass-border);
      box-shadow: 
        0 4px 24px -1px var(--glass-shadow),
        inset 0 1px 0 0 rgba(255, 255, 255, 0.1);
    }

    .variant-light {
      background: rgba(255, 255, 255, 0.5);
      backdrop-filter: blur(8px);
      -webkit-backdrop-filter: blur(8px);
      border: 1px solid rgba(255, 255, 255, 0.2);
    }

    .variant-heavy {
      background: var(--glass-bg-heavy);
      backdrop-filter: blur(24px);
      -webkit-backdrop-filter: blur(24px);
      border: 1px solid var(--glass-border);
      box-shadow: 
        0 8px 32px var(--glass-shadow),
        inset 0 1px 0 0 rgba(255, 255, 255, 0.05);
    }

    .variant-primary {
      background: rgba(14, 165, 233, 0.12);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid rgba(14, 165, 233, 0.25);
    }

    .variant-accent {
      background: rgba(6, 182, 212, 0.12);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid rgba(6, 182, 212, 0.25);
    }

    .variant-warm {
      background: rgba(245, 158, 11, 0.12);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid rgba(245, 158, 11, 0.25);
    }

    /* Hoverable */
    .hoverable {
      cursor: pointer;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 
          0 12px 40px -1px var(--glass-shadow),
          inset 0 1px 0 0 rgba(255, 255, 255, 0.15);
      }

      &:active {
        transform: translateY(-2px);
      }
    }

    /* Glow effect */
    .has-glow {
      box-shadow: 
        0 4px 24px -1px var(--glass-shadow),
        0 0 20px var(--glow-primary),
        inset 0 1px 0 0 rgba(255, 255, 255, 0.1);

      &.variant-primary {
        box-shadow: 
          0 4px 24px -1px var(--glass-shadow),
          0 0 30px var(--glow-primary);
      }

      &.variant-accent {
        box-shadow: 
          0 4px 24px -1px var(--glass-shadow),
          0 0 30px var(--glow-accent);
      }

      &.variant-warm {
        box-shadow: 
          0 4px 24px -1px var(--glass-shadow),
          0 0 30px var(--glow-warm);
      }
    }

    /* Border gradient */
    .has-border-gradient {
      border: none;

      &::before {
        content: '';
        position: absolute;
        inset: 0;
        border-radius: inherit;
        padding: 1px;
        background: var(--gradient-aurora);
        -webkit-mask: 
          linear-gradient(#fff 0 0) content-box, 
          linear-gradient(#fff 0 0);
        mask: 
          linear-gradient(#fff 0 0) content-box, 
          linear-gradient(#fff 0 0);
        -webkit-mask-composite: xor;
        mask-composite: exclude;
        pointer-events: none;
      }
    }

    /* Card sections */
    .glass-card-header {
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--glass-border);
    }

    .glass-card-body {
      padding: 1.5rem;

      &.no-padding {
        padding: 0;
      }
    }

    .glass-card-footer {
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--glass-border);
    }

    /* Dark mode adjustments */
    :host-context([data-theme="dark"]),
    :host-context(.dark) {
      .variant-default {
        background: rgba(30, 41, 59, 0.7);
        border-color: rgba(255, 255, 255, 0.08);
      }

      .variant-light {
        background: rgba(30, 41, 59, 0.5);
        border-color: rgba(255, 255, 255, 0.05);
      }

      .variant-heavy {
        background: rgba(30, 41, 59, 0.85);
        border-color: rgba(255, 255, 255, 0.1);
      }

      .variant-primary {
        background: rgba(56, 189, 248, 0.1);
        border-color: rgba(56, 189, 248, 0.2);
      }

      .variant-accent {
        background: rgba(34, 211, 238, 0.1);
        border-color: rgba(34, 211, 238, 0.2);
      }

      .variant-warm {
        background: rgba(251, 191, 36, 0.1);
        border-color: rgba(251, 191, 36, 0.2);
      }
    }

    /* Fallback for browsers without backdrop-filter */
    @supports not (backdrop-filter: blur(1px)) {
      .glass-card {
        background: var(--color-bg-card) !important;
      }

      :host-context([data-theme="dark"]) .glass-card,
      :host-context(.dark) .glass-card {
        background: var(--color-bg-card) !important;
      }
    }
  `]
})
export class GlassCardComponent {
  /** Card variant */
  variant = input<GlassVariant>('default');
  
  /** Enable hover animation */
  hoverable = input<boolean>(false);
  
  /** Add glow effect */
  glow = input<boolean>(false);
  
  /** Add gradient border */
  borderGradient = input<boolean>(false);
  
  /** Has header content */
  header = input<boolean>(false);
  
  /** Has footer content */
  footer = input<boolean>(false);
  
  /** Remove body padding */
  noPadding = input<boolean>(false);

  protected cardClasses(): string {
    return `variant-${this.variant()}`;
  }
}
