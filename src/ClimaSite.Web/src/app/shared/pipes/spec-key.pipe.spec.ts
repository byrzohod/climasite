import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { SpecKeyPipe } from './spec-key.pipe';

describe('SpecKeyPipe', () => {
  let pipe: SpecKeyPipe;
  let translateSpy: jasmine.SpyObj<TranslateService>;

  beforeEach(() => {
    translateSpy = jasmine.createSpyObj<TranslateService>('TranslateService', ['instant']);

    TestBed.configureTestingModule({
      providers: [
        SpecKeyPipe,
        { provide: TranslateService, useValue: translateSpy }
      ]
    });

    pipe = TestBed.inject(SpecKeyPipe);
  });

  it('should create', () => {
    expect(pipe).toBeTruthy();
  });

  describe('empty / falsy keys', () => {
    it('should return empty string for empty key without hitting translate', () => {
      expect(pipe.transform('')).toBe('');
      expect(translateSpy.instant).not.toHaveBeenCalled();
    });

    it('should return empty string for null key', () => {
      expect(pipe.transform(null as unknown as string)).toBe('');
      expect(translateSpy.instant).not.toHaveBeenCalled();
    });

    it('should return empty string for undefined key', () => {
      expect(pipe.transform(undefined as unknown as string)).toBe('');
      expect(translateSpy.instant).not.toHaveBeenCalled();
    });
  });

  describe('translation found', () => {
    it('should look up the key under the specs.* namespace', () => {
      translateSpy.instant.and.returnValue('Cooling Capacity');

      pipe.transform('cooling_capacity');

      expect(translateSpy.instant).toHaveBeenCalledWith('specs.cooling_capacity');
    });

    it('should return the translation when it differs from the lookup key', () => {
      translateSpy.instant.and.returnValue('Kühlleistung');

      expect(pipe.transform('cooling_capacity')).toBe('Kühlleistung');
    });
  });

  describe('translation missing -> humanized fallback', () => {
    beforeEach(() => {
      // ngx-translate returns the lookup key itself when no translation exists.
      translateSpy.instant.and.callFake((key: string | string[]) => key as string);
    });

    it('should humanize snake_case keys', () => {
      expect(pipe.transform('cooling_capacity')).toBe('Cooling Capacity');
    });

    it('should humanize hyphenated keys', () => {
      expect(pipe.transform('energy-rating')).toBe('Energy Rating');
    });

    it('should split and capitalize camelCase keys', () => {
      expect(pipe.transform('maxTemp')).toBe('Max Temp');
    });

    it('should capitalize a single lowercase word', () => {
      expect(pipe.transform('voltage')).toBe('Voltage');
    });

    it('should handle mixed underscore + camelCase keys', () => {
      expect(pipe.transform('cooling_capacityBtu')).toBe('Cooling Capacity Btu');
    });
  });
});
