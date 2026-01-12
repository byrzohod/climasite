import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AddressCardComponent, AddressData } from './address-card.component';
import { TranslateModule } from '@ngx-translate/core';

describe('AddressCardComponent', () => {
  let component: AddressCardComponent;
  let fixture: ComponentFixture<AddressCardComponent>;

  const mockAddress: AddressData = {
    firstName: 'John',
    lastName: 'Doe',
    addressLine1: '123 Main St',
    addressLine2: 'Apt 4B',
    city: 'Sofia',
    state: 'Sofia City',
    postalCode: '1000',
    country: 'Bulgaria',
    phone: '+359888123456'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AddressCardComponent,
        TranslateModule.forRoot()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AddressCardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should display address when provided', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('John');
    expect(compiled.textContent).toContain('Doe');
    expect(compiled.textContent).toContain('123 Main St');
    expect(compiled.textContent).toContain('Sofia');
  });

  it('should display title when provided', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('title', 'Shipping Address');
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Shipping Address');
  });

  it('should display addressLine2 when provided', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Apt 4B');
  });

  it('should not display addressLine2 when not provided', () => {
    const addressWithoutLine2 = { ...mockAddress, addressLine2: undefined };
    fixture.componentRef.setInput('address', addressWithoutLine2);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).not.toContain('Apt 4B');
  });

  it('should display phone when showPhone is true', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('showPhone', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('+359888123456');
  });

  it('should hide phone when showPhone is false', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('showPhone', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).not.toContain('+359888123456');
  });

  it('should show copy button by default', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('title', 'Address');
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="copy-address-btn"]')).toBeTruthy();
  });

  it('should hide copy button when showCopyButton is false', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('title', 'Address');
    fixture.componentRef.setInput('showCopyButton', false);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="copy-address-btn"]')).toBeFalsy();
  });

  it('should apply compact class when compact is true', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('compact', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('.address-card.compact')).toBeTruthy();
  });

  it('should copy address to clipboard when copy button clicked', fakeAsync(() => {
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('title', 'Address');
    fixture.detectChanges();

    const copyBtn = fixture.nativeElement.querySelector('[data-testid="copy-address-btn"]');
    copyBtn.click();

    tick(); // Wait for the promise to resolve
    fixture.detectChanges();

    expect(navigator.clipboard.writeText).toHaveBeenCalled();
    expect(component.copied()).toBeTrue();
  }));

  it('should emit addressCopied event when copy succeeds', async () => {
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    fixture.componentRef.setInput('address', mockAddress);
    fixture.componentRef.setInput('title', 'Address');
    fixture.detectChanges();

    let emitted = false;
    component.addressCopied.subscribe(() => {
      emitted = true;
    });

    component.copyToClipboard();
    await fixture.whenStable();

    expect(emitted).toBeTrue();
  });

  it('should display state when provided', () => {
    fixture.componentRef.setInput('address', mockAddress);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Sofia City');
  });

  it('should handle address without state', () => {
    const addressWithoutState = { ...mockAddress, state: undefined };
    fixture.componentRef.setInput('address', addressWithoutState);
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('[data-testid="address-content"]')).toBeTruthy();
  });
});
