export interface SavedAddress {
  id: string;
  fullName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
  countryCode: string;
  phone?: string;
  isDefault: boolean;
  type: AddressType;
  createdAt: string;
  updatedAt: string;
}

export type AddressType = 'Shipping' | 'Billing' | 'Both';

export interface CreateAddressRequest {
  fullName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state?: string;
  postalCode: string;
  country: string;
  countryCode: string;
  phone?: string;
  isDefault?: boolean;
  type?: AddressType;
}

export interface UpdateAddressRequest extends CreateAddressRequest {
  addressId: string;
}
