# Scroll-Based Storytelling & Cinematic Web Patterns

## Overview

This document catalogs advanced scroll-driven storytelling techniques for creating immersive, cinematic web experiences. These patterns transform passive scrolling into an active narrative journey, guiding users through content with purpose and emotional resonance.

**Relationship to motion-scroll-patterns.md:** This document extends the foundational patterns with narrative structure, cinematic sequences, and advanced sticky behaviors for storytelling contexts.

---

## 1. Scroll Storytelling Techniques

### 1.1 Narrative Arc Through Scroll

**Pattern Name:** Scrolling Story Arc

**Description:** Structure content to follow a classic narrative arc (exposition, rising action, climax, resolution) mapped to scroll progress. Each scroll segment corresponds to a story phase.

**Structure:**
```
0-15%   → Exposition (Hero, context setting)
15-40%  → Rising Action (Features, problems, journey)
40-60%  → Climax (Key revelation, transformation, main feature)
60-85%  → Falling Action (Evidence, testimonials, details)
85-100% → Resolution (CTA, next steps, closure)
```

**When to Use:**
- Product launches
- Brand story pages
- Case studies
- Annual reports
- Landing pages with conversion goals

**Implementation Approach:**
```typescript
// Define story segments with scroll ranges
const storySegments = [
  { phase: 'exposition', start: 0, end: 0.15, mood: 'intrigue' },
  { phase: 'rising', start: 0.15, end: 0.4, mood: 'building' },
  { phase: 'climax', start: 0.4, end: 0.6, mood: 'peak' },
  { phase: 'falling', start: 0.6, end: 0.85, mood: 'evidence' },
  { phase: 'resolution', start: 0.85, end: 1, mood: 'action' }
];

// Map scroll progress to current phase
function getCurrentPhase(scrollProgress: number) {
  return storySegments.find(
    s => scrollProgress >= s.start && scrollProgress < s.end
  );
}
```

**Performance:** Minimal overhead - just progress tracking

**Accessibility:**
- Content remains fully accessible without scroll effects
- Each section is independently navigable
- Skip links between major sections

---

### 1.2 Content Pacing and Rhythm

**Pattern Name:** Scroll Rhythm Control

**Description:** Vary the "density" of content and interactions to create pacing. Fast sections (less content per scroll) vs. slow sections (sticky, more detail) create rhythm.

**Rhythm Types:**
- **Accelerando:** Content density increases (more reveals per scroll unit)
- **Ritardando:** Content slows down (sticky sections, detail focus)
- **Staccato:** Quick, punchy reveals (stat counters, bullet points)
- **Legato:** Smooth, flowing transitions (parallax, gradual reveals)

**When to Use:**
- Engagement optimization
- Guiding user attention
- Creating memorable moments
- Preventing scroll fatigue

**Implementation:**
```scss
// Fast section - less height, quick progression
.section--fast {
  min-height: 50vh;
  
  .reveal-item {
    animation-range: entry 0% entry 25%;
  }
}

// Slow section - sticky, detailed
.section--slow {
  min-height: 200vh;
  
  .sticky-content {
    position: sticky;
    top: 0;
    height: 100vh;
  }
}

// Staccato section - rapid-fire reveals
.section--staccato {
  .reveal-item {
    animation: pop-in 300ms ease-out forwards;
  }
  
  .reveal-item:nth-child(n) {
    animation-delay: calc(var(--index) * 50ms);
  }
}
```

**Performance:** CSS-only rhythm control has zero JS overhead

**Accessibility:**
- Ensure reduced-motion users see all content
- Don't make interaction timing-dependent

---

### 1.3 Emotional Arc Through Scrolling

**Pattern Name:** Mood Progression

**Description:** Shift visual properties (color temperature, contrast, lighting) as user scrolls to evoke emotional progression.

**Emotional Mapping:**
| Scroll Phase | Emotion | Visual Treatment |
|--------------|---------|------------------|
| 0-20% | Curiosity | Cool colors, soft contrast, subtle motion |
| 20-50% | Anticipation | Warmer colors, increasing contrast |
| 50-70% | Excitement | Vibrant colors, high contrast, dynamic motion |
| 70-90% | Trust | Calm colors, balanced, social proof |
| 90-100% | Confidence | Strong brand colors, clear CTA focus |

**When to Use:**
- Emotional brand storytelling
- Product experiences
- Donation/cause pages
- Luxury product showcases

**Implementation:**
```scss
:root {
  --scroll-progress: 0;
}

.page {
  // Color temperature shift
  --color-temp: calc(6000 - (var(--scroll-progress) * 2000));
  
  // Dynamic background
  background: linear-gradient(
    to bottom,
    hsl(220, 30%, 10%) 0%,
    hsl(calc(220 - var(--scroll-progress) * 30), 50%, 20%) 50%,
    hsl(190, 40%, 15%) 100%
  );
}

// JavaScript updates --scroll-progress
```

```typescript
window.addEventListener('scroll', () => {
  const progress = window.scrollY / (document.body.scrollHeight - window.innerHeight);
  document.documentElement.style.setProperty('--scroll-progress', progress.toString());
}, { passive: true });
```

**Performance:**
- Use CSS custom properties for smooth interpolation
- GPU-accelerated gradient transitions
- Throttle scroll updates with rAF

**Accessibility:**
- Don't rely on color alone for meaning
- Ensure text contrast remains WCAG compliant throughout
- Provide alternative cues for emotional content

---

### 1.4 Chapter/Section Markers

**Pattern Name:** Scroll Chapter Navigation

**Description:** Visual indicators showing current position in the narrative, with clickable markers for direct navigation.

**Marker Styles:**
- **Dot Navigation:** Vertical dots indicating sections
- **Progress Steps:** Labeled step indicators
- **Chapter Titles:** Contextual section names
- **Mini-map:** Visual representation of page structure

**When to Use:**
- Long-form content
- Multi-section landing pages
- Case studies
- Documentation

**Implementation:**
```html
<nav class="chapter-nav" aria-label="Page sections">
  <ol class="chapter-markers">
    <li class="chapter-marker active" data-section="hero">
      <button aria-current="true">
        <span class="marker-dot"></span>
        <span class="marker-label">Introduction</span>
      </button>
    </li>
    <li class="chapter-marker" data-section="features">
      <button>
        <span class="marker-dot"></span>
        <span class="marker-label">Features</span>
      </button>
    </li>
    <!-- More markers -->
  </ol>
</nav>
```

```scss
.chapter-nav {
  position: fixed;
  right: 2rem;
  top: 50%;
  transform: translateY(-50%);
  z-index: 100;
}

.chapter-markers {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  list-style: none;
}

.chapter-marker {
  .marker-dot {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    background: var(--color-text-muted);
    transition: transform 200ms ease-out, background 200ms ease-out;
  }
  
  .marker-label {
    position: absolute;
    right: 100%;
    padding-right: 1rem;
    opacity: 0;
    transform: translateX(10px);
    transition: opacity 200ms ease-out, transform 200ms ease-out;
    white-space: nowrap;
  }
  
  &:hover, &.active {
    .marker-dot {
      transform: scale(1.3);
      background: var(--color-primary);
    }
    
    .marker-label {
      opacity: 1;
      transform: translateX(0);
    }
  }
}
```

**Performance:** Intersection Observer for section detection

**Accessibility:**
- Use `<nav>` with `aria-label`
- Active section marked with `aria-current="true"`
- Keyboard navigable
- Labels visible on focus

---

### 1.5 Reading Progress Indication

**Pattern Name:** Multi-Level Progress

**Description:** Show both overall page progress and current section progress for context.

**Progress Types:**
- **Page Progress:** Overall document position
- **Section Progress:** Progress within current sticky section
- **Time Estimate:** "3 min read remaining"
- **Content Progress:** "3 of 7 features"

**Implementation:**
```scss
.progress-container {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
}

.progress-bar--page {
  height: 3px;
  background: var(--color-primary);
  transform-origin: left;
  animation: scale-x linear;
  animation-timeline: scroll(root);
}

.progress-bar--section {
  height: 2px;
  background: var(--color-secondary);
  transform-origin: left;
  animation: scale-x linear;
  animation-timeline: view();
  animation-range: contain;
}

@keyframes scale-x {
  from { transform: scaleX(0); }
  to { transform: scaleX(1); }
}
```

**Accessibility:**
- Use `role="progressbar"` with `aria-valuenow`
- Announce major milestones via live region
- Don't make progress required for understanding

---

## 2. Cinematic Section Patterns

### 2.1 Hero Sequences

**Pattern Name:** Cinematic Hero Opener

**Description:** Full-viewport hero that unfolds as user scrolls, revealing layers of content like opening credits of a film.

**Sequence Stages:**
1. **Entrance:** Initial impact - logo/headline fade in
2. **Reveal:** Supporting text/image layers appear
3. **Invitation:** Scroll indicator or CTA becomes prominent
4. **Exit:** Hero content fades/transforms as content appears

