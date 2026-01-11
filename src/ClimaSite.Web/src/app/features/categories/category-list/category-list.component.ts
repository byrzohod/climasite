import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="category-list-container">
      <h1>{{ 'nav.categories' | translate }}</h1>
      <p>Category list coming soon...</p>
    </div>
  `,
  styles: [`
    .category-list-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 1rem;
        color: var(--color-text-primary);
      }
    }
  `]
})
export class CategoryListComponent {}
