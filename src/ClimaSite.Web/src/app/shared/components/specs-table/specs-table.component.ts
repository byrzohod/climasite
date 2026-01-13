import { Component, input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { SpecKeyPipe } from '../../pipes/spec-key.pipe';

export interface SpecificationGroup {
  name: string;
  icon?: string;
  specs: Record<string, unknown>;
}

@Component({
  selector: 'app-specs-table',
  standalone: true,
  imports: [CommonModule, TranslateModule, SpecKeyPipe],
  template: `
    <div class="specs-table" data-testid="specs-table">
      @if (grouped()) {
        @for (group of specGroups(); track group.name) {
          <div class="spec-group">
            <h4 class="group-title">
              @if (group.icon) {
                <span class="icon" [innerHTML]="group.icon"></span>
              }
              {{ group.name }}
            </h4>
            <table class="specs">
              <tbody>
                @for (spec of getSpecs(group.specs); track spec.key) {
                  <tr>
                    <th>{{ spec.key | specKey }}</th>
                    <td [class.highlight]="isHighlighted(spec.key)">
                      {{ formatValue(spec.value) }}
                      @if (getUnit(spec.key)) {
                        <span class="unit">{{ getUnit(spec.key) }}</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      } @else {
        <div class="search-wrapper" *ngIf="searchable()">
          <input
            type="text"
            class="search-input"
            [placeholder]="'common.search' | translate"
            (input)="onSearch($event)"
            data-testid="specs-search" />
        </div>

        <table class="specs">
          <tbody>
            @for (spec of filteredSpecs(); track spec.key) {
              <tr [class.expanded]="expandedKeys().has(spec.key)">
                <th (click)="toggleExpand(spec.key)">
                  {{ spec.key | specKey }}
                  @if (hasLongValue(spec.value)) {
                    <span class="expand-icon">
                      {{ expandedKeys().has(spec.key) ? '-' : '+' }}
                    </span>
                  }
                </th>
                <td [class.highlight]="isHighlighted(spec.key)">
                  @if (hasLongValue(spec.value) && !expandedKeys().has(spec.key)) {
                    {{ truncateValue(spec.value) }}...
                  } @else {
                    {{ formatValue(spec.value) }}
                  }
                  @if (getUnit(spec.key)) {
                    <span class="unit">{{ getUnit(spec.key) }}</span>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>

        @if (showMore() && hasMoreSpecs()) {
          <button
            class="show-more-btn"
            (click)="toggleShowAll()"
            data-testid="show-more-specs">
            @if (showAll()) {
              {{ 'common.showLess' | translate }}
            } @else {
              {{ 'common.showMore' | translate }} ({{ remainingCount() }})
            }
          </button>
        }
      }
    </div>
  `,
  styles: [`
    .specs-table {
      background: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      overflow: hidden;
    }

    .spec-group {
      &:not(:last-child) {
        border-bottom: 1px solid var(--color-border);
      }
    }

    .group-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin: 0;
      padding: 1rem 1.25rem;
      background: var(--color-bg-secondary);
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--color-text-primary);
      border-bottom: 1px solid var(--color-border);

      .icon {
        width: 20px;
        height: 20px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--color-primary);
      }
    }

    .search-wrapper {
      padding: 0.75rem 1rem;
      border-bottom: 1px solid var(--color-border);
    }

    .search-input {
      width: 100%;
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--color-border);
      border-radius: 6px;
      background: var(--color-bg-secondary);
      color: var(--color-text-primary);
      font-size: 0.875rem;

      &:focus {
        outline: none;
        border-color: var(--color-primary);
      }

      &::placeholder {
        color: var(--color-text-secondary);
      }
    }

    .specs {
      width: 100%;
      border-collapse: collapse;

      tr {
        &:not(:last-child) {
          border-bottom: 1px solid var(--color-border);
        }

        &:hover {
          background: var(--color-bg-secondary);
        }

        &.expanded {
          background: rgba(var(--color-primary-rgb), 0.05);
        }
      }

      th, td {
        padding: 0.875rem 1.25rem;
        text-align: left;
        font-size: 0.875rem;
      }

      th {
        width: 40%;
        font-weight: 500;
        color: var(--color-text-secondary);
        cursor: pointer;
        user-select: none;

        .expand-icon {
          float: right;
          color: var(--color-primary);
          font-weight: 700;
        }
      }

      td {
        font-weight: 500;
        color: var(--color-text-primary);

        &.highlight {
          color: var(--color-primary);
          font-weight: 600;
        }

        .unit {
          font-size: 0.75rem;
          color: var(--color-text-secondary);
          margin-left: 0.25rem;
        }
      }
    }

    .show-more-btn {
      width: 100%;
      padding: 0.75rem;
      border: none;
      border-top: 1px solid var(--color-border);
      background: var(--color-bg-secondary);
      color: var(--color-primary);
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s;

      &:hover {
        background: rgba(var(--color-primary-rgb), 0.1);
      }
    }

    @media (max-width: 640px) {
      .specs {
        th, td {
          padding: 0.75rem 1rem;
          font-size: 0.8125rem;
        }

        th {
          width: 45%;
        }
      }
    }
  `]
})
export class SpecsTableComponent {
  specifications = input.required<Record<string, unknown>>();
  groups = input<SpecificationGroup[]>([]);
  highlightKeys = input<string[]>([]);
  searchable = input<boolean>(false);
  showMore = input<boolean>(true);
  initialCount = input<number>(8);
  units = input<Record<string, string>>({});

  grouped = computed(() => this.groups().length > 0);

  specGroups = computed(() => {
    const groups = this.groups();
    if (groups.length === 0) return [];
    return groups;
  });

  private searchTerm = signal('');
  expandedKeys = signal<Set<string>>(new Set());
  showAll = signal(false);

  private allSpecs = computed(() => {
    const specs = this.specifications();
    return Object.entries(specs).map(([key, value]) => ({ key, value }));
  });

  filteredSpecs = computed(() => {
    let specs = this.allSpecs();
    const search = this.searchTerm().toLowerCase();

    if (search) {
      specs = specs.filter(spec =>
        spec.key.toLowerCase().includes(search) ||
        String(spec.value).toLowerCase().includes(search)
      );
    }

    if (!this.showAll() && !search) {
      specs = specs.slice(0, this.initialCount());
    }

    return specs;
  });

  hasMoreSpecs = computed(() => {
    return this.allSpecs().length > this.initialCount();
  });

  remainingCount = computed(() => {
    return this.allSpecs().length - this.initialCount();
  });

  getSpecs(specs: Record<string, unknown>): Array<{ key: string; value: unknown }> {
    return Object.entries(specs).map(([key, value]) => ({ key, value }));
  }

  isHighlighted(key: string): boolean {
    return this.highlightKeys().includes(key);
  }

  getUnit(key: string): string | undefined {
    return this.units()[key];
  }

  formatValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '-';
    }
    if (typeof value === 'boolean') {
      return value ? 'Yes' : 'No';
    }
    if (Array.isArray(value)) {
      return value.join(', ');
    }
    return String(value);
  }

  hasLongValue(value: unknown): boolean {
    return String(value).length > 50;
  }

  truncateValue(value: unknown): string {
    const str = String(value);
    return str.substring(0, 50);
  }

  toggleExpand(key: string): void {
    const current = this.expandedKeys();
    const updated = new Set(current);

    if (updated.has(key)) {
      updated.delete(key);
    } else {
      updated.add(key);
    }

    this.expandedKeys.set(updated);
  }

  toggleShowAll(): void {
    this.showAll.update(v => !v);
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }
}
