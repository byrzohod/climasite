import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '../header/header.component';
import { FooterComponent } from '../footer/footer.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  template: `
    <div class="layout" data-testid="main-layout">
      <app-header />
      <main class="main-content" data-testid="main-content">
        <ng-content></ng-content>
        <router-outlet />
      </main>
      <app-footer />
    </div>
  `,
  styles: [`
    .layout {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
    }

    .main-content {
      flex: 1;
      background-color: var(--color-bg-primary);
      transition: var(--theme-transition);
    }
  `]
})
export class MainLayoutComponent {}
