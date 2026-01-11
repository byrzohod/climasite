import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FooterComponent } from './footer.component';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';

describe('FooterComponent', () => {
  let component: FooterComponent;
  let fixture: ComponentFixture<FooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        FooterComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FooterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have footer element', () => {
    const footer = fixture.nativeElement.querySelector('[data-testid="footer"]');
    expect(footer).toBeTruthy();
  });

  it('should have newsletter form', () => {
    const input = fixture.nativeElement.querySelector('[data-testid="newsletter-input"]');
    const submit = fixture.nativeElement.querySelector('[data-testid="newsletter-submit"]');
    expect(input).toBeTruthy();
    expect(submit).toBeTruthy();
  });

  it('should have current year in copyright', () => {
    const currentYear = new Date().getFullYear();
    expect(component.currentYear).toBe(currentYear);
  });

  it('should have social links', () => {
    const socialLinks = fixture.nativeElement.querySelectorAll('.social-link');
    expect(socialLinks.length).toBeGreaterThan(0);
  });
});
