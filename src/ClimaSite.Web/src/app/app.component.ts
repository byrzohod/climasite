import { Component, inject } from '@angular/core';
import { MainLayoutComponent } from './core/layout/main-layout/main-layout.component';
import { LanguageService } from './core/services/language.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MainLayoutComponent],
  template: `<app-main-layout />`,
  styles: []
})
export class AppComponent {
  title = 'ClimaSite';

  // Inject LanguageService to ensure translations are initialized early
  private readonly languageService = inject(LanguageService);
}