**When to Use:**
- Homepage openings
- Product launches
- Campaign landing pages
- Brand experiences

**Implementation:**
```scss
.hero-cinematic {
  position: relative;
  height: 200vh; // Double height for scroll space
}

.hero-sticky {
  position: sticky;
  top: 0;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.hero-layer {
  position: absolute;
  animation: hero-sequence linear;
  animation-timeline: scroll();
  animation-range: exit 0% exit 100%;
}

.hero-layer--bg {
  @keyframes hero-sequence {
    to { transform: scale(1.2); opacity: 0.3; }
  }
}

.hero-layer--headline {
  @keyframes hero-sequence {
    0% { opacity: 1; transform: translateY(0); }
    50% { opacity: 1; transform: translateY(0); }
    100% { opacity: 0; transform: translateY(-50px); }
  }
}

.hero-layer--subhead {
  opacity: 0;
  @keyframes hero-sequence {
    0% { opacity: 0; transform: translateY(20px); }
    30% { opacity: 1; transform: translateY(0); }
    70% { opacity: 1; transform: translateY(0); }
    100% { opacity: 0; transform: translateY(-30px); }
  }
}
```

**Performance:**
- Use `transform` and `opacity` only
- Single sticky container
- Preload hero images

**Accessibility:**
- All text readable without animation
- Content available immediately for screen readers
- Skip hero option for repeated visits

---

### 2.2 Feature Showcase Sequences

**Pattern Name:** Feature Theater

**Description:** Features are revealed one at a time in a theatrical manner, each getting its moment of focus before transitioning to the next.

**Presentation Styles:**
- **Spotlight:** Single feature highlighted, others dimmed
- **Stage Left/Right:** Features enter from sides alternately
- **Zoom Focus:** Feature image zooms while description appears
- **Card Flip:** Feature cards flip to reveal details

**When to Use:**
- Product feature lists
- Service descriptions
- Capability showcases
- Comparison sections

**Implementation:**
```html
<section class="feature-theater">
  <div class="feature-stage sticky">
    <div class="feature-item" data-feature="1">
      <div class="feature-visual">
        <img src="feature-1.jpg" alt="" loading="lazy">
      </div>
      <div class="feature-content">
        <h3>Feature One</h3>
        <p>Description text</p>
      </div>
    </div>
    <!-- Repeat for each feature -->
  </div>
</section>
```

```scss
.feature-theater {
  // Height = (number of features) * 100vh
  min-height: 400vh;
}

.feature-stage {
  position: sticky;
  top: 0;
  height: 100vh;
}

.feature-item {
  position: absolute;
  inset: 0;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4rem;
  align-items: center;
  padding: 4rem;
  
  opacity: 0;
  pointer-events: none;
  
  &.active {
    opacity: 1;
    pointer-events: auto;
  }
  
  // Alternating layout
  &:nth-child(even) {
    grid-template-columns: 1fr 1fr;
    
    .feature-visual { order: 2; }
    .feature-content { order: 1; }
  }
}

.feature-visual {
  animation: feature-visual-in 600ms ease-out forwards;
  
  @keyframes feature-visual-in {
    from { opacity: 0; transform: scale(0.9); }
    to { opacity: 1; transform: scale(1); }
  }
}

.feature-content {
  animation: feature-content-in 600ms ease-out 200ms forwards;
  opacity: 0;
  
  @keyframes feature-content-in {
    from { opacity: 0; transform: translateX(-20px); }
    to { opacity: 1; transform: translateX(0); }
  }
}
```

**Performance:**
- Only animate visible feature
- Preload adjacent feature images
- Use IntersectionObserver for activation

**Accessibility:**
- All features visible without JS
- Keyboard navigation between features
- Describe transitions for screen readers

---

### 2.3 Before/After Reveals

**Pattern Name:** Comparison Slider & Scroll Reveal

**Description:** Reveal transformation or comparison through scroll or interactive slider.

**Reveal Types:**
- **Slider:** Draggable handle reveals before/after
- **Scroll Wipe:** Scroll progress controls reveal
- **Fade Cross:** Cross-dissolve between states
- **Split Screen:** Synchronized scroll on both sides

**When to Use:**
- Product transformations
- Service results
- Before/after comparisons
- Design iterations

**Implementation - Scroll Wipe:**
```scss
.comparison-section {
  height: 200vh;
}

.comparison-container {
  position: sticky;
  top: 0;
  height: 100vh;
  overflow: hidden;
}

.comparison-before,
.comparison-after {
  position: absolute;
  inset: 0;
}

.comparison-after {
  clip-path: inset(0 calc(100% - var(--reveal, 0%)) 0 0);
  animation: reveal-wipe linear;
  animation-timeline: scroll();
  animation-range: contain;
}

@keyframes reveal-wipe {
  from { clip-path: inset(0 100% 0 0); }
  to { clip-path: inset(0 0 0 0); }
}

// Labels
.comparison-label {
  position: absolute;
  bottom: 2rem;
  padding: 0.5rem 1rem;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  
  &--before { left: 2rem; }
  &--after { right: 2rem; }
}
```

**Implementation - Interactive Slider:**
```typescript
class ComparisonSlider {
  private container: HTMLElement;
  private slider: HTMLElement;
  private afterImage: HTMLElement;
  
  constructor(container: HTMLElement) {
    this.container = container;
    this.slider = container.querySelector('.slider-handle')!;
    this.afterImage = container.querySelector('.comparison-after')!;
    
    this.initDrag();
  }
  
  private initDrag() {
    let isDragging = false;
    
    this.slider.addEventListener('pointerdown', (e) => {
      isDragging = true;
      this.slider.setPointerCapture(e.pointerId);
    });
    
    this.container.addEventListener('pointermove', (e) => {
      if (!isDragging) return;
      
      const rect = this.container.getBoundingClientRect();
      const x = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
      
      this.afterImage.style.clipPath = `inset(0 ${(1 - x) * 100}% 0 0)`;
      this.slider.style.left = `${x * 100}%`;
    });
    
    this.slider.addEventListener('pointerup', () => {
      isDragging = false;
    });
  }
}
```

**Accessibility:**
- Provide text description of difference
- Keyboard control for slider (arrow keys)
- `aria-valuenow` for slider position
- Alternative: show both images stacked on mobile

---

### 2.4 Timeline Presentations

**Pattern Name:** Scroll Timeline

**Description:** Chronological content revealed along a visual timeline as user scrolls.

**Timeline Styles:**
- **Vertical Line:** Classic timeline with alternating entries
- **Horizontal Scroll:** Horizontal timeline within vertical scroll
- **Radial:** Circular timeline unwrapping
- **Path-based:** SVG path that draws as scroll progresses

**When to Use:**
- Company history
- Product evolution
- Project milestones
- Event sequences

**Implementation:**
```html
<section class="timeline-section">
  <div class="timeline-track">
    <svg class="timeline-line" viewBox="0 0 10 1000" preserveAspectRatio="none">
      <path d="M5 0 V1000" stroke="var(--color-primary)" stroke-width="2" 
            stroke-dasharray="1000" stroke-dashoffset="1000"
            class="timeline-path"/>
    </svg>
    
    <div class="timeline-entries">
      <article class="timeline-entry">
        <time datetime="2020-01">January 2020</time>
        <h3>Milestone Title</h3>
        <p>Description</p>
      </article>
      <!-- More entries -->
    </div>
  </div>
</section>
```

```scss
.timeline-section {
  padding: 4rem 0;
}

.timeline-track {
  position: relative;
  max-width: 800px;
  margin: 0 auto;
}

.timeline-line {
  position: absolute;
  left: 50%;
  top: 0;
  width: 2px;
  height: 100%;
  transform: translateX(-50%);
}

.timeline-path {
  animation: draw-line linear;
  animation-timeline: view();
  animation-range: entry 10% exit 90%;
}

@keyframes draw-line {
  to { stroke-dashoffset: 0; }
}

.timeline-entry {
  position: relative;
  width: 45%;
  padding: 1.5rem;
  margin-bottom: 3rem;
  
  opacity: 0;
  transform: translateY(30px);
  animation: entry-reveal linear;
  animation-timeline: view();
  animation-range: entry 0% entry 50%;
  
  &:nth-child(odd) {
    margin-left: 0;
    text-align: right;
    
    &::after {
      right: -2rem;
    }
  }
  
  &:nth-child(even) {
    margin-left: 55%;
    
    &::after {
      left: -2rem;
    }
  }
  
  // Connection dot
  &::after {
    content: '';
    position: absolute;
    top: 1.5rem;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: var(--color-primary);
    border: 3px solid var(--color-background);
  }
}

@keyframes entry-reveal {
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
```

**Performance:**
- SVG path animation is GPU-accelerated
- Use Intersection Observer for entry reveals
- Limit visible entries in DOM for very long timelines

**Accessibility:**
- Semantic `<time>` elements
- Logical reading order regardless of visual layout
- Screen reader friendly without animation

