import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { AddressService } from '../../../core/services/address.service';
import { SavedAddress, CreateAddressRequest, AddressType } from '../../../core/models/address.model';

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="addresses-container">
      <div class="addresses-header">
        <h1>{{ 'account.addresses.title' | translate }}</h1>
        <button
          class="btn btn-primary"
          (click)="openAddModal()"
          data-testid="add-address-btn"
        >
          <span class="icon">+</span>
          {{ 'account.addresses.addNew' | translate }}
        </button>
      </div>

      @if (addressService.isLoading()) {
        <div class="loading-spinner">
          <div class="spinner"></div>
        </div>
      } @else if (addressService.addresses().length === 0) {
        <div class="empty-state">
          <div class="empty-icon">📍</div>
          <h2>{{ 'account.addresses.noAddresses' | translate }}</h2>
          <p>{{ 'account.addresses.noAddressesDescription' | translate }}</p>
          <button class="btn btn-primary" (click)="openAddModal()">
            {{ 'account.addresses.addFirstAddress' | translate }}
          </button>
        </div>
      } @else {
        <div class="addresses-grid">
          @for (address of addressService.addresses(); track address.id) {
            <div class="address-card" [class.default]="address.isDefault" data-testid="address-card">
              @if (address.isDefault) {
                <div class="default-badge">{{ 'account.addresses.default' | translate }}</div>
              }
              <div class="address-type">{{ address.type }}</div>
              <div class="address-content">
                <p class="name">{{ address.fullName }}</p>
                <p>{{ address.addressLine1 }}</p>
                @if (address.addressLine2) {
                  <p>{{ address.addressLine2 }}</p>
                }
                <p>{{ address.city }}@if (address.state) {, {{ address.state }}} {{ address.postalCode }}</p>
                <p>{{ address.country }}</p>
                @if (address.phone) {
                  <p class="phone">{{ address.phone }}</p>
                }
              </div>
              <div class="address-actions">
                @if (!address.isDefault) {
                  <button
                    class="btn btn-outline btn-sm"
                    (click)="setAsDefault(address)"
                    data-testid="set-default-btn"
                  >
                    {{ 'account.addresses.setAsDefault' | translate }}
                  </button>
                }
                <button
                  class="btn btn-outline btn-sm"
                  (click)="openEditModal(address)"
                  data-testid="edit-address-btn"
                >
                  {{ 'account.addresses.edit' | translate }}
                </button>
                <button
                  class="btn btn-outline btn-sm btn-danger"
                  (click)="confirmDelete(address)"
                  data-testid="delete-address-btn"
                >
                  {{ 'account.addresses.delete' | translate }}
                </button>
              </div>
            </div>
          }
        </div>
      }

      <!-- Add/Edit Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal-content" (click)="$event.stopPropagation()" data-testid="address-modal">
            <div class="modal-header">
              <h2>{{ editingAddress() ? ('account.addresses.editAddress' | translate) : ('account.addresses.addAddress' | translate) }}</h2>
              <button class="close-btn" (click)="closeModal()" aria-label="Close">&times;</button>
            </div>
            <form (ngSubmit)="saveAddress()" #addressForm="ngForm">
              <div class="form-grid">
                <div class="form-group full-width">
                  <label for="fullName">{{ 'account.addresses.fullName' | translate }} *</label>
                  <input
                    type="text"
                    id="fullName"
                    name="fullName"
                    [(ngModel)]="formData.fullName"
                    required
                    data-testid="address-fullname"
                  />
                </div>

                <div class="form-group full-width">
                  <label for="addressLine1">{{ 'account.addresses.addressLine1' | translate }} *</label>
                  <input
                    type="text"
                    id="addressLine1"
                    name="addressLine1"
                    [(ngModel)]="formData.addressLine1"
                    required
                    data-testid="address-line1"
                  />
                </div>

                <div class="form-group full-width">
                  <label for="addressLine2">{{ 'account.addresses.addressLine2' | translate }}</label>
                  <input
                    type="text"
                    id="addressLine2"
                    name="addressLine2"
                    [(ngModel)]="formData.addressLine2"
                    data-testid="address-line2"
                  />
                </div>

                <div class="form-group">
                  <label for="city">{{ 'account.addresses.city' | translate }} *</label>
                  <input
                    type="text"
                    id="city"
                    name="city"
                    [(ngModel)]="formData.city"
                    required
                    data-testid="address-city"
                  />
                </div>

                <div class="form-group">
                  <label for="state">{{ 'account.addresses.state' | translate }}</label>
                  <input
                    type="text"
                    id="state"
                    name="state"
                    [(ngModel)]="formData.state"
                    data-testid="address-state"
                  />
                </div>

                <div class="form-group">
                  <label for="postalCode">{{ 'account.addresses.postalCode' | translate }} *</label>
                  <input
                    type="text"
                    id="postalCode"
                    name="postalCode"
                    [(ngModel)]="formData.postalCode"
                    required
                    data-testid="address-postal"
                  />
                </div>

                <div class="form-group">
                  <label for="country">{{ 'account.addresses.country' | translate }} *</label>
                  <select
                    id="country"
                    name="country"
                    [(ngModel)]="formData.country"
                    (ngModelChange)="onCountryChange($event)"
                    required
                    data-testid="address-country"
                  >
                    <option value="">{{ 'account.addresses.selectCountry' | translate }}</option>
                    @for (c of countries; track c.code) {
                      <option [value]="c.name">{{ c.name }}</option>
                    }
                  </select>
                </div>

                <div class="form-group">
                  <label for="phone">{{ 'account.addresses.phone' | translate }}</label>
                  <input
                    type="tel"
                    id="phone"
                    name="phone"
                    [(ngModel)]="formData.phone"
                    data-testid="address-phone"
                  />
                </div>

                <div class="form-group">
                  <label for="type">{{ 'account.addresses.type' | translate }}</label>
                  <select
                    id="type"
                    name="type"
                    [(ngModel)]="formData.type"
                    data-testid="address-type"
                  >
                    <option value="Shipping">{{ 'account.addresses.typeShipping' | translate }}</option>
                    <option value="Billing">{{ 'account.addresses.typeBilling' | translate }}</option>
                    <option value="Both">{{ 'account.addresses.typeBoth' | translate }}</option>
                  </select>
                </div>

                <div class="form-group checkbox-group full-width">
                  <label class="checkbox-label">
                    <input
                      type="checkbox"
                      name="isDefault"
                      [(ngModel)]="formData.isDefault"
                      data-testid="address-default"
                    />
                    {{ 'account.addresses.makeDefault' | translate }}
                  </label>
                </div>
              </div>

              <div class="modal-actions">
                <button type="button" class="btn btn-outline" (click)="closeModal()">
                  {{ 'common.cancel' | translate }}
                </button>
                <button
                  type="submit"
                  class="btn btn-primary"
                  [disabled]="!addressForm.valid || addressService.isLoading()"
                  data-testid="save-address-btn"
                >
                  @if (addressService.isLoading()) {
                    <span class="spinner-sm"></span>
                  }
                  {{ editingAddress() ? ('common.save' | translate) : ('common.add' | translate) }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Delete Confirmation Modal -->
      @if (showDeleteConfirm()) {
        <div class="modal-overlay" (click)="cancelDelete()">
          <div class="modal-content modal-sm" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h2>{{ 'account.addresses.confirmDelete' | translate }}</h2>
            </div>
            <p>{{ 'account.addresses.confirmDeleteMessage' | translate }}</p>
            <div class="modal-actions">
              <button type="button" class="btn btn-outline" (click)="cancelDelete()">
                {{ 'common.cancel' | translate }}
              </button>
              <button
                type="button"
                class="btn btn-danger"
                (click)="deleteAddress()"
                data-testid="confirm-delete-btn"
              >
                {{ 'common.delete' | translate }}
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .addresses-container {
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
    }

    .addresses-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;

      h1 {
        font-size: 2rem;
        margin: 0;
        color: var(--color-text-primary);
      }
    }

    .btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border-radius: 8px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
      border: none;

      &.btn-primary {
        background-color: var(--color-primary);
        color: white;

        &:hover {
          background-color: var(--color-primary-dark);
        }

        &:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }
      }

      &.btn-outline {
        background: transparent;
        border: 1px solid var(--color-border);
        color: var(--color-text-primary);

        &:hover {
          background-color: var(--color-bg-secondary);
        }
      }

      &.btn-danger {
        background-color: var(--color-error);
        color: white;

        &:hover {
          opacity: 0.9;
        }
      }

      &.btn-sm {
        padding: 0.5rem 1rem;
        font-size: 0.875rem;
      }

      .icon {
        font-size: 1.25rem;
      }
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 4rem 0;

      .spinner {
        width: 40px;
        height: 40px;
        border: 3px solid var(--color-border);
        border-top-color: var(--color-primary);
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .empty-state {
      text-align: center;
      padding: 4rem 2rem;
      background-color: var(--color-bg-secondary);
      border-radius: 12px;

      .empty-icon {
        font-size: 4rem;
        margin-bottom: 1rem;
      }

      h2 {
        color: var(--color-text-primary);
        margin-bottom: 0.5rem;
      }

      p {
        color: var(--color-text-secondary);
        margin-bottom: 1.5rem;
      }
    }

    .addresses-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.5rem;
    }

    .address-card {
      background-color: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      padding: 1.5rem;
      position: relative;

      &.default {
        border-color: var(--color-primary);
        box-shadow: 0 0 0 1px var(--color-primary);
      }

      .default-badge {
        position: absolute;
        top: -10px;
        right: 12px;
        background-color: var(--color-primary);
        color: white;
        padding: 0.25rem 0.75rem;
        border-radius: 4px;
        font-size: 0.75rem;
        font-weight: 600;
      }

      .address-type {
        font-size: 0.75rem;
        color: var(--color-text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-bottom: 0.75rem;
      }

      .address-content {
        p {
          margin: 0;
          color: var(--color-text-primary);
          line-height: 1.5;

          &.name {
            font-weight: 600;
            margin-bottom: 0.25rem;
          }

          &.phone {
            margin-top: 0.5rem;
            color: var(--color-text-secondary);
          }
        }
      }

      .address-actions {
        display: flex;
        gap: 0.5rem;
        margin-top: 1rem;
        padding-top: 1rem;
        border-top: 1px solid var(--color-border);
        flex-wrap: wrap;
      }
    }

    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }

    .modal-content {
      background-color: var(--color-bg-primary);
      border-radius: 12px;
      width: 100%;
      max-width: 600px;
      max-height: 90vh;
      overflow-y: auto;
      padding: 1.5rem;

      &.modal-sm {
        max-width: 400px;
      }
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.5rem;

      h2 {
        margin: 0;
        color: var(--color-text-primary);
      }

      .close-btn {
        background: none;
        border: none;
        font-size: 1.5rem;
        cursor: pointer;
        color: var(--color-text-secondary);

        &:hover {
          color: var(--color-text-primary);
        }
      }
    }

    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;

      .full-width {
        grid-column: 1 / -1;
      }
    }

    .form-group {
      label {
        display: block;
        margin-bottom: 0.5rem;
        font-weight: 500;
        color: var(--color-text-primary);
      }

      input, select {
        width: 100%;
        padding: 0.75rem;
        border: 1px solid var(--color-border);
        border-radius: 8px;
        background-color: var(--color-bg-primary);
        color: var(--color-text-primary);
        font-size: 1rem;

        &:focus {
          outline: none;
          border-color: var(--color-primary);
          box-shadow: 0 0 0 2px rgba(var(--color-primary-rgb), 0.2);
        }
      }
    }

    .checkbox-group {
      .checkbox-label {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        cursor: pointer;

        input[type="checkbox"] {
          width: auto;
        }
      }
    }

    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
      margin-top: 1.5rem;
      padding-top: 1rem;
      border-top: 1px solid var(--color-border);
    }

    .spinner-sm {
      width: 16px;
      height: 16px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      display: inline-block;
    }

    @media (max-width: 640px) {
      .addresses-header {
        flex-direction: column;
        gap: 1rem;
        align-items: flex-start;
      }

      .form-grid {
        grid-template-columns: 1fr;

        .full-width {
          grid-column: 1;
        }
      }

      .address-actions {
        flex-direction: column;

        .btn {
          width: 100%;
          justify-content: center;
        }
      }
    }
  `]
})
export class AddressesComponent implements OnInit {
  readonly addressService = inject(AddressService);

  showModal = signal(false);
  showDeleteConfirm = signal(false);
  editingAddress = signal<SavedAddress | null>(null);
  addressToDelete = signal<SavedAddress | null>(null);

  formData: CreateAddressRequest = this.getEmptyFormData();

  countries = [
    { name: 'Bulgaria', code: 'BG' },
    { name: 'Germany', code: 'DE' },
    { name: 'United States', code: 'US' },
    { name: 'United Kingdom', code: 'GB' },
    { name: 'France', code: 'FR' },
    { name: 'Italy', code: 'IT' },
    { name: 'Spain', code: 'ES' },
    { name: 'Austria', code: 'AT' },
    { name: 'Netherlands', code: 'NL' },
    { name: 'Belgium', code: 'BE' },
    { name: 'Poland', code: 'PL' },
    { name: 'Romania', code: 'RO' },
    { name: 'Greece', code: 'GR' }
  ];

  ngOnInit(): void {
    this.addressService.loadAddresses();
  }

  private getEmptyFormData(): CreateAddressRequest {
    return {
      fullName: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postalCode: '',
      country: '',
      countryCode: '',
      phone: '',
      isDefault: false,
      type: 'Shipping'
    };
  }

  openAddModal(): void {
    this.formData = this.getEmptyFormData();
    this.editingAddress.set(null);
    this.showModal.set(true);
  }

  openEditModal(address: SavedAddress): void {
    this.formData = {
      fullName: address.fullName,
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2 || '',
      city: address.city,
      state: address.state || '',
      postalCode: address.postalCode,
      country: address.country,
      countryCode: address.countryCode,
      phone: address.phone || '',
      isDefault: address.isDefault,
      type: address.type
    };
    this.editingAddress.set(address);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.editingAddress.set(null);
  }

  onCountryChange(countryName: string): void {
    const country = this.countries.find(c => c.name === countryName);
    if (country) {
      this.formData.countryCode = country.code;
    }
  }

  saveAddress(): void {
    // Ensure countryCode is set
    if (!this.formData.countryCode && this.formData.country) {
      this.onCountryChange(this.formData.country);
    }

    const editing = this.editingAddress();
    if (editing) {
      this.addressService.updateAddress(editing.id, this.formData).subscribe(result => {
        if (result.succeeded) {
          this.closeModal();
        }
      });
    } else {
      this.addressService.createAddress(this.formData).subscribe(result => {
        if (result.succeeded) {
          this.closeModal();
        }
      });
    }
  }

  setAsDefault(address: SavedAddress): void {
    this.addressService.setDefaultAddress(address.id).subscribe();
  }

  confirmDelete(address: SavedAddress): void {
    this.addressToDelete.set(address);
    this.showDeleteConfirm.set(true);
  }

  cancelDelete(): void {
    this.showDeleteConfirm.set(false);
    this.addressToDelete.set(null);
  }

  deleteAddress(): void {
    const address = this.addressToDelete();
    if (address) {
      this.addressService.deleteAddress(address.id).subscribe(result => {
        if (result.succeeded) {
          this.cancelDelete();
        }
      });
    }
  }
}
