import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="addresses-container">
      <h1>{{ 'account.addresses.title' | translate }}</h1>
      <p>Address management coming soon...</p>
    </div>
  `,
  styles: [`
    .addresses-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;

      h1 {
        font-size: 2rem;
        margin-bottom: 1rem;
        color: var(--color-text-primary);
      }
    }
  `]
})
export class AddressesComponent {}
