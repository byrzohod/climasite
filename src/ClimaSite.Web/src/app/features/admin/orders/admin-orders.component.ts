import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `<div class="admin-orders"><h1>{{ 'admin.orders.title' | translate }}</h1><p>{{ 'common.comingSoon' | translate }}</p></div>`,
  styles: [`.admin-orders { padding: 2rem; } h1 { margin-bottom: 1rem; }`]
})
export class AdminOrdersComponent {}
