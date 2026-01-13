import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { Component } from '@angular/core';
import { InstallationServiceComponent } from './installation-service.component';
import { InstallationService, ProductInstallationOptions } from '../../services/installation.service';
import { environment } from '../../../../../environments/environment';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<Record<string, string>> {
    return of({
      'products.installation.title': 'Installation Service',
      'products.installation.subtitle': 'Professional installation',
      'products.installation.recommended': 'Recommended',
      'products.installation.fastest': 'Fastest',
      'products.installation.select': 'Select',
      'products.installation.selected': 'Selected',
      'products.installation.estimatedTime': '{{days}} days',
      'products.installation.requestTitle': 'Request Details',
      'products.installation.customerName': 'Name',
      'products.installation.customerEmail': 'Email',
      'products.installation.customerPhone': 'Phone',
      'products.installation.address': 'Address',
      'products.installation.city': 'City',
      'products.installation.postalCode': 'Postal Code',
      'products.installation.country': 'Country',
      'products.installation.selectCountry': 'Select Country',
      'products.installation.submitRequest': 'Submit Request',
      'products.installation.submitting': 'Submitting...',
      'products.installation.requestInstallation': 'Request Installation',
      'products.installation.requestSubmitted': 'Request Submitted!',
      'products.installation.requestSubmittedMessage': 'We will contact you.',
      'products.installation.notAvailable': 'Not available',
      'common.currency': '{{amount}} EUR'
    });
  }
}

@Component({
  standalone: true,
  imports: [InstallationServiceComponent],
  template: '<app-installation-service [productId]="productId" />'
})
class TestHostComponent {
  productId = '123e4567-e89b-12d3-a456-426614174000';
}

describe('InstallationServiceComponent', () => {
  let component: InstallationServiceComponent;
  let fixture: ComponentFixture<TestHostComponent>;
  let httpMock: HttpTestingController;

  const mockOptions: ProductInstallationOptions = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    productName: 'Test AC Unit',
    installationAvailable: true,
    options: [
      {
        type: 'Standard',
        name: 'Standard Installation',
        description: 'Basic installation by certified technicians',
        price: 150.00,
        features: ['Certified technician', 'Equipment setup'],
        estimatedDays: 7
      },
      {
        type: 'Premium',
        name: 'Premium Installation',
        description: 'Full-service installation with extended warranty',
        price: 250.00,
        features: ['Senior technician', 'Extended warranty', 'Priority scheduling'],
        estimatedDays: 5
      },
      {
        type: 'Express',
        name: 'Express Installation',
        description: 'Fast-track installation',
        price: 350.00,
        features: ['Express scheduling', 'Weekend availability'],
        estimatedDays: 2
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        InstallationServiceComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        InstallationService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    httpMock = TestBed.inject(HttpTestingController);
    component = fixture.debugElement.children[0].componentInstance;
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    expect(component).toBeTruthy();
  }));

  it('should load installation options on init', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    expect(component.options()).toEqual(mockOptions);
    expect(component.options()!.options.length).toBe(3);
  }));

  it('should display installation options in template', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    const optionCards = fixture.nativeElement.querySelectorAll('.option-card');
    expect(optionCards.length).toBe(3);
  }));

  it('should select an option when clicked', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    expect(component.selectedOption()).toBeNull();

    component.selectOption(mockOptions.options[0]);
    expect(component.selectedOption()).toEqual(mockOptions.options[0]);
  }));

  it('should show form when option is selected and button clicked', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    component.selectOption(mockOptions.options[0]);
    expect(component.showForm()).toBeFalsy();

    component.showForm.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.request-form-section')).toBeTruthy();
  }));

  it('should validate request form', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    expect(component.requestForm.valid).toBeFalsy();

    component.requestForm.patchValue({
      customerName: 'John Doe',
      customerEmail: 'john@example.com',
      customerPhone: '+359888123456',
      addressLine1: '123 Main St',
      city: 'Sofia',
      postalCode: '1000',
      country: 'Bulgaria'
    });

    expect(component.requestForm.valid).toBeTruthy();
  }));

  it('should validate email format', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();

    component.requestForm.patchValue({ customerEmail: 'invalid-email' });
    expect(component.requestForm.get('customerEmail')?.valid).toBeFalsy();

    component.requestForm.patchValue({ customerEmail: 'valid@email.com' });
    expect(component.requestForm.get('customerEmail')?.valid).toBeTruthy();
  }));

  it('should submit installation request', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    component.selectOption(mockOptions.options[0]);
    component.showForm.set(true);
    component.requestForm.patchValue({
      customerName: 'John Doe',
      customerEmail: 'john@example.com',
      customerPhone: '+359888123456',
      addressLine1: '123 Main St',
      city: 'Sofia',
      postalCode: '1000',
      country: 'Bulgaria'
    });
    fixture.detectChanges();

    component.submitRequest();

    const submitReq = httpMock.expectOne(`${environment.apiUrl}/installation/requests`);
    expect(submitReq.request.method).toBe('POST');
    expect(submitReq.request.body.installationType).toBe('Standard');
    submitReq.flush({ id: 'new-id' });
    tick();
    fixture.detectChanges();

    expect(component.requestSubmitted()).toBeTruthy();
    expect(component.showForm()).toBeFalsy();
  }));

  it('should not submit if form is invalid', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    component.selectOption(mockOptions.options[0]);
    component.submitRequest();

    // Should not make HTTP request
    httpMock.expectNone(`${environment.apiUrl}/installation/requests`);
  }));

  it('should not submit if no option selected', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush(mockOptions);
    tick();
    fixture.detectChanges();

    component.requestForm.patchValue({
      customerName: 'John Doe',
      customerEmail: 'john@example.com',
      customerPhone: '+359888123456',
      addressLine1: '123 Main St',
      city: 'Sofia',
      postalCode: '1000',
      country: 'Bulgaria'
    });

    component.submitRequest();

    // Should not make HTTP request
    httpMock.expectNone(`${environment.apiUrl}/installation/requests`);
  }));

  it('should display message when installation not available', fakeAsync(() => {
    fixture.detectChanges();
    const req = httpMock.expectOne(req => req.url.includes('/installation/options/'));
    req.flush({
      ...mockOptions,
      installationAvailable: false,
      options: []
    });
    tick();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.no-installation')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.options-grid')).toBeFalsy();
  }));
});
