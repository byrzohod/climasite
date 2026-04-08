import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import type { ClimateZone, RoomType } from '../../models/home-v3.models';

/**
 * Canvas 2D axonometric room preview.
 *
 * Intentionally implemented as Canvas 2D rather than WebGL — this is the
 * production reduced-motion / no-WebGL fallback (per ADR 002) and also
 * happens to work fine as the primary renderer. A Three.js upgrade can
 * replace this component later without changing its public inputs.
 *
 * Inputs drive a small deterministic scene: a box room scaled to the
 * area, a furniture set keyed off room type, an AC unit on the wall,
 * and an animated airflow hint. Zone C tints the airflow warm to
 * signal heating mode.
 */
@Component({
  selector: 'app-room-preview',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="preview-frame" role="img" [attr.aria-label]="ariaLabel()">
      <canvas #canvas width="900" height="620"></canvas>
      <div class="stat-chips">
        <div class="chip"><span class="chip-label">Outside</span><span class="chip-value">{{ outsideC() }}°C</span></div>
        <div class="chip"><span class="chip-label">Inside</span><span class="chip-value">{{ insideC() }}°C</span></div>
        <div class="chip"><span class="chip-label">Power</span><span class="chip-value">{{ watts() }} W</span></div>
      </div>
    </div>
  `,
  styleUrl: './room-preview.component.scss',
})
export class RoomPreviewComponent implements OnDestroy {
  readonly area = input.required<number>();
  readonly roomType = input.required<RoomType>();
  readonly zone = input.required<ClimateZone>();
  readonly outsideC = input.required<number>();
  readonly insideC = input.required<number>();
  readonly watts = input.required<number>();
  readonly theme = input<'dark' | 'light'>('dark');

  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly hostRef = inject(ElementRef);
  private rafId: number | null = null;
  private startTime = performance.now();
  private reducedMotion = typeof matchMedia !== 'undefined' && matchMedia('(prefers-reduced-motion: reduce)').matches;

  constructor() {
    effect(() => {
      // Re-read all inputs so effect re-runs on any input change.
      this.area();
      this.roomType();
      this.zone();
      this.theme();
      // Schedule a render after the canvas is in the DOM.
      queueMicrotask(() => this.render());
    });
  }

  ariaLabel(): string {
    return `Axonometric preview of a ${this.area()} square metre ${this.roomType()} room in climate zone ${this.zone()}`;
  }

  ngOnDestroy(): void {
    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
  }

  private render(): void {
    const canvas = this.canvasRef().nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // DPR handling for crispness on retina.
    const dpr = Math.min(window.devicePixelRatio || 1, 2);
    const cssW = canvas.clientWidth || 900;
    const cssH = canvas.clientHeight || 620;
    if (canvas.width !== cssW * dpr || canvas.height !== cssH * dpr) {
      canvas.width = cssW * dpr;
      canvas.height = cssH * dpr;
    }
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    const paint = () => {
      const now = performance.now();
      const t = (now - this.startTime) / 1000;
      this.draw(ctx, cssW, cssH, t);
      if (!this.reducedMotion) {
        this.rafId = requestAnimationFrame(paint);
      }
    };

    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
    paint();
  }

  /** Axonometric projection (ISO-ish). */
  private project(x: number, y: number, z: number, cx: number, cy: number, scale: number): [number, number] {
    const a = 0.52;
    const sx = cx + (x - z) * Math.cos(a) * scale;
    const sy = cy + ((x + z) * Math.sin(a) - y) * scale;
    return [sx, sy];
  }

  private draw(ctx: CanvasRenderingContext2D, w: number, h: number, t: number): void {
    const isDark = this.theme() === 'dark';
    // Background.
    ctx.fillStyle = isDark ? '#0b0f14' : '#f6f8fb';
    ctx.fillRect(0, 0, w, h);

    // Soft radial highlight behind the room.
    const grd = ctx.createRadialGradient(w * 0.55, h * 0.45, 40, w * 0.55, h * 0.45, Math.max(w, h) * 0.7);
    grd.addColorStop(0, isDark ? 'rgba(76,194,255,0.08)' : 'rgba(20,136,209,0.10)');
    grd.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = grd;
    ctx.fillRect(0, 0, w, h);

    // Room box sized by area (loose mapping so 10m² and 120m² both read).
    const a = this.area();
    const side = 3 + Math.sqrt(a) * 0.55;
    const height = 2.6;
    const cx = w * 0.52;
    const cy = h * 0.62;
    const scale = Math.min(42, 260 / side);

    const floorCol = isDark ? '#12202f' : '#e2e8f0';
    const wallBack = isDark ? '#1a2b3d' : '#f8fafc';
    const wallSide = isDark ? '#13202e' : '#ecf1f7';

    // Floor.
    this.polygon(ctx, [
      this.project(0, 0, 0, cx, cy, scale),
      this.project(side, 0, 0, cx, cy, scale),
      this.project(side, 0, side, cx, cy, scale),
      this.project(0, 0, side, cx, cy, scale),
    ], floorCol, 'rgba(0,0,0,0.25)');

    // Back wall.
    this.polygon(ctx, [
      this.project(0, 0, 0, cx, cy, scale),
      this.project(side, 0, 0, cx, cy, scale),
      this.project(side, height, 0, cx, cy, scale),
      this.project(0, height, 0, cx, cy, scale),
    ], wallBack, 'rgba(0,0,0,0.3)');

    // Side wall.
    this.polygon(ctx, [
      this.project(side, 0, 0, cx, cy, scale),
      this.project(side, 0, side, cx, cy, scale),
      this.project(side, height, side, cx, cy, scale),
      this.project(side, height, 0, cx, cy, scale),
    ], wallSide, 'rgba(0,0,0,0.3)');

    // Furniture.
    for (const f of this.furniture(side)) {
      this.box(ctx, f.wx, f.wy, f.wz, f.px, f.py, f.pz, f.top, f.side, f.front, cx, cy, scale);
    }

    // AC unit on back wall, upper-right.
    const acX = side - 1.8;
    const acY = height - 0.55;
    const acZ = 0.05;
    this.box(ctx, 1.2, 0.35, 0.25, acX, acY, acZ, '#e6eef8', '#b9c6d6', '#d0dbe8', cx, cy, scale);
    // LED stripe.
    ctx.fillStyle = this.zone() === 'C' ? '#ff8a4c' : '#4cc2ff';
    const [lx, ly] = this.project(acX + 0.15, acY + 0.06, acZ + 0.26, cx, cy, scale);
    ctx.fillRect(lx, ly, 18, 2);

    // Airflow lines.
    const flowCol = this.zone() === 'C' ? 'rgba(255,138,76,0.75)' : 'rgba(76,194,255,0.75)';
    ctx.strokeStyle = flowCol;
    ctx.lineWidth = 2;
    for (let i = 0; i < 5; i++) {
      const phase = this.reducedMotion ? 0 : (t * 0.8 + i * 0.25) % 1;
      const startX = acX + 0.6;
      const startY = acY - 0.1;
      const startZ = acZ + 0.3;
      const endX = startX - 1.6;
      const endY = startY - 0.9 - i * 0.15;
      const endZ = startZ + 1.4 + i * 0.2;
      const mx = startX + (endX - startX) * phase;
      const my = startY + (endY - startY) * phase;
      const mz = startZ + (endZ - startZ) * phase;
      const [sx, sy] = this.project(startX, startY, startZ, cx, cy, scale);
      const [ex, ey] = this.project(mx, my, mz, cx, cy, scale);
      ctx.globalAlpha = 1 - phase;
      ctx.beginPath();
      ctx.moveTo(sx, sy);
      ctx.lineTo(ex, ey);
      ctx.stroke();
    }
    ctx.globalAlpha = 1;
  }

  private polygon(ctx: CanvasRenderingContext2D, pts: [number, number][], fill: string, stroke: string): void {
    ctx.beginPath();
    pts.forEach(([x, y], i) => (i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y)));
    ctx.closePath();
    ctx.fillStyle = fill;
    ctx.fill();
    ctx.strokeStyle = stroke;
    ctx.lineWidth = 1;
    ctx.stroke();
  }

  private box(
    ctx: CanvasRenderingContext2D,
    wx: number, wy: number, wz: number,
    px: number, py: number, pz: number,
    top: string, side: string, front: string,
    cx: number, cy: number, scale: number,
  ): void {
    // Top face.
    this.polygon(ctx, [
      this.project(px,      py + wy, pz,      cx, cy, scale),
      this.project(px + wx, py + wy, pz,      cx, cy, scale),
      this.project(px + wx, py + wy, pz + wz, cx, cy, scale),
      this.project(px,      py + wy, pz + wz, cx, cy, scale),
    ], top, 'rgba(0,0,0,0.25)');
    // Front face.
    this.polygon(ctx, [
      this.project(px,      py,      pz + wz, cx, cy, scale),
      this.project(px + wx, py,      pz + wz, cx, cy, scale),
      this.project(px + wx, py + wy, pz + wz, cx, cy, scale),
      this.project(px,      py + wy, pz + wz, cx, cy, scale),
    ], front, 'rgba(0,0,0,0.25)');
    // Right face.
    this.polygon(ctx, [
      this.project(px + wx, py,      pz,      cx, cy, scale),
      this.project(px + wx, py,      pz + wz, cx, cy, scale),
      this.project(px + wx, py + wy, pz + wz, cx, cy, scale),
      this.project(px + wx, py + wy, pz,      cx, cy, scale),
    ], side, 'rgba(0,0,0,0.25)');
  }

  /** Furniture recipes per room type. Positions are in metres inside the room box. */
  private furniture(side: number): Array<{
    wx: number; wy: number; wz: number;
    px: number; py: number; pz: number;
    top: string; side: string; front: string;
  }> {
    const palette = {
      wood: { top: '#8b6b48', side: '#6d5138', front: '#7a5d42' },
      fabric: { top: '#4b6584', side: '#334764', front: '#3d5373' },
      white: { top: '#e2e8f0', side: '#b5bfcc', front: '#cbd3df' },
      dark: { top: '#2d3a4b', side: '#1e2a38', front: '#26323f' },
    };
    const type = this.roomType();
    const mid = side / 2;
    if (type === 'living') {
      return [
        { wx: 2.2, wy: 0.5, wz: 0.9, px: 0.6, py: 0, pz: mid + 0.3, ...palette.fabric },
        { wx: 1.0, wy: 0.4, wz: 0.5, px: 1.2, py: 0, pz: mid - 0.5, ...palette.wood },
        { wx: 0.9, wy: 1.6, wz: 0.35, px: side - 1.1, py: 0, pz: 0.2, ...palette.wood },
        { wx: 0.5, wy: 1.1, wz: 0.5, px: 0.2, py: 0, pz: mid, ...palette.dark },
      ];
    }
    if (type === 'bedroom') {
      return [
        { wx: 1.8, wy: 0.55, wz: 2.1, px: 0.6, py: 0, pz: mid - 0.2, ...palette.fabric },
        { wx: 0.45, wy: 0.55, wz: 0.45, px: 0.1, py: 0, pz: mid - 0.4, ...palette.wood },
        { wx: 0.45, wy: 0.55, wz: 0.45, px: 2.55, py: 0, pz: mid - 0.4, ...palette.wood },
        { wx: 2.0, wy: 1.3, wz: 0.15, px: 0.5, py: 0, pz: mid - 0.35, ...palette.dark },
      ];
    }
    if (type === 'office') {
      return [
        { wx: 1.6, wy: 0.75, wz: 0.8, px: 0.7, py: 0, pz: 0.8, ...palette.wood },
        { wx: 0.6, wy: 1.1, wz: 0.6, px: 1.2, py: 0, pz: 1.8, ...palette.dark },
        { wx: 1.1, wy: 0.5, wz: 0.08, px: 0.9, py: 0.75, pz: 1.0, ...palette.dark },
        { wx: 0.9, wy: 1.8, wz: 0.4, px: side - 1.1, py: 0, pz: 0.2, ...palette.wood },
      ];
    }
    // commercial — open layout with desks
    return [
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 0.6, py: 0, pz: 0.6, ...palette.white },
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 0.6, py: 0, pz: 2.0, ...palette.white },
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 2.2, py: 0, pz: 0.6, ...palette.white },
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 2.2, py: 0, pz: 2.0, ...palette.white },
    ];
  }
}
