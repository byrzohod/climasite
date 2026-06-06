import { apiErrorToTranslationKey, isTranslationKey, toTranslationKey } from './translation-key.util';

describe('translation-key utilities', () => {
  it('detects dotted translation keys', () => {
    expect(isTranslationKey('checkout.errors.placeOrderFailed')).toBeTrue();
    expect(isTranslationKey('Plain backend error')).toBeFalse();
  });

  it('keeps existing translation keys and maps raw text to the fallback key', () => {
    expect(toTranslationKey('products.qa.errors.submitAnswerFailed', 'common.error')).toBe('products.qa.errors.submitAnswerFailed');
    expect(toTranslationKey('Your card was declined', 'checkout.payment.errors.failed')).toBe('checkout.payment.errors.failed');
  });

  it('extracts only translation-key API messages from error responses', () => {
    expect(apiErrorToTranslationKey(
      { error: { message: 'checkout.errors.placeOrderFailed' } },
      'checkout.errors.fallback'
    )).toBe('checkout.errors.placeOrderFailed');

    expect(apiErrorToTranslationKey(
      { error: { message: 'Insufficient stock' } },
      'checkout.errors.placeOrderFailed'
    )).toBe('checkout.errors.placeOrderFailed');
  });
});
