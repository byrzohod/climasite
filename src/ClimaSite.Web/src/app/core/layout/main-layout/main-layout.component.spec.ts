import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MainLayoutComponent } from './main-layout.component';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { PLATFORM_ID } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('MainLayoutComponent', () => {
  let component: MainLayoutComponent;
  let fixture: ComponentFixture<MainLayoutComponent>;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [
        MainLayoutComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ],
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' },
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have main layout element', () => {
    const layout = fixture.nativeElement.querySelector('[data-testid="main-layout"]');
    expect(layout).toBeTruthy();
  });

  it('should have header', () => {
    const header = fixture.nativeElement.querySelector('[data-testid="header"]');
    expect(header).toBeTruthy();
  });

  it('should have main content', () => {
    const main = fixture.nativeElement.querySelector('[data-testid="main-content"]');
    expect(main).toBeTruthy();
  });

  it('should have footer', () => {
    const footer = fixture.nativeElement.querySelector('[data-testid="footer"]');
    expect(footer).toBeTruthy();
  });
});