---

### 2.5 Stats and Data Visualization Reveals

**Pattern Name:** Animated Data Reveal

**Description:** Statistics and data visualizations animate to their final values as they scroll into view.

**Animation Types:**
- **Count Up:** Numbers increment to final value
- **Chart Draw:** Bars/lines grow to final size
- **Percentage Ring:** Circular progress fills
- **Comparison Bars:** Bars extend in sequence

**When to Use:**
- Statistics sections
- Impact metrics
- Achievement showcases
- Performance comparisons

**Implementation - Count Up:**
```typescript
class CountUp {
  private element: HTMLElement;
  private target: number;
  private duration: number;
  private startTime: number | null = null;
  
  constructor(element: HTMLElement, target: number, duration = 2000) {
    this.element = element;
    this.target = target;
    this.duration = duration;
  }
  
  start() {
    this.startTime = null;
    requestAnimationFrame(this.animate.bind(this));
  }
  
  private animate(currentTime: number) {
    if (!this.startTime) this.startTime = currentTime;
    
    const elapsed = currentTime - this.startTime;
    const progress = Math.min(elapsed / this.duration, 1);
    
    // Ease out cubic
    const easeOut = 1 - Math.pow(1 - progress, 3);
    const current = Math.round(this.target * easeOut);
    
    this.element.textContent = current.toLocaleString();
    
    if (progress < 1) {
      requestAnimationFrame(this.animate.bind(this));
    }
  }
}

// Trigger on scroll
const observer = new IntersectionObserver((entries) => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      const target = parseInt(entry.target.dataset.count!, 10);
      new CountUp(entry.target as HTMLElement, target).start();
      observer.unobserve(entry.target);
    }
  });
}, { threshold: 0.5 });

document.querySelectorAll('[data-count]').forEach(el => observer.observe(el));
```

**Implementation - Chart Bars:**
```scss
.stat-bar {
  height: 24px;
  background: var(--color-surface);
  border-radius: 12px;
  overflow: hidden;
  
  .stat-bar-fill {
    height: 100%;
    background: var(--color-primary);
    border-radius: 12px;
    transform-origin: left;
    transform: scaleX(0);
    
    animation: bar-fill 1s ease-out forwards;
    animation-timeline: view();
    animation-range: entry 20% entry 80%;
  }
}

@keyframes bar-fill {
  to { transform: scaleX(var(--fill-percent, 1)); }
}
```

```html
<div class="stat-bar">
  <div class="stat-bar-fill" style="--fill-percent: 0.85" aria-valuenow="85" aria-valuemax="100">
    <span class="sr-only">85%</span>
  </div>
</div>
```

**Performance:**
- Use CSS transforms for bar fills
- Limit simultaneous animations
- Consider reducing animation on low-power devices

**Accessibility:**
- Final values accessible in DOM immediately
- `aria-valuenow` and `aria-valuemax` for progress
- Announce significant statistics via live region

---

## 3. Sticky Section Behaviors

### 3.1 Pin-and-Transform Sections

**Pattern Name:** Sticky Transform Stage

**Description:** Section pins to viewport while internal content transforms based on scroll position. Multiple states or "frames" are shown while pinned.

**Frame Concepts:**
- **Frame 1-3:** Different content states shown during pin
- **Transform Progress:** 0-1 value mapped from scroll
- **Keyframe Transitions:** Discrete state changes

**When to Use:**
- Product demonstrations
- Multi-step explanations
- Feature deep-dives
- Interactive tutorials

**Implementation:**
```html
<section class="pin-section" style="--num-frames: 4">
  <div class="pin-container">
    <div class="pin-stage">
      <!-- Frame content -->
      <div class="frame frame-1">Content for frame 1</div>
      <div class="frame frame-2">Content for frame 2</div>
      <div class="frame frame-3">Content for frame 3</div>
      <div class="frame frame-4">Content for frame 4</div>
    </div>
  </div>
</section>
```

```scss
.pin-section {
  // Height determines scroll duration while pinned
  height: calc(100vh * var(--num-frames, 3));
}

.pin-container {
  position: sticky;
  top: 0;
  height: 100vh;
  overflow: hidden;
}

.pin-stage {
  height: 100%;
  animation: stage-progress linear;
  animation-timeline: view();
  animation-range: contain;
}

.frame {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.3s ease-out;
}

// JavaScript controls frame visibility based on scroll
```

```typescript
class PinTransformSection {
  private section: HTMLElement;
  private frames: HTMLElement[];
  private numFrames: number;
  
  constructor(section: HTMLElement) {
    this.section = section;
    this.frames = Array.from(section.querySelectorAll('.frame'));
    this.numFrames = this.frames.length;
    
    this.initScrollTracking();
  }
  
  private initScrollTracking() {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          this.startTracking();
        } else {
          this.stopTracking();
        }
      });
    }, { threshold: 0 });
    
    observer.observe(this.section);
  }
  
  private trackingRAF: number | null = null;
  
  private startTracking() {
    const track = () => {
      const rect = this.section.getBoundingClientRect();
      const sectionHeight = this.section.offsetHeight;
      const viewportHeight = window.innerHeight;
      
      // Progress from 0 (section just entered) to 1 (section about to leave)
      const scrolledIntoSection = -rect.top;
      const scrollableDistance = sectionHeight - viewportHeight;
      const progress = Math.max(0, Math.min(1, scrolledIntoSection / scrollableDistance));
      
      // Determine active frame
      const frameIndex = Math.floor(progress * this.numFrames);
      const clampedIndex = Math.min(frameIndex, this.numFrames - 1);
      
      this.frames.forEach((frame, i) => {
        frame.style.opacity = i === clampedIndex ? '1' : '0';
        frame.style.pointerEvents = i === clampedIndex ? 'auto' : 'none';
      });
      
      this.trackingRAF = requestAnimationFrame(track);
    };
    
    this.trackingRAF = requestAnimationFrame(track);
  }
  
  private stopTracking() {
    if (this.trackingRAF) {
      cancelAnimationFrame(this.trackingRAF);
      this.trackingRAF = null;
    }
  }
}
```

**Performance:**
- Use `position: sticky` (native, performant)
- Track scroll with rAF throttling
- Only transform/opacity changes

**Accessibility:**
- All frames in DOM, accessible via keyboard
- Active frame focusable
- Announce frame changes via live region

---

### 3.2 Horizontal Scroll Within Vertical

**Pattern Name:** Horizontal Scroll Hijack

**Description:** Vertical scrolling translates to horizontal movement within a sticky container, creating a horizontal carousel effect.

**When to Use:**
- Product showcases
- Portfolio galleries
- Feature cards that benefit from horizontal layout
- Image series

**Implementation:**
```html
<section class="horizontal-scroll-section">
  <div class="horizontal-container">
    <div class="horizontal-track">
      <article class="horizontal-card">Card 1</article>
      <article class="horizontal-card">Card 2</article>
      <article class="horizontal-card">Card 3</article>
      <article class="horizontal-card">Card 4</article>
      <article class="horizontal-card">Card 5</article>
    </div>
  </div>
</section>
```

```scss
.horizontal-scroll-section {
  // Height = viewport + (track width - viewport width)
  // This creates the scroll space needed
  height: 300vh;
}

.horizontal-container {
  position: sticky;
  top: 0;
  height: 100vh;
  overflow: hidden;
}

.horizontal-track {
  display: flex;
  gap: 2rem;
  padding: 2rem;
  height: 100%;
  align-items: center;
  
  // CSS scroll-driven animation
  animation: scroll-horizontal linear;
  animation-timeline: view();
  animation-range: contain;
}

@keyframes scroll-horizontal {
  to {
    // Move left by (total track width - viewport)
    transform: translateX(calc(-100% + 100vw));
  }
}

.horizontal-card {
  flex: 0 0 400px;
  height: 80%;
  background: var(--color-surface);
  border-radius: 1rem;
  padding: 2rem;
}
```

**Alternative - JavaScript with calculated width:**
```typescript
class HorizontalScrollSection {
  private section: HTMLElement;
  private track: HTMLElement;
  private trackWidth: number = 0;
  
  constructor(section: HTMLElement) {
    this.section = section;
    this.track = section.querySelector('.horizontal-track')!;
    this.calculate();
    
    window.addEventListener('resize', () => this.calculate());
    window.addEventListener('scroll', () => this.onScroll(), { passive: true });
  }
  
  private calculate() {
    this.trackWidth = this.track.scrollWidth;
    const overflow = this.trackWidth - window.innerWidth;
    // Set section height to create enough scroll space
    this.section.style.height = `${window.innerHeight + overflow}px`;
  }
  
  private onScroll() {
    const rect = this.section.getBoundingClientRect();
    const sectionHeight = this.section.offsetHeight;
    const viewportHeight = window.innerHeight;
    
    const scrolledIntoSection = -rect.top;
    const scrollableDistance = sectionHeight - viewportHeight;
    const progress = Math.max(0, Math.min(1, scrolledIntoSection / scrollableDistance));
    
    const maxTranslate = this.trackWidth - window.innerWidth;
    this.track.style.transform = `translateX(${-progress * maxTranslate}px)`;
  }
}
```

