import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `<div class="admin-users"><h1>{{ 'admin.users.title' | translate }}</h1><p>{{ 'common.comingSoon' | translate }}</p></div>`,
  styles: [`.admin-users { padding: 2rem; } h1 { margin-bottom: 1rem; }`]
})
export class AdminUsersComponent {}
