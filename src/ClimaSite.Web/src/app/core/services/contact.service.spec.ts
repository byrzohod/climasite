import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ContactService } from './contact.service';

describe('ContactService', () => {
  let service: ContactService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ContactService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('POSTs the payload to the contact endpoint and returns the id', () => {
    const payload = { name: 'A', email: 'a@b.com', subject: 'S', message: 'M' };
    let result: { id: string } | undefined;

    service.submit(payload).subscribe(r => (result = r));

    const req = httpMock.expectOne(r => r.url.endsWith('/api/contact') && r.method === 'POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 'contact-1' });

    expect(result?.id).toBe('contact-1');
  });
});
