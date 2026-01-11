import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      [class]="cardClasses()"
      [attr.data-testid]="testId()"
    >
      @if (hasHeader()) {
        <div class="card-header">
          <ng-content select="[card-header]"></ng-content>
        </div>
      }
      <div class="card-body">
        <ng-content></ng-content>
      </div>
      @if (hasFooter()) {
        <div class="card-footer">
          <ng-content select="[card-footer]"></ng-content>
        </div>
      }
    </div>
  `,
  styles: [`
    .card {
      background-color: var(--color-bg-card);
      border-radius: 0.75rem;
      box-shadow: 0 1px 3px 0 var(--shadow-color), 0 1px 2px -1px var(--shadow-color);
      transition: box-shadow 0.2s ease, transform 0.2s ease;
      overflow: hidden;
    }

    .card-hoverable:hover {
      box-shadow: 0 10px 15px -3px var(--shadow-color-lg), 0 4px 6px -4px var(--shadow-color);
      transform: translateY(-2px);
    }

    .card-clickable {
      cursor: pointer;
    }

    .card-bordered {
      border: 1px solid var(--color-border-primary);
      box-shadow: none;
    }

    .card-header {
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--color-border-primary);
      font-weight: 600;
    }

    .card-body {
      padding: 1.5rem;
    }

    .card-footer {
      padding: 1rem 1.5rem;
      border-top: 1px solid var(--color-border-primary);
      background-color: var(--color-bg-secondary);
    }

    /* Padding variants */
    .card-compact .card-body {
      padding: 1rem;
    }

    .card-compact .card-header,
    .card-compact .card-footer {
      padding: 0.75rem 1rem;
    }

    .card-spacious .card-body {
      padding: 2rem;
    }
  `]
})
export class CardComponent {
  readonly hoverable = input<boolean>(false);
  readonly clickable = input<boolean>(false);
  readonly bordered = input<boolean>(false);
  readonly padding = input<'default' | 'compact' | 'spacious'>('default');
  readonly hasHeader = input<boolean>(false);
  readonly hasFooter = input<boolean>(false);
  readonly testId = input<string>('card');

  protected cardClasses = () => {
    const classes = ['card'];

    if (this.hoverable()) classes.push('card-hoverable');
    if (this.clickable()) classes.push('card-clickable');
    if (this.bordered()) classes.push('card-bordered');
    if (this.padding() !== 'default') classes.push(`card-${this.padding()}`);

    return classes.join(' ');
  };
}
