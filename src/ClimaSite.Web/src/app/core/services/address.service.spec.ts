import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AddressService } from './address.service';
import { SavedAddress, CreateAddressRequest } from '../models/address.model';
import { environment } from '../../../environments/environment';

describe('AddressService', () => {
  let service: AddressService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/addresses`;

  const addressOne: SavedAddress = {
    id: 'address-1',
    fullName: 'Test User',
    addressLine1: '123 Test Street',
    addressLine2: '',
    city: 'Sofia',
    state: 'Sofia City',
    postalCode: '1000',
    country: 'Bulgaria',
    countryCode: 'BG',
    phone: '+359888123456',
    isDefault: true,
    type: 'Shipping',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };

  const addressTwo: SavedAddress = {
    ...addressOne,
    id: 'address-2',
    fullName: 'Billing User',
    addressLine1: '456 Billing Street',
    isDefault: false,
    type: 'Both'
  };

  const createRequest: CreateAddressRequest = {
    fullName: 'Test User',
    addressLine1: '123 Test Street',
    addressLine2: '',
    city: 'Sofia',
    state: 'Sofia City',
    postalCode: '1000',
    country: 'Bulgaria',
    countryCode: 'BG',
    phone: '+359888123456',
    isDefault: true,
    type: 'Shipping'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AddressService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AddressService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load addresses from the API', fakeAsync(() => {
    service.loadAddresses();
    tick();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('GET');
    req.flush([addressOne, addressTwo]);
    tick();

    expect(service.addresses()).toEqual([addressOne, addressTwo]);
    expect(service.defaultAddress()).toEqual(addressOne);
    expect(service.shippingAddresses()).toEqual([addressOne, addressTwo]);
    expect(service.billingAddresses()).toEqual([addressTwo]);
    expect(service.isLoading()).toBeFalse();
  }));

  it('should create an address from a raw address DTO response', fakeAsync(() => {
    let succeeded = false;

    service.createAddress(createRequest).subscribe(result => {
      succeeded = result.succeeded;
      expect(result.value).toEqual(addressOne);
    });
    tick();

    const req = httpMock.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createRequest);
    req.flush(addressOne);
    tick();

    expect(succeeded).toBeTrue();
    expect(service.addresses()).toEqual([addressOne]);
    expect(service.defaultAddress()).toEqual(addressOne);
    expect(service.isLoading()).toBeFalse();
  }));

  it('should clear the previous default when creating a default address', fakeAsync(() => {
    service.loadAddresses();
    const loadReq = httpMock.expectOne(apiUrl);
    loadReq.flush([addressOne]);
    tick();

    const newDefault = { ...addressTwo, isDefault: true };

    service.createAddress({ ...createRequest, fullName: 'Billing User', type: 'Both' }).subscribe();
    const createReq = httpMock.expectOne(apiUrl);
    createReq.flush(newDefault);
    tick();

    expect(service.addresses()).toEqual([
      { ...addressOne, isDefault: false },
      newDefault
    ]);
    expect(service.defaultAddress()).toEqual(newDefault);
  }));

  it('should update an address from a raw address DTO response', fakeAsync(() => {
    service.loadAddresses();
    const loadReq = httpMock.expectOne(apiUrl);
    loadReq.flush([addressOne, addressTwo]);
    tick();

    const updated = { ...addressTwo, isDefault: true };

    service.updateAddress(addressTwo.id, { ...createRequest, fullName: addressTwo.fullName, type: 'Both' }).subscribe(result => {
      expect(result.succeeded).toBeTrue();
      expect(result.value).toEqual(updated);
    });

    const updateReq = httpMock.expectOne(`${apiUrl}/${addressTwo.id}`);
    expect(updateReq.request.method).toBe('PUT');
    updateReq.flush(updated);
    tick();

    expect(service.addresses()).toEqual([
      { ...addressOne, isDefault: false },
      updated
    ]);
  }));

  it('should delete an address after a no-content API response', fakeAsync(() => {
    service.loadAddresses();
    const loadReq = httpMock.expectOne(apiUrl);
    loadReq.flush([addressOne, addressTwo]);
    tick();

    service.deleteAddress(addressOne.id).subscribe(result => {
      expect(result.succeeded).toBeTrue();
      expect(result.value).toBeTrue();
    });

    const deleteReq = httpMock.expectOne(`${apiUrl}/${addressOne.id}`);
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null);
    tick();

    expect(service.addresses()).toEqual([addressTwo]);
  }));

  it('should set default address from a raw address DTO response', fakeAsync(() => {
    service.loadAddresses();
    const loadReq = httpMock.expectOne(apiUrl);
    loadReq.flush([addressOne, addressTwo]);
    tick();

    const newDefault = { ...addressTwo, isDefault: true };

    service.setDefaultAddress(addressTwo.id).subscribe(result => {
      expect(result.succeeded).toBeTrue();
      expect(result.value).toEqual(newDefault);
    });

    const defaultReq = httpMock.expectOne(`${apiUrl}/${addressTwo.id}/default`);
    expect(defaultReq.request.method).toBe('PUT');
    defaultReq.flush(newDefault);
    tick();

    expect(service.addresses()).toEqual([
      { ...addressOne, isDefault: false },
      newDefault
    ]);
    expect(service.defaultAddress()).toEqual(newDefault);
  }));

  it('should expose translated error keys on create failure', fakeAsync(() => {
    service.createAddress(createRequest).subscribe(result => {
      expect(result.succeeded).toBeFalse();
      expect(result.error).toBe('account.addresses.errors.createFailed');
    });

    const req = httpMock.expectOne(apiUrl);
    req.error(new ErrorEvent('Network error'));
    tick();

    expect(service.error()).toBe('account.addresses.errors.createFailed');
    expect(service.isLoading()).toBeFalse();
  }));
});
