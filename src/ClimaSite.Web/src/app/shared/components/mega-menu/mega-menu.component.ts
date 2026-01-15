import { Component, inject, signal, OnInit, HostListener, ElementRef, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CategoryService } from '../../../core/services/category.service';
import { CategoryTree } from '../../../core/models/category.model';

@Component({
  selector: 'app-mega-menu',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  template: `
    <div class="mega-menu-wrapper">
      <!-- Trigger Button -->
      <button
        type="button"
        class="mega-menu-trigger"
        (mouseenter)="openMenu()"
        (click)="toggleMenu()"
        [attr.aria-expanded]="isOpen()"
        aria-haspopup="true"
        data-testid="mega-menu-trigger"
      >
        <span>{{ 'nav.products' | translate }}</span>
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="chevron" [class.chevron--open]="isOpen()">
          <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/>
        </svg>
      </button>

      <!-- Mega Menu Dropdown -->
      @if (isOpen()) {
        <div
          class="mega-menu"
          (mouseleave)="closeMenu()"
          data-testid="mega-menu-dropdown"
        >
          <div class="mega-menu-container">
            <!-- Category Navigation -->
            <nav class="category-nav">
              @for (category of categories(); track category.id) {
                <button
                  type="button"
                  class="category-item"
                  [class.category-item--active]="activeCategory()?.id === category.id"
                  (mouseenter)="setActiveCategory(category)"
                  (click)="navigateToCategory(category)"
                  data-testid="category-item"
                >
                  <span class="category-icon">
                    @switch (category.slug) {
                      @case ('air-conditioning') {
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                          <path d="M12 2.25a.75.75 0 01.75.75v2.25a.75.75 0 01-1.5 0V3a.75.75 0 01.75-.75zM7.5 12a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM18.894 6.166a.75.75 0 00-1.06-1.06l-1.591 1.59a.75.75 0 101.06 1.061l1.591-1.59zM21.75 12a.75.75 0 01-.75.75h-2.25a.75.75 0 010-1.5H21a.75.75 0 01.75.75zM17.834 18.894a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 10-1.061 1.06l1.59 1.591zM12 18a.75.75 0 01.75.75V21a.75.75 0 01-1.5 0v-2.25A.75.75 0 0112 18zM7.758 17.303a.75.75 0 00-1.061-1.06l-1.591 1.59a.75.75 0 001.06 1.061l1.591-1.59zM6 12a.75.75 0 01-.75.75H3a.75.75 0 010-1.5h2.25A.75.75 0 016 12zM6.697 7.757a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 00-1.061 1.06l1.59 1.591z"/>
                        </svg>
                      }
                      @case ('heating') {
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                          <path fill-rule="evenodd" d="M12.963 2.286a.75.75 0 00-1.071-.136 9.742 9.742 0 00-3.539 6.177A7.547 7.547 0 016.648 6.61a.75.75 0 00-1.152.082A9 9 0 1015.68 4.534a7.46 7.46 0 01-2.717-2.248zM15.75 14.25a3.75 3.75 0 11-7.313-1.172c.628.465 1.35.81 2.133 1a5.99 5.99 0 011.925-3.545 3.75 3.75 0 013.255 3.717z" clip-rule="evenodd"/>
                        </svg>
                      }
                      @case ('ventilation') {
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                          <path d="M12 2.25c-5.385 0-9.75 4.365-9.75 9.75s4.365 9.75 9.75 9.75 9.75-4.365 9.75-9.75S17.385 2.25 12 2.25zm-.53 5.47a.75.75 0 011.06 0l3 3a.75.75 0 010 1.06l-3 3a.75.75 0 11-1.06-1.06l1.72-1.72H8.25a.75.75 0 010-1.5h4.94l-1.72-1.72a.75.75 0 010-1.06z"/>
                        </svg>
                      }
                      @case ('water-purification') {
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                          <path fill-rule="evenodd" d="M12 2.25c-2.429 0-4.817.178-7.152.521C2.87 3.061 1.5 4.795 1.5 6.741v6.018c0 1.946 1.37 3.68 3.348 3.97a49.007 49.007 0 004.902.604c1.464.079 2.94-.04 4.384-.335a.75.75 0 00.614-.716v-1.87a.75.75 0 01.264-.578l2.114-1.82a.75.75 0 01.995.045l2.12 2.08a.75.75 0 01.22.529v1.38a.75.75 0 00.735.758 49.04 49.04 0 003.304-.229c1.978-.29 3.348-2.024 3.348-3.97V6.741c0-1.946-1.37-3.68-3.348-3.97A49.218 49.218 0 0012 2.25zM8.25 8.625a1.125 1.125 0 100 2.25 1.125 1.125 0 000-2.25zm2.625 1.125a1.125 1.125 0 112.25 0 1.125 1.125 0 01-2.25 0z" clip-rule="evenodd"/>
                        </svg>
                      }
                      @default {
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                          <path fill-rule="evenodd" d="M5.25 2.25a3 3 0 00-3 3v4.318a3 3 0 00.879 2.121l9.58 9.581c.92.92 2.39.92 3.31 0l4.17-4.171a2.343 2.343 0 000-3.311l-9.58-9.581a3 3 0 00-2.122-.879H5.25zM6.375 7.5a1.125 1.125 0 100-2.25 1.125 1.125 0 000 2.25z" clip-rule="evenodd"/>
                        </svg>
                      }
                    }
                  </span>
                  <!-- I18N-002 FIX: Use translate pipe for category names -->
                  <span class="category-name">{{ category.name | translate }}</span>
                  @if (category.children && category.children.length > 0) {
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="category-arrow">
                      <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
                    </svg>
                  }
                </button>
              }
            </nav>

            <!-- Subcategories Panel -->
            @if (activeCategory() && activeCategory()!.children && activeCategory()!.children.length > 0) {
              <div class="subcategories-panel" data-testid="subcategories-panel">
                <div class="panel-header">
                  <!-- I18N-002 FIX: Use translate pipe for panel title -->
                  <h3 class="panel-title">{{ activeCategory()!.name | translate }}</h3>
                  <!-- NAV-001 FIX: Use route-based navigation instead of query params -->
                  <a
                    [routerLink]="['/products/category', activeCategory()!.slug]"
                    class="view-all-link"
                    (click)="closeMenu()"
                  >
                    {{ 'common.viewAll' | translate }}
                    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638l-3.96-3.67a.75.75 0 111.02-1.16l5.25 4.875a.75.75 0 010 1.16l-5.25 4.875a.75.75 0 11-1.02-1.16l3.96-3.67H3.75A.75.75 0 013 10z" clip-rule="evenodd"/>
                    </svg>
                  </a>
                </div>
                <div class="subcategories-grid">
                  @for (subcat of activeCategory()!.children; track subcat.id) {
                    <!-- NAV-001 FIX: Use route-based navigation -->
                    <!-- I18N-002 FIX: Use translate pipe for subcategory names -->
                    <a
                      [routerLink]="['/products/category', subcat.slug]"
                      class="subcategory-link"
                      (click)="closeMenu()"
                      data-testid="subcategory-link"
                    >
                      {{ subcat.name | translate }}
                    </a>
                  }
                </div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .mega-menu-wrapper {
      position: relative;
    }

    .mega-menu-trigger {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.75rem 1rem;
      background: none;
      border: none;
      color: var(--color-text-secondary);
      font-weight: 500;
      font-size: 0.875rem;
      cursor: pointer;
      border-bottom: 2px solid transparent;
      transition: color 0.2s, border-color 0.2s;

      &:hover {
        color: var(--color-text-primary);
      }

      &[aria-expanded="true"] {
        color: var(--color-primary);
        border-bottom-color: var(--color-primary);
      }
    }

    .chevron {
      width: 1rem;
      height: 1rem;
      transition: transform 0.2s;

      &--open {
        transform: rotate(180deg);
      }
    }

    .mega-menu {
      position: absolute;
      top: 100%;
      left: 50%;
      transform: translateX(-50%);
      min-width: 800px;
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border-primary);
      border-radius: 0.75rem;
      box-shadow: 0 20px 50px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      animation: menuSlideDown 0.2s ease;
    }

    @keyframes menuSlideDown {
      from {
        opacity: 0;
        transform: translateX(-50%) translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateX(-50%) translateY(0);
      }
    }

    .mega-menu-container {
      display: flex;
      min-height: 400px;
    }

    .category-nav {
      width: 280px;
      padding: 0.75rem;
      background: var(--color-bg-secondary);
      border-right: 1px solid var(--color-border-primary);
      border-radius: 0.75rem 0 0 0.75rem;
    }

    .category-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      width: 100%;
      padding: 0.75rem 1rem;
      background: none;
      border: none;
      border-radius: 0.5rem;
      color: var(--color-text-primary);
      font-size: 0.875rem;
      font-weight: 500;
      text-align: left;
      cursor: pointer;
      transition: background-color 0.15s, color 0.15s;

      &:hover {
        background-color: var(--color-bg-hover);
      }

      &--active {
        background-color: var(--color-primary-light);
        color: var(--color-primary);

        .category-icon {
          color: var(--color-primary);
        }
      }
    }

    .category-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2rem;
      height: 2rem;
      border-radius: 0.5rem;
      background: var(--color-bg-tertiary);
      color: var(--color-text-tertiary);

      svg {
        width: 1.125rem;
        height: 1.125rem;
      }
    }

    .category-name {
      flex: 1;
    }

    .category-arrow {
      width: 1rem;
      height: 1rem;
      color: var(--color-text-tertiary);
    }

    .subcategories-panel {
      flex: 1;
      padding: 1.5rem;
    }

    .panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1.5rem;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--color-border-primary);
    }

    .panel-title {
      font-size: 1.125rem;
      font-weight: 600;
      color: var(--color-text-primary);
      margin: 0;
    }

    .view-all-link {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      color: var(--color-primary);
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      transition: color 0.15s;

      &:hover {
        color: var(--color-primary-hover);
      }

      svg {
        width: 1rem;
        height: 1rem;
      }
    }

    .subcategories-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.5rem;
    }

    .subcategory-link {
      padding: 0.625rem 0.875rem;
      color: var(--color-text-secondary);
      font-size: 0.875rem;
      text-decoration: none;
      border-radius: 0.375rem;
      transition: background-color 0.15s, color 0.15s;

      &:hover {
        background-color: var(--color-bg-hover);
        color: var(--color-text-primary);
      }
    }

    /* Responsive */
    @media (max-width: 1024px) {
      .mega-menu {
        min-width: 600px;
      }

      .subcategories-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    @media (max-width: 768px) {
      .mega-menu {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        min-width: unset;
        border-radius: 0;
        transform: none;
        animation: none;
      }

      .mega-menu-container {
        flex-direction: column;
        min-height: 100vh;
      }

      .category-nav {
        width: 100%;
        border-right: none;
        border-bottom: 1px solid var(--color-border-primary);
        border-radius: 0;
      }

      .subcategories-panel {
        flex: 1;
        overflow-y: auto;
      }

      .subcategories-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class MegaMenuComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly elementRef = inject(ElementRef);

  @Output() menuClosed = new EventEmitter<void>();

  readonly categories = signal<CategoryTree[]>([]);
  readonly activeCategory = signal<CategoryTree | null>(null);
  readonly isOpen = signal(false);
  readonly isLoading = signal(false);

  private closeTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadCategories();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.closeMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closeMenu();
  }

  loadCategories(): void {
    // Load mock categories for now until the API is available
    this.categories.set(this.getMockCategories());

    // Uncomment when API is ready:
    // this.isLoading.set(true);
    // this.categoryService.getCategoryTree().subscribe({
    //   next: (data) => {
    //     this.categories.set(data);
    //     this.isLoading.set(false);
    //   },
    //   error: () => this.isLoading.set(false)
    // });
  }

  openMenu(): void {
    if (this.closeTimeout) {
      clearTimeout(this.closeTimeout);
      this.closeTimeout = null;
    }
    this.isOpen.set(true);
    if (this.categories().length > 0 && !this.activeCategory()) {
      this.activeCategory.set(this.categories()[0]);
    }
  }

  closeMenu(): void {
    this.closeTimeout = setTimeout(() => {
      this.isOpen.set(false);
      this.activeCategory.set(null);
      this.menuClosed.emit();
    }, 200);
  }

  toggleMenu(): void {
    if (this.isOpen()) {
      this.closeMenu();
    } else {
      this.openMenu();
    }
  }

  setActiveCategory(category: CategoryTree): void {
    this.activeCategory.set(category);
  }

  navigateToCategory(category: CategoryTree): void {
    // Navigation is handled by routerLink in template
    this.closeMenu();
  }

  // I18N-002 FIX: Use translation keys instead of hardcoded strings
  private getMockCategories(): CategoryTree[] {
    return [
      {
        id: '1',
        name: 'categories.airConditioning',
        slug: 'air-conditioning',
        children: [
          { id: '1-1', name: 'categories.wallMountedAc', slug: 'wall-mounted-ac', children: [] },
          { id: '1-2', name: 'categories.multiSplit', slug: 'multi-split', children: [] },
          { id: '1-3', name: 'categories.floorAc', slug: 'floor-ac', children: [] },
          { id: '1-4', name: 'categories.cassetteAc', slug: 'cassette-ac', children: [] },
          { id: '1-5', name: 'categories.ductedAc', slug: 'ducted-ac', children: [] },
          { id: '1-6', name: 'categories.heatPumps', slug: 'heat-pumps', children: [] },
          { id: '1-7', name: 'categories.airPurifiers', slug: 'air-purifiers', children: [] },
          { id: '1-8', name: 'categories.dehumidifiers', slug: 'dehumidifiers', children: [] },
          { id: '1-9', name: 'categories.vrvVrf', slug: 'vrv-vrf', children: [] },
        ]
      },
      {
        id: '2',
        name: 'categories.heatingSystems',
        slug: 'heating',
        children: [
          { id: '2-1', name: 'categories.electricHeaters', slug: 'electric-heaters', children: [] },
          { id: '2-2', name: 'categories.gasHeaters', slug: 'gas-heaters', children: [] },
          { id: '2-3', name: 'categories.infraredHeaters', slug: 'infrared-heaters', children: [] },
          { id: '2-4', name: 'categories.convectors', slug: 'convectors', children: [] },
          { id: '2-5', name: 'categories.radiators', slug: 'radiators', children: [] },
          { id: '2-6', name: 'categories.underfloorHeating', slug: 'underfloor-heating', children: [] },
        ]
      },
      {
        id: '3',
        name: 'categories.ventilation',
        slug: 'ventilation',
        children: [
          { id: '3-1', name: 'categories.exhaustFans', slug: 'exhaust-fans', children: [] },
          { id: '3-2', name: 'categories.ductFans', slug: 'duct-fans', children: [] },
          { id: '3-3', name: 'categories.recoveryVentilators', slug: 'recovery-ventilators', children: [] },
          { id: '3-4', name: 'categories.airCurtains', slug: 'air-curtains', children: [] },
          { id: '3-5', name: 'categories.industrialFans', slug: 'industrial-fans', children: [] },
        ]
      },
      {
        id: '4',
        name: 'categories.waterPurification',
        slug: 'water-purification',
        children: [
          { id: '4-1', name: 'categories.waterFilters', slug: 'water-filters', children: [] },
          { id: '4-2', name: 'categories.reverseOsmosis', slug: 'reverse-osmosis', children: [] },
          { id: '4-3', name: 'categories.uvSterilizers', slug: 'uv-sterilizers', children: [] },
          { id: '4-4', name: 'categories.waterSofteners', slug: 'water-softeners', children: [] },
          { id: '4-5', name: 'categories.consumables', slug: 'water-consumables', children: [] },
        ]
      },
      {
        id: '5',
        name: 'categories.accessories',
        slug: 'accessories',
        children: [
          { id: '5-1', name: 'categories.remoteControls', slug: 'remote-controls', children: [] },
          { id: '5-2', name: 'categories.installationKits', slug: 'installation-kits', children: [] },
          { id: '5-3', name: 'categories.copperPipes', slug: 'copper-pipes', children: [] },
          { id: '5-4', name: 'categories.refrigerants', slug: 'refrigerants', children: [] },
          { id: '5-5', name: 'categories.bracketsMounts', slug: 'brackets-mounts', children: [] },
          { id: '5-6', name: 'categories.drainPumps', slug: 'drain-pumps', children: [] },
        ]
      }
    ];
  }
}
