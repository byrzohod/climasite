import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ShareProductComponent } from './share-product.component';
import { TranslateModule } from '@ngx-translate/core';
import { PLATFORM_ID } from '@angular/core';

describe('ShareProductComponent', () => {
  let component: ShareProductComponent;
  let fixture: ComponentFixture<ShareProductComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ShareProductComponent,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ShareProductComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('productName', 'Test Air Conditioner');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display share button', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('.share-trigger');
    expect(button).toBeTruthy();
  });

  it('should be closed by default', () => {
    expect(component.isOpen()).toBeFalse();
    const compiled = fixture.nativeElement;
    const dropdown = compiled.querySelector('.share-dropdown');
    expect(dropdown).toBeFalsy();
  });

  it('should open dropdown when clicked', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('.share-trigger');
    button.click();
    fixture.detectChanges();

    expect(component.isOpen()).toBeTrue();
    const dropdown = compiled.querySelector('.share-dropdown');
    expect(dropdown).toBeTruthy();
  });

  it('should close dropdown when clicked again', () => {
    component.toggleDropdown();
    fixture.detectChanges();
    expect(component.isOpen()).toBeTrue();

    component.toggleDropdown();
    fixture.detectChanges();
    expect(component.isOpen()).toBeFalse();
  });

  it('should display share options when open', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const options = compiled.querySelectorAll('.share-option');
    // Copy link + 4 share options
    expect(options.length).toBe(5);
  });

  it('should have Facebook share option', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const facebook = compiled.querySelector('[data-testid="share-facebook"]');
    expect(facebook).toBeTruthy();
  });

  it('should have Twitter share option', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const twitter = compiled.querySelector('[data-testid="share-twitter"]');
    expect(twitter).toBeTruthy();
  });

  it('should have WhatsApp share option', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const whatsapp = compiled.querySelector('[data-testid="share-whatsapp"]');
    expect(whatsapp).toBeTruthy();
  });

  it('should have Email share option', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const email = compiled.querySelector('[data-testid="share-email"]');
    expect(email).toBeTruthy();
  });

  it('should have copy link button', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const copyLink = compiled.querySelector('[data-testid="copy-link"]');
    expect(copyLink).toBeTruthy();
  });

  it('should show copied state after copying', fakeAsync(async () => {
    // Mock clipboard API
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    component.toggleDropdown();
    fixture.detectChanges();

    await component.copyToClipboard();
    fixture.detectChanges();

    expect(component.copied()).toBeTrue();

    tick(2000);
    expect(component.copied()).toBeFalse();
  }));

  it('should close dropdown when share option is clicked', () => {
    component.toggleDropdown();
    fixture.detectChanges();

    component.closeDropdown();
    fixture.detectChanges();

    expect(component.isOpen()).toBeFalse();
  });

  it('should use custom productUrl if provided', fakeAsync(() => {
    fixture.componentRef.setInput('productUrl', 'https://example.com/custom-product');
    fixture.detectChanges();

    component.toggleDropdown();
    tick(100);
    fixture.detectChanges();

    const options = component.shareOptions();
    const facebook = options.find(o => o.name === 'Facebook');
    expect(facebook?.url).toContain('example.com');
  }));

  it('should encode product name in share URLs', fakeAsync(() => {
    fixture.componentRef.setInput('productName', 'Test & Special Product');
    fixture.detectChanges();

    component.toggleDropdown();
    tick(100);
    fixture.detectChanges();

    const options = component.shareOptions();
    const twitter = options.find(o => o.name === 'Twitter');
    expect(twitter?.url).toContain('text=');
  }));

  it('should set aria-expanded attribute correctly', () => {
    const compiled = fixture.nativeElement;
    const button = compiled.querySelector('.share-trigger');

    expect(button.getAttribute('aria-expanded')).toBe('false');

    component.toggleDropdown();
    fixture.detectChanges();

    expect(button.getAttribute('aria-expanded')).toBe('true');
  });
});
