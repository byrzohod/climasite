import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { InstallationService, ProductInstallationOptions, CreateInstallationRequestData } from './installation.service';
import { environment } from '../../../../environments/environment';

describe('InstallationService', () => {
  let service: InstallationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        InstallationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(InstallationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getInstallationOptions', () => {
    it('should fetch installation options for a product', () => {
      const productId = '123e4567-e89b-12d3-a456-426614174000';
      const mockResponse: ProductInstallationOptions = {
        productId,
        productName: 'Test AC Unit',
        installationAvailable: true,
        options: [
          {
            type: 'Standard',
            name: 'Standard Installation',
            description: 'Basic installation',
            price: 150,
            features: ['Feature 1', 'Feature 2'],
            estimatedDays: 7
          }
        ]
      };

      service.getInstallationOptions(productId).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/installation/options/${productId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('createInstallationRequest', () => {
    it('should submit an installation request', () => {
      const requestData: CreateInstallationRequestData = {
        productId: '123e4567-e89b-12d3-a456-426614174000',
        installationType: 'Standard',
        customerName: 'John Doe',
        customerEmail: 'john@example.com',
        customerPhone: '+359888123456',
        addressLine1: '123 Main St',
        city: 'Sofia',
        postalCode: '1000',
        country: 'Bulgaria'
      };

      const mockResponse = {
        id: 'request-id',
        ...requestData,
        productName: 'Test AC',
        status: 'Pending',
        estimatedPrice: 150,
        createdAt: '2024-01-15T10:00:00Z'
      };

      service.createInstallationRequest(requestData).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/installation/requests`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(requestData);
      req.flush(mockResponse);
    });

    it('should include optional fields in request', () => {
      const requestData: CreateInstallationRequestData = {
        productId: '123e4567-e89b-12d3-a456-426614174000',
        installationType: 'Premium',
        customerName: 'John Doe',
        customerEmail: 'john@example.com',
        customerPhone: '+359888123456',
        addressLine1: '123 Main St',
        addressLine2: 'Apt 5',
        city: 'Sofia',
        postalCode: '1000',
        country: 'Bulgaria',
        preferredDate: '2024-02-01',
        preferredTimeSlot: 'morning',
        notes: 'Ring doorbell twice'
      };

      service.createInstallationRequest(requestData).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/installation/requests`);
      expect(req.request.body.addressLine2).toBe('Apt 5');
      expect(req.request.body.preferredDate).toBe('2024-02-01');
      expect(req.request.body.preferredTimeSlot).toBe('morning');
      expect(req.request.body.notes).toBe('Ring doorbell twice');
      req.flush({});
    });
  });
});