**Performance:**
- CSS-only version is most performant
- JavaScript version uses transform only
- Consider will-change on track during scroll

**Accessibility:**
- Provide alternative vertical navigation
- Cards must be focusable and keyboard accessible
- Consider native horizontal scroll on mobile

---

### 3.3 Card Stacking Effects

**Pattern Name:** Stack & Peel Cards

**Description:** Cards stack on top of each other and peel away or fan out as user scrolls, revealing content underneath.

**Stacking Variations:**
- **Deck Peel:** Top card slides away, revealing next
- **Fan Spread:** Cards fan out from stacked position
- **Z-Stack:** Cards have depth, scroll reduces stack
- **Accordion:** Cards expand from compressed stack

**When to Use:**
- Feature comparisons
- Testimonial series
- Step sequences
- Layered information

**Implementation - Deck Peel:**
```scss
.card-stack-section {
  height: 400vh; // 4 cards
}

.card-stack-container {
  position: sticky;
  top: 10vh;
  height: 80vh;
  perspective: 1000px;
}

.stack-card {
  position: absolute;
  inset: 0;
  background: var(--color-surface);
  border-radius: 1rem;
  padding: 2rem;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.1);
  
  // Stacking order (first card on top)
  &:nth-child(1) { z-index: 4; }
  &:nth-child(2) { z-index: 3; }
  &:nth-child(3) { z-index: 2; }
  &:nth-child(4) { z-index: 1; }
  
  // Initial stacked offset
  @for $i from 1 through 4 {
    &:nth-child(#{$i}) {
      transform: translateY(#{($i - 1) * 10}px) scale(#{1 - ($i - 1) * 0.02});
    }
  }
}

// Animation controlled by JavaScript scroll progress
.stack-card.exiting {
  animation: card-peel 0.5s ease-out forwards;
}

@keyframes card-peel {
  to {
    transform: translateX(-120%) rotateZ(-10deg);
    opacity: 0;
  }
}
```

```typescript
class CardStackSection {
  private section: HTMLElement;
  private cards: HTMLElement[];
  private currentCardIndex = 0;
  
  constructor(section: HTMLElement) {
    this.section = section;
    this.cards = Array.from(section.querySelectorAll('.stack-card'));
    
    window.addEventListener('scroll', () => this.onScroll(), { passive: true });
  }
  
  private onScroll() {
    const rect = this.section.getBoundingClientRect();
    const sectionHeight = this.section.offsetHeight;
    const viewportHeight = window.innerHeight;
    
    const scrolledIntoSection = -rect.top;
    const scrollableDistance = sectionHeight - viewportHeight;
    const progress = Math.max(0, Math.min(1, scrolledIntoSection / scrollableDistance));
    
    // Determine which card should be on top
    const cardProgress = progress * this.cards.length;
    const targetCardIndex = Math.floor(cardProgress);
    
    // Mark cards as exited
    this.cards.forEach((card, i) => {
      if (i < targetCardIndex) {
        card.classList.add('exiting');
      } else {
        card.classList.remove('exiting');
      }
    });
    
    // Animate current card based on sub-progress
    const currentCard = this.cards[targetCardIndex];
    if (currentCard) {
      const cardSubProgress = cardProgress - targetCardIndex;
      currentCard.style.transform = `
        translateX(${-cardSubProgress * 120}%) 
        rotateZ(${-cardSubProgress * 10}deg)
      `;
      currentCard.style.opacity = `${1 - cardSubProgress}`;
    }
  }
}
```

**Performance:**
- Use transform and opacity only
- Limit to 5-7 cards maximum
- Consider reducing shadow complexity on scroll

**Accessibility:**
- All cards in DOM and accessible
- Logical tab order
- Announce current card via aria-live

---

### 3.4 Content Swapping While Pinned

**Pattern Name:** Sticky Content Swap

**Description:** A container remains fixed while its content morphs or swaps between different states based on scroll position.

**Swap Types:**
- **Text Swap:** Headlines/descriptions change
- **Image Swap:** Product images rotate
- **Layout Swap:** Entire layout reconfigures
- **Data Swap:** Charts/stats update

**When to Use:**
- Product configurators
- Feature comparisons
- Interactive demos
- Multi-faceted explanations

**Implementation:**
```html
<section class="content-swap-section">
  <div class="swap-container">
    <div class="swap-visual">
      <img src="product-1.jpg" alt="" class="swap-image" data-state="1">
      <img src="product-2.jpg" alt="" class="swap-image" data-state="2">
      <img src="product-3.jpg" alt="" class="swap-image" data-state="3">
    </div>
    
    <div class="swap-content">
      <div class="swap-text" data-state="1">
        <h3>Feature One</h3>
        <p>Description for state one</p>
      </div>
      <div class="swap-text" data-state="2">
        <h3>Feature Two</h3>
        <p>Description for state two</p>
      </div>
      <div class="swap-text" data-state="3">
        <h3>Feature Three</h3>
        <p>Description for state three</p>
      </div>
    </div>
    
    <nav class="swap-indicators" aria-label="Feature navigation">
      <button class="indicator active" data-state="1" aria-current="true">1</button>
      <button class="indicator" data-state="2">2</button>
      <button class="indicator" data-state="3">3</button>
    </nav>
  </div>
</section>
```

```scss
.content-swap-section {
  height: 300vh;
}

.swap-container {
  position: sticky;
  top: 0;
  height: 100vh;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4rem;
  padding: 4rem;
  align-items: center;
}

.swap-visual {
  position: relative;
  aspect-ratio: 4/3;
}

.swap-image {
  position: absolute;
  inset: 0;
  object-fit: cover;
  opacity: 0;
  transform: scale(1.05);
  transition: opacity 0.5s ease-out, transform 0.5s ease-out;
  
  &.active {
    opacity: 1;
    transform: scale(1);
  }
}

.swap-text {
  position: absolute;
  opacity: 0;
  transform: translateY(20px);
  transition: opacity 0.4s ease-out, transform 0.4s ease-out;
  
  &.active {
    position: relative;
    opacity: 1;
    transform: translateY(0);
  }
}

.swap-indicators {
  position: absolute;
  bottom: 2rem;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  gap: 1rem;
}

.indicator {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  border: 2px solid var(--color-border);
  background: transparent;
  cursor: pointer;
  transition: background 0.2s, border-color 0.2s;
  
  &.active, &:hover {
    background: var(--color-primary);
    border-color: var(--color-primary);
    color: white;
  }
}
```

```typescript
class ContentSwapSection {
  private section: HTMLElement;
  private states: number;
  private currentState = 1;
  
  constructor(section: HTMLElement) {
    this.section = section;
    this.states = section.querySelectorAll('[data-state]').length / 2; // images + text
    
    this.initIndicators();
    window.addEventListener('scroll', () => this.onScroll(), { passive: true });
  }
  
  private initIndicators() {
    const indicators = this.section.querySelectorAll('.indicator');
    indicators.forEach(indicator => {
      indicator.addEventListener('click', () => {
        const state = parseInt(indicator.dataset.state!, 10);
        this.setActiveState(state);
      });
    });
  }
  
  private onScroll() {
    const rect = this.section.getBoundingClientRect();
    const sectionHeight = this.section.offsetHeight;
    const viewportHeight = window.innerHeight;
    
    const scrolledIntoSection = -rect.top;
    const scrollableDistance = sectionHeight - viewportHeight;
    const progress = Math.max(0, Math.min(1, scrolledIntoSection / scrollableDistance));
    
    const targetState = Math.floor(progress * this.states) + 1;
    const clampedState = Math.max(1, Math.min(targetState, this.states));
    
    if (clampedState !== this.currentState) {
      this.setActiveState(clampedState);
    }
  }
  
  private setActiveState(state: number) {
    this.currentState = state;
    
    // Update all elements
    this.section.querySelectorAll('[data-state]').forEach(el => {
      const elState = parseInt(el.dataset.state!, 10);
      el.classList.toggle('active', elState === state);
    });
    
    // Update indicators
    this.section.querySelectorAll('.indicator').forEach(indicator => {
      const indState = parseInt(indicator.dataset.state!, 10);
      indicator.classList.toggle('active', indState === state);
      indicator.setAttribute('aria-current', indState === state ? 'true' : 'false');
    });
  }
}
```

**Performance:**
- Preload all swap content
- Use opacity transitions
- Consider lazy loading non-initial states

**Accessibility:**
- All states accessible via indicators
- Keyboard navigation supported
- State changes announced
- Works without JavaScript

---

### 3.5 Progress-Linked Transformations

**Pattern Name:** Scroll-Progress Transform

**Description:** Element properties (rotation, scale, position, color) are directly mapped to scroll progress, creating continuous transformation.

