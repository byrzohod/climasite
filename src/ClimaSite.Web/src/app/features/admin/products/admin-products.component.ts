import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `<div class="admin-products"><h1>Product Management</h1><p>Coming soon...</p></div>`,
  styles: [`.admin-products { padding: 2rem; } h1 { margin-bottom: 1rem; }`]
})
export class AdminProductsComponent {}
