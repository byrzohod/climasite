import { WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
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
});