**Transformation Types:**
- **Rotation:** Element rotates based on scroll
- **Scale:** Grows/shrinks with scroll
- **Position:** Moves along path
- **Morph:** Shape changes (SVG morph)
- **Color:** Hue/saturation shifts

**When to Use:**
- Product 360 views
- Interactive illustrations
- Data visualization builds
- Ambient background effects

**Implementation - Continuous Rotation:**
```scss
.progress-transform-section {
  height: 300vh;
}

.transform-container {
  position: sticky;
  top: 0;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.rotating-product {
  width: 400px;
  height: 400px;
  
  animation: rotate-full linear;
  animation-timeline: scroll();
  animation-range: contain;
}

@keyframes rotate-full {
  from { transform: rotateY(0deg); }
  to { transform: rotateY(360deg); }
}

// Or for image sequence (360 product view)
.product-360 {
  position: relative;
  width: 400px;
  height: 400px;
  
  .product-frame {
    position: absolute;
    inset: 0;
    opacity: 0;
    
    @for $i from 1 through 36 {
      &:nth-child(#{$i}) {
        animation: frame-show linear;
        animation-timeline: scroll();
        animation-range: 
          contain #{($i - 1) * (100% / 36)} 
          contain #{$i * (100% / 36)};
      }
    }
  }
}

@keyframes frame-show {
  0%, 100% { opacity: 0; }
  50% { opacity: 1; }
}
```

**Implementation - Scale Progress:**
```scss
.scale-element {
  animation: scale-up linear;
  animation-timeline: view();
  animation-range: entry 0% exit 0%;
}

@keyframes scale-up {
  from { transform: scale(0.5); opacity: 0; }
  50% { transform: scale(1); opacity: 1; }
  to { transform: scale(1.5); opacity: 0; }
}
```

**Performance:**
- CSS scroll-driven animations are GPU-accelerated
- Avoid complex SVG morphs
- Use discrete steps for image sequences

**Accessibility:**
- Provide static alternative
- Don't require scroll for critical content
- Respect reduced motion preference

---

## 4. Content Reveal Techniques

### 4.1 Fade-In on Scroll

**Pattern Name:** Scroll Fade Reveal

**Description:** Content fades from invisible to visible as it enters the viewport.

**Fade Variations:**
- **Simple Fade:** Opacity 0 to 1
- **Fade Up:** Fade + translate from below
- **Fade In Scale:** Fade + scale from smaller
- **Fade Blur:** Fade + blur to clear

**When to Use:**
- Standard content sections
- Lists and grids
- Supporting content
- Subtle reveals

**Implementation:**
```scss
// Base reveal class
.reveal {
  opacity: 0;
  animation: reveal-fade linear forwards;
  animation-timeline: view();
  animation-range: entry 10% entry 40%;
}

// Variations
.reveal--up {
  transform: translateY(40px);
  
  @keyframes reveal-fade {
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
}

.reveal--scale {
  transform: scale(0.9);
  
  @keyframes reveal-fade {
    to {
      opacity: 1;
      transform: scale(1);
    }
  }
}

.reveal--blur {
  filter: blur(10px);
  
  @keyframes reveal-fade {
    to {
      opacity: 1;
      filter: blur(0);
    }
  }
}

// Reduced motion
@media (prefers-reduced-motion: reduce) {
  .reveal {
    opacity: 1;
    transform: none;
    filter: none;
    animation: none;
  }
}
```

**Performance:**
- Pure CSS, GPU-accelerated
- Blur can be expensive on large elements
- Use sparingly on complex content

**Accessibility:**
- Content in DOM from start
- Animation is enhancement only
- Early completion for readability

---

### 4.2 Slide-In from Edges

**Pattern Name:** Edge Slide Reveal

**Description:** Content slides in from the edge of the viewport or its container.

**Slide Directions:**
- **Left:** Information, supporting content
- **Right:** Visuals, key features
- **Bottom:** Call-to-actions, new sections
- **Top:** Headers, navigation updates

**When to Use:**
- Alternating content layouts
- Feature introductions
- Navigation reveals
- Emphasis moments

**Implementation:**
```scss
.slide-in {
  animation: slide-reveal linear forwards;
  animation-timeline: view();
  animation-range: entry 0% entry 50%;
}

.slide-in--left {
  opacity: 0;
  transform: translateX(-100px);
  
  @keyframes slide-reveal {
    to {
      opacity: 1;
      transform: translateX(0);
    }
  }
}

.slide-in--right {
  opacity: 0;
  transform: translateX(100px);
  
  @keyframes slide-reveal {
    to {
      opacity: 1;
      transform: translateX(0);
    }
  }
}

// Alternating children
.alternating-content > *:nth-child(odd) {
  @extend .slide-in--left;
}

.alternating-content > *:nth-child(even) {
  @extend .slide-in--right;
}
```

**Performance:**
- Transform only = GPU accelerated
- Keep slide distance reasonable (100px or less)
- Avoid sliding very large elements

**Accessibility:**
- No content hidden permanently
- Focus order matches visual order
- Skip option for long sequences

---

### 4.3 Scale and Blur Reveals

**Pattern Name:** Focus Reveal

**Description:** Content transitions from blurred/small to clear/full-size, mimicking camera focus effect.

**When to Use:**
- Hero images
- Product glamour shots
- Emphasis moments
- Artistic transitions

**Implementation:**
```scss
.focus-reveal {
  filter: blur(20px);
  transform: scale(1.1);
  opacity: 0;
  
  animation: focus-in linear forwards;
  animation-timeline: view();
  animation-range: entry 0% entry 60%;
}

@keyframes focus-in {
  to {
    filter: blur(0);
    transform: scale(1);
    opacity: 1;
  }
}

// Performance optimization: GPU layer
.focus-reveal {
  will-change: filter, transform, opacity;
}

// Reset after animation
.focus-reveal:not([style*="animation"]) {
  will-change: auto;
}
```

**Performance:**
- `filter: blur()` can be expensive
- Use on images, not text
- Limit to key moments
- Consider using backdrop-filter for backgrounds

**Accessibility:**
- Never leave content blurred
- Complete animation early in entry
- Provide static fallback

---

### 4.4 Staggered List Appearances

**Pattern Name:** Cascade Reveal

**Description:** List items appear in sequence, creating a cascade or waterfall effect.

**Stagger Patterns:**
- **Linear:** Fixed delay between items
- **Exponential:** Delays increase
- **Grid Wave:** Diagonal or radial pattern
- **Random:** Varied delays for organic feel

**When to Use:**
- Product grids
- Feature lists
- Team member cards
- Portfolio items
- Navigation menus

**Implementation:**
```scss
.stagger-list {
  --stagger-delay: 100ms;
  
  > * {
    opacity: 0;
    transform: translateY(30px);
    animation: stagger-in 500ms ease-out forwards;
    animation-timeline: view();
    animation-range: entry 0% entry 50%;
  }
  
  // Linear stagger
  @for $i from 1 through 20 {
    > *:nth-child(#{$i}) {
      animation-delay: calc((#{$i} - 1) * var(--stagger-delay));
    }
  }
}

@keyframes stagger-in {
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

// Grid wave pattern (for grid layouts)
.grid-wave {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  
  > * {
    opacity: 0;
    animation: stagger-in 500ms ease-out forwards;
  }
  
  // Diagonal wave: delay = row + column
  @for $row from 1 through 10 {
    @for $col from 1 through 3 {
      $index: ($row - 1) * 3 + $col;
      $delay: ($row + $col - 2) * 100ms;
      
      > *:nth-child(#{$index}) {
        animation-delay: #{$delay};
      }
    }
  }
}
```

**JavaScript for Dynamic Lists:**
```typescript
function staggerReveal(container: HTMLElement, options = {
  baseDelay: 0,
  staggerDelay: 100,
  animationClass: 'revealed'
}) {
  const items = container.children;
  
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const index = Array.from(items).indexOf(entry.target as HTMLElement);
        const delay = options.baseDelay + (index * options.staggerDelay);
        
        setTimeout(() => {
          entry.target.classList.add(options.animationClass);
        }, delay);
        
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.1 });
  
  Array.from(items).forEach(item => observer.observe(item));
}
```

**Performance:**
- Limit active animations (max 10-15 simultaneous)
- Use CSS for static lists
- Consider lazy loading for long lists

**Accessibility:**
- All items visible without animation
- Don't delay critical content excessively
- Reduce stagger for prefers-reduced-motion

---

### 4.5 Text Character Animations

**Pattern Name:** Character Cascade

**Description:** Text reveals character by character or word by word, creating a typewriter or flowing text effect.

**Text Animation Types:**
- **Typewriter:** Characters appear sequentially
- **Word Cascade:** Words fade/slide in
- **Line Reveal:** Lines appear with mask
- **Scramble:** Characters scramble before resolving

**When to Use:**
- Hero headlines
- Key messages
- Quotes
- Dramatic reveals
- Loading states with text

