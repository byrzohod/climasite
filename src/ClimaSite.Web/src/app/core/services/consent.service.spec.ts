import { TestBed } from '@angular/core/testing';
import { ConsentService } from './consent.service';

const STORAGE_KEY = 'climasite_cookie_consent';

describe('ConsentService', () => {
  beforeEach(() => {
    localStorage.removeItem(STORAGE_KEY);
  });

  afterEach(() => {
    localStorage.removeItem(STORAGE_KEY);
  });

  function create(): ConsentService {
    TestBed.configureTestingModule({ providers: [ConsentService] });
    return TestBed.inject(ConsentService);
  }

  it('should be created and undecided by default', () => {
    const service = create();
    expect(service).toBeTruthy();
    expect(service.hasDecided()).toBeFalse();
    expect(service.accepted()).toBeFalse();
    expect(service.nonEssentialGranted()).toBeFalse();
  });

  it('should grant consent and persist on accept()', () => {
    const service = create();
    service.accept();

    expect(service.hasDecided()).toBeTrue();
    expect(service.accepted()).toBeTrue();
    expect(service.nonEssentialGranted()).toBeTrue();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('accepted');
  });

  it('should record a decision but not grant on reject()', () => {
    const service = create();
    service.reject();

    expect(service.hasDecided()).toBeTrue();
    expect(service.accepted()).toBeFalse();
    expect(service.nonEssentialGranted()).toBeFalse();
    expect(localStorage.getItem(STORAGE_KEY)).toBe('rejected');
  });

  it('should hydrate an existing accepted decision from localStorage', () => {
    localStorage.setItem(STORAGE_KEY, 'accepted');
    const service = create();

    expect(service.hasDecided()).toBeTrue();
    expect(service.accepted()).toBeTrue();
  });

  it('should hydrate an existing rejected decision from localStorage', () => {
    localStorage.setItem(STORAGE_KEY, 'rejected');
    const service = create();

    expect(service.hasDecided()).toBeTrue();
    expect(service.accepted()).toBeFalse();
  });

  it('should ignore an invalid stored value and remain undecided', () => {
    localStorage.setItem(STORAGE_KEY, 'garbage');
    const service = create();

    expect(service.hasDecided()).toBeFalse();
  });
});
