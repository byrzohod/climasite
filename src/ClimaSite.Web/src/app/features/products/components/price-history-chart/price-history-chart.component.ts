import { Component, input, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { PriceHistoryService, ProductPriceHistory, PricePoint } from '../../services/price-history.service';

interface ChartPoint {
  x: number;
  y: number;
  price: number;
  date: Date;
  reason: string;
}

@Component({
  selector: 'app-price-history-chart',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="price-history-section">
      <div class="section-header">
        <h3>{{ 'products.priceHistory.title' | translate }}</h3>
        <div class="period-selector">
          <button
            type="button"
            [class.active]="selectedPeriod() === 30"
            (click)="changePeriod(30)"
          >
            {{ 'products.priceHistory.days30' | translate }}
          </button>
          <button
            type="button"
            [class.active]="selectedPeriod() === 60"
            (click)="changePeriod(60)"
          >
            {{ 'products.priceHistory.days60' | translate }}
          </button>
          <button
            type="button"
            [class.active]="selectedPeriod() === 90"
            (click)="changePeriod(90)"
          >
            {{ 'products.priceHistory.days90' | translate }}
          </button>
        </div>
      </div>

      @if (loading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <span>{{ 'common.loading' | translate }}</span>
        </div>
      }

      @if (!loading() && priceHistory()) {
        <div class="price-stats">
          <div class="stat">
            <span class="stat-label">{{ 'products.priceHistory.current' | translate }}</span>
            <span class="stat-value current">{{ priceHistory()!.currentPrice | number:'1.2-2' }} EUR</span>
          </div>
          <div class="stat">
            <span class="stat-label">{{ 'products.priceHistory.lowest' | translate }}</span>
            <span class="stat-value lowest">{{ priceHistory()!.lowestPrice | number:'1.2-2' }} EUR</span>
          </div>
          <div class="stat">
            <span class="stat-label">{{ 'products.priceHistory.highest' | translate }}</span>
            <span class="stat-value highest">{{ priceHistory()!.highestPrice | number:'1.2-2' }} EUR</span>
          </div>
          <div class="stat">
            <span class="stat-label">{{ 'products.priceHistory.average' | translate }}</span>
            <span class="stat-value average">{{ priceHistory()!.averagePrice | number:'1.2-2' }} EUR</span>
          </div>
        </div>

        @if (chartPoints().length > 1) {
          <div class="chart-container">
            <svg
              class="price-chart"
              [attr.viewBox]="'0 0 ' + chartWidth + ' ' + chartHeight"
              preserveAspectRatio="xMidYMid meet"
            >
              <!-- Grid lines -->
              @for (line of gridLines(); track line.y) {
                <line
                  [attr.x1]="chartPadding.left"
                  [attr.y1]="line.y"
                  [attr.x2]="chartWidth - chartPadding.right"
                  [attr.y2]="line.y"
                  class="grid-line"
                />
                <text
                  [attr.x]="chartPadding.left - 10"
                  [attr.y]="line.y + 4"
                  class="y-label"
                >
                  {{ line.value | number:'1.0-0' }}
                </text>
              }

              <!-- X-axis labels -->
              @for (label of xLabels(); track label.x) {
                <text
                  [attr.x]="label.x"
                  [attr.y]="chartHeight - 5"
                  class="x-label"
                >
                  {{ label.label }}
                </text>
              }

              <!-- Price line -->
              <polyline
                [attr.points]="linePath()"
                class="price-line"
                fill="none"
              />

              <!-- Area under the line -->
              <polygon
                [attr.points]="areaPath()"
                class="price-area"
              />

              <!-- Data points -->
              @for (point of chartPoints(); track point.x) {
                <circle
                  [attr.cx]="point.x"
                  [attr.cy]="point.y"
                  r="4"
                  class="data-point"
                  (mouseenter)="showTooltip(point)"
                  (mouseleave)="hideTooltip()"
                />
              }
            </svg>

            @if (tooltipPoint()) {
              <div
                class="tooltip"
                [style.left.px]="tooltipPoint()!.x"
                [style.top.px]="tooltipPoint()!.y - 50"
              >
                <div class="tooltip-date">{{ tooltipPoint()!.date | date:'mediumDate' }}</div>
                <div class="tooltip-price">{{ tooltipPoint()!.price | number:'1.2-2' }} EUR</div>
                <div class="tooltip-reason">{{ tooltipPoint()!.reason }}</div>
              </div>
            }
          </div>
        } @else {
          <div class="no-data-message">
            {{ 'products.priceHistory.noHistory' | translate }}
          </div>
        }
      }

      @if (!loading() && !priceHistory()) {
        <div class="no-data-message">
          {{ 'products.priceHistory.notAvailable' | translate }}
        </div>
      }
    </div>
  `,
  styles: [`
    .price-history-section {
      background: var(--surface-color);
      border-radius: 12px;
      padding: 1.5rem;
      margin-top: 2rem;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;
      flex-wrap: wrap;
      gap: 1rem;
    }

    .section-header h3 {
      margin: 0;
      font-size: 1.25rem;
      color: var(--text-primary);
    }

    .period-selector {
      display: flex;
      gap: 0.5rem;
    }

    .period-selector button {
      padding: 0.5rem 1rem;
      border: 1px solid var(--border-color);
      background: transparent;
      border-radius: 6px;
      cursor: pointer;
      font-size: 0.875rem;
      color: var(--text-secondary);
      transition: all 0.2s;
    }

    .period-selector button:hover {
      border-color: var(--primary-color);
      color: var(--primary-color);
    }

    .period-selector button.active {
      background: var(--primary-color);
      border-color: var(--primary-color);
      color: white;
    }

    .loading-state {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 2rem;
      color: var(--text-secondary);
    }

    .spinner {
      width: 20px;
      height: 20px;
      border: 2px solid var(--border-color);
      border-top-color: var(--primary-color);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .price-stats {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    @media (max-width: 768px) {
      .price-stats {
        grid-template-columns: repeat(2, 1fr);
      }
    }

    .stat {
      text-align: center;
      padding: 1rem;
      background: var(--bg-color);
      border-radius: 8px;
    }

    .stat-label {
      display: block;
      font-size: 0.75rem;
      color: var(--text-secondary);
      margin-bottom: 0.25rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .stat-value {
      font-size: 1.25rem;
      font-weight: 600;
    }

    .stat-value.current {
      color: var(--primary-color);
    }

    .stat-value.lowest {
      color: var(--success-color, #22c55e);
    }

    .stat-value.highest {
      color: var(--error-color, #ef4444);
    }

    .stat-value.average {
      color: var(--text-primary);
    }

    .chart-container {
      position: relative;
      width: 100%;
      aspect-ratio: 2 / 1;
      min-height: 200px;
    }

    .price-chart {
      width: 100%;
      height: 100%;
    }

    .grid-line {
      stroke: var(--border-color);
      stroke-width: 1;
      stroke-dasharray: 4 4;
    }

    .y-label, .x-label {
      font-size: 10px;
      fill: var(--text-secondary);
    }

    .y-label {
      text-anchor: end;
    }

    .x-label {
      text-anchor: middle;
    }

    .price-line {
      stroke: var(--primary-color);
      stroke-width: 2;
      stroke-linecap: round;
      stroke-linejoin: round;
    }

    .price-area {
      fill: var(--primary-color);
      opacity: 0.1;
    }

    .data-point {
      fill: var(--primary-color);
      cursor: pointer;
      transition: r 0.2s;
    }

    .data-point:hover {
      r: 6;
    }

    .tooltip {
      position: absolute;
      background: var(--surface-color);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 0.75rem;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      pointer-events: none;
      z-index: 10;
      transform: translateX(-50%);
      min-width: 120px;
    }

    .tooltip-date {
      font-size: 0.75rem;
      color: var(--text-secondary);
      margin-bottom: 0.25rem;
    }

    .tooltip-price {
      font-size: 1rem;
      font-weight: 600;
      color: var(--primary-color);
    }

    .tooltip-reason {
      font-size: 0.75rem;
      color: var(--text-secondary);
      margin-top: 0.25rem;
    }

    .no-data-message {
      text-align: center;
      padding: 2rem;
      color: var(--text-secondary);
    }
  `]
})
export class PriceHistoryChartComponent {
  productId = input.required<string>();

  priceHistory = signal<ProductPriceHistory | null>(null);
  loading = signal(true);
  selectedPeriod = signal(90);
  tooltipPoint = signal<ChartPoint | null>(null);

  readonly chartWidth = 600;
  readonly chartHeight = 300;
  readonly chartPadding = { top: 20, right: 20, bottom: 30, left: 60 };

  constructor(private priceHistoryService: PriceHistoryService) {
    effect(() => {
      const id = this.productId();
      const period = this.selectedPeriod();
      if (id) {
        this.loadPriceHistory(id, period);
      }
    });
  }

  chartPoints = computed<ChartPoint[]>(() => {
    const history = this.priceHistory();
    if (!history || history.pricePoints.length === 0) return [];

    const points = history.pricePoints;
    const prices = points.map(p => p.price);
    const minPrice = Math.min(...prices) * 0.95;
    const maxPrice = Math.max(...prices) * 1.05;

    const plotWidth = this.chartWidth - this.chartPadding.left - this.chartPadding.right;
    const plotHeight = this.chartHeight - this.chartPadding.top - this.chartPadding.bottom;

    return points.map((point, index) => {
      const x = this.chartPadding.left + (index / (points.length - 1 || 1)) * plotWidth;
      const y = this.chartPadding.top + (1 - (point.price - minPrice) / (maxPrice - minPrice || 1)) * plotHeight;

      return {
        x,
        y,
        price: point.price,
        date: new Date(point.date),
        reason: point.reason
      };
    });
  });

  linePath = computed(() => {
    return this.chartPoints()
      .map(p => `${p.x},${p.y}`)
      .join(' ');
  });

  areaPath = computed(() => {
    const points = this.chartPoints();
    if (points.length === 0) return '';

    const bottomY = this.chartHeight - this.chartPadding.bottom;
    const firstX = points[0].x;
    const lastX = points[points.length - 1].x;

    const linePart = points.map(p => `${p.x},${p.y}`).join(' ');
    return `${firstX},${bottomY} ${linePart} ${lastX},${bottomY}`;
  });

  gridLines = computed(() => {
    const history = this.priceHistory();
    if (!history || history.pricePoints.length === 0) return [];

    const prices = history.pricePoints.map(p => p.price);
    const minPrice = Math.min(...prices) * 0.95;
    const maxPrice = Math.max(...prices) * 1.05;

    const plotHeight = this.chartHeight - this.chartPadding.top - this.chartPadding.bottom;
    const lines: { y: number; value: number }[] = [];

    const step = (maxPrice - minPrice) / 4;
    for (let i = 0; i <= 4; i++) {
      const value = minPrice + step * i;
      const y = this.chartPadding.top + (1 - (value - minPrice) / (maxPrice - minPrice || 1)) * plotHeight;
      lines.push({ y, value });
    }

    return lines;
  });

  xLabels = computed(() => {
    const points = this.chartPoints();
    if (points.length < 2) return [];

    const labels: { x: number; label: string }[] = [];
    const numLabels = Math.min(5, points.length);
    const step = Math.floor(points.length / (numLabels - 1));

    for (let i = 0; i < points.length; i += step) {
      const point = points[i];
      labels.push({
        x: point.x,
        label: point.date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
      });
    }

    // Ensure last point is included
    const lastPoint = points[points.length - 1];
    if (labels[labels.length - 1].x !== lastPoint.x) {
      labels.push({
        x: lastPoint.x,
        label: lastPoint.date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
      });
    }

    return labels;
  });

  private loadPriceHistory(productId: string, daysBack: number): void {
    this.loading.set(true);
    this.priceHistoryService.getPriceHistory(productId, daysBack).subscribe({
      next: (history) => {
        this.priceHistory.set(history);
        this.loading.set(false);
      },
      error: () => {
        this.priceHistory.set(null);
        this.loading.set(false);
      }
    });
  }

  changePeriod(days: number): void {
    this.selectedPeriod.set(days);
  }

  showTooltip(point: ChartPoint): void {
    this.tooltipPoint.set(point);
  }

  hideTooltip(): void {
    this.tooltipPoint.set(null);
  }
}
