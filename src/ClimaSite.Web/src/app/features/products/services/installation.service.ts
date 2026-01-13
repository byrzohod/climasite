import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface InstallationOption {
  type: string;
  name: string;
  description: string;
  price: number;
  features: string[];
  estimatedDays: number;
}

export interface ProductInstallationOptions {
  productId: string;
  productName: string;
  installationAvailable: boolean;
  options: InstallationOption[];
}

export interface InstallationRequest {
  id: string;
  productId: string;
  productName: string;
  installationType: string;
  status: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  postalCode: string;
  country: string;
  preferredDate?: string;
  preferredTimeSlot?: string;
  scheduledDate?: string;
  notes?: string;
  estimatedPrice: number;
  finalPrice?: number;
  createdAt: string;
}

export interface CreateInstallationRequestData {
  productId: string;
  installationType: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  postalCode: string;
  country: string;
  preferredDate?: string;
  preferredTimeSlot?: string;
  notes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InstallationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/installation`;

  getInstallationOptions(productId: string): Observable<ProductInstallationOptions> {
    return this.http.get<ProductInstallationOptions>(`${this.apiUrl}/options/${productId}`);
  }

  createInstallationRequest(data: CreateInstallationRequestData): Observable<InstallationRequest> {
    return this.http.post<InstallationRequest>(`${this.apiUrl}/requests`, data);
  }
}