**Implementation - Word Cascade:**
```html
<h1 class="word-cascade" data-text="Welcome to our platform">
  <span class="word" style="--i: 0">Welcome</span>
  <span class="word" style="--i: 1">to</span>
  <span class="word" style="--i: 2">our</span>
  <span class="word" style="--i: 3">platform</span>
</h1>
```

```scss
.word-cascade {
  .word {
    display: inline-block;
    opacity: 0;
    transform: translateY(20px);
    animation: word-in 500ms ease-out forwards;
    animation-delay: calc(var(--i) * 100ms);
  }
}

@keyframes word-in {
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

// Scroll-triggered version
.word-cascade--scroll {
  .word {
    animation: word-in 500ms ease-out forwards paused;
    animation-timeline: view();
    animation-range: entry 20% entry 80%;
    animation-delay: calc(var(--i) * 50ms);
  }
}
```

**Implementation - Line Reveal:**
```scss
.line-reveal {
  overflow: hidden;
  
  .line {
    display: block;
    transform: translateY(100%);
    animation: line-up 600ms ease-out forwards;
  }
  
  @for $i from 1 through 5 {
    .line:nth-child(#{$i}) {
      animation-delay: #{($i - 1) * 150}ms;
    }
  }
}

@keyframes line-up {
  to { transform: translateY(0); }
}
```

**JavaScript - Split Text Utility:**
```typescript
function splitText(element: HTMLElement, type: 'chars' | 'words' | 'lines' = 'words') {
  const text = element.textContent || '';
  let items: string[];
  
  switch (type) {
    case 'chars':
      items = text.split('');
      break;
    case 'words':
      items = text.split(' ');
      break;
    case 'lines':
      // Would need layout calculation
      items = [text];
      break;
  }
  
  element.innerHTML = items
    .map((item, i) => `<span class="${type.slice(0, -1)}" style="--i: ${i}">${item}</span>`)
    .join(type === 'words' ? ' ' : '');
  
  // Preserve original text for accessibility
  element.setAttribute('aria-label', text);
}
```

**Performance:**
- Many DOM elements can be expensive
- Use CSS transforms only
- Consider using canvas for complex effects
- Limit to short text

**Accessibility:**
- Preserve original text in `aria-label`
- Screen readers read original text
- Animation is visual enhancement only
- Complete animation quickly

---

## 5. Background Effects

### 5.1 Parallax Layers (Subtle)

**Pattern Name:** Layered Parallax

**Description:** Multiple background layers move at different speeds, creating subtle depth. Modern approach uses minimal movement ratios.

**Layer Structure:**
- **Layer 0 (Back):** Moves slowest (0.2x scroll speed)
- **Layer 1 (Mid):** Moderate speed (0.5x)
- **Layer 2 (Content):** Normal speed (1x)
- **Layer 3 (Front):** Fastest (1.2x) - sparingly used

**When to Use:**
- Hero backgrounds
- Section dividers
- Ambient decoration
- Immersive experiences

**Implementation - CSS Only:**
```scss
.parallax-container {
  position: relative;
  overflow: hidden;
}

.parallax-layer {
  position: absolute;
  inset: -20%; // Extra space for movement
  
  animation: parallax linear;
  animation-timeline: view();
  animation-range: entry 0% exit 100%;
}

.parallax-layer--back {
  @keyframes parallax {
    from { transform: translateY(-10%); }
    to { transform: translateY(10%); }
  }
}

.parallax-layer--mid {
  @keyframes parallax {
    from { transform: translateY(-5%); }
    to { transform: translateY(5%); }
  }
}

.parallax-layer--front {
  @keyframes parallax {
    from { transform: translateY(-15%); }
    to { transform: translateY(15%); }
  }
}
```

**Implementation - JavaScript with Smooth Scroll:**
```typescript
class ParallaxLayers {
  private layers: { element: HTMLElement; speed: number }[];
  
  constructor(container: HTMLElement) {
    this.layers = Array.from(container.querySelectorAll('[data-parallax-speed]')).map(el => ({
      element: el as HTMLElement,
      speed: parseFloat(el.getAttribute('data-parallax-speed') || '0.5')
    }));
    
    this.init();
  }
  
  private init() {
    let ticking = false;
    
    window.addEventListener('scroll', () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          this.update();
          ticking = false;
        });
        ticking = true;
      }
    }, { passive: true });
  }
  
  private update() {
    const scrollY = window.scrollY;
    
    this.layers.forEach(({ element, speed }) => {
      const offset = scrollY * (1 - speed);
      element.style.transform = `translate3d(0, ${offset}px, 0)`;
    });
  }
}
```

**Performance:**
- Use transform3d for GPU acceleration
- Keep ratios subtle (0.2-0.8 range)
- Limit to 3-4 layers maximum
- Use will-change sparingly

**Accessibility:**
- Disable for prefers-reduced-motion
- Decorative only, no content in parallax layers
- Ensure readability of foreground content

---

### 5.2 Gradient Transitions on Scroll

**Pattern Name:** Scroll Gradient Shift

**Description:** Background gradients morph colors or angles as user scrolls through content.

**Gradient Types:**
- **Hue Shift:** Colors transition across spectrum
- **Position Shift:** Gradient origin moves
- **Angle Rotation:** Linear gradient rotates
- **Opacity Blend:** Overlays fade in/out

**When to Use:**
- Mood transitions
- Section differentiation
- Ambient atmosphere
- Emotional storytelling

**Implementation:**
```scss
:root {
  --scroll-progress: 0;
}

.gradient-bg {
  position: fixed;
  inset: 0;
  z-index: -1;
  
  // Hue rotation based on scroll
  background: linear-gradient(
    calc(135deg + var(--scroll-progress) * 45deg),
    hsl(calc(220 + var(--scroll-progress) * 60), 70%, 20%) 0%,
    hsl(calc(260 + var(--scroll-progress) * 40), 60%, 30%) 50%,
    hsl(calc(200 + var(--scroll-progress) * 80), 50%, 15%) 100%
  );
}

// Alternative: gradient overlay
.gradient-overlay {
  position: fixed;
  inset: 0;
  z-index: -1;
  
  &::before,
  &::after {
    content: '';
    position: absolute;
    inset: 0;
    transition: opacity 0.5s ease-out;
  }
  
  &::before {
    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
    opacity: calc(1 - var(--scroll-progress));
  }
  
  &::after {
    background: linear-gradient(135deg, #0f3460 0%, #533483 100%);
    opacity: var(--scroll-progress);
  }
}
```

```typescript
// Update CSS variable on scroll
window.addEventListener('scroll', () => {
  const progress = window.scrollY / (document.body.scrollHeight - window.innerHeight);
  document.documentElement.style.setProperty('--scroll-progress', progress.toString());
}, { passive: true });
```

**Performance:**
- CSS gradients are GPU-rendered
- Avoid animating `background` directly
- Use opacity crossfade for complex gradients
- Throttle scroll updates

**Accessibility:**
- Ensure text contrast in all gradient states
- Don't convey meaning through color alone
- Provide stable reading surface

---

### 5.3 Video/Image Sequences

**Pattern Name:** Scroll-Driven Video/Sequence

**Description:** Video playback or image sequence is controlled by scroll position rather than time.

**Implementation Types:**
- **Video Scrub:** HTML5 video currentTime linked to scroll
- **Image Sequence:** Pre-rendered frames shown based on scroll
- **Canvas Render:** Frames drawn to canvas for performance

**When to Use:**
- Product reveals
- 360 product views
- Complex animations
- Cinematic experiences

**Implementation - Video Scrub:**
```typescript
class ScrollVideo {
  private video: HTMLVideoElement;
  private container: HTMLElement;
  
  constructor(video: HTMLVideoElement, container: HTMLElement) {
    this.video = video;
    this.container = container;
    
    this.video.pause();
    this.video.currentTime = 0;
    
    this.init();
  }
  
  private init() {
    // Ensure video is ready
    this.video.addEventListener('loadedmetadata', () => {
      this.bindScroll();
    });
  }
  
  private bindScroll() {
    let ticking = false;
    
    window.addEventListener('scroll', () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          this.updateVideoTime();
          ticking = false;
        });
        ticking = true;
      }
    }, { passive: true });
  }
  
  private updateVideoTime() {
    const rect = this.container.getBoundingClientRect();
    const containerHeight = this.container.offsetHeight;
    const viewportHeight = window.innerHeight;
    
    const scrolledIntoContainer = -rect.top;
    const scrollableDistance = containerHeight - viewportHeight;
    const progress = Math.max(0, Math.min(1, scrolledIntoContainer / scrollableDistance));
    
    const targetTime = progress * this.video.duration;
    
    // Smooth seeking
    if (Math.abs(this.video.currentTime - targetTime) > 0.1) {
      this.video.currentTime = targetTime;
    }
  }
}
```

