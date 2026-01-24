import { Component, inject, HostBinding } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet, ChildrenOutletContexts } from '@angular/router';
import { trigger, transition, style, animate, query, group, AnimationMetadata } from '@angular/animations';
import { HeaderComponent } from '../header/header.component';
import { FooterComponent } from '../footer/footer.component';
import { ToastContainerComponent } from '../../../shared/components/toast/toast.component';
import { AnimationService } from '../../services/animation.service';

// Route animation trigger with fade and slide effect
export const routeAnimations = trigger('routeAnimations', [
  transition('* <=> *', [
    // Set up initial states for entering and leaving views
    query(':enter, :leave', [
      style({
        position: 'absolute',
        top: 0,
        left: 0,
        width: '100%',
        opacity: 0,
      })
    ], { optional: true }),
    
    // Animate leaving page - fade out
    query(':leave', [
      style({ opacity: 1 }),
      animate('200ms ease-in', style({ opacity: 0 }))
    ], { optional: true }),
    
    // Animate entering page - fade in with subtle slide up
    query(':enter', [
      style({ opacity: 0, transform: 'translateY(20px)' }),
      animate('300ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
    ], { optional: true })
  ])
]);

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet, HeaderComponent, FooterComponent, ToastContainerComponent],
  animations: [routeAnimations],
  template: `
    <div class="layout" data-testid="main-layout">
      <app-header />
      <main class="main-content" data-testid="main-content" [@routeAnimations]="getRouteAnimationData()">
        <ng-content></ng-content>
        <router-outlet />
      </main>
      <app-footer />
      <app-toast-container />
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
      position: relative;
      background-color: var(--color-bg-primary);
      transition: var(--theme-transition);
      /* Ensure content doesn't overflow during animation */
      overflow-x: hidden;
    }
  `]
})
export class MainLayoutComponent {
  private readonly contexts = inject(ChildrenOutletContexts);
  private readonly animationService = inject(AnimationService);
  
  /**
   * Disable animations when user prefers reduced motion
   * This host binding disables all Angular animations in this component tree
   */
  @HostBinding('@.disabled')
  get animationsDisabled(): boolean {
    return this.animationService.prefersReducedMotion();
  }
  
  /**
   * Get the animation state from the current route's data
   * This drives the routeAnimations trigger
   */
  getRouteAnimationData(): string {
    const context = this.contexts.getContext('primary');
    return context?.route?.snapshot?.data?.['animation'] || 'default';
  }
}
