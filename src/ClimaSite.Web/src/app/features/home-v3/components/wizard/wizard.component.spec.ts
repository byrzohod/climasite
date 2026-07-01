import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';

import { HomeWizardStateService } from '../../services/home-wizard-state.service';
import { WizardComponent } from './wizard.component';

describe('WizardComponent', () => {
  let fixture: ComponentFixture<WizardComponent>;
  let state: HomeWizardStateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        WizardComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WizardComponent);
    state = TestBed.inject(HomeWizardStateService);
    fixture.detectChanges();
  });

  it('renders the default wizard state with accessible controls', () => {
    const slider = fixture.debugElement.query(By.css('[data-testid="home-v3-area-slider"]')).nativeElement as HTMLInputElement;
    const livingButton = fixture.debugElement.query(By.css('[data-testid="home-v3-room-living"]')).nativeElement as HTMLButtonElement;
    const bedroomButton = fixture.debugElement.query(By.css('[data-testid="home-v3-room-bedroom"]')).nativeElement as HTMLButtonElement;
    const zoneButton = fixture.debugElement.query(By.css('[data-testid="home-v3-zone-B"]')).nativeElement as HTMLButtonElement;
    const zoneCButton = fixture.debugElement.query(By.css('[data-testid="home-v3-zone-C"]')).nativeElement as HTMLButtonElement;

    expect(slider.value).toBe('35');
    expect(slider.getAttribute('aria-valuenow')).toBe('35');
    expect(livingButton.getAttribute('role')).toBe('radio');
    expect(livingButton.getAttribute('aria-checked')).toBe('true');
    expect(livingButton.getAttribute('tabindex')).toBe('0');
    expect(bedroomButton.getAttribute('tabindex')).toBe('-1');
    expect(zoneButton.getAttribute('aria-checked')).toBe('true');
    expect(zoneButton.getAttribute('tabindex')).toBe('0');
    expect(zoneCButton.getAttribute('tabindex')).toBe('-1');
  });

  it('updates the shared state from area, room, and zone controls', () => {
    const slider = fixture.debugElement.query(By.css('[data-testid="home-v3-area-slider"]')).nativeElement as HTMLInputElement;
    slider.value = '42';
    slider.dispatchEvent(new Event('input'));

    fixture.debugElement.query(By.css('[data-testid="home-v3-room-bedroom"]')).nativeElement.click();
    fixture.debugElement.query(By.css('[data-testid="home-v3-zone-C"]')).nativeElement.click();
    fixture.detectChanges();

    expect(state.area()).toBe(42);
    expect(state.roomType()).toBe('bedroom');
    expect(state.zone()).toBe('C');
    expect(fixture.debugElement.query(By.css('[data-testid="home-v3-room-bedroom"]')).nativeElement.getAttribute('aria-checked')).toBe('true');
    expect(fixture.debugElement.query(By.css('[data-testid="home-v3-zone-C"]')).nativeElement.getAttribute('aria-checked')).toBe('true');
  });

  it('supports arrow-key navigation for radio groups', async () => {
    const livingButton = fixture.debugElement.query(By.css('[data-testid="home-v3-room-living"]')).nativeElement as HTMLButtonElement;
    const zoneBButton = fixture.debugElement.query(By.css('[data-testid="home-v3-zone-B"]')).nativeElement as HTMLButtonElement;

    livingButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const bedroomButton = fixture.debugElement.query(By.css('[data-testid="home-v3-room-bedroom"]')).nativeElement as HTMLButtonElement;
    expect(state.roomType()).toBe('bedroom');
    expect(bedroomButton.getAttribute('aria-checked')).toBe('true');
    expect(bedroomButton.getAttribute('tabindex')).toBe('0');

    bedroomButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'End' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const commercialButton = fixture.debugElement.query(By.css('[data-testid="home-v3-room-commercial"]')).nativeElement as HTMLButtonElement;
    expect(state.roomType()).toBe('commercial');
    expect(commercialButton.getAttribute('aria-checked')).toBe('true');
    expect(commercialButton.getAttribute('tabindex')).toBe('0');

    zoneBButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const zoneAButton = fixture.debugElement.query(By.css('[data-testid="home-v3-zone-A"]')).nativeElement as HTMLButtonElement;
    expect(state.zone()).toBe('A');
    expect(zoneAButton.getAttribute('aria-checked')).toBe('true');
    expect(zoneAButton.getAttribute('tabindex')).toBe('0');
  });

  it('prevents Home and End browser defaults even when the selected option does not change', () => {
    const livingButton = fixture.debugElement.query(By.css('[data-testid="home-v3-room-living"]')).nativeElement as HTMLButtonElement;
    const homeEvent = new KeyboardEvent('keydown', { key: 'Home' });
    spyOn(homeEvent, 'preventDefault').and.callThrough();

    livingButton.dispatchEvent(homeEvent);

    expect(homeEvent.preventDefault).toHaveBeenCalled();
    expect(state.roomType()).toBe('living');
  });
});