**Implementation - Image Sequence (Canvas):**
```typescript
class ScrollImageSequence {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private images: HTMLImageElement[] = [];
  private currentFrame = 0;
  private totalFrames: number;
  
  constructor(canvas: HTMLCanvasElement, imagePaths: string[]) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d')!;
    this.totalFrames = imagePaths.length;
    
    this.preloadImages(imagePaths);
  }
  
  private async preloadImages(paths: string[]) {
    const loadPromises = paths.map((path, i) => {
      return new Promise<void>((resolve) => {
        const img = new Image();
        img.onload = () => {
          this.images[i] = img;
          resolve();
        };
        img.src = path;
      });
    });
    
    await Promise.all(loadPromises);
    this.drawFrame(0);
    this.bindScroll();
  }
  
  private bindScroll() {
    let ticking = false;
    
    window.addEventListener('scroll', () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          this.updateFrame();
          ticking = false;
        });
        ticking = true;
      }
    }, { passive: true });
  }
  
  private updateFrame() {
    const progress = window.scrollY / (document.body.scrollHeight - window.innerHeight);
    const targetFrame = Math.floor(progress * (this.totalFrames - 1));
    
    if (targetFrame !== this.currentFrame && this.images[targetFrame]) {
      this.currentFrame = targetFrame;
      this.drawFrame(targetFrame);
    }
  }
  
  private drawFrame(index: number) {
    const img = this.images[index];
    if (img) {
      this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
      this.ctx.drawImage(img, 0, 0, this.canvas.width, this.canvas.height);
    }
  }
}
```

**Performance:**
- Canvas is most performant for image sequences
- Video scrubbing can be janky on some browsers
- Preload all frames before user reaches section
- Use compressed video/optimized images
- Consider WebGL for very large sequences

**Accessibility:**
- Provide text description of sequence content
- Offer play button for reduced-motion users
- Don't require scroll for critical information

---

### 5.4 Particle Effects

**Pattern Name:** Ambient Particles

**Description:** Floating particles create atmosphere and depth. Movement is subtle and responds to scroll.

**Particle Types:**
- **Dust Motes:** Small, slow, random movement
- **Bubbles:** Upward drift, varied sizes
- **Sparkles:** Bright, fade in/out
- **Geometric:** Abstract shapes, structured movement

**When to Use:**
- Hero backgrounds
- Loading states
- Celebratory moments
- Ambient atmosphere

**Implementation - CSS Only (Limited):**
```scss
.particles-container {
  position: fixed;
  inset: 0;
  z-index: -1;
  overflow: hidden;
  pointer-events: none;
}

.particle {
  position: absolute;
  width: 4px;
  height: 4px;
  background: rgba(255, 255, 255, 0.5);
  border-radius: 50%;
  animation: particle-float 20s infinite ease-in-out;
  
  @for $i from 1 through 20 {
    &:nth-child(#{$i}) {
      left: random(100) * 1%;
      top: random(100) * 1%;
      animation-delay: random(20) * -1s;
      animation-duration: 15 + random(10) * 1s;
      width: 2 + random(4) * 1px;
      height: 2 + random(4) * 1px;
      opacity: 0.3 + random() * 0.5;
    }
  }
}

@keyframes particle-float {
  0%, 100% {
    transform: translate(0, 0);
  }
  25% {
    transform: translate(10px, -20px);
  }
  50% {
    transform: translate(-5px, -10px);
  }
  75% {
    transform: translate(15px, 5px);
  }
}
```

**Implementation - Canvas (Performance Optimized):**
```typescript
class ParticleSystem {
  private canvas: HTMLCanvasElement;
  private ctx: CanvasRenderingContext2D;
  private particles: Particle[] = [];
  private animationFrame: number | null = null;
  
  constructor(canvas: HTMLCanvasElement, count = 50) {
    this.canvas = canvas;
    this.ctx = canvas.getContext('2d')!;
    
    this.resize();
    this.createParticles(count);
    this.animate();
    
    window.addEventListener('resize', () => this.resize());
  }
  
  private resize() {
    this.canvas.width = window.innerWidth;
    this.canvas.height = window.innerHeight;
  }
  
  private createParticles(count: number) {
    for (let i = 0; i < count; i++) {
      this.particles.push({
        x: Math.random() * this.canvas.width,
        y: Math.random() * this.canvas.height,
        radius: 1 + Math.random() * 3,
        speedX: (Math.random() - 0.5) * 0.5,
        speedY: (Math.random() - 0.5) * 0.5,
        opacity: 0.2 + Math.random() * 0.5
      });
    }
  }
  
  private animate() {
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    
    this.particles.forEach(particle => {
      // Update position
      particle.x += particle.speedX;
      particle.y += particle.speedY;
      
      // Wrap around edges
      if (particle.x < 0) particle.x = this.canvas.width;
      if (particle.x > this.canvas.width) particle.x = 0;
      if (particle.y < 0) particle.y = this.canvas.height;
      if (particle.y > this.canvas.height) particle.y = 0;
      
      // Draw
      this.ctx.beginPath();
      this.ctx.arc(particle.x, particle.y, particle.radius, 0, Math.PI * 2);
      this.ctx.fillStyle = `rgba(255, 255, 255, ${particle.opacity})`;
      this.ctx.fill();
    });
    
    this.animationFrame = requestAnimationFrame(() => this.animate());
  }
  
  destroy() {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
    }
  }
}

interface Particle {
  x: number;
  y: number;
  radius: number;
  speedX: number;
  speedY: number;
  opacity: number;
}
```

**Performance:**
- Canvas is more performant than DOM particles
- Limit particle count (30-50 for ambient)
- Use simple shapes
- Consider pausing when off-screen

**Accessibility:**
- Purely decorative - `aria-hidden="true"`
- Disable for prefers-reduced-motion
- No content in particle layer

---

### 5.5 Ambient Motion

**Pattern Name:** Subtle Background Motion

**Description:** Gentle, continuous movement that creates life without demanding attention.

**Motion Types:**
- **Floating Elements:** Slow drift
- **Rotating Accents:** Gradual rotation
- **Pulsing Shapes:** Subtle scale breathing
- **Gradient Animation:** Slow color shifts

**When to Use:**
- Always-on background enhancement
- Empty states
- Loading states
- Hero sections

**Implementation:**
```scss
.ambient-bg {
  position: fixed;
  inset: 0;
  z-index: -1;
  overflow: hidden;
}

.ambient-shape {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.3;
  animation: ambient-float 20s infinite ease-in-out;
  
  &--1 {
    width: 600px;
    height: 600px;
    background: var(--color-primary);
    top: -200px;
    left: -100px;
    animation-duration: 25s;
  }
  
  &--2 {
    width: 400px;
    height: 400px;
    background: var(--color-secondary);
    bottom: -150px;
    right: -100px;
    animation-duration: 30s;
    animation-delay: -10s;
  }
  
  &--3 {
    width: 300px;
    height: 300px;
    background: var(--color-accent);
    top: 50%;
    left: 50%;
    animation-duration: 35s;
    animation-delay: -5s;
  }
}

@keyframes ambient-float {
  0%, 100% {
    transform: translate(0, 0) rotate(0deg);
  }
  25% {
    transform: translate(50px, -30px) rotate(5deg);
  }
  50% {
    transform: translate(-20px, 20px) rotate(-5deg);
  }
  75% {
    transform: translate(30px, 40px) rotate(3deg);
  }
}

// Reduced motion alternative
@media (prefers-reduced-motion: reduce) {
  .ambient-shape {
    animation: none;
  }
}
```

**Performance:**
- Use filter blur on GPU layer
- Keep shapes simple
- Limit to 3-5 ambient elements
- Use long animation durations (20s+)

**Accessibility:**
- Purely decorative
- Never distracting
- Respects reduced motion preference

---

## 6. Performance Budgets

### 6.1 Animation Frame Budgets

**Target:** 60fps = 16.67ms per frame

**Budget Allocation:**
| Task | Budget |
|------|--------|
| JavaScript | 3-4ms |
| Style calculations | 2ms |
| Layout | 0ms (avoid) |
| Paint | 2-3ms |
| Composite | 1-2ms |
| Headroom | 5-6ms |

**Guidelines:**
- Aim for <10ms total scripting per frame
- Use CSS animations over JavaScript when possible
- Batch DOM reads, then DOM writes
- Never read and write DOM interleaved

**Measurement:**
```typescript
// Performance measurement wrapper
function measureFrameTime(label: string, fn: () => void) {
  const start = performance.now();
  fn();
  const duration = performance.now() - start;
  
  if (duration > 10) {
    console.warn(`${label} took ${duration.toFixed(2)}ms - over budget!`);
  }
}

// Use in animation loop
function animate() {
  measureFrameTime('scroll-update', () => {
    updateScrollProgress();
    updateAnimations();
  });
  
  requestAnimationFrame(animate);
}
```

---

### 6.2 Scroll Jank Prevention

**Common Causes:**
1. Expensive scroll handlers
2. Forced synchronous layouts
3. Paint-triggering properties
4. Too many simultaneous animations

