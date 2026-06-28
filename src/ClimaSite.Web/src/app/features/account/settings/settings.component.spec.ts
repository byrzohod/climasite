import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AnimationService, MotionPreference } from '../../../core/services/animation.service';
import { SettingsComponent } from './settings.component';

const translations: Record<string, string> = {
  'accessibility.reduceMotion.label': 'Reduce motion',
  'accessibility.reduceMotion.description': 'Disable non-essential animations.',
  'accessibility.reduceMotion.system': 'System',
  'accessibility.reduceMotion.on': 'On',
  'accessibility.reduceMotion.off': 'Off'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

describe('SettingsComponent', () => {
  let fixture: ComponentFixture<SettingsComponent>;
  let component: SettingsComponent;

  beforeEach(async () => {
    // Mock AnimationService so the embedded reduce-motion toggle has no
    // real scroll/media-query side effects.
    const animationService = jasmine.createSpyObj<AnimationService>(
      'AnimationService',
      ['setMotionPreference'],
      { motionPreference: signal<MotionPreference>('system') }
    );

    await TestBed.configureTestingModule({
      imports: [
        SettingsComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: AnimationService, useValue: animationService }
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    fixture = TestBed.createComponent(SettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and render the settings page container', () => {
    expect(component).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="settings-page"]')).toBeTruthy();
  });

  it('renders the translated page heading', () => {
    const heading = fixture.nativeElement.querySelector('h1') as HTMLElement;
    expect(heading.textContent).toContain(translations['accessibility.reduceMotion.label']);
  });

  it('embeds the reduce-motion toggle control', () => {
    expect(fixture.nativeElement.querySelector('[data-testid="reduce-motion-toggle"]')).toBeTruthy();
  });
});
