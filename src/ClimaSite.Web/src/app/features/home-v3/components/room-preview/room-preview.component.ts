import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, effect, inject, input, viewChild } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import type { ClimateZone, RoomType } from '../../models/home-v3.models';

interface PaintSet {
  top: string;
  side: string;
  front: string;
}

interface CanvasPalette {
  background: string;
  glow: string;
  glowTransparent: string;
  stroke: string;
  strokeStrong: string;
  floor: string;
  wallBack: string;
  wallSide: string;
  ac: PaintSet;
  ledCool: string;
  ledWarm: string;
  flowCool: string;
  flowWarm: string;
  furniture: {
    wood: PaintSet;
    fabric: PaintSet;
    light: PaintSet;
    dark: PaintSet;
  };
}

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
  imports: [TranslateModule],
  template: `
    <div
      class="preview-frame"
      role="img"
      [attr.aria-label]="'homeV3.preview.ariaLabel' | translate: {
        area: area(),
        roomType: ('homeV3.roomType.' + roomType() | translate),
        zone: ('homeV3.zone.' + zone() + '.name' | translate)
      }"
    >
      <canvas #canvas width="900" height="620"></canvas>
      <div class="stat-chips">
        <div class="chip">
          <span class="chip-label">{{ 'homeV3.preview.outside' | translate }}</span>
          <span class="chip-value">{{ outsideC() }}°C</span>
        </div>
        <div class="chip">
          <span class="chip-label">{{ 'homeV3.preview.inside' | translate }}</span>
          <span class="chip-value">{{ insideC() }}°C</span>
        </div>
        <div class="chip">
          <span class="chip-label">{{ 'homeV3.preview.power' | translate }}</span>
          <span class="chip-value">{{ watts() }} W</span>
        </div>
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
    const palette = this.canvasPalette();

    // Background.
    ctx.fillStyle = palette.background;
    ctx.fillRect(0, 0, w, h);

    // Soft radial highlight behind the room.
    const grd = ctx.createRadialGradient(w * 0.55, h * 0.45, 40, w * 0.55, h * 0.45, Math.max(w, h) * 0.7);
    grd.addColorStop(0, palette.glow);
    grd.addColorStop(1, palette.glowTransparent);
    ctx.fillStyle = grd;
    ctx.fillRect(0, 0, w, h);

    // Room box sized by area (loose mapping so 10m² and 120m² both read).
    const a = this.area();
    const side = 3 + Math.sqrt(a) * 0.55;
    const height = 2.6;
    const cx = w * 0.52;
    const cy = h * 0.62;
    const scale = Math.min(42, 260 / side);

    // Floor.
    this.polygon(ctx, [
      this.project(0, 0, 0, cx, cy, scale),
      this.project(side, 0, 0, cx, cy, scale),
      this.project(side, 0, side, cx, cy, scale),
      this.project(0, 0, side, cx, cy, scale),
    ], palette.floor, palette.stroke);

    // Back wall.
    this.polygon(ctx, [
      this.project(0, 0, 0, cx, cy, scale),
      this.project(side, 0, 0, cx, cy, scale),
      this.project(side, height, 0, cx, cy, scale),
      this.project(0, height, 0, cx, cy, scale),
    ], palette.wallBack, palette.strokeStrong);

    // Side wall.
    this.polygon(ctx, [
      this.project(side, 0, 0, cx, cy, scale),
      this.project(side, 0, side, cx, cy, scale),
      this.project(side, height, side, cx, cy, scale),
      this.project(side, height, 0, cx, cy, scale),
    ], palette.wallSide, palette.strokeStrong);

    // Furniture.
    for (const f of this.furniture(side, palette)) {
      this.box(ctx, f.wx, f.wy, f.wz, f.px, f.py, f.pz, f.top, f.side, f.front, cx, cy, scale, palette.stroke);
    }

    // AC unit on back wall, upper-right.
    const acX = side - 1.8;
    const acY = height - 0.55;
    const acZ = 0.05;
    this.box(
      ctx,
      1.2,
      0.35,
      0.25,
      acX,
      acY,
      acZ,
      palette.ac.top,
      palette.ac.side,
      palette.ac.front,
      cx,
      cy,
      scale,
      palette.stroke,
    );
    // LED stripe.
    ctx.fillStyle = this.zone() === 'C' ? palette.ledWarm : palette.ledCool;
    const [lx, ly] = this.project(acX + 0.15, acY + 0.06, acZ + 0.26, cx, cy, scale);
    ctx.fillRect(lx, ly, 18, 2);

    // Airflow lines.
    const flowCol = this.zone() === 'C' ? palette.flowWarm : palette.flowCool;
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
    stroke: string,
  ): void {
    // Top face.
    this.polygon(ctx, [
      this.project(px,      py + wy, pz,      cx, cy, scale),
      this.project(px + wx, py + wy, pz,      cx, cy, scale),
      this.project(px + wx, py + wy, pz + wz, cx, cy, scale),
      this.project(px,      py + wy, pz + wz, cx, cy, scale),
    ], top, stroke);
    // Front face.
    this.polygon(ctx, [
      this.project(px,      py,      pz + wz, cx, cy, scale),
      this.project(px + wx, py,      pz + wz, cx, cy, scale),
      this.project(px + wx, py + wy, pz + wz, cx, cy, scale),
      this.project(px,      py + wy, pz + wz, cx, cy, scale),
    ], front, stroke);
    // Right face.
    this.polygon(ctx, [
      this.project(px + wx, py,      pz,      cx, cy, scale),
      this.project(px + wx, py,      pz + wz, cx, cy, scale),
      this.project(px + wx, py + wy, pz + wz, cx, cy, scale),
      this.project(px + wx, py + wy, pz,      cx, cy, scale),
    ], side, stroke);
  }

  /** Furniture recipes per room type. Positions are in metres inside the room box. */
  private furniture(side: number, palette: CanvasPalette): Array<{
    wx: number; wy: number; wz: number;
    px: number; py: number; pz: number;
    top: string; side: string; front: string;
  }> {
    const furniture = palette.furniture;
    const type = this.roomType();
    const mid = side / 2;
    if (type === 'living') {
      return [
        { wx: 2.2, wy: 0.5, wz: 0.9, px: 0.6, py: 0, pz: mid + 0.3, ...furniture.fabric },
        { wx: 1.0, wy: 0.4, wz: 0.5, px: 1.2, py: 0, pz: mid - 0.5, ...furniture.wood },
        { wx: 0.9, wy: 1.6, wz: 0.35, px: side - 1.1, py: 0, pz: 0.2, ...furniture.wood },
        { wx: 0.5, wy: 1.1, wz: 0.5, px: 0.2, py: 0, pz: mid, ...furniture.dark },
      ];
    }
    if (type === 'bedroom') {
      return [
        { wx: 1.8, wy: 0.55, wz: 2.1, px: 0.6, py: 0, pz: mid - 0.2, ...furniture.fabric },
        { wx: 0.45, wy: 0.55, wz: 0.45, px: 0.1, py: 0, pz: mid - 0.4, ...furniture.wood },
        { wx: 0.45, wy: 0.55, wz: 0.45, px: 2.55, py: 0, pz: mid - 0.4, ...furniture.wood },
        { wx: 2.0, wy: 1.3, wz: 0.15, px: 0.5, py: 0, pz: mid - 0.35, ...furniture.dark },
      ];
    }
    if (type === 'office') {
      return [
        { wx: 1.6, wy: 0.75, wz: 0.8, px: 0.7, py: 0, pz: 0.8, ...furniture.wood },
        { wx: 0.6, wy: 1.1, wz: 0.6, px: 1.2, py: 0, pz: 1.8, ...furniture.dark },
        { wx: 1.1, wy: 0.5, wz: 0.08, px: 0.9, py: 0.75, pz: 1.0, ...furniture.dark },
        { wx: 0.9, wy: 1.8, wz: 0.4, px: side - 1.1, py: 0, pz: 0.2, ...furniture.wood },
      ];
    }
    // commercial — open layout with desks
    return [
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 0.6, py: 0, pz: 0.6, ...furniture.light },
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 0.6, py: 0, pz: 2.0, ...furniture.light },
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 2.2, py: 0, pz: 0.6, ...furniture.light },
      { wx: 1.2, wy: 0.75, wz: 0.6, px: 2.2, py: 0, pz: 2.0, ...furniture.light },
    ];
  }

  private canvasPalette(): CanvasPalette {
    const styles = getComputedStyle(this.hostRef.nativeElement);
    const token = (name: string) => styles.getPropertyValue(name).trim();

    return {
      background: token('--home-v3-canvas-bg'),
      glow: token('--home-v3-canvas-glow'),
      glowTransparent: token('--home-v3-canvas-glow-transparent'),
      stroke: token('--home-v3-canvas-stroke'),
      strokeStrong: token('--home-v3-canvas-stroke-strong'),
      floor: token('--home-v3-floor'),
      wallBack: token('--home-v3-wall-back'),
      wallSide: token('--home-v3-wall-side'),
      ac: {
        top: token('--home-v3-ac-top'),
        side: token('--home-v3-ac-side'),
        front: token('--home-v3-ac-front'),
      },
      ledCool: token('--home-v3-led-cool'),
      ledWarm: token('--home-v3-led-warm'),
      flowCool: token('--home-v3-flow-cool'),
      flowWarm: token('--home-v3-flow-warm'),
      furniture: {
        wood: {
          top: token('--home-v3-furniture-wood-top'),
          side: token('--home-v3-furniture-wood-side'),
          front: token('--home-v3-furniture-wood-front'),
        },
        fabric: {
          top: token('--home-v3-furniture-fabric-top'),
          side: token('--home-v3-furniture-fabric-side'),
          front: token('--home-v3-furniture-fabric-front'),
        },
        light: {
          top: token('--home-v3-furniture-light-top'),
          side: token('--home-v3-furniture-light-side'),
          front: token('--home-v3-furniture-light-front'),
        },
        dark: {
          top: token('--home-v3-furniture-dark-top'),
          side: token('--home-v3-furniture-dark-side'),
          front: token('--home-v3-furniture-dark-front'),
        },
      },
    };
  }
}
