import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AnimationService } from './animation.service';

/**
 * Confetti particle interface
 */
interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  color: string;
  size: number;
  rotation: number;
  rotationSpeed: number;
  opacity: number;
  shape: 'square' | 'circle' | 'ribbon';
}

/**
 * ConfettiService - Celebration animation for order confirmations
 * 
 * Features:
 * - Canvas-based confetti animation
 * - Uses brand colors from CSS variables
 * - Realistic physics (gravity, wind, rotation)
 * - Respects reduced motion preferences
 * - Automatic cleanup after animation
 */
@Injectable({ providedIn: 'root' })
export class ConfettiService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly animationService = inject(AnimationService);

  private canvas: HTMLCanvasElement | null = null;
  private ctx: CanvasRenderingContext2D | null = null;
  private particles: Particle[] = [];
  private animationId: number | null = null;
  private startTime = 0;
  
  // Animation configuration
  private readonly PARTICLE_COUNT = 75; // 50-100 range
  private readonly ANIMATION_DURATION = 3000; // 3 seconds
  private readonly GRAVITY = 0.25;
  private readonly WIND = 0.02;
  private readonly FADE_START = 2000; // Start fading at 2 seconds

  /**
   * Launch confetti celebration from center of screen
   */
  burst(): void {
    if (!this.isBrowser) return;

    // Check for reduced motion preference
    if (this.animationService.prefersReducedMotion()) {
      this.showSimpleSparkle();
      return;
    }

    this.createCanvas();
    this.createParticles();
    this.startTime = performance.now();
    this.animate();
  }

  /**
   * Show a simple sparkle effect for users who prefer reduced motion
   */
  private showSimpleSparkle(): void {
    const sparkle = document.createElement('div');
    sparkle.className = 'confetti-sparkle';
    sparkle.innerHTML = '✨';
    sparkle.style.cssText = `
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      font-size: 4rem;
      z-index: 9999;
      pointer-events: none;
      animation: sparkle-fade 1s ease-out forwards;
    `;

    // Add keyframe animation if not exists
    if (!document.querySelector('#confetti-sparkle-style')) {
      const style = document.createElement('style');
      style.id = 'confetti-sparkle-style';
      style.textContent = `
        @keyframes sparkle-fade {
          0% { opacity: 1; transform: translate(-50%, -50%) scale(1); }
          100% { opacity: 0; transform: translate(-50%, -50%) scale(1.5); }
        }
      `;
      document.head.appendChild(style);
    }

    document.body.appendChild(sparkle);

    setTimeout(() => {
      sparkle.remove();
    }, 1000);
  }

  /**
   * Create the canvas element for confetti animation
   */
  private createCanvas(): void {
    // Clean up any existing canvas
    this.cleanup();

    this.canvas = document.createElement('canvas');
    this.canvas.id = 'confetti-canvas';
    this.canvas.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 9999;
    `;
    
    this.canvas.width = window.innerWidth;
    this.canvas.height = window.innerHeight;
    
    document.body.appendChild(this.canvas);
    this.ctx = this.canvas.getContext('2d');

    // Handle window resize
    window.addEventListener('resize', this.handleResize);
  }

  /**
   * Handle window resize during animation
   */
  private handleResize = (): void => {
    if (this.canvas) {
      this.canvas.width = window.innerWidth;
      this.canvas.height = window.innerHeight;
    }
  };

  /**
   * Get brand colors from CSS variables
   */
  private getBrandColors(): string[] {
    const computedStyle = getComputedStyle(document.documentElement);
    
    // Get colors from CSS variables (theme-aware)
    const colors = [
      computedStyle.getPropertyValue('--color-primary-400').trim() || '#38bdf8',
      computedStyle.getPropertyValue('--color-primary-500').trim() || '#0ea5e9',
      computedStyle.getPropertyValue('--color-accent-400').trim() || '#22d3ee',
      computedStyle.getPropertyValue('--color-accent-500').trim() || '#06b6d4',
      computedStyle.getPropertyValue('--color-warm-400').trim() || '#fbbf24',
      computedStyle.getPropertyValue('--color-warm-500').trim() || '#f59e0b',
      computedStyle.getPropertyValue('--color-aurora-400').trim() || '#2dd4bf',
      computedStyle.getPropertyValue('--color-success-400').trim() || '#34d399',
      computedStyle.getPropertyValue('--color-ember-400').trim() || '#fb923c',
    ];

    return colors.filter(c => c); // Filter out empty strings
  }

  /**
   * Create confetti particles
   */
  private createParticles(): void {
    this.particles = [];
    const colors = this.getBrandColors();
    const centerX = window.innerWidth / 2;
    const centerY = window.innerHeight / 2;
    const shapes: Particle['shape'][] = ['square', 'circle', 'ribbon'];

    for (let i = 0; i < this.PARTICLE_COUNT; i++) {
      // Random angle for burst direction (mostly upward)
      const angle = (Math.random() * Math.PI * 0.8) + Math.PI * 0.1; // 10 to 170 degrees (mostly up)
      const velocity = 8 + Math.random() * 12; // Random initial velocity

      this.particles.push({
        x: centerX + (Math.random() - 0.5) * 100, // Slight spread at origin
        y: centerY + (Math.random() - 0.5) * 50,
        vx: Math.cos(angle) * velocity * (Math.random() > 0.5 ? 1 : -1),
        vy: -Math.sin(angle) * velocity, // Negative for upward movement
        color: colors[Math.floor(Math.random() * colors.length)],
        size: 6 + Math.random() * 8,
        rotation: Math.random() * 360,
        rotationSpeed: (Math.random() - 0.5) * 15,
        opacity: 1,
        shape: shapes[Math.floor(Math.random() * shapes.length)]
      });
    }
  }

  /**
   * Animation loop using requestAnimationFrame
   */
  private animate = (): void => {
    if (!this.ctx || !this.canvas) return;

    const elapsed = performance.now() - this.startTime;

    // Check if animation should end
    if (elapsed >= this.ANIMATION_DURATION) {
      this.cleanup();
      return;
    }

    // Clear canvas
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

    // Calculate global fade
    let globalOpacity = 1;
    if (elapsed > this.FADE_START) {
      globalOpacity = 1 - (elapsed - this.FADE_START) / (this.ANIMATION_DURATION - this.FADE_START);
    }

    // Update and draw particles
    for (const particle of this.particles) {
      // Apply physics
      particle.vy += this.GRAVITY; // Gravity
      particle.vx += this.WIND; // Slight wind to the right
      
      // Air resistance
      particle.vx *= 0.99;
      particle.vy *= 0.99;

      // Update position
      particle.x += particle.vx;
      particle.y += particle.vy;

      // Update rotation
      particle.rotation += particle.rotationSpeed;

      // Calculate particle opacity (individual fade based on position + global fade)
      const positionFade = particle.y > this.canvas.height * 0.8 
        ? 1 - (particle.y - this.canvas.height * 0.8) / (this.canvas.height * 0.2)
        : 1;
      particle.opacity = Math.max(0, Math.min(1, positionFade * globalOpacity));

      // Draw particle
      this.drawParticle(particle);
    }

    // Continue animation
    this.animationId = requestAnimationFrame(this.animate);
  };

  /**
   * Draw a single particle
   */
  private drawParticle(particle: Particle): void {
    if (!this.ctx || particle.opacity <= 0) return;

    this.ctx.save();
    this.ctx.translate(particle.x, particle.y);
    this.ctx.rotate((particle.rotation * Math.PI) / 180);
    this.ctx.globalAlpha = particle.opacity;
    this.ctx.fillStyle = particle.color;

    switch (particle.shape) {
      case 'square':
        this.ctx.fillRect(
          -particle.size / 2,
          -particle.size / 2,
          particle.size,
          particle.size
        );
        break;

      case 'circle':
        this.ctx.beginPath();
        this.ctx.arc(0, 0, particle.size / 2, 0, Math.PI * 2);
        this.ctx.fill();
        break;

      case 'ribbon':
        // Ribbon shape - elongated rectangle
        this.ctx.fillRect(
          -particle.size / 2,
          -particle.size / 6,
          particle.size,
          particle.size / 3
        );
        break;
    }

    this.ctx.restore();
  }

  /**
   * Clean up canvas and animation
   */
  private cleanup(): void {
    if (this.animationId !== null) {
      cancelAnimationFrame(this.animationId);
      this.animationId = null;
    }

    if (this.canvas) {
      window.removeEventListener('resize', this.handleResize);
      this.canvas.remove();
      this.canvas = null;
      this.ctx = null;
    }

    this.particles = [];
  }

  /**
   * Stop any ongoing animation
   */
  stop(): void {
    this.cleanup();
  }
}
