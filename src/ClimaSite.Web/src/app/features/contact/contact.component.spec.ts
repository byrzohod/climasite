import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ContactComponent } from './contact.component';
import { TranslateModule } from '@ngx-translate/core';
import { ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';

describe('ContactComponent', () => {
  let component: ContactComponent;
  let fixture: ComponentFixture<ContactComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ContactComponent,
        TranslateModule.forRoot(),
        ReactiveFormsModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ContactComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Contact Form', () => {
    it('should have a contact form', () => {
      expect(component.contactForm).toBeTruthy();
    });

    it('should have required form controls', () => {
      expect(component.contactForm.get('name')).toBeTruthy();
      expect(component.contactForm.get('email')).toBeTruthy();
      expect(component.contactForm.get('subject')).toBeTruthy();
      expect(component.contactForm.get('message')).toBeTruthy();
    });

    it('should be invalid when empty', () => {
      expect(component.contactForm.valid).toBeFalse();
    });

    it('should be valid when all fields are filled correctly', () => {
      component.contactForm.patchValue({
        name: 'Test User',
        email: 'test@example.com',
        subject: 'Test Subject',
        message: 'Test message content'
      });

      expect(component.contactForm.valid).toBeTrue();
    });

    it('should validate email format', () => {
      component.contactForm.patchValue({
        name: 'Test User',
        email: 'invalid-email',
        subject: 'Test Subject',
        message: 'Test message'
      });

      expect(component.contactForm.get('email')?.errors?.['email']).toBeTrue();
    });

    it('should require name field', () => {
      component.contactForm.patchValue({
        name: '',
        email: 'test@example.com',
        subject: 'Test Subject',
        message: 'Test message'
      });

      expect(component.contactForm.get('name')?.errors?.['required']).toBeTrue();
    });
  });

  describe('Form Submission', () => {
    it('should not submit if form is invalid', () => {
      component.onSubmit();
      expect(component.isSubmitting()).toBeFalse();
    });

    it('should set isSubmitting to true during submission', fakeAsync(() => {
      component.contactForm.patchValue({
        name: 'Test User',
        email: 'test@example.com',
        subject: 'Test Subject',
        message: 'Test message'
      });

      component.onSubmit();

      expect(component.isSubmitting()).toBeTrue();

      tick(1000);

      expect(component.isSubmitting()).toBeFalse();
    }));

    it('should show success message after successful submission', fakeAsync(() => {
      component.contactForm.patchValue({
        name: 'Test User',
        email: 'test@example.com',
        subject: 'Test Subject',
        message: 'Test message'
      });

      component.onSubmit();
      tick(1000);

      expect(component.submitSuccess()).toBeTrue();
    }));

    it('should reset form after successful submission', fakeAsync(() => {
      component.contactForm.patchValue({
        name: 'Test User',
        email: 'test@example.com',
        subject: 'Test Subject',
        message: 'Test message'
      });

      component.onSubmit();
      tick(1000);

      expect(component.contactForm.get('name')?.value).toBeFalsy();
      expect(component.contactForm.get('email')?.value).toBeFalsy();
    }));
  });

  describe('Map Section', () => {
    it('should have map section in template', () => {
      const mapSection = fixture.debugElement.query(By.css('.map-section'));
      expect(mapSection).toBeTruthy();
    });

    it('should have map container with testid', () => {
      const mapContainer = fixture.debugElement.query(By.css('[data-testid="contact-map"]'));
      expect(mapContainer).toBeTruthy();
    });

    it('should have map iframe', () => {
      const iframe = fixture.debugElement.query(By.css('[data-testid="contact-map"] iframe'));
      expect(iframe).toBeTruthy();
    });

    it('should have map iframe with OpenStreetMap source', () => {
      const iframe = fixture.debugElement.query(By.css('[data-testid="contact-map"] iframe'));
      const src = iframe.nativeElement.getAttribute('src');
      expect(src).toContain('openstreetmap.org');
    });

    it('should have map link that opens in new tab', () => {
      const mapLink = fixture.debugElement.query(By.css('[data-testid="contact-map"] a.map-link'));
      expect(mapLink).toBeTruthy();
      expect(mapLink.nativeElement.getAttribute('target')).toBe('_blank');
    });

    it('should have map link with security attributes', () => {
      const mapLink = fixture.debugElement.query(By.css('[data-testid="contact-map"] a.map-link'));
      const rel = mapLink.nativeElement.getAttribute('rel');
      expect(rel).toContain('noopener');
      expect(rel).toContain('noreferrer');
    });
  });

  describe('Contact Info', () => {
    it('should display contact info items', () => {
      const infoItems = fixture.debugElement.queryAll(By.css('.info-item'));
      expect(infoItems.length).toBeGreaterThanOrEqual(4);
    });

    it('should display address info', () => {
      const compiled = fixture.nativeElement;
      expect(compiled.textContent).toContain('contact.info.address');
    });

    it('should display phone info', () => {
      const compiled = fixture.nativeElement;
      expect(compiled.textContent).toContain('contact.info.phone');
    });

    it('should display email info', () => {
      const compiled = fixture.nativeElement;
      expect(compiled.textContent).toContain('contact.info.email');
    });

    it('should display hours info', () => {
      const compiled = fixture.nativeElement;
      expect(compiled.textContent).toContain('contact.info.hours');
    });
  });

  describe('UI State', () => {
    it('should show submit button', () => {
      const submitButton = fixture.debugElement.query(By.css('[data-testid="contact-submit"]'));
      expect(submitButton).toBeTruthy();
    });

    it('should disable submit button when form is invalid', () => {
      const submitButton = fixture.debugElement.query(By.css('[data-testid="contact-submit"]'));
      expect(submitButton.nativeElement.disabled).toBeTrue();
    });

    it('should enable submit button when form is valid', () => {
      component.contactForm.patchValue({
        name: 'Test User',
        email: 'test@example.com',
        subject: 'Test Subject',
        message: 'Test message'
      });
      fixture.detectChanges();

      const submitButton = fixture.debugElement.query(By.css('[data-testid="contact-submit"]'));
      expect(submitButton.nativeElement.disabled).toBeFalse();
    });
  });
});
