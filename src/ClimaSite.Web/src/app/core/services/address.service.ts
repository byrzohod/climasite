import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SavedAddress, CreateAddressRequest } from '../models/address.model';

export interface ApiResult<T> {
  succeeded: boolean;
  value?: T;
  error?: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AddressService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/addresses`;

  // Signal-based state
  private readonly _addresses = signal<SavedAddress[]>([]);
  private readonly _isLoading = signal(false);
  private readonly _error = signal<string | null>(null);

  // Public readonly signals
  readonly addresses = this._addresses.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();

  // Computed signals
  readonly defaultAddress = computed(() => this._addresses().find(a => a.isDefault) ?? null);
  readonly shippingAddresses = computed(() => this._addresses().filter(a => a.type === 'Shipping' || a.type === 'Both'));
  readonly billingAddresses = computed(() => this._addresses().filter(a => a.type === 'Billing' || a.type === 'Both'));
  readonly hasAddresses = computed(() => this._addresses().length > 0);

  loadAddresses(): void {
    this._isLoading.set(true);
    this._error.set(null);

    this.http.get<SavedAddress[]>(this.apiUrl)
      .pipe(
        tap(addresses => {
          this._addresses.set(addresses);
          this._isLoading.set(false);
        }),
        catchError(error => {
          console.error('Failed to load addresses:', error);
          this._addresses.set([]);
          this._error.set('account.addresses.errors.loadFailed');
          this._isLoading.set(false);
          return of([]);
        })
      )
      .subscribe();
  }

  getAddress(id: string): Observable<SavedAddress | null> {
    return this.http.get<SavedAddress>(`${this.apiUrl}/${id}`)
      .pipe(
        catchError(error => {
          console.error('Failed to get address:', error);
          return of(null);
        })
      );
  }

  createAddress(request: CreateAddressRequest): Observable<ApiResult<SavedAddress>> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http.post<SavedAddress>(this.apiUrl, request)
      .pipe(
        map(address => {
          const currentAddresses = this._addresses();
          // If new address is default, update other addresses
          if (address.isDefault) {
            const updated = currentAddresses.map(a => ({ ...a, isDefault: false }));
            this._addresses.set([...updated, address]);
          } else {
            this._addresses.set([...currentAddresses, address]);
          }
          this._isLoading.set(false);
          return { succeeded: true, value: address };
        }),
        catchError(error => {
          console.error('Failed to create address:', error);
          this._error.set('account.addresses.errors.createFailed');
          this._isLoading.set(false);
          return of({ succeeded: false, error: 'account.addresses.errors.createFailed' });
        })
      );
  }

  updateAddress(id: string, request: CreateAddressRequest): Observable<ApiResult<SavedAddress>> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http.put<SavedAddress>(`${this.apiUrl}/${id}`, { ...request, addressId: id })
      .pipe(
        map(address => {
          const currentAddresses = this._addresses();
          // If updated address is default, update other addresses
          const updated = currentAddresses.map(a => {
            if (a.id === id) {
              return address;
            }
            if (address.isDefault) {
              return { ...a, isDefault: false };
            }
            return a;
          });
          this._addresses.set(updated);
          this._isLoading.set(false);
          return { succeeded: true, value: address };
        }),
        catchError(error => {
          console.error('Failed to update address:', error);
          this._error.set('account.addresses.errors.updateFailed');
          this._isLoading.set(false);
          return of({ succeeded: false, error: 'account.addresses.errors.updateFailed' });
        })
      );
  }

  deleteAddress(id: string): Observable<ApiResult<boolean>> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http.delete<void>(`${this.apiUrl}/${id}`)
      .pipe(
        map(() => {
          const currentAddresses = this._addresses();
          this._addresses.set(currentAddresses.filter(a => a.id !== id));
          this._isLoading.set(false);
          return { succeeded: true, value: true };
        }),
        catchError(error => {
          console.error('Failed to delete address:', error);
          this._error.set('account.addresses.errors.deleteFailed');
          this._isLoading.set(false);
          return of({ succeeded: false, error: 'account.addresses.errors.deleteFailed' });
        })
      );
  }

  setDefaultAddress(id: string): Observable<ApiResult<SavedAddress>> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http.put<SavedAddress>(`${this.apiUrl}/${id}/default`, {})
      .pipe(
        map(address => {
          const currentAddresses = this._addresses();
          const updated = currentAddresses.map(a => {
            if (a.id === id) {
              return address;
            }
            return { ...a, isDefault: false };
          });
          this._addresses.set(updated);
          this._isLoading.set(false);
          return { succeeded: true, value: address };
        }),
        catchError(error => {
          console.error('Failed to set default address:', error);
          this._error.set('account.addresses.errors.setDefaultFailed');
          this._isLoading.set(false);
          return of({ succeeded: false, error: 'account.addresses.errors.setDefaultFailed' });
        })
      );
  }

  clearAddresses(): void {
    this._addresses.set([]);
    this._error.set(null);
  }
}
