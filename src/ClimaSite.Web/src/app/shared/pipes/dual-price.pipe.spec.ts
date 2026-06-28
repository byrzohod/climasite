import { BGN_PER_EUR, DualPricePipe } from './dual-price.pipe';

describe('DualPricePipe', () => {
  let pipe: DualPricePipe;

  beforeEach(() => {
    pipe = new DualPricePipe();
  });

  it('uses the fixed lev peg 1.95583', () => {
    expect(BGN_PER_EUR).toBe(1.95583);
  });

  it('renders EUR primary + BGN secondary via the peg', () => {
    // 99.99 * 1.95583 = 195.5634... → 195.56
    expect(pipe.transform(99.99)).toBe('€99.99 / 195.56 лв');
  });

  it('formats thousands with grouping (matches the currency:EUR output)', () => {
    // 1000 * 1.95583 = 1955.83
    expect(pipe.transform(1000)).toBe('€1,000.00 / 1,955.83 лв');
  });

  it('handles zero', () => {
    expect(pipe.transform(0)).toBe('€0.00 / 0.00 лв');
  });

  it('always shows two decimals', () => {
    // 5 * 1.95583 = 9.77915 → 9.78
    expect(pipe.transform(5)).toBe('€5.00 / 9.78 лв');
  });

  it('negates BOTH sides for a negative value (bundle savings row)', () => {
    // -66.70 * 1.95583 = -130.453861 → -130.45 (both EUR and BGN carry the sign)
    expect(pipe.transform(-66.70)).toBe('-€66.70 / -130.45 лв');
  });

  it('returns empty string for null / undefined / NaN', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform(Number.NaN)).toBe('');
  });
});
