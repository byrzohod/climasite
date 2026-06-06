const TRANSLATION_KEY_PATTERN = /^[A-Za-z][A-Za-z0-9]*(?:[._-][A-Za-z0-9]+)+$/;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

export function isTranslationKey(value: unknown): value is string {
  return typeof value === 'string' && TRANSLATION_KEY_PATTERN.test(value);
}

export function toTranslationKey(value: unknown, fallbackKey: string): string {
  return isTranslationKey(value) ? value : fallbackKey;
}

export function apiErrorToTranslationKey(error: unknown, fallbackKey: string): string {
  if (!isRecord(error)) {
    return fallbackKey;
  }

  const responseBody = isRecord(error['error']) ? error['error'] : null;
  return toTranslationKey(responseBody?.['message'] ?? error['message'], fallbackKey);
}