**Solutions:**

**1. Passive Event Listeners:**
```typescript
window.addEventListener('scroll', handler, { passive: true });
```

**2. rAF Throttling:**
```typescript
let ticking = false;

function onScroll() {
  if (!ticking) {
    requestAnimationFrame(() => {
      updateAnimations();
      ticking = false;
    });
    ticking = true;
  }
}
```

**3. Debounce Non-Critical Updates:**
```typescript
function debounce<T extends (...args: any[]) => void>(fn: T, delay: number): T {
  let timeoutId: ReturnType<typeof setTimeout>;
  
  return ((...args: Parameters<T>) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  }) as T;
}

// Use for non-critical updates like progress indicators
const updateProgressDebounced = debounce(updateProgress, 100);
```

**4. Avoid Layout Thrashing:**
```typescript
// BAD - causes layout thrashing
elements.forEach(el => {
  const height = el.offsetHeight; // Read
  el.style.height = height + 10 + 'px'; // Write
});

// GOOD - batch reads, then writes
const heights = elements.map(el => el.offsetHeight); // All reads
elements.forEach((el, i) => {
  el.style.height = heights[i] + 10 + 'px'; // All writes
});
```

**5. Use CSS Scroll-Driven Animations:**
```css
/* Browser handles optimization */
.element {
  animation: reveal linear;
  animation-timeline: view();
}
```

---

### 6.3 Battery/CPU Considerations

**Power Consumption Hierarchy:**
1. CSS animations (lowest)
2. Web Animations API
3. requestAnimationFrame
4. setInterval (avoid)
5. Canvas/WebGL (highest)

**Optimization Strategies:**

**1. Reduce Animation When Not Visible:**
```typescript
const observer = new IntersectionObserver((entries) => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      entry.target.classList.add('animate');
    } else {
      entry.target.classList.remove('animate');
    }
  });
});
```

**2. Respect Battery Status (when available):**
```typescript
if ('getBattery' in navigator) {
  (navigator as any).getBattery().then((battery: any) => {
    if (battery.level < 0.2 && !battery.charging) {
      document.body.classList.add('low-power-mode');
    }
  });
}
```

```scss
.low-power-mode {
  .ambient-animation,
  .particle-effect,
  .parallax-layer {
    animation: none !important;
  }
}
```

**3. Use prefers-reduced-motion:**
```typescript
const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

if (prefersReducedMotion.matches) {
  // Skip complex animations
  initMinimalAnimations();
} else {
  initFullAnimations();
}

// Listen for changes
prefersReducedMotion.addEventListener('change', (e) => {
  if (e.matches) {
    disableComplexAnimations();
  } else {
    enableComplexAnimations();
  }
});
```

**4. Limit Concurrent Animations:**
```typescript
class AnimationBudget {
  private activeCount = 0;
  private maxConcurrent = 10;
  private queue: (() => void)[] = [];
  
  canStart(): boolean {
    return this.activeCount < this.maxConcurrent;
  }
  
  register(onComplete: () => void): boolean {
    if (this.canStart()) {
      this.activeCount++;
      return true;
    }
    
    this.queue.push(onComplete);
    return false;
  }
  
  complete() {
    this.activeCount--;
    
    if (this.queue.length > 0 && this.canStart()) {
      const next = this.queue.shift();
      next?.();
    }
  }
}
```

---

### 6.4 Graceful Degradation

**Degradation Levels:**
1. **Full Experience:** All animations, effects
2. **Reduced Motion:** Instant transitions, no movement
3. **Low Power:** Essential animations only
4. **No JavaScript:** Static content, CSS-only enhancements

**Implementation Strategy:**

```scss
// Level 1: Full experience (default)
.reveal-element {
  opacity: 0;
  transform: translateY(40px);
  animation: reveal 600ms ease-out forwards;
  animation-timeline: view();
}

// Level 2: Reduced motion
@media (prefers-reduced-motion: reduce) {
  .reveal-element {
    opacity: 1;
    transform: none;
    animation: fade-only 200ms ease-out forwards;
  }
}

// Level 3: Print / no-animation context
@media print {
  .reveal-element {
    opacity: 1 !important;
    transform: none !important;
    animation: none !important;
  }
}
```

```typescript
// Progressive enhancement check
function initAnimations() {
  // Check for animation support
  const supportsScrollTimeline = CSS.supports('animation-timeline', 'view()');
  const supportsIntersectionObserver = 'IntersectionObserver' in window;
  
  if (supportsScrollTimeline) {
    // Use native CSS scroll-driven animations
    document.body.classList.add('scroll-timeline-supported');
  } else if (supportsIntersectionObserver) {
    // Fallback to Intersection Observer
    initIntersectionObserverAnimations();
  } else {
    // Final fallback: show all content
    document.body.classList.add('animations-disabled');
  }
}
```

```scss
// Fallback styles
.animations-disabled {
  .reveal-element {
    opacity: 1;
    transform: none;
  }
}
```

**Feature Detection Checklist:**
- [ ] CSS scroll-driven animations
- [ ] Intersection Observer
- [ ] Web Animations API
- [ ] requestAnimationFrame
- [ ] CSS transforms
- [ ] CSS transitions
- [ ] CSS filters

---

## 7. Accessibility Summary

### Universal Requirements

1. **Respect `prefers-reduced-motion`:**
   - Disable or minimize all motion
   - Provide instant transitions instead
   - Test with setting enabled

2. **Content First:**
   - All content accessible without animation
   - Animation enhances, never gates content
   - Progressive enhancement approach

3. **Keyboard Navigation:**
   - All interactive elements reachable
   - Focus order matches visual order
   - Focus visible at all times

4. **Screen Reader Compatibility:**
   - Use semantic HTML
   - ARIA labels for interactive elements
   - Announce significant state changes

5. **Timing Considerations:**
   - Don't require precise timing
   - Allow users to pause/stop animations
   - No seizure-triggering flashes (3/second max)

### Pattern-Specific Accessibility

| Pattern | Key Considerations |
|---------|-------------------|
| Sticky Sections | Skip links, keyboard navigation through states |
| Parallax | Disable on reduced motion, no content in layers |
| Text Animation | Preserve original text, complete quickly |
| Video Scrub | Play button alternative, captions |
| Particles | `aria-hidden`, pure decoration |
| Progress Indicators | `aria-valuenow`, optional announcements |

---

## 8. Quick Reference

### When to Use Each Pattern

| Goal | Recommended Pattern |
|------|-------------------|
| Build narrative | Scroll Story Arc, Chapter Navigation |
| Show transformation | Before/After, Sticky Transform |
| Present features | Feature Theater, Card Stacking |
| Create atmosphere | Gradient Transitions, Ambient Motion |
| Reveal content | Fade-In, Staggered Lists |
| Show progress | Timeline, Stats Counter |
| Showcase product | Horizontal Scroll, Image Sequence |

### Performance Impact Ratings

| Pattern | CPU Impact | GPU Impact | Battery Impact |
|---------|-----------|-----------|----------------|
| CSS Scroll Animations | Low | Low | Low |
| Intersection Observer | Low | None | Low |
| Parallax (CSS) | Low | Medium | Low |
| Parallax (JS) | Medium | Medium | Medium |
| Video Scrub | High | Medium | High |
| Image Sequence (Canvas) | Medium | Medium | Medium |
| Particles (CSS) | Medium | Low | Medium |
| Particles (Canvas) | Medium-High | Medium | High |
| Gradient Animation | Low | Medium | Low |

### Browser Support Summary

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| `animation-timeline: scroll()` | 115+ | Flag | No | 115+ |
| `animation-timeline: view()` | 115+ | Flag | No | 115+ |
| Intersection Observer | 51+ | 55+ | 12.1+ | 15+ |
| CSS Sticky | 56+ | 32+ | 13+ | 16+ |
| Web Animations API | 36+ | 48+ | 13.1+ | 79+ |

---

## 9. Implementation Checklist

Before shipping scroll-based storytelling:

- [ ] All content accessible without JavaScript
- [ ] Reduced motion preference respected
- [ ] Keyboard navigation works throughout
- [ ] Focus management correct in sticky sections
- [ ] Screen reader tested
- [ ] Performance profiled (< 16ms frames)
- [ ] Battery impact considered
- [ ] Safari fallbacks implemented
- [ ] Mobile touch experience tested
- [ ] Loading states for heavy assets
- [ ] Skip links for long sections
- [ ] Progress indicators for orientation

---

## 10. Resources

- [MDN: Scroll-driven Animations](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_scroll-driven_animations)
- [web.dev: Scroll-driven Animations](https://developer.chrome.com/docs/css-ui/scroll-driven-animations)
- [WCAG 2.1 Animation Guidelines](https://www.w3.org/WAI/WCAG21/Understanding/animation-from-interactions)
- [Scroll-Driven Animations Specification](https://www.w3.org/TR/scroll-animations-1/)
