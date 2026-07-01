import { WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AddressService } from '../../../core/services/address.service';
import { SavedAddress } from '../../../core/models/address.model';
import { AddressesComponent } from './addresses.component';

const translations: Record<string, string> = {
  'account.addresses.title': 'Saved Addresses',
  'account.addresses.addNew': 'Add New Address',
  'account.addresses.default': 'Default',
  'account.addresses.setAsDefault': 'Set as Default',
  'account.addresses.edit': 'Edit',
  'account.addresses.delete': 'Delete',
  'account.addresses.typeShipping': 'Localized Shipping',
  'account.addresses.typeBilling': 'Localized Billing',
  'account.addresses.typeBoth': 'Localized Both'
};

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(_lang: string): Observable<Record<string, string>> {
    return of(translations);
  }
}

describe('AddressesComponent', () => {
  let fixture: ComponentFixture<AddressesComponent>;
  let component: AddressesComponent;
  let addressService: {
    addresses: WritableSignal<SavedAddress[]>;
    isLoading: WritableSignal<boolean>;
    loadAddresses: jasmine.Spy;
  };

  const savedAddress: SavedAddress = {
    id: 'address-1',
    fullName: 'Test User',
    addressLine1: '1 Test Street',
    city: 'Sofia',
    postalCode: '1000',
    country: 'Bulgaria',
    countryCode: 'BG',
    isDefault: true,
    type: 'Both',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };

  beforeEach(async () => {
    addressService = {
      addresses: signal([savedAddress]),
      isLoading: signal(false),
      loadAddresses: jasmine.createSpy('loadAddresses')
    };

    await TestBed.configureTestingModule({
      imports: [
        AddressesComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: AddressService, useValue: addressService }
      ]
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', translations);
    translate.use('en');

    fixture = TestBed.createComponent(AddressesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders saved address types through translation keys', () => {
    const addressType = fixture.nativeElement.querySelector('.address-type') as HTMLElement;

    expect(addressType.textContent?.trim()).toBe('Localized Both');
    expect(addressType.textContent).not.toBe('Both');
  });

  it('loads addresses on init', () => {
    expect(addressService.loadAddresses).toHaveBeenCalled();
  });

  // B-001: the hand-rolled `.modal-overlay` (no role/aria/Escape/focus-trap) is replaced by the
  // shared accessible <app-modal>, which provides role=dialog + aria-modal + Escape + focus trap +
  // focus restore.
  describe('B-001: accessible address dialogs via <app-modal>', () => {
    afterEach(() => {
      // <app-modal> locks body scroll while open; reset so other specs aren't affected.
      document.body.style.overflow = '';
    });

    it('renders the add dialog as an <app-modal> with role=dialog + aria-modal (no legacy overlay)', () => {
      component.openAddModal();
      fixture.detectChanges();

      const modal = fixture.debugElement.query(By.css('app-modal'));
      expect(modal).withContext('add/edit dialog should be an <app-modal>').toBeTruthy();

      const dialog = fixture.debugElement.query(By.css('[data-testid="address-modal"]'));
      expect(dialog).toBeTruthy();
      expect(dialog.nativeElement.getAttribute('role')).toBe('dialog');
      expect(dialog.nativeElement.getAttribute('aria-modal')).toBe('true');

      // The old hand-rolled overlay is gone.
      expect(fixture.debugElement.query(By.css('.modal-overlay'))).toBeNull();
    });

    it('renders the delete-confirm dialog as an <app-modal> with role=dialog', () => {
      component.confirmDelete(savedAddress);
      fixture.detectChanges();

      const dialog = fixture.debugElement.query(By.css('[data-testid="delete-address-modal"]'));
      expect(dialog).toBeTruthy();
      expect(dialog.nativeElement.getAttribute('role')).toBe('dialog');
      expect(dialog.nativeElement.getAttribute('aria-modal')).toBe('true');
      // The confirm button is preserved inside the shared modal's footer slot.
      expect(fixture.debugElement.query(By.css('[data-testid="confirm-delete-btn"]'))).toBeTruthy();
      expect(fixture.debugElement.query(By.css('.modal-overlay'))).toBeNull();
    });

    it('closes the add dialog when the modal (closed) output fires', () => {
      component.openAddModal();
      fixture.detectChanges();
      expect(component.showModal()).toBeTrue();

      const closeButton = fixture.debugElement.query(By.css('.modal-close'));
      closeButton.nativeElement.click();
      fixture.detectChanges();

      expect(component.showModal()).toBeFalse();
      expect(fixture.debugElement.query(By.css('[data-testid="address-modal"]'))).toBeNull();
    });

    it('closes the add dialog on Escape (shared modal closeOnEscape)', fakeAsync(() => {
      component.openAddModal();
      fixture.detectChanges();
      tick();

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges();

      expect(component.showModal()).toBeFalse();
    }));

    it('closes the delete dialog on Escape', fakeAsync(() => {
      component.confirmDelete(savedAddress);
      fixture.detectChanges();
      tick();

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges();

      expect(component.showDeleteConfirm()).toBeFalse();
    }));
  });

  // B-001 (council MEDIUM): the dialogs are wrapped in @if, so closing DESTROYS the modal before its
  // isOpen->false focus-restore path runs. ModalComponent.ngOnDestroy must restore focus to the
  // trigger so keyboard/SR focus order isn't lost on <body> (WCAG 2.4.3). These tests need the
  // fixture attached to the live document for document.activeElement to be meaningful.
  describe('B-001: focus returns to the trigger after a dialog is destroyed on close', () => {
    beforeEach(() => {
      document.body.appendChild(fixture.nativeElement);
    });

    afterEach(() => {
      document.body.style.overflow = '';
      fixture.nativeElement.remove();
    });

    it('restores focus to add-address-btn after the add dialog closes on Escape', fakeAsync(() => {
      const addBtn = fixture.debugElement.query(By.css('[data-testid="add-address-btn"]')).nativeElement as HTMLButtonElement;
      addBtn.focus();
      expect(document.activeElement).toBe(addBtn);

      component.openAddModal();
      fixture.detectChanges();
      tick(); // flush the focus-trap setTimeout — focus moves into the dialog
      expect(document.activeElement).not.toBe(addBtn);

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges(); // @if destroys the modal -> ngOnDestroy restores focus
      tick();

      expect(document.activeElement).toBe(addBtn);
    }));

    it('restores focus to delete-address-btn after the delete dialog closes on Escape', fakeAsync(() => {
      const delBtn = fixture.debugElement.query(By.css('[data-testid="delete-address-btn"]')).nativeElement as HTMLButtonElement;
      delBtn.focus();
      expect(document.activeElement).toBe(delBtn);

      component.confirmDelete(savedAddress);
      fixture.detectChanges();
      tick();

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges();
      tick();

      expect(document.activeElement).toBe(delBtn);
    }));
  });
});
